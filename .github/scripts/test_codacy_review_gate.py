import unittest

from codacy_review_gate import CODACY_BOT_ID, review_result


def review(head, verdict, *, identity=CODACY_BOT_ID, timestamp="2026-01-01T00:00:00Z"):
    return {
        "id": 1,
        "user": {"id": identity, "type": "Bot"},
        "commit_id": head,
        "submitted_at": timestamp,
        "state": "COMMENTED",
        "body": f"### Pull Request Overview\n\n:white_check_mark: **{verdict}**",
    }


class ReviewResultTests(unittest.TestCase):
    def test_missing_stale_or_wrong_bot_is_pending(self):
        reviews = [
            review("previous", "No blocking issues noticed"),
            review("current", "No blocking issues noticed", identity=123),
        ]
        self.assertIsNone(review_result("current", reviews)[0])

    def test_latest_blocking_review_overrides_previous_success(self):
        reviews = [
            review("current", "No blocking issues noticed"),
            review("current", "Resolve issues before merging", timestamp="2026-01-01T00:01:00Z"),
        ]
        self.assertEqual(review_result("current", reviews)[0], "failure")

    def test_recognized_current_review_can_pass(self):
        self.assertEqual(review_result("current", [review("current", "No blocking issues noticed")])[0], "success")

    def test_unrecognized_review_requires_action(self):
        self.assertEqual(review_result("current", [review("current", "Other text")])[0], "action_required")


if __name__ == "__main__":
    unittest.main()
