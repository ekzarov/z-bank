# FEATURE004-PEER-001 external response

- Reviewer: Claude Code CLI 2.1.173, fresh read-only Opus session
- Base: `9815c010f0890c00b5dc230013a08ed795692ee9`
- Head: `2206d1b06dac1c40ef05f4226d0cd6c46905b7e4`
- Eligibility: acknowledged; separate detached worktree remained clean
- Result: `FINDINGS`

## Finding 1 - Playwright reference mismatch

**Severity:** Medium

The cash Playwright path expected a 12-digit transaction reference, while the
application creates and renders a 32-character lowercase hexadecimal GUID.
The tagged happy path would therefore fail on a real stand. The reviewer asked
for a `[0-9a-f]{32}` assertion and a live run before delivery sign-off.

## Finding 2 - idempotency test gap

**Severity:** Low-Medium

The reviewer found no automated coverage for missing, oversized, or otherwise
invalid `Idempotency-Key` values. It also noted that T001 claimed unit-level
idempotency/provenance coverage while replay and conflict were covered only by
integration tests. It requested rejected-key API coverage with zero bookings
and recommended direct service tests for replay and conflict.

## Informational observations

- The UI generates a new UUID per user invocation instead of persisting a key
  across a transport retry; the in-flight guard still prevents double-clicks.
- The UI maximum amount exceeds JavaScript's safe integer range; the .NET
  decimal domain remains authoritative.
- The authorization gate and transaction each read the account, which is safe
  but redundant.
- HTTP 400 for insufficient funds is permitted by the SDD.

## Tests run by reviewer

- Backend unit tests: 53 passed.
- Angular, SQL integration, and Playwright were reviewed statically because the
  isolated reviewer worktree had no Node dependencies or provisioned runtime.

The reviewer enumerated FR-001 through FR-012 and found no other actionable
SDD, authorization, transaction, migration, or UI defects.
