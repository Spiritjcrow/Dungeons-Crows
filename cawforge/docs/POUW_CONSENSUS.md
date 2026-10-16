# PoUW feasibility: contribution accounting is not blockchain consensus

Genesis solves **proof-of-accepted-output** under one project's approved job policy: register a task, compute result, independently recompute it, issue signed local simulated credits. It does **not** demonstrate trustless decentralized work difficulty, objective competitive block selection, Sybil resistance, liveness, leader election or economic security of a cryptocurrency.

## Fundamental design problems for a true PoUW chain

1. Useful tasks are heterogeneous; neither usefulness nor difficulty can be inferred reliably from hash count, clock time or self-reported GPU hours.
2. Reproducible outputs can be precomputed, copied or resold; otherwise verifiers may require as much work as producers.
3. Non-deterministic AI, rendering and artistic judgment often cannot be cheaply and objectively verified.
4. Game task demand is variable and can be fabricated by colluding task sponsors, producing circular, resource-wasting rewards.
5. Adversaries can create many identities, hoard task results, submit low-quality optimized artifacts or exploit scoring models.
6. A ledger maintained by one coordinator is not decentralized consensus. Even multiple signatures cannot substitute for a tested Byzantine-fault-tolerant or economically secured consensus process.

## Next research protocol

Benchmark fixed-seed procedural generation, content-addressed compilation, deterministic test runs and image rendering with reference outputs. Collect verification cost relative to production cost, objective utility proxy and replay/collusion failure rates. Evaluate a **hybrid architecture**: audited work-credit issuance on an existing consensus-secured *testnet*, with independent validator attestations, rather than representing PoUW itself as consensus. Only propose native PoUW consensus after formal security assumptions, adversarial simulations and external peer review.

## Sources for review

- *A systematic literature review on proof-of-useful work consensus algorithms* (2025), Blockchain: Research and Applications, doi:10.1016/j.bcra.2025.100387.
- NIST FIPS 186-5, Digital Signature Standard (2023), EdDSA and signing requirements, https://csrc.nist.gov/pubs/fips/186-5/final .
- SEC, *Transactions Involving Crypto Assets* (April 22, 2026), https://www.sec.gov/resources-small-businesses/capital-raising-building-blocks/transactions-involving-crypto-assets .

No claim is made that these sources validate CAWF security, governance or legal status.
