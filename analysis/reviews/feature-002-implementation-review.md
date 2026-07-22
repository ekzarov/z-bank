# Feature 002 Implementation Review

## Scope

- Date: 2026-07-22
- Feature: `002-customer-management`
- Orchestrator: OpenAI Codex
- External reviewer: Claude Code CLI 2.1.173, fresh read-only session
- Reviewed revision: `d1f39d1c6cc7b0fa07778f5c59366da31dd719df`
- Result: findings corrected; repeat review pending

## Round 1

Claude independently regenerated the diff, traced FR-001 through FR-013 and
T001 through T009, and ran the backend, SQL Server, and Angular gates. It
reported five items:

1. The Feature 002 migration clears pre-feature free-form customer links before
   adding the new foreign key. Accepted as a deployment-documentation gap. The
   runbook now requires `migrate` followed immediately by `provision-demo` for
   the demo and warns that non-demo links require an explicit data migration.
2. T001 claimed exact domain boundaries without testing all of them. Accepted.
   Unit coverage now includes ID and sort-code formats, min/max age, required
   fields, exact/max lengths, country code, email, and credit-score boundaries.
3. `SaveWithConflictAsync` only caught and rethrew the same exception. Accepted
   as cleanup; callers now invoke the repository directly.
4. Failed commands do not create audit rows. Rejected as a defect: rejected
   commands are not persisted mutations. FR-008 now states that successful
   mutations and their audit record commit atomically, while rejected commands
   leave neither domain changes nor a misleading success record.
5. Unused title-length and generated-API-documentation observations were not
   actionable for this slice. The supported-title allow-list is deliberately
   stricter, and the REST contract is the governed SDD/controller contract.

## Verification After Corrections

- Unit tests: 29 passed.
- SQL Server integration tests: 23 passed.
- Angular tests: 23 passed.
- Angular production build: passed.
- Workbook audit: passed, 135 rows.
- SDD audit: passed, 135 rows and 27 artifacts.
- Legacy evidence audit: passed, 512 references.
- Repeat external review: pending on the corrected immutable revision.

## Interaction Log

| Round | External result | Primary disposition | Correction | Repeat result |
|---|---|---|---|---|
| 1 | `FINDINGS` | Three accepted, audit interpretation clarified, two observations rejected | Tests, service cleanup, SDD, and deployment runbook updated | Pending |
