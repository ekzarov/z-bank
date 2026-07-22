# Feature 009: Delivery, Resilience, and Operational Hardening

## Traceability

- Workbook rows: 128-153
- Owner decisions: D-006, D-014, D-015, D-022
- Depends on: Features 001-008

## Goal

Deliver and operate the modern application through Docker Compose with secure
configuration, health/diagnostic evidence, and repeatable smoke tests while
explicitly not claiming IBM mainframe runtime parity.

## User Stories

### US1 - Deploy the modern system (P1)

An operator can build pinned images, apply migrations explicitly, start SQL
Server/API/frontend, and verify health without modifying the legacy snapshot.

### US2 - Diagnose and recover (P1)

An operator can distinguish web, API, database, configuration, and dependency
failures through health checks and structured logs without exposing secrets.

## Functional Requirements

- **FR-001** Compose SHALL define separate pinned frontend, API, and SQL Server
  services, persistent database storage, internal networking, and bounded
  health/readiness checks.
- **FR-002** API startup SHALL wait only through orchestration readiness and
  SHALL NOT apply migrations or seed data.
- **FR-003** Deployment documentation SHALL require an explicit migration step
  before API rollout and an optional explicit demo/import step.
- **FR-004** Production configuration SHALL use environment/secret injection;
  repository configuration SHALL contain no usable passwords, certificates,
  tokens, or trust-all TLS settings.
- **FR-005** nginx SHALL provide same-origin SPA fallback and `/api` proxying,
  security headers, request limits, and HTTPS-forwarding readiness.
- **FR-006** The API SHALL expose liveness and readiness separately; readiness
  SHALL verify required SQL connectivity without mutating data.
- **FR-007** Logs SHALL be structured, correlated, privacy-aware, and sufficient
  to diagnose failed requests and operator commands.
- **FR-008** Container processes SHALL run with least practical privilege,
  bounded resources, graceful shutdown, and restart policies suitable for demo.
- **FR-009** A smoke command SHALL verify frontend, authentication, API, SQL
  readiness, and one read-only business path after explicit provisioning.
- **FR-010** CICS/IMS transaction deployment, Wazi/DBB, Zowe, zOpenDebug,
  Liberty WAR, 3270, JCL, IBM MQ documentation-only claims, and unsecured IBM
  management settings SHALL NOT be implemented in the target.
- **FR-011** The repository MAY continue to host the Stage 3 simulator and
  original legacy frontend for evidence, but deployment SHALL label them as
  simulated/static and separate them from the modern target.
- **FR-012** The unavailable authorized IBM runtime SHALL remain an open
  residual-risk item; target acceptance SHALL not relabel simulated rows as
  live-observed.
- **FR-013** Bank display name and sort code SHALL come from validated target
  configuration. Unused legacy helper programs, the fixed-account history
  diagnostic, and the broken TOC generator SHALL NOT be ported (D-022).
- **FR-014** Unhandled infrastructure failures during financial commands SHALL
  roll back domain and audit writes atomically and emit correlated diagnostics
  without sensitive data (D-006).

## Success Criteria

- A clean environment can execute explicit migration/import, start Compose,
  pass health and smoke checks, and restart without data loss.
- Secret scanning and automated container/configuration tests cover startup
  without migration, secret injection, network exposure, request limits,
  security headers, least privilege, resource bounds, and graceful shutdown.
- Playwright runs the consolidated critical happy path against deployed services.
