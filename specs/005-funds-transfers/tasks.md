# Tasks: Funds Transfers

- [ ] T001 Add unit tests for transfer validation, authorization, inactive/
  closed source and destination accounts, product, currency, two-decimal
  precision, system-managed sort codes, funds, and idempotency rules.
- [ ] T002 Add categorized SQL Server tests for active-account eligibility,
  atomic paired mutation/history, rollback, deterministic locking/concurrency,
  `SourceSystem=Modern`, and retry behavior.
- [ ] T003 Add API tests for customer/operator transfer permissions, results,
  stable failures, and non-disclosure.
- [ ] T004 Implement the transfer command and paired transaction/audit model.
- [ ] T005 Implement the internal-transfer endpoint and typed DTOs.
- [ ] T006 Add Vitest tests and implement customer/operator transfer UI.
- [ ] T007 Add tagged Playwright successful and rejected transfer paths.
- [ ] T008 Run gates, update SDD/tasks/workbook, deploy this slice, and prepare
  Stage 9/10 evidence before Feature 006 starts.
