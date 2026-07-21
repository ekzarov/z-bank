# Feature 002: Customer Management

## Traceability

- Workbook rows: 22-39, 87, 90, 97
- Owner decisions: D-008, D-009, D-010
- Depends on: Feature 001

## Goal

Let authorized staff find, create, update, and retire customers through one
consistent UI/API while customers can view their own profile.

## User Stories

### US1 - Find and view customers (P1)

An operator can search by normalized customer ID or name and open a customer;
a customer can open only their own profile.

### US2 - Create a customer (P1)

An operator can validate customer data, receive a credit assessment, and create
the customer atomically with provenance and audit evidence.

### US3 - Maintain a customer (P1)

An operator can update customer details and retire an eligible customer; stale
updates, missing records, validation failures, and customers with blocking
accounts produce explicit non-destructive results.

## Functional Requirements

- **FR-001** Customer identifiers SHALL be immutable strings and SHALL not lose
  leading zeroes.
- **FR-002** Operators SHALL search by exact ID and case-insensitive normalized
  name; customers SHALL retrieve only their associated profile.
- **FR-003** A missing customer SHALL return `404` without leaking unrelated
  customer information.
- **FR-004** Create/update SHALL validate required identity/contact fields,
  supported date formats, lengths, and business ranges at the API boundary and
  domain boundary.
- **FR-005** Customer creation SHALL call an `ICreditAssessmentProvider` port.
  The demo implementation SHALL be deterministic and clearly identified as
  simulated; failures SHALL not create a partial customer.
- **FR-006** Create/update SHALL use optimistic concurrency and return a
  conflict for stale writes.
- **FR-007** Customer retirement SHALL be rejected while active accounts or
  unresolved financial obligations exist; successful retirement SHALL be a
  soft state transition, not destructive history deletion.
- **FR-008** Every mutation SHALL record actor, timestamp, action, entity ID,
  result, and correlation ID without sensitive field values.
- **FR-009** The model SHALL retain `SourceSystem` (`Cics`, `Ims`, `Modern`) and
  optional source identifier as provenance, not as routing behavior.
- **FR-010** Supported REST resources SHALL be documented from the target
  contract; missing generated legacy response files SHALL not be reproduced.

## Intentional Deviations

- CICS and IMS lookup/update routes become one customer resource.
- Unsupported web deletion becomes a real authorized retirement workflow.
- The unavailable credit agency is represented by a replaceable port and a
  labelled deterministic demo implementation, not a claimed integration.

## Success Criteria

- Unit tests cover validation, normalization, eligibility, and credit outcomes.
- SQL Server/API tests cover search, CRUD, concurrency, relationships, rollback,
  role/ownership rules, and audit records.
- Angular tests and Playwright cover operator CRUD and customer self-view.
