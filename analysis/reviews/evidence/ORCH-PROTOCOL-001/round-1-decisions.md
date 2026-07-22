# ORCH-PROTOCOL-001 Round 1 Decisions

- Reviewer: Claude Code CLI, fresh read-only session
- Base revision: `10ebe787b6041d7df3bdcef0d5761631a46c82ed`
- Reviewer result: `FINDINGS`
- Repository mutation by reviewer: none; status matched before and after

## Validated Findings

| Finding | Primary-agent decision | Resolution |
|---|---|---|
| Formal clean-worktree rule conflicted with pre-commit review | Accepted in part: the rule was valid for formal passes, but peer-review handling was ambiguous | Split peer review from formal review; peer review preserves the expected dirty delta, while formal review uses a committed immutable ref and clean worktree |
| Missing confidentiality/data-egress constraint | Accepted | Added classification and explicit prohibitions for credentials, personal data, regulated data, and unapproved repository content |
| Mandatory/optional slice-review wording conflict | Accepted | Made external peer review mandatory after slice tests pass |
| Formal reviewer could rely on an orchestrator-curated diff | Accepted | Formal reviewer must independently regenerate and enumerate scope from immutable revisions |
| Raw-response retention was not durable or consistent | Accepted | Formal-pass packets, checkpoints, and raw responses are retained under `analysis/reviews/evidence/<packet-id>/` with digests in the report |
| Orchestrator context loss was not fail-closed | Accepted | Applied checkpoint restore and acknowledgement rules to reviewer and orchestrator context resets |

No finding was rejected. A fresh final-diff review is required after these
material corrections.

The required final review is retained in [`round-2-raw.md`](round-2-raw.md),
SHA-256 `e181e013b56ef3d375e488c6f1c1c335256c6d59a8e38e2c046e47e507c897cf`,
and returned `CLEAN`.
