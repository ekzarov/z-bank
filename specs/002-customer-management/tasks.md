# Tasks: Customer Management

- [ ] T001 Add customer domain/value-object unit tests for validation,
  normalization, exact required fields/date/title boundaries, status
  transitions, five-provider aggregation, partial/total failure, and review date.
- [ ] T002 Add categorized SQL Server tests for schema constraints, search,
  concurrency, retirement relationships, rollback, provenance, and audit.
- [ ] T003 Add API tests for operator CRUD, customer self-view, authorization,
  validation, returned ID/sort code, not-found, conflict, atomic allocation,
  composed read-only account summaries, and standard failures.
- [ ] T004 Implement the Customer aggregate, application use cases, repository
  port, credit-assessment port, and audit contract.
- [ ] T005 Add EF entities/configurations/constants and a versioned Customer
  migration without startup application.
- [ ] T006 Implement target customer endpoints and deterministic simulated
  credit adapter.
- [ ] T007 Add Vitest tests for search/details/forms/error and permission states,
  including stale-state clearing, loading completion, disabled mutations, and
  create/update navigation or redisplay.
- [ ] T008 Implement operator customer workspace and customer self-profile.
- [ ] T009 Add tagged Playwright customer create/find/update/retire happy path.
- [ ] T010 Run gates, update SDD/tasks/workbook, deploy this slice, and prepare
  Stage 9/10 evidence before Feature 003 starts.
