# Stage 8 Delivery - Feature 009

## Scope

- Feature: `009-delivery-operations`
- Date: 2026-07-23
- Delivered revision: `7872384`
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

The two-round peer-reviewed Feature 009 revision was merged and deployed without
resetting or importing demo data. Protected SQL and demo credentials remained
only in the server-side `.env` file. Deployment followed the explicit operator
path:

```text
docker compose --profile tools build setup api ui
docker compose --profile tools run --rm setup inspect-migrations
docker compose --profile tools run --rm setup migrate --environment Demo
docker compose up -d db api ui
```

The SQL container was recreated on the new internal network with its existing
persistent volume. `inspect-migrations` reported all eight migrations applied
and none pending. The explicit migrate command applied nothing and wrote a
successful operator audit. API startup still performs no schema or data
mutation.

## Smoke Result

- SQL Server, API, and unprivileged nginx UI: healthy.
- API and SQL: internal-only; UI exposed on loopback to the host proxy.
- Customer, operator, and administrator role smoke: passed.
- Public runtime identity, liveness/readiness, and security headers: passed.
- Public HTTPS Playwright: 15 passed, 0 failed.
- Existing banking data and credentials remained usable.

Credentials were loaded in process memory from the protected server environment
and were not written to repository evidence or test output.
