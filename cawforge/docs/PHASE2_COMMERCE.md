# Phase 2: freemium storefront + simulated CAWF research (2026-10-16)

## Confirmed state
- Dungeons & Crows game remains **free to download and play**.
- Optional one-time cosmetics/character packs have **per-item list prices of $4.99 USD or less**. No purchase required for ordinary play; don't create pay-to-win advantages.
- Authorized Stripe account is a **New business sandbox** (`livemode=false`), not a live payout account. User intends Jessica to be the merchant account owner, but account ownership has not been independently verified. Jessica must personally authorize any future live operations.
- Stripe sandbox product `prod_VSF9EXeeVtppax` is a **$0.99** *Crow Feather Cosmetic (SANDBOX TEST)*; price `price_1URKfRFt4M6t8VNH8dTHtsKf`; Stripe test link ID `plink_1URKfZFt4M6t8VNHgoosxniO`. This is only a mock checkout. No entitlement or cash payout existed at creation.
- Stripe test link: <https://buy.stripe.com/test_5kQ00jejMeH8buggxDcAo00>. Test purchases only; don't use real credit cards here.
- CAW Forge Genesis 0.1 is separate and continues to issue **nontransferable, valueless simulation credits** only. CAWF cannot be used for purchases, wages, redemption, or withdrawals.

## Ready to run locally, offline
Python 3.10+ required. Standard library only for Commerce, no Stripe SDK or real card data.

```sh
cd cawforge
export PYTHONPATH=src
python3 -m cawforge.commerce_cli --db .cawforge/commerce-sandbox.sqlite3 init
python3 -m unittest discover -s tests -v
python3 -m cawforge.commerce_cli --db .cawforge/commerce-sandbox.sqlite3 status
```

The CLI can process a genuine **signed test webhook** after a Stripe endpoint has been securely configured. The connector alone does not create a webhook endpoint or make purchases happen automatically. Set locally (never commit):

```sh
export STRIPE_SANDBOX_WEBHOOK_SECRET='whsec_<issued_for_your_endpoint>'
export STRIPE_SANDBOX_PAYMENT_LINK_ID='plink_1URKfZFt4M6t8VNHgoosxniO'
python3 -m cawforge.commerce_cli --db .cawforge/commerce-sandbox.sqlite3 consume-signed-event --json-file event.json --signature-file stripe-signature-header.txt
```

- Both `event.json` and its `Stripe-Signature` header must be captured together from **the same trusted Stripe webhook request**, without changing the raw bytes.
- Verified payment events result in `UNCLAIMED` purchases. This is intentional: shared payment links don't prove which logged-in game account owns the purchase, and `client_reference_id` is not itself authenticated. **No automatic game entitlement is delivered yet.**
- Following out-of-band proof of purchaser identity, an authorized operator can manually assign a single test purchase:

```sh
python3 -m cawforge.commerce_cli --db .cawforge/commerce-sandbox.sqlite3 manual-claim --session-id cs_test_<session_id> --player-id verified_player_0001 --buyer-verified
python3 -m cawforge.commerce_cli --db .cawforge/commerce-sandbox.sqlite3 entitlements --player-id verified_player_0001
```

`--buyer-verified` is an operator assertion, **not** a security control suitable for a public web endpoint. Never expose this command to players. Every event ID is deduplicated; different events with the same session or Payment Intent cannot duplicate purchases. Sandbox event/receipt checks reject live-mode inputs. Demo unit tests synthesize *locally signed fixtures*, not real Stripe confirmations.

## Road to actual automatic delivery
1. **Owner approval and authentication:** build real account logins; avoid trusting purchaser-controlled IDs. Implement authenticated, server-created Checkout Sessions for web *only* after appropriate Stripe permission and secure deployment, with server-signed purchase intents and verified `client_reference_id` mapping. Or secure payment link purchase claim flow requiring session ownership evidence; do not simply send a session ID to an unauthenticated client.
2. **Production hosted HTTPS webhook:** separately obtain endpoint signing secret in the sandbox; validate raw-body signature as in `commerce.py`. Require rate limiting, logging, secret rotation, redaction, backups, and alerts. Stripe event delivery can be duplicated/delayed; handle delayed payment completion. The current CLI is not a public HTTP service.
3. **Verify line items at Stripe:** before production fulfillment, retrieve Checkout Session with server-side Stripe API and separately fetch its line items. Do not rely solely on `amount_total` and Payment Link ID when more than one SKU or discounts/adjustments are supported.
4. **Reversals and consumer protection:** handle refunds, disputes, chargebacks, charge reversals, and account deletion; reconcile daily against Stripe. Phase 2 does **not** implement those, so do not use it for live sales.
5. **Android platform separation:** Google Play digital in-game purchases generally require Play Billing **or enrollment in a permitted alternative program**, depending on market and current terms. Do not put this Stripe web payment link in a Play-distributed Android game's in-app purchase path without compliance. Web storefront and independently distributed builds are separate delivery channels.
6. **Tax and business review:** clarify the legal seller, account ownership, consumer refund policy, sales tax, income tax, and merchant payout routing. Do not represent Jessica's sandbox credentials or ownership as permission to redirect funds or automatically issue payments.

## Simple conservative financial scenario (illustration, not forecast)

At $1.99 per purchase, 100 completed purchases = **$199 gross**, before taxes, payment fees, refunds, marketplace fees, development, and other costs. For $0.99 purchases, 100 sales = $99 gross. If no buyers purchase, revenue is $0.00. A Stripe sandbox never creates withdrawable proceeds.

### Milestone gates
| Phase | What counts as evidence | Revenue status |
|---|---|---|
| A | Local sandbox accounting test passing | $0 withdrawable |
| B | Independently received Stripe-signed test webhook accepted; game ownership confirmed | $0 withdrawable |
| C | Play-compliant Android purchase test and authenticated web purchase tests | $0 withdrawable |
| D | Jessica explicitly approves a verified live merchant account and launch; refund/tax/security checks pass | Could accept real payments |
| E | Actual settled eligible live sales and bank payout eligibility | Withdrawals depend on provider settlement/timing |

**Next developer handoff:** Selma Black builds authenticated account/session mapping; Selma White independently verifies player claim and QA, Sol Blue audits signatures and identity boundaries, Sol Red attacks replays/claims/refunds, Sol UltraViolet approves privacy/tax/payout policy and launch gate. No human approvals or external reviews are assumed completed.
