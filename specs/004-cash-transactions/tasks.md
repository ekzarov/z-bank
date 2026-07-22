# Tasks: Deposits and Withdrawals

- [x] T001 Add unit tests for positive amounts, direction, account type,
  active/closed status, ownership, funds/overdraft, currency precision,
  system-managed sort code, Modern provenance, and idempotency.
- [x] T002 Add categorized SQL Server tests for atomic booking, rollback,
  audit/history, concurrency, precision, and idempotency constraints.
- [x] T003 Add API tests for customer/operator authorization, successful
  balances, stable errors, foreign/missing/inactive accounts, derived sort code,
  Modern provenance, and retries.
- [x] T004 Implement transaction domain records, cash policies, application
  commands, and transaction boundary.
- [x] T005 Add separate EF configurations/constants and a versioned transaction
  migration without startup application.
- [x] T006 Implement unified deposit/withdrawal endpoints and Problem Details.
- [x] T007 Add Vitest tests and implement deposit/withdrawal UI with accurate
  resulting balances and accessible error states.
- [x] T008 Add tagged Playwright cash-operation happy and insufficient-funds
  paths.
- [ ] T009 Run gates, update SDD/tasks/workbook, deploy this slice, and prepare
  Stage 9/10 evidence before Feature 005 starts.
