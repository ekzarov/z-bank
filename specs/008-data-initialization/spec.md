# Feature 008: Explicit Data Initialization and Import

## Traceability

- Workbook rows: 119-126
- Owner decisions: D-013, D-021
- Depends on: target schemas from Features 001-007

## Goal

Replace IBM DB2/IMS loaders with auditable, explicitly invoked schema migration
and data import/demo-data operations that never mutate storage at application
startup.

## User Stories

### US1 - Apply schema deliberately (P1)

An operator can inspect and explicitly apply versioned EF migrations to a
chosen environment and obtain a deterministic success/failure result.

### US2 - Load legacy or demo data deliberately (P1)

An operator can validate and import customers, accounts, relationships,
history, and required reference/status data using an explicit idempotent command.

## Functional Requirements

- **FR-001** API startup SHALL NOT call `Migrate`, `EnsureCreated`, seeders, or
  any schema/data repair routine.
- **FR-002** Every schema change SHALL be represented by a reviewed versioned EF
  migration with a documented explicit apply command.
- **FR-003** Import SHALL support customers, accounts, ownership relationships,
  history, legacy transaction-run status/timing/customer linkage, and required
  reference data in dependency order.
- **FR-004** Import SHALL preserve string identifiers, decimal precision,
  source identifiers, `SourceSystem`, and known timestamps where available.
- **FR-005** Input SHALL be validated before mutation and errors SHALL identify
  the source record without exposing secrets or unrelated personal data.
- **FR-006** The command SHALL be idempotent: rerunning identical input SHALL
  not duplicate entities or financial activity.
- **FR-007** Referential and financial integrity failures SHALL not leave a
  partially trusted dataset. Input SHALL be validated into staging first and
  promoted atomically. Failed promotion leaves only an auditable failed staging
  run, never trusted domain rows; retry resumes by input fingerprint.
- **FR-008** Demo data SHALL be versioned and deterministic and SHALL use no
  committed usable passwords. Initial credentials come from operator input or
  secret configuration and require change where applicable.
- **FR-009** Every invocation SHALL produce counts, rejects, duration, input
  fingerprint, migration version, and an operator audit record.
- **FR-010** IMS-specific staging without loading and DB2 bind/package mechanics
  SHALL NOT be reproduced; equivalent target readiness is migration/import
  validation.
- **FR-011** The explicit demo reset command MAY accept start/end/step/seed
  controls, SHALL be deterministic, SHALL require destructive confirmation and
  environment authorization, and SHALL never run as normal startup (D-021).
- **FR-012** Migration/import/reset execution SHALL require an authorized
  operator and least-privilege database credentials distinct from API runtime
  credentials; production reset SHALL be disabled by default.

## Success Criteria

- Starting the API against an empty database does not create any object.
- Integration tests apply migrations to real SQL Server, import customers,
  accounts, history and transaction-run status twice, and prove idempotency,
  atomic staging/promotion, FK integrity, precision, authorization, and rollback.
- Operator documentation demonstrates inspect/apply/import/verify commands.
