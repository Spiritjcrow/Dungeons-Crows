"""Coordinator queue, worker receipts, independent verifier and append-only accounting."""
import json
import os
import re
import secrets
import sqlite3
from datetime import datetime, timezone
from pathlib import Path

from . import crypto, work

SUPPLY_CAP_MICRO = 21_000_000 * 1_000_000
WORKER_DOMAIN = "CAWF-WORK-RECEIPT-v1"
VERIFY_DOMAIN = "CAWF-VERIFICATION-v1"
GENESIS_PREV = "0" * 64


class VerificationError(Exception):
    pass


def utc():
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def state_dir(home):
    return Path(home).expanduser().resolve()


def db_conn(home):
    db = state_dir(home) / "genesis.sqlite3"
    if not db.is_file():
        raise ValueError("state absent: run `init` first")
    c = sqlite3.connect(db, timeout=15)
    c.row_factory = sqlite3.Row
    c.execute("PRAGMA busy_timeout=15000")
    return c


def meta(conn):
    return {r["key"]: r["value"] for r in conn.execute("SELECT key,value FROM meta")}


def initialize(home):
    root = state_dir(home)
    root.mkdir(parents=True, exist_ok=True, mode=0o700)
    os.chmod(root, 0o700)
    dbpath = root / "genesis.sqlite3"
    if dbpath.exists() or (root / "keys").exists():
        raise ValueError("state already initialized; will not overwrite private keys")
    public_worker = crypto.save_key(root / "keys", "worker")
    public_verifier = crypto.save_key(root / "keys", "verifier")
    conn = sqlite3.connect(dbpath)
    try:
        conn.executescript("""
            PRAGMA journal_mode=WAL;
            CREATE TABLE meta(key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE jobs(
              job_id TEXT PRIMARY KEY, spec TEXT NOT NULL, status TEXT NOT NULL,
              output TEXT, receipt TEXT, reason TEXT, created TEXT NOT NULL
            );
            CREATE TABLE ledger(
              position INTEGER PRIMARY KEY, job_id TEXT NOT NULL UNIQUE,
              entry TEXT NOT NULL, verifier_signature TEXT NOT NULL,
              entry_hash TEXT NOT NULL UNIQUE,
              FOREIGN KEY(job_id) REFERENCES jobs(job_id)
            );
        """)
        for k, v in dict(genesis_id=secrets.token_hex(16), worker_public=public_worker,
                         verifier_public=public_verifier, protocol="CAWF-SIM-v0.1").items():
            conn.execute("INSERT INTO meta VALUES (?,?)", (k, v))
        conn.commit()
    finally:
        conn.close()
    os.chmod(dbpath, 0o600)
    return {"protocol": "CAWF-SIM-v0.1", "worker_public": public_worker,
            "verifier_public": public_verifier, "message": "simulated only; keys stay local"}


def enqueue(home, job_id, params):
    if not re.fullmatch(r"[A-Za-z0-9_.-]{1,64}", job_id):
        raise ValueError("invalid job ID")
    work.validate_params(params)
    conn = db_conn(home)
    try:
        m = meta(conn)
        spec = {"job_id": job_id, "kind": work.KIND, "params": params,
                "genesis_id": m["genesis_id"], "approved_by": "coordinator-policy-v1"}
        conn.execute("INSERT INTO jobs (job_id,spec,status,created) VALUES (?,?,?,?)",
                     (job_id, crypto.canonical(spec).decode(), "QUEUED", utc()))
        conn.commit()
        return spec
    finally:
        conn.close()


def process(home, job_id=None):
    conn = db_conn(home)
    try:
        conn.execute("BEGIN IMMEDIATE")
        if job_id:
            job = conn.execute("SELECT * FROM jobs WHERE job_id=? AND status='QUEUED'", (job_id,)).fetchone()
        else:
            job = conn.execute("SELECT * FROM jobs WHERE status='QUEUED' ORDER BY created, job_id LIMIT 1").fetchone()
        if not job:
            raise ValueError("no queued job matching selection")
        spec = json.loads(job["spec"])
        output = work.generate(spec["params"])
        p = {"version": 1, "domain": "CAWF-SIM-v0.1", "genesis_id": spec["genesis_id"],
             "job_id": job["job_id"], "worker_id": "selma-black-local-demo",
             "job_sha256": crypto.sha(spec), "output_sha256": crypto.sha(output),
             "output_meta": {"format": work.KIND, "dimensions": [output["width"], output["height"]],
                             "rooms": len(output["rooms"])},
             "timestamp_utc": utc(), "nonce": secrets.token_hex(16)}
        signature = crypto.sign(crypto.load_private(state_dir(home) / "keys" / "worker.pem"), WORKER_DOMAIN, p)
        receipt = {"payload": p, "signature_ed25519": signature}
        conn.execute("UPDATE jobs SET status='WORKED', output=?, receipt=? WHERE job_id=?",
                     (crypto.canonical(output).decode(), crypto.canonical(receipt).decode(), job["job_id"]))
        conn.commit()
        return {"job_id": job["job_id"], "receipt": receipt, "output_sha256": crypto.sha(output)}
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


