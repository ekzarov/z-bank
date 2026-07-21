# Feature 006: Transaction History

## Traceability

- Workbook rows: 91-96, 98
- Owner decisions: D-006, D-008
- Depends on: Features 001-005

## Goal

Provide authorized, pageable transaction history and transaction details from
the unified immutable ledger without reproducing dead generated API mappings.

## User Stories

### US1 - Browse recent transactions (P1)

A customer can browse recent activity for an owned account; an operator can
browse an authorized customer's account with clear ordering and pagination.

### US2 - View one transaction (P1)

An authorized user can open a transaction and see amount, direction, status,
description, booking time, reference, related transfer reference, and source
provenance where applicable.

## Functional Requirements

- **FR-001** History SHALL be ordered by booking timestamp descending with a
  deterministic transaction-ID tie-breaker.
- **FR-002** The default page size SHALL be 50 and SHALL have a documented
  bounded maximum; pagination SHALL not skip/duplicate stable records.
- **FR-003** Customer ownership and staff policies SHALL be enforced on list
  and detail endpoints.
- **FR-004** Missing or unauthorized records SHALL not disclose foreign data.
- **FR-005** Imported CICS/IMS history SHALL retain source identifiers and
  `SourceSystem`; modern booked activity SHALL use the same target DTO.
- **FR-006** Transaction records SHALL be immutable through public APIs.
- **FR-007** Standard validation, authentication, authorization, not-found,
  conflict, and server failures SHALL use target Problem Details.
- **FR-008** The target SHALL expose only implemented supported resources. It
  SHALL NOT recreate unbound operation mappings or missing generated YAML.
- **FR-009** An account with no transactions SHALL return an empty page, not an
  error.

## Success Criteria

- SQL Server/API tests prove authorization, ordering, stable pagination,
  detail lookup, empty history, provenance, and immutability.
- Angular/Playwright tests cover list-to-detail navigation and empty state.
