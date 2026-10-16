"""Sandbox-only, no-network Stripe receipt validation and safe entitlements.

This module does NOT collect card data, issue real money, or provide a hosted
webhook. Checkout is performed by Stripe; receipts must be authentic webhook
payloads. It intentionally leaves purchases UNCLAIMED until buyer identity has
been verified out of band by a trusted game operator.
"""
from __future__ import annotations

import hashlib
import hmac
import json
import os
import re
import sqlite3
import time
from pathlib import Path

MAX_EVENT_BYTES = 64 * 1024
DEFAULT_TOLERANCE_SEC = 300
ALLOWED_EVENT_TYPES = {"checkout.session.completed", "checkout.session.async_payment_succeeded"}
SKU = "dc.crow-feather.cosmetic.v1"
PRICE_CENTS = 99
CURRENCY = "usd"

class CommerceError(ValueError):
    pass


def _id(value: str, prefix: str) -> str:
    if not isinstance(value, str) or not re.fullmatch(re.escape(prefix) + r"[A-Za-z0-9_]{5,180}", value):
        raise CommerceError("invalid identifier")
    return value


def verify_stripe_signature(payload: bytes, header: str, secret: str, *, now: int | None = None, tolerance: int = DEFAULT_TOLERANCE_SEC) -> dict:
    """Verify authentic Stripe-Signature on exact raw body (HMAC SHA256)."""
    if not isinstance(payload, bytes) or not payload or len(payload) > MAX_EVENT_BYTES:
        raise CommerceError("invalid webhook body size")
    if not isinstance(header, str) or not isinstance(secret, str) or not secret.startswith("whsec_") or len(secret) < 12:
        raise CommerceError("missing webhook signing configuration")
    parts = []
    for part in header.split(","):
        pair = part.strip().split("=", 1)
        if len(pair) == 2:
            parts.append((pair[0], pair[1]))
    ts_values = [value for key, value in parts if key == "t"]
    sigs = [value for key, value in parts if key == "v1"]
    if len(ts_values) != 1 or not sigs or not ts_values[0].isdigit():
        raise CommerceError("invalid webhook signature header")
    timestamp = int(ts_values[0])
    now = int(time.time()) if now is None else int(now)
    if tolerance <= 0 or abs(now - timestamp) > tolerance:
        raise CommerceError("expired or future webhook signature")
    expected = hmac.new(secret.encode(), ts_values[0].encode() + b"." + payload, hashlib.sha256).hexdigest()
    if not any(len(s) == 64 and hmac.compare_digest(expected, s) for s in sigs):
        raise CommerceError("webhook signature invalid")
    try:
        event = json.loads(payload.decode("utf-8"))
    except (ValueError, UnicodeDecodeError) as error:
        raise CommerceError("invalid event JSON") from error
    if not isinstance(event, dict):
        raise CommerceError("invalid event type")
    return event


def connect(path: str | Path) -> sqlite3.Connection:
    path = Path(path)
    if not path.is_file():
        raise CommerceError("commerce state not initialized")
    conn = sqlite3.connect(path, isolation_level=None, timeout=15)
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA busy_timeout=15000")
    return conn


def initialize(path: str | Path) -> None:
    path = Path(path)
    path.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
    if path.exists():
        raise CommerceError("state already exists: refusing reset")
    conn = sqlite3.connect(path)
    try:
        conn.executescript("""
          CREATE TABLE events(event_id TEXT PRIMARY KEY, session_id TEXT NOT NULL, outcome TEXT NOT NULL);
          CREATE TABLE purchases(
            session_id TEXT PRIMARY KEY, payment_link TEXT NOT NULL,
            sku TEXT NOT NULL, amount_cents INTEGER NOT NULL, currency TEXT NOT NULL,
            payment_intent TEXT, claim_status TEXT NOT NULL CHECK(claim_status IN ('UNCLAIMED','CLAIMED')),
            player_id TEXT, created_event_id TEXT NOT NULL UNIQUE,
            FOREIGN KEY(created_event_id) REFERENCES events(event_id));
          CREATE UNIQUE INDEX purchase_payment_intent ON purchases(payment_intent) WHERE payment_intent IS NOT NULL;
        """)
        conn.commit()
    finally:
        conn.close()
    os.chmod(path, 0o600)


