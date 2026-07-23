# Plan: Monthly Statements

## Design

Add a statement application service and immutable statement snapshot tables.
Read booked transactions through the history query model using the documented
UTC bank timezone and reject non-UTC configuration. Reject future periods.
Calculate totals with decimal arithmetic and reject failed
reconciliation. Use the last pre-period booked balance (or derive from the
first in-period booking) as the historical opening point, order bookings by
timestamp/reference, represent the statement date as a local calendar date,
and snapshot the generation-time available balance.
Persist content/version metadata transactionally.

The API exposes explicit generation and read/download resources. Angular
renders responsive statement content and a print-friendly view. A portable PDF
export MAY be added only if its library and deterministic test strategy are
approved during implementation; HTML/print is the required baseline.

Bulk generation resolves accounts by configured bank sort code, isolates each
account result, and returns a run summary. Immutable/idempotent snapshots make
successful accounts safe to skip during an operator retry; failed accounts
retain diagnostics without publishing partial content. A retry may submit only
failed account identifiers; an omitted list processes all retained accounts,
including closed accounts, for the configured sort code.

## Verification

Use a controllable clock. Unit-test month boundaries, leap years, timezones,
exact customer/account/transaction fields, totals, and empty history.
Categorized SQL Server/API tests use real data, multi-account bulk runs, and
authorization. Vitest verifies rendering; Playwright verifies generation and
print/download access.
