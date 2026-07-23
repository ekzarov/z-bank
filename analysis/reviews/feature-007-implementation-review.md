# Feature 007 implementation review

## Scope

- Feature: `007-monthly-statements`
- Reviewer: Claude Code CLI, two fresh read-only sessions
- Orchestrator: OpenAI Codex
- Reviewed candidate: base `da61e78` plus the Feature 007 working-tree delta
- Result: findings corrected; immutable acceptance remains for Stage 10

## Round 1

- Packet: `FEATURE007-PEER-001`
- Session: `175759a5-04c7-44f0-9409-4cbb8bfab2dc`
- Result: findings
- Accepted:
  - add primary prior-period opening-balance coverage;
  - store the statement date as `DateOnly` so timezone conversion cannot move
    it to another calendar day;
  - derive equal-timestamp financial ordering independently from display order;
  - compare data-version references ordinally;
  - add negative authorization coverage.
- Rejected: none.

## Round 2

- Packet: `FEATURE007-PEER-002-FINAL`
- Session: `cfb0fc41-3f0b-4e94-89fc-695e8dd92703`
- Result: findings
- Accepted and corrected:
  - synchronize tasks and workbook rows 100-107 in this delivery;
  - test failed-only bulk retry and invalid sort-code scope against the real API;
  - test the service concurrent-reuse path and SQL unique-conflict rollback;
  - prove retained closed accounts participate in bulk generation;
  - define and enforce UTC as the statement presentation timezone;
  - reject future statement periods;
  - preserve cancellation instead of converting it to a failed statement;
  - align the Angular fixture with the `DateOnly` JSON contract.
- Accepted as optional hardening, not required for this slice:
  - future ledger-chain validation may be tightened beyond fail-closed
    reconciliation if imported histories reveal ambiguous balance cycles;
  - Problem Details status for a reconciliation refusal may later move from 503
    to 409/422 without changing the stable problem code.
- Rejected: none.

The reviewer could not run shell commands in round 2. The orchestrator verified
the complete diff and confirmed the exact worktree manifest was unchanged
before and after the session. No reviewer-created file or repository mutation
was present.

## Corrected gates

- .NET unit tests: 90 passed.
- SQL Server integration tests: 58 passed.
- Angular/Vitest tests: 47 passed.
- Angular production build: passed.
- Playwright full suite: 13 passed.
- Legacy simulation tests: 28 passed.
- SDD lifecycle tests: 3 passed.
- Workbook, SDD, and legacy-evidence audits: passed.

## Gate

No known material slice-review finding remains. Because the second and final
discussion round returned findings before the corrections were applied, the
corrected immutable revision must receive the normal fresh Stage 10 independent
acceptance before Feature 007 is accepted.
