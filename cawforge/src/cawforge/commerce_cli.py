"""Offline operator CLI for test receipts, never a hosted billing endpoint."""
import argparse
import json
import os
import sys
from pathlib import Path
from . import commerce


def main(argv=None):
    parser = argparse.ArgumentParser(description="CAW Forge web-commerce sandbox only")
    parser.add_argument("--db", default=".cawforge/commerce-sandbox.sqlite3")
    sub = parser.add_subparsers(dest="action", required=True)
    sub.add_parser("init")
    sub.add_parser("status")
    consume = sub.add_parser("consume-signed-event")
    consume.add_argument("--json-file", required=True)
    consume.add_argument("--signature-file", required=True)
    claim = sub.add_parser("manual-claim")
    claim.add_argument("--session-id", required=True)
    claim.add_argument("--player-id", required=True)
    claim.add_argument("--buyer-verified", action="store_true")
    view = sub.add_parser("entitlements")
    view.add_argument("--player-id", required=True)
    opts = parser.parse_args(argv)
    try:
        if opts.action == "init":
            commerce.initialize(opts.db)
            result = {"status": "initialized", "mode":"SANDBOX_ONLY"}
        elif opts.action == "status":
            result = commerce.status(opts.db)
        elif opts.action == "consume-signed-event":
            secret = os.environ.get("STRIPE_SANDBOX_WEBHOOK_SECRET", "")
            link_id = os.environ.get("STRIPE_SANDBOX_PAYMENT_LINK_ID", "")
            result = commerce.accept_event(opts.db, Path(opts.json_file).read_bytes(),
                     Path(opts.signature_file).read_text().strip(), secret, link_id)
        elif opts.action == "manual-claim":
            result = commerce.assign_to_verified_player(opts.db, opts.session_id, opts.player_id,
                                                         buyer_identity_verified=opts.buyer_verified)
        else:
            result = {"player_id":opts.player_id, "entitlements":commerce.entitlements(opts.db,opts.player_id)}
        print(json.dumps(result, indent=2, sort_keys=True))
    except (OSError, commerce.CommerceError) as error:
        print(json.dumps({"error":str(error)}), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
