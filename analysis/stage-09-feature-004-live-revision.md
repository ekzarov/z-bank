# Stage 9 Live Revision - Feature 004

## Scope

- Feature: `004-cash-transactions`
- Workbook rows: 60-78, 89
- Date: 2026-07-22
- Result: clean for the delivered cash-transaction slice

## Legacy Baseline

The original Bank of Z web edge remains available at `/z-bank/`. Real CICS,
IMS, DB2, and 3270 cash processing still requires the unavailable authorized
IBM runtime. The approved Stage 3 result remains `partial-simulated`; no static
or simulated legacy behavior is relabelled as live-observed.

## Modern Walkthrough

The deployed HTTPS application was exercised as a real customer:

- An active transactional account accepted a positive decimal deposit.
- The response and account view displayed the actual resulting balance and
  immutable transaction reference instead of the legacy `$N/A` defect.
- A withdrawal beyond available funds and overdraft returned stable Problem
  Details and did not change the balance.
- A valid withdrawal committed and the refreshed account view showed the
  resulting balance.
- Authentication, account ownership, product eligibility, positive amount,
  precision, and system-managed sort code remained server-enforced.
- A repeated idempotency key replayed the original result; conflicting reuse
  was rejected without a duplicate booking.

Real SQL Server tests separately prove balance/history/audit/idempotency
atomicity, rollback, decimal precision, foreign-account non-disclosure, unique
references, and serialized concurrent withdrawals.

## Workbook Reconciliation

Feature 004 rows 60-78 and 89 now record destination implementation notes and
concrete code/test evidence. `UF-006` and revision gap `R1-G04` are closed.
The Feature 004 row inside mixed epic `UF-008` is green while that epic remains
open for Feature 006 transaction history. No unrelated detail row was closed.

## Findings

No cash-transaction parity or correctness finding remains. The observed demo
credential handoff confusion is an operational usability concern, not a cash
transaction defect. It is recorded in Feature 009 so delivery documentation
can name demo personas and the secret source without committing credentials.

## Gates

- Release build: passed with 0 warnings/errors.
- Unit tests: 57 passed.
- Real SQL Server integration tests: 42 passed.
- Angular tests: 32 passed.
- Local Playwright: 7 passed.
- Public HTTPS Playwright: 7 passed.
- Workbook audit: passed, 80 closed and 55 open rows.
- SDD audit: passed, 135 rows, 9 slices, 27 artifacts, and 112
  feature-qualified requirements.
- Legacy evidence audit: passed, 512 references.
- Excel collapsed, Feature 004 detail, and revision-sheet visual render: passed.

The deterministic gates are clean. The slice proceeds to a fresh read-only
Stage 10 reviewer against the committed immutable candidate.
