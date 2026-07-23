# Feature 007: Monthly Statements

## Traceability

- Workbook rows: 100-107
- Owner decision: D-012
- Depends on: Features 001-006

## Goal

Generate reproducible monthly account statements containing the useful legacy
financial content without reproducing JCL, PL/I printer mechanics, or fixed
page breaks.

## User Stories

### US1 - Generate a statement (P1)

An operator can explicitly generate statements for a month and selected scope;
a customer can request/view a statement for an owned account.

### US2 - Read statement content (P1)

The statement shows customer/account identity, period transactions, opening
balance, credits, debits, closing/available balance, count, and a clear empty
period state.

## Functional Requirements

- **FR-001** Statement generation SHALL require an explicit invocation and
  validated account/scope and calendar month; it SHALL NOT run at startup. An
  operator MAY request all accounts for the configured bank sort code. Bulk
  results SHALL report per-account success/failure and support retry without
  republishing successful immutable snapshots.
- **FR-002** The period SHALL use UTC and inclusive start, exclusive next-month
  boundaries. Non-UTC configuration SHALL fail fast. The displayed statement date SHALL be the
  final calendar date of that configured local month and SHALL be stored
  without a timezone offset; generation time SHALL remain a separate timestamp.
  A future calendar month SHALL be rejected; the current month MAY be
  generated and replaced only when its data version changes.
- **FR-003** Content SHALL include statement date, customer identity/address,
  phone, account identity/type/currency, interest rate, overdraft limit,
  ordered booked transactions, and summary. Each transaction SHALL include
  date/time, type/direction, reference, description, and amount; balances belong
  to the summary, not individual legacy transaction lines.
- **FR-004** Summary SHALL reconcile opening balance plus credits minus debits
  to closing balance and expose available balance and transaction count.
  Opening balance SHALL use the last booked actual balance before the period
  when available, otherwise derive from the first in-period booked result; an
  empty history without an earlier booking uses the account actual balance.
  Available balance is the immutable generation-time account value.
- **FR-005** An empty period SHALL produce a valid statement with an explicit
  no-transactions message and zero transaction totals.
- **FR-006** Generated content SHALL be an immutable snapshot with a generation
  ID and input/data timestamps so later profile changes do not alter it.
- **FR-007** Generation SHALL be idempotent for the same account/period/data
  version or create a clearly versioned replacement. Operator retry MAY name
  only failed account identifiers; omitted scope means every account for the
  configured bank sort code, including retained closed accounts.
- **FR-008** Customers SHALL access only owned statements; operators SHALL use
  staff policy and every generation/download SHALL be audited.
- **FR-009** Data/query failures SHALL fail the statement and record diagnostics;
  untrusted partial totals SHALL not be published.
- **FR-010** JCL defaults, SYSPRINT formatting, and fixed printer pagination
  SHALL NOT be ported. The UI/export SHALL use modern responsive presentation.

## Calculation Decisions

- Statement transactions are ordered oldest first by booking timestamp and
  then by immutable reference so equal timestamps remain deterministic.
- Credits and debits use the target's typed transaction direction. The legacy
  batch's `CR`/`CRD` string check contradicts the canonical `CRE`/`PCR` values
  and is treated as a corrected legacy defect rather than reproduced.
- A source read or reconciliation failure publishes no statement snapshot.
  Bulk generation isolates that account, reports diagnostics, and allows a
  scoped retry while already successful snapshots are reused.

## Success Criteria

- Unit tests cover period boundaries, exact statement fields, and reconciliation.
- SQL Server tests cover transaction selection, empty periods, snapshots,
  idempotency, authorization, and failure rollback.
- Playwright covers account and bulk generation, per-account bulk results,
  retry, and view/download for populated and empty statements.
