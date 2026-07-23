# Tasks: Funds Transfers

- [x] T001 Add unit/domain tests for transfer validation, inactive/
  closed source and destination accounts, product, currency, two-decimal
  precision, explicit CURRENT/SAVING/ISA eligibility and LOAN/MORTGAGE
  rejection, system-managed sort codes, funds, and idempotency rules.
  Authorization and non-disclosure are covered by T003.
- [x] T002 Add categorized SQL Server tests for active-account eligibility,
  atomic paired mutation/history, rollback, deterministic locking/concurrency,
  `SourceSystem=Modern`, and retry behavior.
- [x] T003 Add API tests for customer/operator transfer permissions, results,
  stable failures, and non-disclosure.
- [x] T004 Implement the transfer command and paired transaction/audit model.
- [x] T005 Implement the internal-transfer endpoint and typed DTOs.
- [x] T006 Add Vitest tests and implement customer/operator transfer UI.
- [x] T007 Add tagged Playwright successful and rejected transfer paths.
- [x] T008 Run gates, update SDD/tasks/workbook, deploy this slice, and prepare
  Stage 9/10 evidence before Feature 006 starts.
