# Plan: Monthly Statements

## Design

Add a statement application service and immutable statement snapshot tables.
Read booked transactions through the history query model using a configured
bank timezone. Calculate totals with decimal arithmetic and reject failed
reconciliation. Persist content/version metadata transactionally.

The API exposes explicit generation and read/download resources. Angular
renders responsive statement content and a print-friendly view. A portable PDF
export MAY be added only if its library and deterministic test strategy are
approved during implementation; HTML/print is the required baseline.

Bulk generation resolves accounts by configured bank sort code, isolates each
account result, and returns a run summary. Immutable/idempotent snapshots make
successful accounts safe to skip during an operator retry; failed accounts
retain diagnostics without publishing partial content.

## Verification

Use a controllable clock. Unit-test month boundaries, leap years, timezones,
exact customer/account/transaction fields, totals, and empty history.
Categorized SQL Server/API tests use real data, multi-account bulk runs, and
authorization. Vitest verifies rendering; Playwright verifies generation and
print/download access.