def validate_receipt(job, m):
    if not job["output"] or not job["receipt"]:
        raise VerificationError("missing asset/receipt")
    spec = json.loads(job["spec"])
    asset = json.loads(job["output"])
    receipt = json.loads(job["receipt"])
    p = receipt["payload"]
    if not crypto.verify(m["worker_public"], WORKER_DOMAIN, p, receipt["signature_ed25519"]):
        raise VerificationError("worker signature invalid")
    if (p.get("version") != 1 or p.get("domain") != "CAWF-SIM-v0.1"
        or p.get("genesis_id") != m["genesis_id"] or p.get("job_id") != job["job_id"]
        or p.get("worker_id") != "selma-black-local-demo"
        or spec["kind"] != work.KIND or spec["genesis_id"] != m["genesis_id"]
        or p.get("job_sha256") != crypto.sha(spec) or p.get("output_sha256") != crypto.sha(asset)
        or p.get("output_meta") != {"format": work.KIND,
                                     "dimensions": [asset["width"], asset["height"]],
                                     "rooms": len(asset["rooms"]) }):
        raise VerificationError("receipt/job/asset mismatch or wrong domain")
    ok, reason = work.quality_check(asset, spec["params"])
    if not ok:
        raise VerificationError(reason)
    return spec, asset, receipt


def review(home, job_id):
    conn = db_conn(home)
    try:
        conn.execute("BEGIN IMMEDIATE")
        job = conn.execute("SELECT * FROM jobs WHERE job_id=?", (job_id,)).fetchone()
        if job is None:
            raise ValueError("job not found")
        if job["status"] != "WORKED":
            raise VerificationError("cannot verify: not in WORKED state (duplicate/replay or not ready)")
        m = meta(conn)
        try:
            spec, asset, receipt = validate_receipt(job, m)
        except (VerificationError, KeyError, ValueError, TypeError, json.JSONDecodeError) as error:
            conn.execute("UPDATE jobs SET status='REJECTED',reason=? WHERE job_id=?", (str(error), job_id))
            conn.commit()
            raise VerificationError(str(error))
        amount = work.reward_micro(spec["params"])
        position, issued = conn.execute("SELECT COUNT(*), COALESCE(SUM(json_extract(entry,'$.micro_cawf')),0) FROM ledger").fetchone()
        if issued + amount > SUPPLY_CAP_MICRO:
            raise VerificationError("simulated issuance cap exceeded")
        prev = conn.execute("SELECT entry_hash FROM ledger ORDER BY position DESC LIMIT 1").fetchone()
        body = {"position": position + 1, "previous_sha256": prev[0] if prev else GENESIS_PREV,
                "genesis_id": m["genesis_id"], "job_id": job_id,
                "worker_id": receipt["payload"]["worker_id"], "receipt_sha256": crypto.sha(receipt),
                "asset_sha256": crypto.sha(asset), "difficulty_units": work.difficulty_units(spec["params"]),
                "quality_bp": 10000, "utility_bp": 10000, "micro_cawf": amount,
                "approved_utc": utc(), "reward_type": "NONTRANSFERABLE_SIMULATION"}
        signer = crypto.load_private(state_dir(home) / "keys" / "verifier.pem")
        vsig = crypto.sign(signer, VERIFY_DOMAIN, body)
        entry_hash = crypto.sha({"entry": body, "verifier_signature": vsig})
        conn.execute("INSERT INTO ledger VALUES (?,?,?,?,?)", (position + 1, job_id,
                     crypto.canonical(body).decode(), vsig, entry_hash))
        conn.execute("UPDATE jobs SET status='ACCEPTED',reason=? WHERE job_id=?", ("independently regenerated and verified", job_id))
        conn.commit()
        return {"job_id": job_id, "accepted": True, "micro_cawf": amount,
                "balance_cawf": f"{(issued+amount)/1_000_000:.6f}",
                "ledger_sha256": entry_hash, "verifier_signature_ed25519": vsig}
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