def validate_payment_event(event: dict, *, expected_link: str) -> tuple[str, str, str | None, str]:
    """Only accept a fixed-price, sandbox Stripe Checkout payment for the known link."""
    if not isinstance(event, dict) or event.get("type") not in ALLOWED_EVENT_TYPES:
        raise CommerceError("event does not prove completed payment")
    if event.get("livemode") is not False or event.get("account"):
        raise CommerceError("reject live or Connect-account events")
    event_id = _id(event.get("id"), "evt_")
    data = event.get("data")
    obj = data.get("object") if isinstance(data, dict) else None
    if not isinstance(obj, dict) or obj.get("object") != "checkout.session":
        raise CommerceError("missing Checkout Session")
    session_id = _id(obj.get("id"), "cs_test_")
    if obj.get("livemode") is not False:
        raise CommerceError("live checkout session rejected")
    if obj.get("mode") != "payment" or obj.get("payment_status") != "paid":
        raise CommerceError("payment is not settled")
    if obj.get("payment_link") != expected_link:
        raise CommerceError("unrecognized payment link")
    if type(obj.get("amount_total")) is not int or obj["amount_total"] != PRICE_CENTS:
        raise CommerceError("incorrect paid amount")
    if obj.get("currency") != CURRENCY:
        raise CommerceError("wrong currency")
    intent = obj.get("payment_intent")
    if intent is not None:
        _id(intent, "pi_")
    return event_id, session_id, intent, expected_link


def accept_event(path: str | Path, payload: bytes, signature: str, webhook_secret: str, expected_link: str, *, now: int | None = None) -> dict:
    _id(expected_link, "plink_")
    event = verify_stripe_signature(payload, signature, webhook_secret, now=now)
    event_id, session_id, intent, link = validate_payment_event(event, expected_link=expected_link)
    conn = connect(path)
    try:
        conn.execute("BEGIN IMMEDIATE")
        if conn.execute("SELECT 1 FROM events WHERE event_id=?", (event_id,)).fetchone():
            conn.rollback()
            return {"status": "DUPLICATE_EVENT", "session_id": session_id, "entitlement_granted": False}
        duplicate = conn.execute("SELECT 1 FROM purchases WHERE session_id=? OR (payment_intent IS NOT NULL AND payment_intent=?)", (session_id, intent)).fetchone()
        outcome = "DUPLICATE_PURCHASE" if duplicate else "UNCLAIMED"
        conn.execute("INSERT INTO events(event_id,session_id,outcome) VALUES(?,?,?)", (event_id,session_id,outcome))
        if not duplicate:
            conn.execute("""INSERT INTO purchases
                (session_id,payment_link,sku,amount_cents,currency,payment_intent,claim_status,player_id,created_event_id)
                VALUES(?,?,?,?,?,?, 'UNCLAIMED',NULL,?)""",
                (session_id,link,SKU,PRICE_CENTS,CURRENCY,intent,event_id))
        conn.commit()
        return {"status": outcome, "session_id":session_id, "sku":SKU, "entitlement_granted":False}
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


def assign_to_verified_player(path: str | Path, session_id: str, player_id: str, *, buyer_identity_verified: bool = False) -> dict:
    """Operator-only manual step. Caller MUST verify real purchaser identity first.

    No web route or self-claim API is exposed. The flag is a required process gate,
    NOT cryptographic proof; do not deploy this as a public endpoint.
    """
    _id(session_id, "cs_test_")
    if not isinstance(player_id, str) or not re.fullmatch(r"[A-Za-z0-9_-]{6,64}", player_id):
        raise CommerceError("invalid player identifier")
    if buyer_identity_verified is not True:
        raise CommerceError("buyer identity not verified")
    conn = connect(path)
    try:
        conn.execute("BEGIN IMMEDIATE")
        purchase = conn.execute("SELECT * FROM purchases WHERE session_id=?", (session_id,)).fetchone()
        if purchase is None:
            raise CommerceError("verified purchase not found")
        if purchase["claim_status"] == "CLAIMED":
            if purchase["player_id"] != player_id:
                raise CommerceError("already claimed by another player")
            conn.rollback()
            return {"status":"ALREADY_CLAIMED", "sku":purchase["sku"], "player_id":player_id}
        conn.execute("UPDATE purchases SET claim_status='CLAIMED',player_id=? WHERE session_id=?", (player_id,session_id))
        conn.commit()
        return {"status":"CLAIMED", "sku":purchase["sku"], "player_id":player_id}
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


def entitlements(path: str | Path, player_id: str) -> list[str]:
    """Trusted server-side internal call; do not expose as an unauthenticated API."""
    conn = connect(path)
    try:
        return sorted({r[0] for r in conn.execute("SELECT sku FROM purchases WHERE player_id=? AND claim_status='CLAIMED'", (player_id,))})
    finally:
        conn.close()


def status(path: str | Path) -> dict:
    conn = connect(path)
    try:
        summary = conn.execute("SELECT COUNT(*), SUM(claim_status='CLAIMED') FROM purchases").fetchone()
        return {"mode":"SANDBOX_ONLY", "currency":"usd", "verified_test_purchases":summary[0], "claimed_test_purchases":summary[1] or 0,
                "unclaimed_test_purchases":summary[0]-(summary[1] or 0), "live_revenue_usd":"0.00", "withdrawable_usd":"0.00"}
    finally:
        conn.close()
