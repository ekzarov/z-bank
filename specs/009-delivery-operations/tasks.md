# Tasks: Delivery, Resilience, and Operational Hardening

- [ ] T001 Add read-only liveness/readiness tests and structured-log redaction
  tests, plus pre-implementation tests for startup without migration, secret
  injection, internal-only networking, non-root runtime, request limits/security
  headers, resource bounds, graceful shutdown, and financial rollback logging.
- [ ] T002 Implement separate API liveness/readiness endpoints, validated bank
  identity options and runtime display, structured correlated/redacted logging,
  graceful shutdown, and atomic financial-failure diagnostics.
- [ ] T003 Add pinned multi-stage API and Angular/nginx container builds with
  non-root runtime configuration where supported.
- [ ] T004 Add Compose services, persistent storage, internal networking,
  health checks, resource guidance, and explicit migration/import profiles.
- [ ] T005 Add nginx same-origin routing, SPA fallback, security headers, and
  HTTPS-forwarding configuration.
- [ ] T006 Add secret scan, dependency/build/test/image/Compose validation gates.
- [ ] T007 Add deployment, migration, import, backup, rollback, and troubleshooting
  runbooks that preserve the IBM-runtime residual-risk statement and explicitly
  dispose unused helpers, fixed-account diagnostics, and broken TOC tooling.
- [ ] T008 Add tagged Playwright deployed-system smoke and restart/persistence
  verification.
- [ ] T009 Run slice Stage 9/10 evidence, then run the final consolidated Stage
  10 acceptance only after all prior slices are accepted.
