"""Publish the independent Codacy review decision on GitHub's test merge commit."""

from collections import defaultdict
from datetime import datetime, timezone
import json
import os
from urllib.parse import urlencode
from urllib.request import Request, urlopen


CHECK_NAME = "Codacy AI Review"
CODACY_BOT_ID = 61871480
ACTIONS_APP_ID = 15368
PROTECTED_BASES = {"main", "feature/microsharp-capture-liveness"}
VERDICTS = {
    "No blocking issues noticed": "success",
    "Worth a closer look": "action_required",
    "Resolve issues before merging": "failure",
}


def review_result(head, reviews):
    matching = [
        review for review in reviews
        if review["user"] and review["user"]["id"] == CODACY_BOT_ID
        and review["user"]["type"] == "Bot" and review["commit_id"] == head
    ]
    if not matching:
        return None, "Waiting for Codacy AI Reviewer on this commit."
    last = max(matching, key=lambda review: (review["submitted_at"] or "", review["id"]))
    for line in (last["body"] or "").splitlines():
        if not line or line.startswith("### Pull Request Overview"):
            continue
        for verdict, conclusion in VERDICTS.items():
            if line.endswith(f"**{verdict}**") and last["state"] != "DISMISSED":
                return conclusion, f"Codacy AI Reviewer: {verdict}."
        break
    return "action_required", "Codacy AI Reviewer did not supply a recognized current verdict."


class GitHub:
    def __init__(self):
        self.root = f"{os.environ['GITHUB_API_URL']}/repos/{os.environ['GITHUB_REPOSITORY']}"
        self.headers = {
            "Accept": "application/vnd.github+json",
            "Authorization": f"Bearer {os.environ['GH_TOKEN']}",
            "X-GitHub-Api-Version": "2022-11-28",
            "Content-Type": "application/json",
        }

    def request(self, path, method="GET", payload=None):
        body = json.dumps(payload).encode() if payload is not None else None
        request = Request(self.root + path, data=body, headers=self.headers, method=method)
        with urlopen(request, timeout=20) as response:
            return json.load(response)

    def paginate(self, path, **query):
        for page in range(1, 1001):
            params = urlencode(dict(query, per_page=100, page=page))
            items = self.request(f"{path}?{params}")
            yield from items
            if len(items) < 100:
                return
        raise RuntimeError(f"Pagination exceeded the limit for {path}")


def publish_check(api, merge_sha, pulls):
    decisions = []
    for pull in pulls:
        reviews = list(api.paginate(f"/pulls/{pull['number']}/reviews"))
        outcome, detail = review_result(pull["head"]["sha"], reviews)
        decisions.append((outcome, f"PR #{pull['number']}: {detail}"))
    conclusions = {outcome for outcome, _ in decisions}
    if conclusions == {"success"}:
        result = "success"
    elif "failure" in conclusions:
        result = "failure"
    elif "action_required" in conclusions:
        result = "action_required"
    else:
        result = None
    status = "completed" if result else "in_progress"
    summary = "\n\n".join(detail for _, detail in decisions)
    query = urlencode(dict(check_name=CHECK_NAME, app_id=ACTIONS_APP_ID, per_page=1))
    existing = api.request(f"/commits/{merge_sha}/check-runs?{query}")["check_runs"]
    current = existing[0] if existing else None
    if current and (current["status"], current["conclusion"], current["output"]["summary"]) == (
        status, result, summary
    ):
        return
    update = {"status": status, "output": {"title": CHECK_NAME, "summary": summary}}
    if result:
        update.update(conclusion=result, completed_at=datetime.now(timezone.utc).isoformat())
    if current and current["status"] != "completed":
        api.request(f"/check-runs/{current['id']}", "PATCH", update)
    else:
        api.request("/check-runs", "POST", dict(update, head_sha=merge_sha, name=CHECK_NAME))


def main():
    api = GitHub()
    checks = defaultdict(list)
    for pull in api.paginate("/pulls", state="open"):
        if pull["draft"] or pull["base"]["ref"] not in PROTECTED_BASES:
            continue
        current = api.request(f"/pulls/{pull['number']}")
        if current["state"] == "open" and not current["draft"] and current["mergeable"] is True:
            checks[current["merge_commit_sha"]].append(current)
    for merge_sha, pulls in checks.items():
        publish_check(api, merge_sha, pulls)


if __name__ == "__main__":
    main()
