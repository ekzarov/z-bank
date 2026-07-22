# Feature 001: Secure Access and Application Shell

## Traceability

- Workbook rows: 8-20, 109, 113-117
- Owner decisions: D-001, D-002, D-014
- Priority: P1, first delivery slice

## Goal

Provide one secure browser entry point for customers and bank staff, replacing
CICS key/menu navigation, IMS command sessions, the channel selector, and the
unauthenticated legacy control panel.

## User Stories

### US1 - Sign in and reach authorized work (P1)

As a customer or bank employee, I can sign in and see only navigation and API
operations permitted by my role.

**Acceptance:** valid credentials create a secure session; invalid credentials
do not reveal whether a user exists; anonymous and wrong-role requests are
rejected; customer, operator, and administrator navigation differ.

### US2 - End a session reliably (P1)

As an authenticated user, I can sign out and the session is invalidated even
when a dependent profile lookup is unavailable.

**Acceptance:** logout never reports success while leaving a usable session;
subsequent protected calls require authentication.

### US3 - Navigate and recover from invalid routes (P2)

As a user, I can navigate the modern application without terminal key codes or
a CICS/IMS selector, and I receive an accessible not-found or unavailable view
for bad routes and API outages.

## Functional Requirements

- **FR-001** The system SHALL authenticate users using ASP.NET Core Identity
  credentials stored with the framework's current password hashing.
- **FR-002** The API SHALL issue same-origin, HTTP-only, secure-in-production,
  `SameSite=Lax` cookie sessions and SHALL protect unsafe requests against CSRF.
- **FR-003** The system SHALL define `Customer`, `Operator`, and `Administrator`
  roles and enforce authorization in the API, not only in Angular navigation.
- **FR-004** A customer SHALL be scoped to their own customer record and owned
  accounts; an operator SHALL manage customer banking workflows; an
  administrator SHALL manage operational access permitted by later slices.
- **FR-005** Authentication failures SHALL use a generic response and SHALL not
  disclose account existence, password validity, or internal exceptions.
- **FR-006** Repeated failed login attempts SHALL trigger configurable lockout.
- **FR-007** Logout SHALL revoke the current session independently of customer
  profile retrieval and SHALL return an accurate result.
- **FR-008** The Angular shell SHALL expose role-filtered navigation and a clear
  authenticated identity indicator.
- **FR-009** The target SHALL use normal routes and commands; CICS PF keys, IMS
  message commands, C/IMS-prefixed customer identifiers, and the CICS/IMS web
  selector SHALL NOT be reproduced.
- **FR-010** Unknown UI routes SHALL render a not-found view; unavailable API
  calls SHALL render a recoverable error without exposing stack traces.
- **FR-011** The production web entry point SHALL serve Angular and proxy `/api`
  from one HTTPS-ready origin. Development MAY use the Angular proxy.
- **FR-012** No placeholder OAuth endpoint or scope SHALL be advertised as a
  working identity provider. External federation is outside this slice.
- **FR-013** Normal startup SHALL NOT create users, roles, schema, or demo data.
  Provisioning uses the explicit mechanism specified by Feature 008.
- **FR-014** Authentication and authorization events SHALL be structured and
  auditable without logging passwords, cookies, tokens, or sensitive payloads.

## Intentional Deviations

- Terminal menu numbers, PF3/PF12 behavior, and unsupported-key messages are
  replaced by standard web navigation (D-001).
- Channel-prefixed customer identifiers and channel-routing validation are
  replaced by channel-neutral customer IDs (D-001).
- IMS duplicate-login and logout replacement quirks are not preserved (D-002).
- Exact IMS terminal messages, login timestamps, and false-success behavior are
  legacy evidence only; target authentication follows FR-001 through FR-007.
- The unauthenticated control panel and placeholder OAuth URLs are not ported
  (D-002).
- Static proxy implementation details are replaced by same-origin routing;
  observable 404/unavailable outcomes remain (D-014).

## Out of Scope

- Customer/account business screens beyond navigation placeholders.
- External OIDC/OAuth provider integration.
- User administration UI; operator credentials are provisioned explicitly.

## Success Criteria

- API authorization tests cover anonymous, wrong-role, customer, operator, and
  administrator access.
- Angular tests cover role-filtered navigation and error states.
- Playwright covers sign-in, protected navigation, and sign-out.
