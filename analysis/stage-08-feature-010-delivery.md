# Stage 8 Delivery - Feature 010

## Scope

- Feature: `010-access-administration`
- Date: 2026-07-23
- Delivered revision: `8e9ac55`
- Target stand: `https://legacy-transformation-demo.olsys.dev/`
- Legacy path: `/z-bank/`
- Modern path: `/z-bank-new/`

## Deployment

Feature 010 was deployed without resetting or importing demo data. Protected
SQL and demo credentials remained only in the server-side `.env` file. The
operator path was explicit:

```text
docker compose --profile tools build setup api ui
docker compose --profile tools run --rm setup inspect-migrations
docker compose --profile tools run --rm setup migrate --environment Demo
docker compose up -d --force-recreate api ui
```

The pre-migration inspection reported `AddAccessAdministration` pending. The
explicit migration command applied it successfully. A second inspection
reported all nine migrations applied and none pending. API startup still
performs no schema or data mutation.

## Smoke Result

- SQL Server, API, and unprivileged nginx UI: healthy.
- API and SQL: internal-only; UI exposed on loopback to the host proxy.
- Customer, Operator, and Administrator role smoke: passed.
- Administrator user search, lockout action, and security-audit observation:
  passed.
- Public HTTPS Playwright: 18 passed, 0 failed.
- Desktop visual walkthrough: passed after correcting user-name/role spacing.
- Existing banking data and credentials remained usable.

Credentials were loaded in process memory from the protected server environment
and were not written to repository evidence or test output.
