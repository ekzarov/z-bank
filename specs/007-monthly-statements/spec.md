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
  validated account/scope and calendar month; it SHALL NOT run at startup.
- **FR-002** The period SHALL use one documented timezone and inclusive start,
  exclusive next-month boundaries.
- **FR-003** Content SHALL include statement date, customer identity/address,
  account identity/type/currency, ordered booked transactions, and summary.
- **FR-004** Summary SHALL reconcile opening balance plus credits minus debits
  to closing balance and expose available balance and transaction count.
- **FR-005** An empty period SHALL produce a valid statement with an explicit
  no-transactions message and zero transaction totals.
- **FR-006** Generated content SHALL be an immutable snapshot with a generation
  ID and input/data timestamps so later profile changes do not alter it.
- **FR-007** Generation SHALL be idempotent for the same account/period/data
  version or create a clearly versioned replacement.
- **FR-008** Customers SHALL access only owned statements; operators SHALL use
  staff policy and every generation/download SHALL be audited.
- **FR-009** Data/query failures SHALL fail the statement and record diagnostics;
  untrusted partial totals SHALL not be published.
- **FR-010** JCL defaults, SYSPRINT formatting, and fixed printer pagination
  SHALL NOT be ported. The UI/export SHALL use modern responsive presentation.

## Success Criteria

- Unit tests cover period boundaries and reconciliation.
- SQL Server tests cover transaction selection, empty periods, snapshots,
  idempotency, authorization, and failure rollback.
- Playwright covers generate/view/download for a populated and empty statement.
