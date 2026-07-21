# Feature 005: Funds Transfers

## Traceability

- Workbook rows: 80-85
- Owner decisions: D-004, D-006, D-011
- Depends on: Features 001-004

## Goal

Transfer funds between eligible Bank of Z accounts atomically with consistent
authorization, available-funds enforcement, and transaction evidence.

## User Stories

### US1 - Transfer between accounts (P1)

A customer can transfer between owned eligible accounts, and an operator can
perform an authorized internal transfer, then see both resulting balances and
a correlation reference.

### US2 - Reject an unsafe transfer (P1)

Invalid, same-account, unauthorized, missing, closed, currency-incompatible, or
insufficient-funds transfers leave both accounts and history unchanged.

## Functional Requirements

- **FR-001** A transfer SHALL require distinct source and destination accounts
  and a strictly positive decimal amount.
- **FR-002** Customer transfers SHALL require ownership of the source account;
  destination details SHALL be disclosed only as required for confirmation.
- **FR-003** Both accounts SHALL be active and currency-compatible; supported
  product policies SHALL be evaluated before mutation.
- **FR-004** Available funds/overdraft policy SHALL be enforced before debit;
  the legacy absence of an evidenced pre-transfer check SHALL not be preserved.
- **FR-005** Source debit, destination credit, paired immutable transaction
  records, audit evidence, and idempotency record SHALL commit atomically.
- **FR-006** Both sides SHALL share a transfer correlation ID while retaining
  distinct transaction references.
- **FR-007** Safe retry behavior SHALL match Feature 004 idempotency rules.
- **FR-008** Bank-to-bank screen/program definitions that were not deployed
  SHALL NOT be represented as a supported external transfer capability.
- **FR-009** Failure SHALL return stable Problem Details and SHALL never expose
  a partial balance change.

## Success Criteria

- Unit tests cover validation, ownership, product/currency, and funds rules.
- SQL Server tests prove two-account atomicity, rollback, concurrency, paired
  history/audit, and idempotency.
- Playwright covers a successful owned-account transfer and rejected funds case.
