# Plan: Access Administration

## Technical Context

- Backend: .NET 10 ASP.NET Core Web API, ASP.NET Core Identity, EF Core 10, and
  SQL Server.
- Frontend: Angular 22 standalone components with same-origin cookie/CSRF
  requests.
- Tests: xUnit unit tests, categorized real-SQL-Server integration tests,
  Angular Vitest, and Playwright.

## Backend Design

Add access-administration contracts and an application port whose
Infrastructure implementation coordinates `UserManager<ApplicationUser>`,
Identity roles, customer validation, optimistic concurrency, and the shared
EF Core transaction. User lists use deterministic normalized ordering and
bounded paging. Mutations preserve exactly one governed role and protect the
current and last unlocked Administrator.

Replace logger-only security auditing with an asynchronous application audit
port and append-only EF Core `SecurityAuditRecord`. Session login/logout and
administration mutations use the same writer. Administration mutations and
their success audit share an explicit SQL transaction; rejected attempts emit
bounded failure events without secret values. Security-stamp validation and
stamp updates revoke changed-user sessions. V1 sets the Identity security-stamp
validation interval to zero so the next protected request revalidates the
changed user immediately.

Add a versioned migration only. Neither API startup nor frontend startup runs
migrations or provisions identities.

## API Shape

- `GET /api/administration/users`
- `POST /api/administration/users`
- `GET /api/administration/users/{id}`
- `PUT /api/administration/users/{id}/role`
- `PUT /api/administration/users/{id}/lockout`
- `GET /api/administration/security-audit`

All endpoints require the Administrator policy, use Problem Details, bounded
request models, and opaque versions. Mutation responses return the updated
safe user view.

## Frontend Design

Replace `RoleWorkspaceComponent` with a dedicated standalone administration
workspace. Use tabs for Users and Security audit. Users includes search,
bounded paging, selection, creation, governed role assignment, and lock/unlock
commands. Security audit includes compact filters and a scan-friendly table.
Confirmation is required for lock and role changes. No password is retained
after submission.

The route and navigation remain `/administration`/Administrator. The target
surface inventory changes from `gap` to `implemented` only with exact
role-bound test evidence.

## Verification

- Unit-test invariant decisions independently of Identity/SQL where practical.
- Run all administration API integration tests against the categorized SQL
  Server fixture, including real FK/unique/concurrency/transaction behavior.
- Test Angular service/component states with Vitest.
- Run Administrator Playwright against the deployed Compose topology and
  verify a resulting audit event.
- Run all existing authentication, target-surface, SDD, workbook, build, and
  deployment gates.

## Risks

- Identity mutations span multiple framework calls; explicit transaction and
  concurrency tests are mandatory.
- Immediate session revocation performs an Identity lookup on protected
  requests. Measure this explicit v1 security tradeoff before production scale
  and only relax it through a requirement change that defines an accepted
  revocation bound.
- Audit browsing can expose operational metadata; DTOs and filters remain
  bounded and administrator-only.
