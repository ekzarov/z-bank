# Stage 9 Live Revision - Feature 003

## Scope

- Feature: `003-account-management`
- Workbook rows: 41-58, 88, 110-112
- Date: 2026-07-22
- Result: clean for the delivered slice

## Legacy Baseline

The legacy web edge remains available at `/z-bank/`. Real CICS, IMS, DB2,
3270 account screens, and z/OS Connect mappings still require the unavailable
IBM runtime. The approved Stage 3 result remains `partial-simulated`; no
static-only or simulated behavior was relabelled as live-observed.

## Modern Walkthrough

The deployed HTTPS application was exercised as real users:

- Operator opened a customer's complete paged account portfolio.
- Operator followed a direct account-detail link.
- Operator created an account with generated identity, configured sort code,
  dates, zero balances, and audit evidence.
- Operator updated product metadata without changing balances or
  system-managed statement dates.
- Operator safely closed an eligible zero-balance account while retaining its
  record and audit history.
- Customer access remained relationship-bound to the customer's own accounts.
- Invalid, missing, foreign, and unsupported requests returned stable Problem
  Details without disclosing or mutating protected data.

The real-SQL integration suite separately verifies the concurrent ten-account
limit, complete pagination, FK and check constraints, decimal precision,
optimistic concurrency, eligibility-gated closure, and atomic rollback of
account plus audit persistence.

## Workbook Reconciliation

Twenty-one Feature 003 detail rows now record destination implementation notes
and concrete code/test evidence. `UF-004`, `UF-005`, revision gap `R1-G03`, and
the now-complete cross-cutting `UF-010` are closed. The Feature 003 row inside
`UF-008` is green while that mixed epic remains open for Features 004 and 006.
No unrelated detail row was closed.

## Findings

None. Channel-neutral account resources, normalized product types, bounded
pagination, retained-history closure, system-managed dates, and atomic account
creation are the owner-approved decisions already represented in Stage 4 and
the SDD.

## Gates

- Unit tests: 45 passed.
- Real SQL Server integration tests: 36 passed.
- Angular tests: 30 passed.
- Public HTTPS Playwright: 7 passed.
- Workbook audit: passed, 60 closed and 75 open rows.
- SDD audit: passed, 135 rows and 27 artifacts.
- Legacy evidence audit: passed, 512 references.
- Excel collapsed, expanded-detail, and revision-sheet visual render: passed.

Stage 10 Pass 005 returned the slice for task synchronization, explicit
active-account wording, and missing product-type validation. The corrections
were deployed at `d19bf21`; all deterministic and public gates above were
rerun, and no workbook status changed.
