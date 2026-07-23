# Stage 9 Live Revision - Feature 009

## Scope

- Feature: `009-delivery-operations`
- Workbook rows: 128-153
- Date: 2026-07-23
- Result: clean

## Modern Walkthrough

The deployed target was exercised through the real HTTPS edge and the server
operator shell:

- the bank display name and sort code came from validated target configuration;
- liveness remained process-only while readiness checked SQL connectivity;
- normal startup did not invoke setup, migration, import, or reset;
- API and SQL had no public host ports and communicated on an internal network;
- API and UI ran as non-root processes with read-only filesystems, bounded
  resources, restart policies, health checks, and graceful stop settings;
- nginx served the `/z-bank-new/` SPA, proxied same-origin API and health
  routes, enforced request limits, and emitted the declared security headers;
- customer, operator, and administrator authentication and read paths passed;
- persistent banking data survived SQL-container recreation.

## Peer-Review Corrections

Claude Round 1 found a missing infrastructure-failure rollback/logging test, a
false-negative delivery regex, and secret-scan blind spots. All were accepted
and corrected. The new SQL integration test writes financial and audit changes
inside a serializable transaction, injects a failure, and proves rollback plus
safe correlated diagnostics.

Claude Round 2 independently confirmed those corrections and found one low
test-only flaky substring assertion. The marker was changed to a distinctive
financial amount, and the intended failure path passed again. No material
production finding remains.

## Gates

- Backend unit tests: 99 passed.
- Real SQL Server integration tests: 71 passed.
- Angular tests: 49 passed.
- Angular production build under Node 24.15.0: passed.
- Public HTTPS Playwright: 15 passed.
- Legacy simulator: 28 passed.
- Workbook audit: 135 closed, 0 open.
- SDD audit: 135 rows, 9 slices, 27 artifacts, 113 requirements.
- Legacy evidence audit: 512 references.
- Delivery audit: 11 controls.
- Secret scan: 342 tracked production/config files before the evidence-only
  commits; expanded scope includes workflows.
- Compose validation, server role smoke, and restart/persistence verification:
  passed.

The final slice proceeds to fresh read-only Stage 10 acceptance and the
consolidated nine-slice migration gate.
