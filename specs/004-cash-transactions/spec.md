# Feature 004: Deposits and Withdrawals

## Traceability

- Workbook rows: 60-78, 89
- Owner decisions: D-003, D-004, D-005, D-006, D-011
- Depends on: Features 001-003

## Goal

Let authorized customers and operators deposit to or withdraw from eligible
accounts through one rule set with atomic balances, history, and audit.

## User Stories

### US1 - Deposit funds (P1)

A customer or operator can deposit a positive amount into an authorized,
eligible transactional account and immediately see the resulting balance.

### US2 - Withdraw funds (P1)

A customer or operator can withdraw a positive amount when available funds and
product rules allow it.

### US3 - Understand rejected activity (P1)

Invalid amounts, identifiers, ownership, product types, insufficient funds,
closed accounts, and concurrency conflicts fail without changing any balance or
writing a booked transaction.

## Functional Requirements

- **FR-001** Deposit and withdrawal commands SHALL accept decimal amounts that
  are strictly greater than zero and have currency-compatible precision.
- **FR-002** Direction SHALL be represented by the command/transaction type;
  signed values SHALL NOT reverse operation direction.
- **FR-003** A customer SHALL mutate only an owned account; an operator SHALL
  act only under the operator policy. Ownership failures SHALL not expose a
  foreign account.
- **FR-004** Cash operations SHALL be allowed only for active transactional
  accounts. Loan/mortgage cash mutation and channel/facility bypasses SHALL be
  rejected.
- **FR-005** Withdrawals SHALL enforce sufficient available funds and the
  configured overdraft rule consistently for customer and operator activity.
- **FR-006** Balance mutation, immutable booked transaction, last-transaction
  reference, and audit record SHALL commit atomically in SQL Server.
- **FR-007** Every successful command SHALL return account ID, amount, currency,
  transaction reference, actual balance, and available balance.
- **FR-008** A rejected command SHALL leave balances/history unchanged and
  return Problem Details with a stable business error code.
- **FR-009** An idempotency key SHALL prevent duplicate booking after a safe
  client retry and SHALL reject reuse with a different request payload.
- **FR-010** The target deposit view SHALL display the returned balance and
  SHALL NOT preserve the CICS `$N/A` rendering defect.
- **FR-011** SourceSystem SHALL be recorded on imported historical activity;
  new activity uses `Modern` and never selects CICS/IMS processing branches.

## Intentional Deviations

- IMS mismatched-customer mutation, signed/zero amounts, and negative-balance
  withdrawal behavior are security/data-integrity defects and are corrected.
- PAYMENT/teller facility differences are replaced by one product policy.
- CICS and IMS deposit endpoints become one target operation.

## Success Criteria

- Unit tests cover amount, direction, product, funds, and idempotency rules.
- Real SQL Server tests prove atomic balance/history/audit writes, rollback,
  concurrency, precision, and idempotency uniqueness.
- Playwright covers customer deposit, withdrawal, updated balance, and a
  rejected insufficient-funds attempt.
