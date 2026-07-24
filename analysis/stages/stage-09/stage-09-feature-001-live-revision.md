# Stage 9 Live Revision - Feature 001

## Scope

- Feature: `001-secure-access-shell`
- Workbook rows: 8-20, 109, 113-117
- Date: 2026-07-22
- Result: clean for the delivered slice

## Legacy Baseline

The legacy web edge remained available at `/z-bank/` after deployment. It
continues to expose the static unauthenticated channel and z/OS Connect edge.
Real CICS, IMS, DB2, and 3270 authentication flows cannot be executed without
the external IBM runtime. Their approved Stage 3 outcome remains
`partial-simulated`; this revision does not relabel them as live-observed.

## Modern Walkthrough

The deployed HTTPS application was exercised through the browser:

- anonymous access redirected to sign-in;
- valid customer sign-in, protected navigation, sign-out, and post-logout
  rejection passed;
- Customer saw only `My banking`;
- Operator saw only `Customer operations`;
- Administrator saw only `Administration`;
- the role-specific destination opened for each identity;
- API authorization, generic failures, lockout, CSRF, and Problem Details
  remained covered by the real-SQL integration suite.

After the Stage 10 Pass 001 correction loop, the deployed application was
rebuilt from `c8d6e79` and the complete public walkthrough was repeated. The
public Playwright run completed five tests with five passes, including a
simulated API outage that opened the recoverable unavailable page. The workbook
now marks all 19 slice rows implemented and provides target file, test, route,
and deployment evidence. `UF-001` and revision gap `R1-G01` are closed.

## Findings

None. Observed differences from the legacy terminal/channel model are the
owner-approved deviations D-001, D-002, and D-014 already represented in the
workbook and Feature 001 SDD.

## Gates

- Workbook audit: passed, 19 closed and 116 open rows.
- SDD audit: passed, 135 rows and 27 artifacts.
- Legacy evidence audit: passed, 512 references.
- Angular unit tests: 15 passed.
- Public HTTPS Playwright: 5 passed.
