# Modern Bank of Z

The modern system is delivered as an Angular 22 UI, a .NET 10 API, and SQL
Server. Normal application startup never creates schema, roles, or users.

Set the two values from `.env.example`, then run:

```powershell
docker compose up -d db
docker compose --profile tools build setup
docker compose --profile tools run --rm setup migrate
docker compose --profile tools run --rm setup provision-demo
docker compose up -d --build api ui
```

Open `http://localhost:8088/z-bank-new/`. Demo users are `customer`, `operator`, and
`administrator`; all use the password supplied as `BANKOFZ_DEMO_PASSWORD`.
