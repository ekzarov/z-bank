# Feature 010: Access Administration

## Traceability

- Workbook rows: none; this is an explicitly approved target-only control.
- Owner decision: retain the target Administrator role and replace its visible
  placeholder with useful access-administration capabilities (2026-07-23).
- Depends on: Features 001, 008, and 009.
- Priority: P1 correction slice for the reopened target-surface gate.

## Goal

Give authorized administrators a useful, auditable workspace for managing
application identities, business roles, and lockouts without exposing secrets
or weakening the Customer and Operator separation.

## User Stories

### US1 - Find and inspect users (P1)

As an administrator, I can search and page through application users and see
their role, customer association, lockout state, failed-attempt count, and
version without seeing password or session material.

### US2 - Create an application user (P1)

As an administrator, I can create a Customer, Operator, or Administrator
identity with one valid business role and an initial password supplied through
the protected request body.

### US3 - Change role and lockout state safely (P1)

As an administrator, I can change another user's role and lock or unlock their
access while self-lockout, self-role removal, stale writes, and removal of the
last usable Administrator are rejected.

### US4 - Review security activity (P1)

As an administrator, I can page and filter immutable security events for login,
logout, user creation, role change, lock, and unlock outcomes without secrets
or sensitive payloads.

## Functional Requirements

- **FR-001** Every access-administration API and UI operation SHALL require the
  `Administrator` policy. Anonymous, Customer, and Operator callers SHALL be
  rejected by the API independently of Angular navigation.
- **FR-002** The API SHALL provide bounded, stable, deterministic user paging
  and normalized search by user name or email. The default and maximum page
  sizes SHALL be explicit.
- **FR-003** User views SHALL expose only identifier, user name, email, exactly
  one business role, optional customer association, lockout state/end, failed
  access count, and an opaque concurrency version.
- **FR-004** User creation SHALL enforce unique normalized user name and email,
  the configured Identity password policy, and exactly one role from
  `Customer`, `Operator`, or `Administrator`.
- **FR-005** A Customer identity SHALL reference an existing active customer.
  Operator and Administrator identities SHALL have no customer association.
- **FR-006** The API SHALL never return, log, persist outside Identity, or place
  in an audit event a submitted password, password hash, cookie, token, or
  antiforgery value.
- **FR-007** Role changes SHALL preserve exactly one business role and validate
  the customer-association invariant in FR-005.
- **FR-008** Lock SHALL set an effective lockout and revoke existing sessions;
  unlock SHALL clear lockout and failed-access state. The changed user's next
  protected request SHALL not continue under a stale authorization state.
- **FR-009** An administrator SHALL NOT lock itself or change its own business
  role through this feature.
- **FR-010** The system SHALL prevent any mutation that would leave no unlocked
  Administrator identity. Concurrent attempts SHALL be serialized or rejected
  safely.
- **FR-011** User mutations SHALL require the latest opaque version. Stale
  requests return `409 Conflict` and do not partially change identity or audit
  state.
- **FR-012** Users SHALL NOT be hard-deleted by this feature. Access removal is
  represented by lockout so identity and audit history remain referentially
  meaningful.
- **FR-013** Security events SHALL be persisted append-only with UTC timestamp,
  event name, actor identifier, optional subject identifier, success flag,
  bounded outcome code, and correlation identifier.
- **FR-014** Login and logout outcomes from Feature 001 and every successful or
  rejected administration mutation SHALL write a security event. Audit-write
  failure during an administration mutation SHALL fail and roll back that
  mutation.
- **FR-015** Audit query SHALL return newest-first bounded pages and support
  optional event-name, actor/subject, success, and UTC date-range filters.
- **FR-016** The Angular `/administration` workspace SHALL provide Users and
  Security audit views with explicit loading, empty, validation, conflict,
  forbidden, and failure states. Destructive access changes require clear
  confirmation.
- **FR-017** Normal application startup SHALL NOT create schema, roles, users,
  or audit rows. Schema changes use a versioned EF Core migration; demo identity
  changes remain explicit setup commands.
- **FR-018** API validation and authorization failures SHALL use Problem Details
  without leaking account existence beyond the authenticated Administrator
  workspace or exposing internal exception details.

## Intentional Target-Only Scope

Legacy `frontend/admin.html` was an unauthenticated staff control panel, not an
identity-administration system. Its banking operations are already mapped to
the target Operator. This feature is an owner-approved modernization control
and does not claim legacy parity evidence.

## Out Of Scope

- Password reset, recovery, forced password change, MFA, and federation.
- Custom permission or role-definition editing beyond the three governed roles.
- User deletion, audit deletion/editing, session browsing, and impersonation.
- Customer-record creation; Customer identities link to existing customers.

## Success Criteria

- SQL Server integration tests prove authorization, paging/search, role and
  customer invariants, lock/unlock/session revocation, last-admin protection,
  optimistic concurrency, audit filtering, secret exclusion, and rollback.
- Angular unit tests cover both administration views and all required states.
- A tagged Playwright scenario signs in as Administrator, performs a reversible
  user lock/unlock or role-safe operation, and observes the resulting security
  event.
- The target-surface audit reports `/administration` as implemented only after
  the role-bound useful-action tests exist and Stage 9 executes them.
