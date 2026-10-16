# CAW Forge Genesis (CAWF) · v0.1.0

**Create. Verify. Earn.** Experimental, **NON-MONETARY** contribution accounting for Dungeons & Crows × SelmaOS × Sol UltraViolet.

## Operational proof

The included `dc.dungeon.map.v1` job creates an actual deterministic dungeon asset: walls, traversable floors, rooms, a starting location, an exit and walkable corridors. A separately invoked verification phase checks the worker's Ed25519-signed receipt, independently recomputes the map, checks reachability, and signs a tamper-evident ledger entry before awarding *simulated*, non-transferable CAWF credits. **This is not mining and not blockchain consensus.**

## One-command local demonstration (Linux, Chromebook Linux, Termux/rootless Android)

Requires Python 3.10+ and the `cryptography` Python package (`python -m pip install 'cryptography>=41,<50'` if available). Does not require cloud accounts, root, Android developer mode, or network once dependencies are installed. On Android, use a Python environment with the package available; packaging is not yet validated on every Termux build.

```sh
sh ./scripts/run_genesis.sh
```

Or run manually:

```sh
export PYTHONPATH=src
python3 -m cawforge --home .cawforge init
python3 -m cawforge --home .cawforge enqueue dc-genesis-dungeon-001 --seed 42 --width 28 --height 18 --rooms 4
python3 -m cawforge --home .cawforge work --job-id dc-genesis-dungeon-001
python3 -m cawforge --home .cawforge verify dc-genesis-dungeon-001
python3 -m cawforge --home .cawforge audit
python3 -m cawforge --home .cawforge status
python3 -m cawforge --home .cawforge export --to ./out
python3 -m cawforge --home .cawforge dashboard --to ./out/dashboard.html
```

To validate a public proof on another device without private keys, obtain the **trusted worker and verifier public keys via an independent channel** and run:

```sh
PYTHONPATH=src python3 -m cawforge --home /tmp/cawverify verify-proof demo/genesis_proof.json --worker-public '<trusted worker key>' --verifier-public '<trusted verifier key>'
```

Do **not** copy keys from the proof file and treat them as trusted: an attacker could substitute the file and both keys. This verifies the exported evidence, not independent consensus or a fully isolated service.

Run security and reproducibility tests:

```sh
PYTHONPATH=src python3 -m unittest discover -s tests -v
```

**Note:** `demo` creates new keys and refuses to overwrite existing state. Use another clean state directory for a fresh run. `.cawforge/` includes sensitive local signing keys, and must never be synchronized to a public repo or copied to a chat.

## Sample expected reward

`width=28, height=18, rooms=4`: difficulty = `ceil(504/256) + ceil(4/4) = 3`. Confirmed quality = 10,000 basis points and policy-approved utility = 10,000 basis points. `min(5,000,000, 250,000 × 3 × 10,000 × 10,000 / 100,000,000) = 750,000 microCAWF = 0.750000 CAWF`. All units are local simulation credits with **zero asserted market value**.

## Functional layout

- `src/cawforge/work.py`: deterministic Dungeons & Crows asset build and structural acceptance checks
- `src/cawforge/engine.py`: job queue, signed receipts, verifier, issuance cap, ledger, anti-replay, public proof export
- `src/cawforge/crypto.py`: SHA-256 + domain-separated Ed25519
- `src/cawforge/cli.py`: rootless dispatcher, governance readouts, offline HTML dashboard
- `tests/test_genesis.py`: replay, tamper, work, wallet, issuance and verification tests
- `docs/SECURITY.md`: threat model, operational boundaries, rollback
- `docs/GOVERNANCE.md`: consent, allocations and release gates
- `docs/HANDOFFS.md`: executable Black/White/Blue/Red/UltraViolet coordinator assignments
- `docs/POUW_CONSENSUS.md`: research limitations; no consensus claim

## Security and economics disclaimer

This is a **single-machine coordinator** and a *logically separate* worker/verifier pair. There is **no independent trust domain** until the verifier and keys move to independently controlled hardware/services. A hash chain alone is not tamper-proof and does not establish consensus. Credits cannot be withdrawn, transferred, sold or exchanged. A genesis supply ceiling of 21 million simulated CAWF is a placeholder subject to explicit governance approval. This software has not had an independent security audit or a legal/tax review.
