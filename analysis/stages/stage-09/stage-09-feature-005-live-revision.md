# Stage 9 Live Revision - Feature 005

## Scope

- Feature: `005-funds-transfers`
- Workbook rows: 80-85
- Date: 2026-07-23
- Result: clean for the delivered funds-transfer slice

## Legacy Baseline

The public legacy web edge remains reachable at `/z-bank/`. Authorized IBM
CICS, IMS, DB2, and 3270 execution remains unavailable, so the transfer runtime
baseline is still explicitly `partial-simulated`. The traceable simulator was
rerun: 28 tests passed and its walkthrough exercised the CICS atomic transfer.

The baseline confirms source debit/destination credit, rollback semantics, one
source-side processed-transaction record, internally supplied sort code, and
the observed absence of a pre-transfer overdraft rejection.

## Modern Walkthrough

The deployed HTTPS application was exercised as customer and operator:

- A customer transferred between two owned active GBP demo accounts and saw
  the resulting balance and correlation reference.
- An operator transferred between managed active accounts.
- Insufficient funds was rejected without a partial balance change.
- Same-account, product, activity, ownership/non-disclosure, currency,
  precision, and idempotency rules remain server-enforced.
- Debit, credit, paired immutable Modern history, paired audit, and idempotency
  commit in one serializable SQL transaction.

The deliberate differences are already governed: D-004 enforces available
funds/overdraft, D-006 writes paired correlated history/audit instead of one
legacy source record, D-011 excludes the non-deployed bank-to-bank surface, and
D-020 keeps sort codes system-managed.

## Workbook Reconciliation

Rows 80-85 now contain destination implementation notes and concrete code/test
evidence. `UF-007` is green and passed. No Feature 006 history row or unrelated
scenario was closed.

## Findings

No new parity, authorization, atomicity, or usability finding was observed for
this slice. The unavailable IBM runtime remains a recorded residual risk and
is not represented as live-verified.

## Gates

- Release build: passed with 0 warnings/errors.
- Unit tests: 71 passed.
- Real SQL Server integration tests: 46 passed.
- Angular tests: 34 passed.
- Local Playwright: 9 passed.
- Public HTTPS Playwright: 9 passed.
- Legacy simulator: 28 passed; transfer walkthrough passed.
- Workbook audit: passed, 86 closed and 49 open rows.
- SDD audit: passed, 135 rows, 9 slices, 27 artifacts, and 112
  feature-qualified requirements.
- Legacy evidence audit: passed, 512 references.
- SDD lifecycle tests: 3 passed.
- Workbook formula-error scan: 0 matches.

The deterministic gates are clean. The committed candidate proceeds to a fresh
read-only Stage 10 reviewer.
