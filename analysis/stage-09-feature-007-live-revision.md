# Stage 9 Live Revision - Feature 007

## Scope

- Feature: `007-monthly-statements`
- Workbook rows: 100-107
- Date: 2026-07-23
- Result: clean for the delivered monthly-statement slice

## Legacy Baseline

The public legacy web edge remains reachable at `/z-bank/`. Authorized IBM
batch, CICS, IMS, and DB2 execution remains unavailable, so monthly-statement
runtime evidence is still explicitly `partial-simulated` or `static-only`.
The traceable simulator was rerun: 28 tests passed, including statement period,
ordering, empty-history, and reconciliation behavior.

The target preserves statement content and financial rules while applying the
owner-approved D-012 deviation: explicit API/configuration replaces JCL
parameter defaults, and responsive HTML/print replaces fixed printer pages.

## Modern Walkthrough

The deployed HTTPS application was exercised as customer and operator:

- A customer generates and views an owned-account statement.
- UTC month boundaries are inclusive start and exclusive next month.
- Future months and invalid periods are rejected.
- Customer/account identity, booked transactions, and summary values appear.
- Empty periods produce a valid explicit empty statement.
- Snapshot generation is immutable and idempotent for one data version.
- Reconciliation or source failure publishes no partial snapshot.
- Operator bulk generation reports account-level outcomes and retries only the
  selected failed accounts.
- Retained closed accounts remain in the configured sort-code bulk scope.
- Statement mutations retain CSRF, role, ownership, audit, and non-disclosure
  controls.

## Workbook Reconciliation

Rows 100-107 now contain destination implementation and test evidence. UF-009
is green and passed; `R1-G07` is closed. The workbook audit also narrowed
D-012 attribution to rows 101 and 106, the two scenarios that actually carry
the approved JCL/default and pagination deviations.

## Findings

Claude's two peer-review rounds identified test and contract gaps. All material
findings were corrected and verified. No new parity, authorization,
reconciliation, idempotency, bulk-isolation, or usability finding was observed
on the deployed slice.

The unavailable IBM runtime remains a recorded residual risk and is not
represented as live-verified.

## Gates

- Unit tests: 90 passed.
- Real SQL Server integration tests: 58 passed.
- Angular tests: 47 passed.
- Angular production build: passed.
- Local Playwright: 14 passed.
- Public HTTPS Playwright: 14 passed.
- Legacy simulator: 28 passed.
- Workbook audit: passed, 101 closed and 34 open rows.
- SDD audit: passed, 135 rows, 9 slices, 27 artifacts, and 112
  feature-qualified requirements.
- Legacy evidence audit: passed, 512 references.
- SDD lifecycle tests: 3 passed.
- Claude slice peer review: 2 usable sessions, all material findings corrected.

The deterministic gates are clean. The committed candidate proceeds to a fresh
read-only Stage 10 reviewer.
