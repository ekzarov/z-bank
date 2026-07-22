# Feature 003: Account Management and Balances

## Traceability

- Workbook rows: 41-58, 88, 110-112
- Owner decisions: D-007, D-008, D-009, D-011, D-016, D-017, D-019, D-020, D-023
- Depends on: Features 001-002

## Goal

Provide a unified, typed view of customer accounts and balances plus authorized
account lifecycle operations without route-specific CICS/IMS inconsistencies.

## User Stories

### US1 - Browse accounts and balances (P1)

An operator can browse a customer's accounts; a customer can browse only their
own. Deep links resolve consistently and empty portfolios are explicit.

### US2 - Create and maintain an account (P1)

An operator can create and update supported account metadata with validated
product rules and audit evidence.

### US3 - Close an eligible account (P1)

An operator can close an eligible zero-balance account; missing, stale, or
ineligible accounts remain unchanged with a clear result.

## Functional Requirements

- **FR-001** Account IDs and account numbers SHALL remain strings; sort codes
  SHALL preserve leading zeroes and validate as six digits where applicable.
- **FR-002** The target SHALL define one `AccountType` vocabulary including
  current, savings/ISA, loan, and mortgage and SHALL explicitly map all known
  legacy values.
- **FR-003** `CHECKING`, raw CICS values, and route-specific differences SHALL
  normalize to the target enum; raw values MAY remain provenance metadata.
- **FR-004** Account details SHALL expose actual balance, available balance,
  currency, product terms, status, owner, and provenance consistently.
- **FR-005A** Portfolio queries SHALL return every authorized account through
  bounded pagination and SHALL NOT reproduce IMS six-entry or CICS ten/twenty
  presentation caps (D-019).
- **FR-005** Customer authorization SHALL be relationship-based and enforced in
  API queries; foreign accounts SHALL not be disclosed.
- **FR-006** Create/update SHALL validate product type, dates, rate, overdraft,
  currency, and identifier formats and use optimistic concurrency. Supported
  types are ISA, CURRENT, LOAN, SAVING, and MORTGAGE. Interest is non-negative,
  at most `9999.99`, and has at most two decimals; loan/mortgage interest is
  non-zero; overdraft is a non-negative integer. Creation SHALL reject an
  eleventh account. The system generates the account number, derives the
  validated sort code from bank configuration, assigns the opening date,
  initializes balances to zero, and manages statement dates (D-016/D-020).
- **FR-006A** Metadata updates SHALL never accept or change actual/available
  balances and SHALL enforce the same product/rate/overdraft boundaries.
- **FR-007** Every creation/update/closure SHALL be audited atomically.
- **FR-008** Closure SHALL require zero settled/available balance and no pending
  work; history SHALL remain and status SHALL become closed. The target SHALL
  NOT reproduce the legacy unconditional hard delete (D-017).
- **FR-009** The dormant IMS old-account zero-balance rule SHALL NOT be ported.
- **FR-010** Legacy web `Feature Not Implemented` account operations SHALL be
  replaced by the supported target lifecycle workflows.
- **FR-011** Missing generated legacy error mappings SHALL be replaced by
  documented target Problem Details, including account list/detail/balance.
- **FR-012** This slice owns authorized complete account portfolio, account
  detail, and actual/available balance read models. Feature 004 owns balance
  mutation; metadata commands in this slice SHALL never mutate balances.
- **FR-013** Account identifier allocation, account persistence, and audit
  persistence SHALL commit atomically. Failure SHALL leave no partial account
  or audit record; consumed identifier gaps are allowed (D-023).

## Success Criteria

- Unit tests cover type mapping, exact rate/overdraft/default rules, the
  ten-account limit, statement-date ownership, and lifecycle/product rules.
- SQL Server tests cover ownership FK, typed constraints, precision, optimistic
  concurrency, closure eligibility, audit atomicity, and rollback of the full
  identifier-allocation/account/audit creation transaction.
- UI/API and Playwright tests cover pagination, list/detail/balance contracts,
  direct deep links, invalid route parameters, create/edit/close, empty,
  unauthorized, not-found, and validation flows.
