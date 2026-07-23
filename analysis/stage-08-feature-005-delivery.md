# Stage 8 Delivery - Feature 005

## Scope

- Feature: `005-funds-transfers`
- Date: 2026-07-23
- Delivered revision: `16048d3`
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

The peer-reviewed Feature 005 revision was deployed without changing the
legacy Bank of Z or XPlanner applications. The SQL schema and demo identities
were updated only through explicit setup commands:

```text
docker compose --profile tools build setup
docker compose --profile tools run --rm setup migrate
docker compose --profile tools run --rm setup provision-demo
docker compose up -d --build api ui
```

`AddInternalTransfers` added the nullable transfer-correlation column and its
index. Normal API startup still runs neither migrations nor provisioning.

## Smoke Result

- Remote SQL Server and API: healthy.
- Remote Angular/nginx UI: running.
- Public landing, legacy, and modern HTTPS routes: reachable.
- Customer authentication and owned-account navigation: passed.
- Customer transfer between two owned demo accounts: passed.
- Customer insufficient-funds rejection: passed.
- Operator transfer between managed accounts: passed.
- Rejected operator transfer preserved the displayed balance: passed.
- Customer, operator, and administrator navigation authorization: passed.
- Public HTTPS Playwright: 9 passed, 0 failed.

Credentials were loaded from the server environment and were not written to
the repository or test output.
