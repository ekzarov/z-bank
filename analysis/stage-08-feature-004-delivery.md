# Stage 8 Delivery - Feature 004

## Scope

- Feature: `004-cash-transactions`
- Date: 2026-07-22
- Delivered revision: `434b884`
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

Feature 004 was built and exercised locally before the same revision was
deployed to the shared server. The existing legacy Bank of Z and XPlanner
deployments were not changed.

The database migration remained an explicit setup operation:

```text
docker compose --profile tools build setup
docker compose --profile tools run --rm setup migrate
docker compose --profile tools run --rm setup provision-demo
docker compose up -d --build api ui
```

The `AddCashTransactions` migration visibly created the immutable transaction,
idempotency, audit, concurrency, precision, and relationship schema. Normal API
startup still performs neither migration nor demo provisioning. Frontend
`.dockerignore` rules reduced its image build context from roughly 246 MB to
about 4 KB without changing the production build.

## Smoke Result

- Local SQL Server, API, and Angular/nginx containers: healthy.
- Remote SQL Server, API, and Angular/nginx containers: healthy.
- Public HTTPS application: reachable at `/z-bank-new/`.
- Customer account creation and account-detail navigation: passed.
- Deposit `125.00` and returned balance/reference rendering: passed.
- Insufficient-funds withdrawal rejection with no mutation: passed.
- Successful withdrawal `125.00` and refreshed balance: passed.
- Existing account edit and close path: passed.
- Local Playwright: 7 passed, 0 failed.
- Public HTTPS Playwright: 7 passed, 0 failed.

Demo customer, operator, and administrator authentication was exercised. A
credential-discoverability usability issue was observed during the walkthrough:
the authentication itself succeeded, but the role/password handoff was
confusing. The secret is not written to repository documentation; a clear
secret-safe demo-persona handoff is assigned to Feature 009 operations.