def audit(home):
    conn = db_conn(home)
    errors = []
    try:
        m = meta(conn)
        prev, total, count = GENESIS_PREV, 0, 0
        rows = conn.execute("SELECT * FROM ledger ORDER BY position").fetchall()
        for row in rows:
            count += 1
            try:
                body = json.loads(row["entry"])
                job = conn.execute("SELECT * FROM jobs WHERE job_id=?", (row["job_id"],)).fetchone()
                if not job or job["status"] != "ACCEPTED":
                    raise VerificationError("accepted job state missing")
                spec, asset, receipt = validate_receipt(job, m)
                expected = work.reward_micro(spec["params"])
                if body["position"] != count or row["position"] != count or body["previous_sha256"] != prev:
                    raise VerificationError("broken ledger chain")
                if (row["job_id"] != body["job_id"] or body["genesis_id"] != m["genesis_id"]
                    or body["asset_sha256"] != crypto.sha(asset)
                    or body["receipt_sha256"] != crypto.sha(receipt)
                    or body["difficulty_units"] != work.difficulty_units(spec["params"])
                    or body["quality_bp"] != 10000 or body["utility_bp"] != 10000
                    or body["micro_cawf"] != expected or body["worker_id"] != receipt["payload"]["worker_id"]
                    or body["reward_type"] != "NONTRANSFERABLE_SIMULATION"):
                    raise VerificationError("entry accounting mismatch")
                if not crypto.verify(m["verifier_public"], VERIFY_DOMAIN, body, row["verifier_signature"]):
                    raise VerificationError("verification signature invalid")
                digest = crypto.sha({"entry": body, "verifier_signature": row["verifier_signature"]})
                if row["entry_hash"] != digest:
                    raise VerificationError("entry digest invalid")
                prev, total = digest, total + expected
            except (VerificationError, KeyError, TypeError, ValueError, json.JSONDecodeError) as exc:
                errors.append({"job_id": row["job_id"], "error": str(exc)})
        accepted_count = conn.execute("SELECT COUNT(*) FROM jobs WHERE status='ACCEPTED'").fetchone()[0]
        if accepted_count != count:
            errors.append({"error": "accepted jobs / ledger count mismatch"})
        if total > SUPPLY_CAP_MICRO:
            errors.append({"error": "total supply cap exceeded"})
        return {"valid": not errors, "verified_entries": count - len([e for e in errors if "job_id" in e]),
                "total_simulated_cawf": f"{total/1_000_000:.6f}", "last_entry_sha256": prev,
                "issues": errors}
    finally:
        conn.close()


def summary(home):
    conn = db_conn(home)
    try:
        jobs = [{"job_id": r["job_id"], "status": r["status"], "reason": r["reason"]}
                for r in conn.execute("SELECT job_id,status,reason FROM jobs ORDER BY created,job_id")]
        wallets = {}
        for row in conn.execute("SELECT entry FROM ledger"):
            entry = json.loads(row[0])
            wallets[entry["worker_id"]] = wallets.get(entry["worker_id"], 0) + entry["micro_cawf"]
        return {"protocol": meta(conn)["protocol"], "mode": "SIMULATION_ONLY", "jobs": jobs,
                "wallets": [{"worker": k, "balance_cawf": f"{v/1_000_000:.6f}", "transferable": False}
                            for k, v in sorted(wallets.items())], "audit": audit(home)}
    finally:
        conn.close()


def export(home, directory):
    out = Path(directory)
    out.mkdir(parents=True, exist_ok=True)
    conn = db_conn(home)
    try:
        proof = {"mode": "SIMULATION_ONLY", "genesis_id": meta(conn)["genesis_id"],
                 "worker_public": meta(conn)["worker_public"],
                 "verifier_public": meta(conn)["verifier_public"],
                 "jobs": [{"job_id": r["job_id"], "spec": json.loads(r["spec"]),
                           "output": json.loads(r["output"]) if r["output"] else None,
                           "receipt": json.loads(r["receipt"]) if r["receipt"] else None,
                           "status": r["status"]} for r in conn.execute("SELECT * FROM jobs ORDER BY created")],
                 "ledger": [{"entry": json.loads(r["entry"]), "verifier_signature": r["verifier_signature"],
                             "entry_hash": r["entry_hash"]} for r in conn.execute("SELECT * FROM ledger ORDER BY position")],
                 "audit": audit(home)}
        (out / "genesis_proof.json").write_text(json.dumps(proof, indent=2) + "\n", encoding="utf-8")
        return out / "genesis_proof.json"
    finally:
        conn.close()


