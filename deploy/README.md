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
