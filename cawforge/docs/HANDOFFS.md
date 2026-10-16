# Pre-executable agent handoffs (local first)

Every handoff references a **working artifact**, measurable acceptance condition and a failure recovery action. Agent labels are assignments, not actual running unattended sessions.

## Selma°Black · Job processor

Run `PYTHONPATH=src python3 -m cawforge --home .cawforge enqueue dc-task-002 --seed 43`, then `... work --job-id dc-task-002`. Output: signed worker receipt and durable asset in the SQLite queue. Acceptance: `WORKED` status. Recovery: rerun `work` only if job remains `QUEUED`; don't regenerate credited jobs.

## Selma°White · Independent evaluator

Copy a **public proof**, not worker private keys, to a separate device; reproduce the asset using `work.generate`, verify the receipt using pinned public key and compare hashes. For Genesis the initial acceptance check is local, using `... verify dc-task-002`. On a separate device, use the `verify-proof` CLI with public keys pinned from an independent channel; do not bring private keys. Acceptance: same output and reachable dungeon, signed ledger approval. Recovery: mark bad output rejected; don't mint on verifier failure.

## Sol Blue · Security research

Run `PYTHONPATH=src python3 -m unittest discover -s tests -v` and review Ed25519 key custody, signing domains, job-policy fields, replay namespaces and hash-link anchoring. Acceptance: tests pass and all remaining threats recorded. Recovery: file narrow failing test with proof of failure.

## Sol Red · Authorized adversarial testing

With a disposable state directory only, attempt receipt alteration, fake results, cross-genesis replay, ledger edit, score inflation, repeated verification and key impersonation. Acceptance: no simulated credits for invalid receipts. Never target third-party networks or non-owned accounts. Recovery: halt release and attach minimally reproducible test.

## Sol UltraViolet · Governance

Inspect the `docs/GOVERNANCE.md` caps, contributor opt-in and allocation holds. Reject any attempt to treat credits as payments, wages or tokens tradable on a market. Acceptance: documented human signoff for policy changes. Recovery: prevent promotion/testnet bridging.

## Selma Coordinator · Continuity

Run `... status`, `... audit`, `... export --to out`, then `... dashboard --to out/dashboard.html`. Check `audit.valid` before distributing reports. Recovery: preserve DB + WAL+SHM evidence and restore verified backup; do not blindly delete corrupted data. Initial workflow is **manually triggered** and does not launch background workers.
