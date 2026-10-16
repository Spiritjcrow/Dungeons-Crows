import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

from cawforge import crypto, engine, work

PARAMS = {"seed": 42, "width": 28, "height": 18, "rooms": 4}


class GenesisTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.home = Path(self.temp.name) / "local-state"
        engine.initialize(self.home)

    def tearDown(self):
        self.temp.cleanup()

    def queue_work(self, name="dc-test-001"):
        engine.enqueue(self.home, name, PARAMS)
        return engine.process(self.home, name)

    def test_real_dungeon_generation_is_deterministic_and_playable(self):
        a = work.generate(PARAMS)
        self.assertEqual(a, work.generate(PARAMS))
        self.assertEqual((len(a["tiles"]), len(a["tiles"][0])), (18, 28))
        self.assertEqual(len(a["rooms"]), 4)
        self.assertTrue(work.quality_check(a, PARAMS)[0])
        self.assertNotEqual(a["tiles"][0], "." * 28)

    def test_proof_signature_ledger_and_exact_reward(self):
        result = self.queue_work()
        self.assertEqual(result["receipt"]["payload"]["output_sha256"], result["output_sha256"])
        accepted = engine.review(self.home, "dc-test-001")
        self.assertTrue(accepted["accepted"])
        self.assertEqual(work.difficulty_units(PARAMS), 3)
        self.assertEqual(accepted["micro_cawf"], 750_000)
        self.assertEqual(accepted["balance_cawf"], "0.750000")
        self.assertTrue(engine.audit(self.home)["valid"])
        self.assertFalse(engine.summary(self.home)["wallets"][0]["transferable"])

    def test_duplicate_job_id_fails(self):
        engine.enqueue(self.home, "same-job", PARAMS)
        with self.assertRaises(sqlite3.IntegrityError):
            engine.enqueue(self.home, "same-job", PARAMS)

    def test_replay_receipt_after_accept_fails(self):
        self.queue_work()
        engine.review(self.home, "dc-test-001")
        with self.assertRaises(engine.VerificationError):
            engine.review(self.home, "dc-test-001")
        self.assertEqual(engine.audit(self.home)["total_simulated_cawf"], "0.750000")

    def test_edited_result_without_signature_rejected(self):
        self.queue_work()
        con = engine.db_conn(self.home)
        with con:
            a = json.loads(con.execute("SELECT output FROM jobs").fetchone()[0])
            a["tiles"][1] = "X" + a["tiles"][1][1:]
            con.execute("UPDATE jobs SET output=?", (json.dumps(a),))
        con.close()
        with self.assertRaises(engine.VerificationError):
            engine.review(self.home, "dc-test-001")
        self.assertEqual(engine.summary(self.home)["jobs"][0]["status"], "REJECTED")
        self.assertEqual(engine.audit(self.home)["total_simulated_cawf"], "0.000000")

    def test_tampered_receipt_signature_rejected(self):
        self.queue_work()
        con = engine.db_conn(self.home)
        with con:
            receipt = json.loads(con.execute("SELECT receipt FROM jobs").fetchone()[0])
            receipt["payload"]["output_sha256"] = "f" * 64
            con.execute("UPDATE jobs SET receipt=?", (json.dumps(receipt),))
        con.close()
        with self.assertRaises(engine.VerificationError):
            engine.review(self.home, "dc-test-001")
        self.assertEqual(len(engine.summary(self.home)["wallets"]), 0)

    def test_same_receipt_cross_genesis_cannot_replay(self):
        self.queue_work()
        other = Path(self.temp.name) / "other-state"
        engine.initialize(other)
        engine.enqueue(other, "dc-test-001", PARAMS)
        con1, con2 = engine.db_conn(self.home), engine.db_conn(other)
        receipt = con1.execute("SELECT receipt,output FROM jobs").fetchone()
        with con2:
            con2.execute("UPDATE jobs SET status='WORKED', receipt=?, output=? WHERE job_id='dc-test-001'",
                         (receipt["receipt"], receipt["output"]))
        con1.close()
        con2.close()
        with self.assertRaises(engine.VerificationError):
            engine.review(other, "dc-test-001")

    def test_faked_job_difficulty_rejected(self):
        self.queue_work()
        conn = engine.db_conn(self.home)
        with conn:
            spec = json.loads(conn.execute("SELECT spec FROM jobs").fetchone()[0])
            spec["params"]["width"] = 64
            conn.execute("UPDATE jobs SET spec=?", (json.dumps(spec),))
        conn.close()
        with self.assertRaises(engine.VerificationError):
            engine.review(self.home, "dc-test-001")

    def test_modified_ledger_detected(self):
        self.queue_work()
        engine.review(self.home, "dc-test-001")
        conn = engine.db_conn(self.home)
        with conn:
            row = conn.execute("SELECT entry FROM ledger").fetchone()[0]
            body = json.loads(row)
            body["micro_cawf"] = 999_000_000
            conn.execute("UPDATE ledger SET entry=?", (json.dumps(body),))
        conn.close()
        self.assertFalse(engine.audit(self.home)["valid"])

    def test_invalid_map_parameters(self):
        with self.assertRaises(ValueError):
            work.generate({**PARAMS, "rooms": 16})
        with self.assertRaises(ValueError):
            work.generate({**PARAMS, "height": True})
        with self.assertRaises(ValueError):
            work.generate({**PARAMS, "seed": -1})

    def test_quality_or_utility_not_worker_controlled(self):
        self.assertEqual(work.reward_micro(PARAMS, 10000, 10000), 750_000)
        self.assertEqual(work.reward_micro(PARAMS, 10000, 0), 0)
        self.assertEqual(work.reward_micro(PARAMS, 5000, 10000), 375_000)
        with self.assertRaises(ValueError):
            work.reward_micro(PARAMS, 10001, 10000)

    def test_batch_resume_and_consistent_chain(self):
        self.queue_work("a")
        engine.review(self.home, "a")
        engine.enqueue(self.home, "b", {**PARAMS, "seed": 43})
        self.assertEqual(engine.summary(self.home)["jobs"][1]["status"], "QUEUED")
        engine.process(self.home)
        engine.review(self.home, "b")
        self.assertTrue(engine.audit(self.home)["valid"])
        self.assertEqual(engine.audit(self.home)["total_simulated_cawf"], "1.500000")
        self.assertEqual(engine.audit(self.home)["verified_entries"], 2)

    def test_independent_public_proof_with_pinned_keys(self):
        self.queue_work()
        engine.review(self.home, "dc-test-001")
        proof = engine.export(self.home, Path(self.temp.name) / "proof")
        con = engine.db_conn(self.home)
        pins = engine.meta(con)
        con.close()
        result = engine.verify_public_proof(proof, pins["worker_public"], pins["verifier_public"])
        self.assertEqual(result["simulated_cawf"], "0.750000")
        with self.assertRaises(engine.VerificationError):
            engine.verify_public_proof(proof, pins["verifier_public"], pins["worker_public"])

    def test_independent_public_proof_detects_edit(self):
        self.queue_work()
        engine.review(self.home, "dc-test-001")
        proofpath = engine.export(self.home, Path(self.temp.name) / "proof")
        con = engine.db_conn(self.home)
        pins = engine.meta(con)
        con.close()
        obj = json.loads(proofpath.read_text())
        obj["jobs"][0]["output"]["tiles"][2] = "X" * 28
        proofpath.write_text(json.dumps(obj))
        with self.assertRaises(engine.VerificationError):
            engine.verify_public_proof(proofpath, pins["worker_public"], pins["verifier_public"])

    def test_export_contains_public_proof_but_no_private_keys(self):
        self.queue_work()
        engine.review(self.home, "dc-test-001")
        path = engine.export(self.home, Path(self.temp.name) / "public")
        result = path.read_text()
        self.assertIn('"ledger"', result)
        self.assertNotIn("PRIVATE KEY", result)
        self.assertTrue(json.loads(result)["audit"]["valid"])


if __name__ == "__main__":
    unittest.main()
