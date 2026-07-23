# Stage 8 Delivery - Feature 006

## Scope

- Feature: `006-transaction-history`
- Date: 2026-07-23
- Delivered revision: `4afb465`
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

The peer-reviewed Feature 006 revision was deployed without changing the
legacy Bank of Z or XPlanner applications. The SQL schema was updated only
through the explicit setup command:

```text
docker compose --profile tools build setup api ui
docker compose --profile tools run --rm setup migrate
docker compose up -d --force-recreate api ui
```

`AddTransactionSourceIdentifier` adds nullable durable provenance to immutable
booked transactions. Normal API startup still runs neither migrations nor demo
provisioning.

## Smoke Result

- Remote SQL Server and API: healthy.
- Remote Angular/nginx UI: running.
- Public landing, legacy, and modern HTTPS routes: `200`.
- Customer history list, UTC date filter, and detail navigation: passed.
- Operator empty and populated history: passed.
- Customer access to the operator-created foreign account: non-disclosing
  `404` path passed.
- Existing authentication, customer management, account, cash, and transfer
  paths: passed.
- Public HTTPS Playwright: 11 passed, 0 failed.

Credentials were loaded from the server environment and were not written to
the repository or test output.
