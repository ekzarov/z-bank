# Stage 8 Delivery - Feature 002

## Scope

- Feature: `002-customer-management`
- Date: 2026-07-22
- Delivered revision: `52869a4`
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

The server checkout was updated to the Feature 002 merge without changing the
legacy Bank of Z or XPlanner deployments. Database operations remained explicit
and outside normal API startup.

The first migration attempt exposed that Compose can reuse an older `setup`
image when source has changed. That attempt correctly reported no pending
migrations and was not treated as success. The corrected sequence was:

```text
docker compose --profile tools build setup
docker compose --profile tools run --rm setup migrate
docker compose --profile tools run --rm setup provision-demo
docker compose up -d --build api ui
```

The `AddCustomerManagement` migration then applied visibly, creating the
Customer sequence, tables, indexes, constraints, rowversion, and identity FK.
`provision-demo` created customer `1000000001` and restored the customer user's
link before the API was restarted. Both deployment runbooks now require the
setup-image build first.

## Smoke Result

- SQL Server, API, and UI containers: healthy/running.
- Public modern UI: HTTP 200.
- Existing Feature 001 access/security/outage flows: passed.
- Customer self-profile and role-filtered navigation: passed.
- Operator create, exact-ID find, update, and retirement: passed.
- Public HTTPS Playwright: 6 passed, 0 failed.

The first E2E run found three stale heading assertions left from Feature 001
placeholder pages. Navigation and the real pages worked; the tests were aligned
to the delivered `My profile` and `Customer workspace` headings and all six
scenarios then passed.

Normal API startup still performs neither migration nor demo provisioning.
