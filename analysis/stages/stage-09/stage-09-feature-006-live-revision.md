# Stage 9 Live Revision - Feature 006

## Scope

- Feature: `006-transaction-history`
- Workbook rows: 91-96, 98
- Date: 2026-07-23
- Result: clean for the delivered transaction-history slice

## Legacy Baseline

The public legacy web edge remains reachable at `/z-bank/`. Authorized IBM
CICS, IMS, DB2, and 3270 execution remains unavailable, so the transaction
history runtime baseline is still explicitly `partial-simulated`. The
traceable simulator was rerun: 28 tests passed, including the published but
unbound legacy account/transaction routes.

The source baseline confirms a bounded latest-50 Java/DB2 history path and
published list/detail API contracts whose generated operation mappings are
missing. The target implements the supported business capability and does not
claim parity with those absent mappings.

## Modern Walkthrough

The deployed HTTPS application was exercised as customer and operator:

- History is newest-first with stable cursor pagination.
- Optional UTC `from` and exclusive `to` filters are applied.
- A customer sees only owned-account history.
- An operator sees authorized account history.
- Missing and unauthorized list/detail requests do not disclose foreign data.
- Empty history is a successful empty page.
- Detail exposes amount, direction, status, description, booking time,
  reference, related transfer reference, and source provenance.
- Public transaction resources remain read-only.

## Workbook Reconciliation

Rows 91-96 and 98 now contain destination implementation evidence. `UF-008`
is green and passed. The visual pass also found and corrected a stale `Rev 1`
status for the already accepted Feature 005; `R1-G05` and `R1-G06` now agree
with the main parity map.

## Findings

No new parity, authorization, pagination, provenance, or usability finding was
observed on the delivered slice. The unavailable IBM runtime remains a
recorded residual risk and is not represented as live-verified.

## Gates

- Unit tests: 75 passed.
- Real SQL Server integration tests: 50 passed.
- Angular tests: 39 passed.
- Local Playwright: 11 passed.
- Public HTTPS Playwright: 11 passed.
- Legacy simulator: 28 passed.
- Workbook audit: passed, 93 closed and 42 open rows.
- SDD audit: passed, 135 rows, 9 slices, 27 artifacts, and 112
  feature-qualified requirements.
- Legacy evidence audit: passed, 512 references.
- SDD lifecycle tests: 3 passed.
- Workbook formula-error scan: 0 matches.
- Claude slice peer review: 2 usable sessions, final result clean.

The deterministic gates are clean. The committed candidate proceeds to a fresh
read-only Stage 10 reviewer.
