# Feature 002: Customer Management

## Traceability

- Workbook rows: 22-39, 87, 90, 97
- Owner decisions: D-008, D-009, D-010, D-018, D-023
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
  name; customers SHALL retrieve only their associated profile. Name search is
  a target capability replacing the unsupported legacy placeholder (D-018).
- **FR-003** A missing customer SHALL return `404` without leaking unrelated
  customer information. The UI SHALL clear stale selection/details, leave the
  loading state, and disable update/retirement actions after a failed lookup.
- **FR-004** Create/update SHALL validate required identity/contact fields,
  supported titles, first/last name, address line 1, numeric/date-of-birth
  formats, lengths, and business ranges at the API and domain boundaries.
- **FR-005** Customer creation SHALL call an `ICreditAssessmentProvider` port.
  The application SHALL request five configured assessments, average all
  successful scores, derive the review date no later than 21 days after
  creation, and reject total provider failure before persistence. The demo
  implementation SHALL be deterministic and clearly identified as simulated.
- **FR-006** Create/update SHALL use optimistic concurrency and return a
  conflict for stale writes.
- **FR-007** Customer retirement SHALL be rejected while active accounts or
  unresolved financial obligations exist; successful retirement SHALL be a
  soft state transition, not destructive history deletion.
- **FR-008** Every successfully persisted mutation SHALL record actor,
  timestamp, action, entity ID, result, and correlation ID without sensitive
  field values in the same transaction. Rejected commands are not mutations
  and SHALL leave neither domain changes nor success-audit records.
- **FR-009** The model SHALL retain `SourceSystem` (`Cics`, `Ims`, `Modern`) and
  optional source identifier as provenance, not as routing behavior.
- **FR-010** Supported REST resources SHALL be documented from the target
  contract; missing generated legacy response files SHALL not be reproduced.
- **FR-011** A successful create SHALL return the immutable customer ID and
  system-managed sort code; successful create/update UI flows SHALL show the
  result and navigate or redisplay the current customer deterministically.
- **FR-012** Identifier allocation and customer persistence SHALL be atomic;
  failures SHALL create neither a customer nor a reusable contract promise
  about gap-free identifiers.
- **FR-013** Customer detail MAY compose read-only account summaries only
  through Feature 003 and MAY link to Feature 004 actions; account/balance/
  deposit semantics remain owned and tested by those slices.

## Intentional Deviations

- CICS and IMS lookup/update routes become one customer resource.
- Unsupported web deletion becomes a real authorized retirement workflow.
- Unsupported name search becomes normalized target search, and stale legacy
  lookup state is corrected (D-018).
- The unavailable credit agency is represented by a replaceable port and a
  labelled deterministic demo implementation, not a claimed integration.

## Success Criteria

- Unit tests cover exact field boundaries, normalization, eligibility, five
  provider aggregation, partial/total provider failure, and review dates.
- SQL Server/API tests cover search, CRUD, concurrency, relationships, rollback,
  role/ownership rules, and audit records.
- Angular tests and Playwright cover operator CRUD, stale-state clearing,
  success navigation/redisplay, the explicit pre-Feature-003 profile boundary,
  and self-view. Composed account summaries remain a Feature 003 criterion.
