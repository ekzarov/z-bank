# Plan: Delivery, Resilience, and Operational Hardening

## Topology

Use a multi-stage .NET 10 API image, Angular build image plus unprivileged nginx
runtime, and a pinned supported SQL Server Linux image. Compose exposes only the
web entry point by default; API and SQL remain internal. Environment-specific
overrides provide host/domain/TLS termination configuration.

Migration is an explicit one-shot operator command/profile using the same
versioned artifact built in Feature 008. Compose service startup contains no
implicit migration dependency. Health checks are read-only.

## Operations and Security

Use structured JSON logs with correlation IDs and redaction. Define liveness,
readiness, graceful shutdown, resource expectations, persistent volume backup
notes, rollback procedure, and image/version inventory. Secret detection runs
in CI and local pre-commit guidance without downloading arbitrary editor tools.
Bank identity is target configuration. Legacy fixed-account diagnostics,
unused helper programs, and broken documentation tooling remain documented
do-not-port evidence. Financial failure injection verifies transaction rollback
and correlated diagnostics.

## Verification

Build all images, validate Compose, apply migration/import explicitly, start
services, run API/frontend health and Playwright smoke, restart, and verify data
persistence. Test unavailable SQL/API behavior. Keep IBM infrastructure blocker
and simulation labels visible in deployment documentation. Run a browser
geometry assertion against the production nginx image so CSP/style-loading
regressions cannot pass through functional-only smoke coverage.
