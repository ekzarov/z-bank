# Bank of Z demo deployment

This deployment builds and starts the components shipped by the upstream
repository:

- a one-shot Gradle builder that creates and stages `api.war`;
- the static/Node frontend on loopback port `3002`;
- IBM z/OS Connect Designer on loopback ports `9082` and `9444`.

Start from the repository root:

```bash
docker compose -p zbank -f deploy/docker-compose.server.yml up -d
```

The first run downloads the large IBM Designer image and can take several
minutes. `zbank-api-builder` must finish with exit code 0 before z/OS Connect
starts. The generated WAR and Gradle caches live in named volumes, so normal
container recreation does not require manual copying into Liberty `dropins`.

The public reverse proxy serves the UI at `/z-bank/`. The deployment-specific
`config.js` keeps browser API traffic under that HTTPS prefix and lets the Node
server proxy it to z/OS Connect without CORS or mixed-content errors.

## Required for real banking operations

The IBM repository does not include CICS, IMS, or DB2 runtimes. Set the following
in a server-side `.env` before treating banking operations as functional:

```text
CICS_USER=
CICS_PASSWORD=
CICS_HOST=
CICS_PORT=
IMS_USER=
IMS_PASSWORD=
IMS_HOST=
IMS_PORT=
IMS_DATASTORE=
```

Without those external IBM Z endpoints, the UI and z/OS Connect edge can run,
but customer/account operations are expected to fail. Do not replace them with
mocks when demonstrating the legacy baseline.

## Modern replacement

The replacement runs as a separate Compose project and does not modify the
legacy containers:

```bash
cd /opt/z-bank/modern
cp .env.example .env
# Replace both placeholder values in .env before continuing.
docker compose up -d db
docker compose --profile tools build setup
docker compose --profile tools run --rm setup migrate
docker compose --profile tools run --rm setup provision-demo
docker compose up -d --build api ui
```

Neither schema migration nor demo identity provisioning runs during normal API
startup. Repeat `setup migrate` explicitly after a deployment that introduces
a new migration. Build the `setup` image first so the command cannot reuse an
older image that lacks the new migration. The UI binds only to
`127.0.0.1:8088`; install the modern locations from
`deploy/nginx-z-bank.conf` and open `/z-bank-new/` through the public HTTPS
origin.

The Feature 002 migration introduces the Customer foreign key. It deliberately
clears pre-feature free-form `AspNetUsers.CustomerId` values before adding that
constraint because no matching Customer rows existed in Feature 001. For the
demo environment, always run `provision-demo` immediately after `migrate`; the
idempotent command creates customer `1000000001` and restores the demo user's
link. For non-demo data, migrate customer records and map existing identity
links explicitly instead of relying on demo provisioning.
