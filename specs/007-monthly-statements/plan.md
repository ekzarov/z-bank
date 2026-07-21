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

## Verification

Use a controllable clock. Unit-test month boundaries, leap years, timezones,
totals, and empty history. Categorized SQL Server/API tests use real data and
authorization. Vitest verifies rendering; Playwright verifies generation and
print/download access.
