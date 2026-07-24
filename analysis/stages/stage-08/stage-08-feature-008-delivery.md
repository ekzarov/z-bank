# Stage 8 Delivery - Feature 008

## Scope

- Feature: `008-data-initialization`
- Date: 2026-07-23
- Delivered revision: `965a850`
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

The peer-reviewed Feature 008 revision was deployed without changing the
legacy Bank of Z or XPlanner applications. Existing modern users, credentials,
and banking data were preserved. No demo reset or import was run.

The server environment received distinct generated SQL credentials for the API
and setup operator. Secrets remained in the server-side `.env` file and were
not printed or committed. Deployment used only explicit operator commands:

```text
docker compose --profile tools build setup api ui
docker compose --profile tools run --rm setup provision-access
docker compose --profile tools run --rm setup inspect-migrations
docker compose --profile tools run --rm setup migrate --environment Demo
docker compose up -d --force-recreate api ui
```

`inspect-migrations` reported only
`20260723140526_AddDataInitialization` as pending. The following `migrate`
command applied it and wrote a successful setup-operation audit. Normal API
startup still runs neither migrations, imports, nor demo provisioning.

## Smoke Result

- Remote SQL Server and API: healthy.
- Remote Angular/nginx UI: running.
- Public landing, legacy, and modern HTTPS routes: `200`.
- Customer, operator, and administrator authentication: passed.
- Existing customer, account, cash, transfer, history, and statement flows:
  passed.
- Public HTTPS Playwright after correction: 14 passed, 0 failed.

Credentials were loaded from the server environment and were not written to
the repository or test output.
