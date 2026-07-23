# Stage 9 Finding - Administrator Surface Has No Useful Action

## Observation

The project owner signed in as `administrator`, opened `/administration`, and
observed a generic workspace containing:

> This workspace is ready for the next migration slice.

The route is publicly reachable after authentication and appears in production
navigation, but it exposes no useful administrative action.

## Root Cause

Legacy `frontend/admin.html` was not a separately authenticated administrator
role. It was an unauthenticated staff control panel linking to customer,
account, and deposit workflows. Those business actions were migrated to the
target `Operator` role across Features 002-007.

Feature 001 introduced a new target `Administrator` role for secure role
separation and stated that operational access would arrive in later slices.
The same spec explicitly deferred user-administration UI. No later slice
defined or implemented an Administrator use case, but the placeholder route,
navigation item, provisioned persona, heading-only E2E assertion, and access
probe remained.

Workbook row 12 was marked implemented from application-shell/navigation
evidence even though its observable outcome enumerates create/delete/update
customer, create/delete/update account, and deposit actions. Those actions are
implemented under `Operator`, but the row-level evidence did not make that
role mapping explicit and did not justify the new actionless Administrator
surface.

## Process Failure

- Route/access/heading evidence was mistaken for useful-function evidence.
- One bundled workbook outcome was closed without action-level target evidence.
- No governed inventory reconciled routes, navigation, roles, useful actions,
  SDD, code, and tests.
- Stage 9 and Stage 10 role smoke opened the Administrator page but did not
  require a meaningful action.
- Independent reviewers checked compliance with the declared SDD; the SDD
  itself allowed a future-slice placeholder and therefore encoded the gap as
  expected behavior.

## New Guardrails

- Constitution Principle XIII requires every shipped surface to be useful.
- `analysis/target-surface-inventory.json` records every Angular route and
  role-visible destination with useful-action evidence.
- `npm --prefix analysis/tools run audit:target` rejects visible gaps,
  actionless roles, missing inventory entries, and placeholder markers.
- Stage 9 now clicks every navigation item for every role and completes the
  listed useful action.
- Stage 10 receives the target-surface inventory; login, `200 OK`, route guard,
  access probe, and heading-only evidence cannot close a surface.

The new audit fails correctly on the current target with four violations.

## Owner Decision

On 2026-07-23 the project owner selected **target-only modernization**.
Feature 010 defines user, role, lockout, and security-audit management and must
traverse Stages 5-10. Until that slice is accepted, the migration is not 100%
complete.
