"""Standalone deterministic tests; fixtures are SYNTHETIC, not Stripe payments."""
import hashlib
import hmac
import json
import sqlite3
import tempfile
import unittest
from pathlib import Path
from cawforge import commerce

SECRET = "whsec_fictional_tests_only"
LINK = "plink_123456789_test"


def make_event(*, event_id="evt_1234567890abc", session="cs_test_1234567890abc", **changes):
    checkout = {"object":"checkout.session", "id":session,"livemode":False, "mode":"payment", "payment_status":"paid",
                "payment_link":LINK, "amount_total":99, "currency":"usd", "payment_intent":"pi_1234567890abc"}
    checkout.update(changes.pop("checkout",{}))
    return {"id":event_id,"type":"checkout.session.completed", "livemode":False, "data":{"object":checkout}, **changes}


def sign(obj, timestamp=100000):
    payload = json.dumps(obj, separators=(",",":"),sort_keys=True).encode()
    sig = hmac.new(SECRET.encode(),str(timestamp).encode()+b"."+payload,hashlib.sha256).hexdigest()
    return payload, f"t={timestamp},v1={sig}"


class CommerceTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.db=Path(self.temp.name)/"private"/"commerce.sqlite3"
        commerce.initialize(self.db)
    def tearDown(self):self.temp.cleanup()
    def ingest(self,obj=None):
        payload,signature=sign(obj or make_event())
        return commerce.accept_event(self.db,payload,signature,SECRET,LINK,now=100000)

    def test_authentic_test_purchase_stays_unclaimed(self):
        result=self.ingest()
        self.assertEqual(result["status"],"UNCLAIMED")
        self.assertEqual(commerce.status(self.db)["verified_test_purchases"],1)
        self.assertEqual(commerce.entitlements(self.db,"player_12345"),[])
        self.assertEqual(commerce.status(self.db)["withdrawable_usd"],"0.00")
    def test_retry_of_same_event_is_idempotent(self):
        self.ingest()
        self.assertEqual(self.ingest()["status"],"DUPLICATE_EVENT")
        self.assertEqual(commerce.status(self.db)["verified_test_purchases"],1)
    def test_duplicate_checkout_with_new_event_id_is_idempotent(self):
        self.ingest()
        self.assertEqual(self.ingest(make_event(event_id="evt_another123456"))["status"],"DUPLICATE_PURCHASE")
        self.assertEqual(commerce.status(self.db)["verified_test_purchases"],1)
    def test_duplicate_payment_intent_with_new_checkout_rejected(self):
        self.ingest()
        self.assertEqual(self.ingest(make_event(event_id="evt_another123456",session="cs_test_different32100"))["status"],"DUPLICATE_PURCHASE")
        self.assertEqual(commerce.status(self.db)["verified_test_purchases"],1)
    def test_modify_raw_body_invalidates_signature(self):
        payload,sig=sign(make_event())
        with self.assertRaisesRegex(commerce.CommerceError,"signature"):
            commerce.accept_event(self.db,payload.replace(b"99",b"98"),sig,SECRET,LINK,now=100000)
    def test_wrong_secret_rejected(self):
        payload,sig=sign(make_event())
        with self.assertRaises(commerce.CommerceError):
            commerce.accept_event(self.db,payload,sig,"whsec_wrong_wrong",LINK,now=100000)
    def test_expired_and_future_rejected(self):
        payload,sig=sign(make_event())
        for now in (99699,100301):
            with self.assertRaisesRegex(commerce.CommerceError,"expired"):
                commerce.accept_event(self.db,payload,sig,SECRET,LINK,now=now)
    def test_unpaid_and_async_not_paid_rejected(self):
        for payment_status in ("unpaid","no_payment_required",None):
            with self.assertRaises(commerce.CommerceError):
                self.ingest(make_event(checkout={"payment_status":payment_status}))
    def test_live_mode_event_or_checkout_rejected(self):
        with self.assertRaises(commerce.CommerceError):self.ingest(make_event(livemode=True))
        with self.assertRaises(commerce.CommerceError):self.ingest(make_event(checkout={"livemode":True}))
    def test_wrong_link_rejected(self):
        with self.assertRaisesRegex(commerce.CommerceError,"link"):
            self.ingest(make_event(checkout={"payment_link":"plink_fake999999"}))
    def test_wrong_amount_currency_or_type_rejected(self):
        for patch in ({"amount_total":100},{"currency":"eur"},{"amount_total":True},{"mode":"subscription"}):
            with self.assertRaises(commerce.CommerceError):self.ingest(make_event(checkout=patch))
    def test_faked_arbitrary_event_rejected(self):
        with self.assertRaises(commerce.CommerceError):self.ingest(make_event(type="charge.succeeded"))
        with self.assertRaises(commerce.CommerceError):self.ingest(make_event(account="acct_fake"))
    def test_untrusted_player_reference_never_autogrants(self):
        self.ingest(make_event(checkout={"client_reference_id":"victim_player"}))
        self.assertEqual(commerce.entitlements(self.db,"victim_player"),[])
    def test_cannot_manually_claim_without_verified_identity(self):
        self.ingest()
        with self.assertRaisesRegex(commerce.CommerceError,"identity"):
            commerce.assign_to_verified_player(self.db,"cs_test_1234567890abc","player_12345")
        self.assertEqual(commerce.status(self.db)["unclaimed_test_purchases"],1)
    def test_verified_manual_claim_is_idempotent_and_immutable(self):
        self.ingest()
        a=commerce.assign_to_verified_player(self.db,"cs_test_1234567890abc","player_12345",buyer_identity_verified=True)
        self.assertEqual(a["status"],"CLAIMED")
        self.assertEqual(commerce.entitlements(self.db,"player_12345"),[commerce.SKU])
        b=commerce.assign_to_verified_player(self.db,"cs_test_1234567890abc","player_12345",buyer_identity_verified=True)
        self.assertEqual(b["status"],"ALREADY_CLAIMED")
        with self.assertRaisesRegex(commerce.CommerceError,"another"):
            commerce.assign_to_verified_player(self.db,"cs_test_1234567890abc","another_player",buyer_identity_verified=True)
    def test_no_database_reset_or_missing_state_silently(self):
        with self.assertRaises(commerce.CommerceError):commerce.initialize(self.db)
        with self.assertRaises(commerce.CommerceError):commerce.status(Path(self.temp.name)/"nonexistent.db")
    def test_synthetic_event_with_malformed_payload_rejected(self):
        with self.assertRaises(commerce.CommerceError):
            commerce.verify_stripe_signature(b"x"*70000,"t=100000,v1=0",SECRET,now=100000)
        with self.assertRaises(commerce.CommerceError):
            commerce.verify_stripe_signature(b"{}","t=notatime,v1=0",SECRET,now=100000)

if __name__=="__main__":unittest.main()
