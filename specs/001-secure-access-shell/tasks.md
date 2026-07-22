# Tasks: Secure Access and Application Shell

## Setup

- [ ] T001 Create the .NET 10 solution and clean backend project boundaries.
- [ ] T002 Create the Angular 22 standalone application with proxy, Vitest, and
  a verified empty application page.
- [ ] T003 Add central package/version settings and CI commands for build and
  test without introducing application behavior.

## Backend Tests First

- [ ] T004 Add unit tests for role and customer-ownership authorization rules.
- [ ] T005 Add categorized SQL Server integration fixture and database reset.
- [ ] T006 Add API integration tests for login, lockout, session, CSRF, logout,
  anonymous access, wrong roles, and Problem Details.

## Backend Implementation

- [ ] T007 Add Identity/role persistence using versioned EF migrations only.
- [ ] T008 Implement session endpoints, cookie/antiforgery configuration, and
  role/ownership policies.
- [ ] T009 Add structured security audit events with sensitive-data filtering.

## Frontend Tests First

- [ ] T010 Add Vitest tests for session state, route guards, role-filtered
  navigation, channel-neutral customer IDs, not-found, and unavailable states.
- [ ] T011 Add Playwright sign-in/protected-route/sign-out happy path tagged
  `@e2e`.

## Frontend and Delivery

- [ ] T012 Implement the accessible application shell, sign-in, logout,
  navigation, and error routes.
- [ ] T013 Add nginx same-origin routing and a slice-specific Compose smoke.
- [ ] T014 Run all gates, update SDD/tasks/workbook destination evidence, deploy
  the slice, and prepare Stage 9/10 evidence.
