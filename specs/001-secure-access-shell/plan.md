# Plan: Secure Access and Application Shell

## Technical Context

- Backend: .NET 10 LTS ASP.NET Core Web API.
- Persistence: EF Core 10 SQL Server provider and ASP.NET Core Identity tables.
- Frontend: Angular 22 standalone, zoneless application using Angular's
  application builder.
- Tests: xUnit, `WebApplicationFactory`, categorized SQL Server integration
  tests, Angular Vitest, and Playwright.

## Structure

Create `modern/BankOfZ.slnx` with `backend/src/BankOfZ.Api`,
`BankOfZ.Application`, `BankOfZ.Domain`, `BankOfZ.Infrastructure`, and focused
unit/integration test projects. Create the Angular workspace at
`modern/frontend/bank-of-z-ui`. Public types remain in separate files and
dependency direction is API -> Application -> Domain, with Infrastructure
implementing application ports.

## Design

ASP.NET Core Identity owns users, password hashing, lockout, roles, and cookie
sessions. A user-to-customer association is nullable and mandatory for the
Customer role. Policies enforce customer ownership and staff capabilities.
Angular obtains a small session DTO and never reads cookie contents. Unsafe API
requests use antiforgery tokens. API errors use Problem Details.

Production nginx serves the Angular build and forwards `/api` to the API, so
browser sessions remain same-origin. No CORS policy is needed for the intended
topology. Development uses an Angular proxy configuration.

Schema creation, Identity role creation, and demo accounts are migrations or
explicit operator commands from Feature 008. The application calls neither
`Migrate` nor `EnsureCreated` and performs no startup seeding.

## Verification

- Unit-test policy/ownership decisions without infrastructure.
- Run authentication and authorization integration tests against the test host
  and categorized SQL Server database.
- Test Angular guards, navigation, and error presentation with Vitest.
- Run a Playwright happy path against Compose for sign-in and sign-out.

## Risks

- Cookie/CSRF integration must be tested through the real browser path.
- No real identity federation exists; do not imply OAuth conformance.
- Angular 22 requires a compatible Node runtime; implementation pins Node 24.
