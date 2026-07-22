# Tasks: Account Management and Balances

- [x] T001 Add unit tests for account type normalization, validation, balance
  representation, the ten-active-account customer limit, system-managed statement
  dates, generated account/default fields, configured bank sort code, exact
  interest/overdraft boundaries, metadata
  balance immutability, and lifecycle eligibility.
- [x] T002 Add categorized SQL Server tests for ownership, indexes, precision,
  configured sort code, complete paged portfolios, concurrency, closure, audit
  constraints, and atomic rollback of identifier allocation/account/audit
  creation.
- [x] T003 Add API tests for customer/operator access, list/detail, CRUD,
  empty/not-found, required product type validation, eleventh-active-account rejection, statement-date
  protection, Problem Details for list/detail/balance, safe closure, direct
  deep links/invalid parameters, and standard failures.
- [x] T004 Implement account domain, application use cases, repository queries,
  explicit legacy type mapping, balance read models, and the transactional
  identifier-allocation/account/audit creation boundary.
- [x] T005 Add separate EF configurations/constants and a versioned Account
  migration without startup application.
- [x] T006 Implement authorized account endpoints and typed DTOs.
- [x] T007 Add Vitest tests and implement account list/details/forms/closure UI.
- [x] T008 Add tagged Playwright browse/create/update/close happy paths.
- [ ] T009 Run gates, update SDD/tasks/workbook, deploy this slice, and prepare
  Stage 9/10 evidence before Feature 004 starts.
