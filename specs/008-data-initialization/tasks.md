# Tasks: Explicit Data Initialization and Import

- [ ] T001 Define and document versioned import/demo-data formats and source-key
  conflict rules, including legacy transaction-run status and reset parameters.
- [ ] T002 Add categorized SQL Server migration tests from empty and previous
  supported schema states.
- [ ] T003 Add categorized import tests for dependency order, integrity,
  precision, idempotency, staging/promotion atomicity, resumable retry,
  transaction-run status, authorization, conflicts, rollback, and run audit;
  add negative reset tests for missing confirmation, unauthorized/non-demo
  environments, production-disabled defaults, deterministic parameters, and
  denial to API-runtime credentials.
- [ ] T004 Add a test proving normal API startup leaves an empty database empty.
- [ ] T005 Implement the explicit import CLI, validators, run ledger, and
  staging/promotion boundary, versioned deterministic demo package, and guarded
  start/end/step/seed reset command.
- [ ] T006 Build and document EF migration bundle/apply/verify commands.
- [ ] T007 Add Compose operator profiles/commands without automatic migration or
  seeding dependencies on API startup.
- [ ] T008 Run gates, update SDD/tasks/workbook, deploy this slice, and prepare
  Stage 9/10 evidence before Feature 009 starts.
