# Modern Bank of Z

The modern system is delivered as an Angular 22 UI, a .NET 10 API, and SQL
Server. Normal API startup never creates or changes schema, data, roles, or
users.

Copy `.env.example` to `.env` and replace every placeholder with a distinct
secret. Database access is deliberately separated:

- `bankofz_api` can read and write application data;
- `bankofz_operator` can explicitly apply migrations and imports;
- `sa` is used only by the one-time access bootstrap command.

## Explicit demo setup

```powershell
docker compose up -d db
docker compose --profile tools build setup
docker compose --profile tools run --rm setup provision-access
docker compose --profile tools run --rm setup inspect-migrations
docker compose --profile tools run --rm setup migrate --environment Demo
docker compose --profile tools run --rm setup reset-demo --environment Demo --confirm RESET-BANK-OF-Z --start 2025-01-01 --end 2026-12-31 --step-days 7 --seed 42
docker compose up -d --build api ui
```

Open `http://localhost:8088/z-bank-new/`. Demo users are `customer`, `operator`,
and `administrator`; all use the operator-supplied `BANKOFZ_DEMO_PASSWORD`.

## Legacy import

The canonical package is UTF-8 JSON with `schemaVersion` equal to
`bank-of-z-import/v1`. Inspect `specs/008-data-initialization/spec.md` for
conflict and audit semantics.

```powershell
docker compose --profile tools run --rm setup import --file /imports/package.json --environment Demo
docker compose --profile tools run --rm setup verify-import --fingerprint <sha256>
```

Mount the package read-only into the setup container for production-like use.
Neither `docker compose up` nor API startup invokes these commands.

## Migration bundle

Build the pinned, self-contained Linux migration bundle from the repository
root:

```powershell
./modern/scripts/build-migration-bundle.ps1
```

The ignored output is `modern/artifacts/efbundle`. Copy it to the target host,
make it executable, and invoke it explicitly with the operator connection:

```bash
chmod +x ./efbundle
./efbundle --connection "$BANKOFZ_OPERATOR_CONNECTION"
```

The bundle applies schema only. It does not import legacy data, reset demo data,
or provision application identities.
