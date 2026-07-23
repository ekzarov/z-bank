# Stage 8 Delivery - Feature 007

## Scope

- Feature: `007-monthly-statements`
- Date: 2026-07-23
- Delivered revision: `462e93b`
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

The peer-reviewed Feature 007 revision was deployed without changing the
legacy Bank of Z or XPlanner applications. The SQL schema was updated only
through the explicit setup command:

```text
docker compose --profile tools build setup api ui
docker compose --profile tools run --rm setup migrate
docker compose up -d --force-recreate api ui
```

`AddMonthlyStatements` adds immutable statement, transaction-snapshot, and
audit tables. Normal API startup still runs neither migrations nor demo
provisioning.

## Smoke Result

- Remote SQL Server and API: healthy.
- Remote Angular/nginx UI: running.
- Public landing, legacy, and modern HTTPS routes: `200`.
- Customer populated statement generation, detail, and print view: passed.
- Operator bulk outcomes and failed-only retry UI: passed.
- Existing authentication, customer, account, cash, transfer, and transaction
  history paths: passed.
- Public HTTPS Playwright: 13 passed, 0 failed.

Credentials were loaded from the server environment and were not written to
the repository or test output.
