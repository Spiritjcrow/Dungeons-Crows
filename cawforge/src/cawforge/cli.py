"""Rootless local CLI for CAW Forge Genesis simulation."""
import argparse
import html
import json
import sys
from pathlib import Path

from . import engine


def dashboard(status, destination):
    jobs = "".join("<tr><td>{}</td><td>{}</td><td>{}</td></tr>".format(
        *(html.escape(str(job.get(x) or "")) for x in ("job_id", "status", "reason"))) for job in status["jobs"])
    wallets = "".join("<tr><td>{}</td><td>{}</td><td>{}</td></tr>".format(
        html.escape(w["worker"]), html.escape(w["balance_cawf"]), "No") for w in status["wallets"])
    audit = status["audit"]
    doc = """<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>CAW Forge Genesis - Simulation Only</title><style>body{background:#10171d;color:#e9f3ec;font:16px system-ui;margin:0;padding:clamp(14px,4vw,44px)}
main{max-width:980px;margin:auto}h1{color:#e7bc59;font-size:clamp(27px,5vw,44px)}.banner{padding:12px;background:#2c201b;border:1px solid #bb7657;border-radius:12px}
.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px;margin:16px 0}.card{padding:18px;border:1px solid #40575a;background:#17242b;border-radius:12px}
small{color:#acc3bc}strong{font-size:24px}table{border-collapse:collapse;width:100%;margin:14px 0 32px}th,td{text-align:left;padding:12px;border-bottom:1px solid #435355;overflow-wrap:anywhere}
caption{text-align:left;font-weight:700;margin:8px}footer{color:#9fb4ad}code{overflow-wrap:anywhere}</style><main>
<header><small>PROJECT GENESIS / DUNGEONS & CROWS × SELMAOS</small><h1>CAW Forge <span aria-hidden='true'>✦</span></h1><p class='banner'><b>SIMULATED CONTRIBUTION CREDITS ONLY.</b> No blockchain, no exchange value, no payouts.</p></header>
<section class='cards'><div class='card'><small>Ledger integrity</small><p><strong>@@AUDIT@@</strong></p></div><div class='card'><small>Verified useful-work receipts</small><p><strong>@@ENTRIES@@</strong></p></div>
<div class='card'><small>Total simulated credits</small><p><strong>@@BALANCE@@ CAWF</strong></p></div></section>
<table><caption>Contributor balances (non-transferable)</caption><thead><tr><th>Worker identity</th><th>Simulated CAWF</th><th>Transferable?</th></tr></thead><tbody>@@WALLETS@@</tbody></table>
<table><caption>Work queue and evidence</caption><thead><tr><th>Job ID</th><th>State</th><th>Verification</th></tr></thead><tbody>@@JOBS@@</tbody></table>
<footer>Offline snapshot. Public-key audit receipts are in genesis_proof.json. Developer research prototype v0.1.</footer></main></html>"""
    for k, v in {"@@AUDIT@@": "PASS" if audit["valid"] else "FAIL", "@@ENTRIES@@": str(audit["verified_entries"]),
                 "@@BALANCE@@": audit["total_simulated_cawf"], "@@WALLETS@@": wallets, "@@JOBS@@": jobs}.items():
        doc = doc.replace(k, v)
    destination = Path(destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(doc, encoding="utf-8")
    return str(destination)


def main(argv=None):
    p = argparse.ArgumentParser(prog="cawforge", description="CAW Forge rootless PoUW contribution prototype")
    p.add_argument("--home", default=".cawforge", help="local state directory; contains private keys, never commit")
    sub = p.add_subparsers(dest="command", required=True)
    sub.add_parser("init")
    e = sub.add_parser("enqueue")
    e.add_argument("job_id")
    e.add_argument("--seed", type=int, default=42)
    e.add_argument("--width", type=int, default=28)
    e.add_argument("--height", type=int, default=18)
    e.add_argument("--rooms", type=int, default=4)
    w = sub.add_parser("work")
    w.add_argument("--job-id")
    v = sub.add_parser("verify")
    v.add_argument("job_id")
    sub.add_parser("audit")
    sub.add_parser("status")
    pv = sub.add_parser("verify-proof")
    pv.add_argument("proof")
    pv.add_argument("--worker-public", required=True, help="trusted worker public key, never derive from proof itself")
    pv.add_argument("--verifier-public", required=True, help="trusted verifier public key, never derive from proof itself")
    ex = sub.add_parser("export")
    ex.add_argument("--to", default="./out")
    d = sub.add_parser("dashboard")
    d.add_argument("--to", default="./out/dashboard.html")
    demo = sub.add_parser("demo")
    demo.add_argument("--to", default="./out")
    args = p.parse_args(argv)
    try:
        if args.command == "init":
            result = engine.initialize(args.home)
        elif args.command == "enqueue":
            result = engine.enqueue(args.home, args.job_id,
                {"seed": args.seed, "width": args.width, "height": args.height, "rooms": args.rooms})
        elif args.command == "work":
            result = engine.process(args.home, args.job_id)
        elif args.command == "verify":
            result = engine.review(args.home, args.job_id)
        elif args.command == "audit":
            result = engine.audit(args.home)
            if not result["valid"]:
                print(json.dumps(result, indent=2))
                return 2
        elif args.command == "verify-proof":
            result = engine.verify_public_proof(args.proof, args.worker_public, args.verifier_public)
        elif args.command == "status":
            result = engine.summary(args.home)
        elif args.command == "export":
            result = {"public_proof_path": str(engine.export(args.home, args.to))}
        elif args.command == "dashboard":
            result = {"dashboard_path": dashboard(engine.summary(args.home), args.to)}
        elif args.command == "demo":
            engine.initialize(args.home)
            engine.enqueue(args.home, "dc-genesis-dungeon-001",
                           {"seed":42, "width":28, "height":18, "rooms":4})
            worked = engine.process(args.home)
            accepted = engine.review(args.home, worked["job_id"])
            exported = engine.export(args.home, args.to)
            dash = dashboard(engine.summary(args.home), Path(args.to) / "dashboard.html")
            result = {"task": "Dungeons & Crows procedural dungeon generation",
                      "status": "VERIFIED_REAL_COMPUTE_SIMULATED_REWARD", "reward": accepted,
                      "public_proof_path": str(exported), "dashboard_path": dash,
                      "audit": engine.audit(args.home)}
        else:
            p.error("unsupported command")
        print(json.dumps(result, indent=2))
        return 0
    except (ValueError, engine.VerificationError) as exc:
        print(json.dumps({"error": str(exc), "command": args.command}), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
