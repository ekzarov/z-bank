# Stage 8 Delivery - Feature 003

## Scope

- Feature: `003-account-management`
- Date: 2026-07-22
- Delivered revision: `d23d75f`
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

The server checkout was updated to the Feature 003 merge without changing the
legacy Bank of Z or XPlanner deployments. Database migration and demo
provisioning remained explicit setup operations and did not move into normal
API startup.

The deployed sequence was:

```text
docker compose --profile tools build setup
docker compose --profile tools run --rm setup migrate
docker compose --profile tools run --rm setup provision-demo
docker compose up -d --build api ui
```

The `AddAccountManagement` migration applied visibly. It created the account
and account-audit schema, sequence, customer relationship, rowversion, indexes,
precision declarations, and database constraints. Explicit demo provisioning
created account `10000000` for the existing demo customer.

## Smoke Result

- SQL Server and API containers: healthy.
- Angular UI container: running and publicly reachable over HTTPS.
- Existing Feature 001 and Feature 002 browser scenarios: passed.
- Operator account browse, direct detail, create, update, and close: passed.
- Customer access remained restricted to the customer's own portfolio.
- Public HTTPS Playwright: 7 passed, 0 failed.

Normal API startup still performs neither migration nor demo provisioning.
