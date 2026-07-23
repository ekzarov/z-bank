# Bank of Z Operations

## Runtime Boundary

The modern demo is a .NET/Angular/SQL Server replacement. The legacy path is a
static/simulated evidence surface because no authorized IBM CICS, IMS, DB2,
3270, Wazi, or z/OS runtime is available. Deployment success does not remove
that residual risk.

## Explicit Deployment

1. Copy `.env.example` to `.env`, set distinct SQL credentials, set the demo
   secret, and restrict the file to the deployment operator.
2. Build: `docker compose --profile tools build setup api ui`.
3. Provision SQL principals: `docker compose --profile tools run --rm setup provision-access`.
4. Inspect migrations: `docker compose --profile tools run --rm setup inspect-migrations`.
5. Apply migrations: `docker compose --profile tools run --rm setup migrate --environment Demo`.
6. Run an import or reset only when explicitly required.
7. Start: `docker compose up -d db api ui`.
8. Verify: `docker compose ps` and `BANKOFZ_BASE_URL=http://localhost:8088 BANKOFZ_DEMO_PASSWORD=... ./scripts/smoke-demo.sh`.

API startup never applies schema or demo data.

## Demo Personas

The provisioned users are `customer`, `operator`, and `administrator`. They use
the operator-supplied `BANKOFZ_DEMO_PASSWORD` from the protected server `.env`
file or the team's approved secret channel. Never paste it into source, tickets,
review packets, or logs. Rotate it by changing the protected value and running
the explicit `reset-demo` command with owner approval.

## Backup, Rollback, and Recovery

- Back up the named SQL volume before a schema change using the organization's
  SQL Server backup policy; do not copy a live volume directory.
- Keep the previous image revision available. Application rollback means
  restoring those image tags and recreating API/UI.
- Never run EF `Down` against production data as an improvised rollback.
  Restore a verified database backup when a data/schema rollback is required.
- Readiness failure means SQL is unavailable or misconfigured. Liveness success
  with readiness failure distinguishes process health from dependency health.
- Use the response `X-Correlation-ID` to find the matching structured API log.
  Logs intentionally exclude request bodies, credentials, and query strings.

## Troubleshooting

1. `docker compose ps` for health and restart state.
2. `docker compose logs --tail 200 api` for correlated JSON diagnostics.
3. `docker compose --profile tools run --rm setup inspect-migrations`.
4. Confirm `.env` contains all documented keys without printing their values.
5. Run `node scripts/verify-delivery.mjs` and `./scripts/scan-secrets.sh`.

Unused legacy helper programs, the fixed-account history diagnostic, broken TOC
generator, trust-all TLS, unsecured management, IBM build/deploy tooling, and
IBM MQ documentation-only claims are intentionally not ported.
