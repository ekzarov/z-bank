# Tasks: Explicit Data Initialization and Import

- [x] T001 Define and document versioned import/demo-data formats and source-key
  conflict rules, including legacy transaction-run status and reset parameters.
- [x] T002 Add categorized SQL Server migration tests from empty and previous
  supported schema states.
- [x] T003 Add categorized import tests for dependency order, integrity,
  precision, idempotency, staging/promotion atomicity, resumable retry,
  transaction-run status, authorization, conflicts, rollback, concurrent/stale
  leases, immutable attempts, running-balance chains, and run audit;
  add negative reset tests for missing confirmation, unauthorized/non-demo
  environments, production-disabled defaults, deterministic parameters, and
  denial to API-runtime credentials; verify the v1 package-size boundary.
- [x] T004 Add a test proving normal API startup leaves an empty database empty.
- [x] T005 Implement the explicit import CLI, validators, run ledger, and
  staging/promotion boundary, versioned deterministic demo package, and guarded
  start/end/step/seed reset command.
- [x] T006 Build and document EF migration bundle/apply/verify commands.
- [x] T007 Add Compose operator profiles/commands without automatic migration or
  seeding dependencies on API startup.
- [ ] T008 Run gates, update SDD/tasks/workbook, deploy this slice, and prepare
  Stage 9/10 evidence before Feature 009 starts.
