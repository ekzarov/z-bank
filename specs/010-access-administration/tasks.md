# Tasks: Access Administration

## SDD And Contracts

- [x] T001 Record the target-only owner decision, target-surface planned
  actions, reverse traceability, and Stage 6 review evidence.
- [ ] T002 Define safe user, mutation, paging, and security-audit contracts plus
  invariant/unit-test cases.

## Backend Tests First

- [ ] T003 Add categorized SQL Server integration tests for Administrator-only
  access, user paging/search/detail, uniqueness, and secret exclusion.
- [ ] T004 Add integration tests for create/role/lock/unlock invariants,
  self-protection, last-admin protection, stale versions, session revocation,
  atomic audit writes, and rollback.
- [ ] T005 Add integration tests for immutable newest-first security-audit
  paging/filtering and persistent login/logout/admin events.

## Backend Implementation

- [ ] T006 Add the append-only security-audit model, mapping/constants, shared
  asynchronous audit port, and versioned EF Core migration.
- [ ] T007 Implement access-administration application contracts/service and
  Identity/SQL repository behavior.
- [ ] T008 Add Administrator-policy API endpoints, Problem Details mappings,
  antiforgery-safe mutations, and security-stamp session revocation.

## Frontend Tests First

- [ ] T009 Add Angular unit tests for administration API calls, user/audit
  paging, forms, confirmations, loading/empty/error/conflict states, and secret
  clearing.
- [ ] T010 Add tagged Playwright coverage for an Administrator useful action
  and resulting audit event using `@surface:administration`
  `@role:Administrator`.

## Frontend And Acceptance

- [ ] T011 Implement the Users and Security audit administration workspace and
  responsive styles, replacing the placeholder component.
- [ ] T012 Run build/unit/integration/Playwright/SDD/workbook/target-surface
  gates, explicitly apply the migration, deploy the slice, and complete Stage
  9/10 evidence before marking the feature accepted.