def verify_public_proof(path, trusted_worker_public, trusted_verifier_public):
    """Offline validation on a different device using pre-shared, pinned public keys.

    This verifies authenticity against trusted keys; it does not make the original
    workload independent of the coordinator or prove a decentralized consensus.
    """
    proof_path = Path(path)
    if proof_path.stat().st_size > 2_000_000:
        raise VerificationError("proof too large")
    proof = json.loads(proof_path.read_text(encoding="utf-8"))
    if proof["mode"] != "SIMULATION_ONLY":
        raise VerificationError("not a simulation-only export")
    if (proof["worker_public"] != trusted_worker_public
        or proof["verifier_public"] != trusted_verifier_public):
        raise VerificationError("public key pin mismatch")
    genesis_id = proof["genesis_id"]
    accepted = {}
    for item in proof["jobs"]:
        if item["status"] != "ACCEPTED":
            continue
        try:
            spec, output, receipt = item["spec"], item["output"], item["receipt"]
            p = receipt["payload"]
            if (spec["genesis_id"] != genesis_id or spec["kind"] != work.KIND
                or p["domain"] != "CAWF-SIM-v0.1" or p["version"] != 1
                or p["genesis_id"] != genesis_id or p["job_id"] != item["job_id"]
                or spec["job_id"] != item["job_id"]
                or p["worker_id"] != "selma-black-local-demo"
                or p["job_sha256"] != crypto.sha(spec) or p["output_sha256"] != crypto.sha(output)
                or p["output_meta"] != {"format": work.KIND,
                                        "dimensions": [output["width"], output["height"]],
                                        "rooms": len(output["rooms"]) }
                or not crypto.verify(trusted_worker_public, WORKER_DOMAIN,
                                     p, receipt["signature_ed25519"])):
                raise VerificationError("invalid signed worker receipt")
            ok, reason = work.quality_check(output, spec["params"])
            if not ok:
                raise VerificationError(reason)
            if item["job_id"] in accepted:
                raise VerificationError("duplicate accepted job")
            accepted[item["job_id"]] = (spec, output, receipt)
        except (KeyError, TypeError, ValueError) as exc:
            raise VerificationError(f"bad public receipt: {exc}") from exc
    prev, total, seen = GENESIS_PREV, 0, set()
    for i, record in enumerate(proof["ledger"], start=1):
        try:
            body, sig = record["entry"], record["verifier_signature"]
            jid = body["job_id"]
            if jid not in accepted or jid in seen:
                raise VerificationError("unregistered/duplicate credited job")
            spec, output, receipt = accepted[jid]
            units, amount = work.difficulty_units(spec["params"]), work.reward_micro(spec["params"])
            if (body["position"] != i or body["previous_sha256"] != prev
                or body["genesis_id"] != genesis_id or body["worker_id"] != receipt["payload"]["worker_id"]
                or body["receipt_sha256"] != crypto.sha(receipt) or body["asset_sha256"] != crypto.sha(output)
                or body["difficulty_units"] != units or body["micro_cawf"] != amount
                or body["quality_bp"] != 10000 or body["utility_bp"] != 10000
                or body["reward_type"] != "NONTRANSFERABLE_SIMULATION"
                or not crypto.verify(trusted_verifier_public, VERIFY_DOMAIN, body, sig)):
                raise VerificationError("invalid signed ledger entry")
            digest = crypto.sha({"entry": body, "verifier_signature": sig})
            if digest != record["entry_hash"]:
                raise VerificationError("ledger entry hash mismatch")
            prev, total = digest, total + amount
            seen.add(jid)
        except (KeyError, TypeError, ValueError) as exc:
            raise VerificationError(f"bad ledger record: {exc}") from exc
    if seen != set(accepted):
        raise VerificationError("ledger missing credited work")
    if total > SUPPLY_CAP_MICRO:
        raise VerificationError("exceeded simulated supply cap")
    return {"valid": True, "verified_jobs": len(seen),
            "simulated_cawf": f"{total/1_000_000:.6f}",
            "ledger_head_sha256": prev, "trust": "pinned-public-keys"}
