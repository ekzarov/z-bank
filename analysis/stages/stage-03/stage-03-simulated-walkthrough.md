# Stage 3 Simulated Legacy Walkthrough

**Date:** 2026-07-21

**Mode:** `simulate`, approved by the project owner

**Outcome:** `partial-simulated`
**Real IBM runtime status:** blocked and unverified

## Scope and evidence class

The walkthrough used the original static Bank of Z web frontend and Node proxy
unchanged, backed by the traceable harness under `simulation/`. The harness is
derived from the COBOL, PL/I, BMS/copybooks, IMS load data, OpenAPI contract,
frontend client, and workbook rows listed in
`simulation/fixtures/legacy-fixture.json`.

This run did **not** execute z/OS, CICS, IMS, DB2, 3270, z/OS Connect, Liberty,
compiled COBOL/PL/I, JCL, or IBM transaction/runtime services. A workbook row
is labeled `Runtime: simulated` only when a durable automated assertion covers
its stated behavior; all other source-derived rows are `Runtime: static-only`.
No row becomes `live-observed` or real-runtime verified from this run.

## Executed contours

| Contour | Executed behavior | Result |
|---|---|---|
| CICS terminal model | Menu dispatch, cancel/exit, invalid menu and unsupported key branches | Passed in harness |
| CICS cash model | Credit/debit, validation, payment-only insufficient-funds and loan restrictions, teller bypass, audit record | Passed in harness |
| CICS transfer model | Positive amount, same-account rejection, two-account mutation, one source-account history record with destination context, no inferred overdraft rejection | Passed in harness |
| IMS message model | Login, duplicate/invalid login, logout false-success defect, deposit/withdraw, invalid/missing data, signed/zero amount behavior, ownership gap | Passed in harness |
| REST/API model | CICS/IMS customer and account routes, deposits, standard errors, deliberately unbound account/transaction collection routes | Passed in HTTP contract tests |
| Existing web UI | Original `legacy/src/frontend` served through its Node proxy; CICS `C0000000001` and IMS `I000000001` customer/account portfolios | Browser smoke passed |
| Persistence transitions | In-memory account, customer, transaction/history, dual-history ordering, and transfer rollback model | Passed in harness; not DB2/IMS proof |
| Monthly batch model | Parameter month, customer/account identity, period rows, empty history, totals, opening/closing balance, footer | Passed in harness; not JCL/PL/I execution |

## Commands and exact results

```text
cd simulation
npm test
```

Result: see the current `npm test` output; the committed suite is the durable
source of the exact count after regression corrections.

```text
npm run walkthrough
```

Result: all scripted steps completed; the intentionally rejected CICS payment
returned `INSUFFICIENT_FUNDS` and the direct IMS ownership-gap step mutated the
account belonging to customer `000000002` while returning customer
`000000001`'s portfolio.

The local HTTP smoke returned `200` for simulator health, the original frontend,
and a customer request proxied through that frontend. Browser smoke displayed:

- CICS customer Martha Antonelli with accounts `10000001` and `10000002`;
- IMS customer Martha Antonelli with accounts `101` and `102`;
- route-specific `CHECKING` for the IMS current account, matching the mapped
  legacy response even though the OpenAPI enum omits it.

Docker Compose was prepared, but Docker Desktop's Linux engine was not running
during this pass. The equivalent services were started directly with Node for
the HTTP/browser smoke. This is an environment limitation, not an IBM-runtime
claim and not a failure of the simulator tests.

## Stage 4 decision candidates confirmed

No new parity-map row was discovered. The simulated run exercised and retained
these already mapped decision candidates:

1. IMS logout can report success after a failed replacement.
2. Direct IMS transactions do not verify that the account belongs to the
   supplied customer.
3. Direct IMS accepts signed and zero amounts, while the web/OpenAPI deposit
   boundary requires a positive amount.
4. CICS payment restrictions differ intentionally from teller behavior.
5. Transfer code has no evidenced pre-transfer overdraft rejection.
6. Published account-list and transaction REST routes lack operation mappings.
7. IMS emits `CHECKING`, which is outside the published Account enum.
8. The web control panel has no application authentication/authorization.
9. The CICS deposit response is scalar while the legacy deposit page expects
   arrays for its success-balance display.

These are requirements-revision inputs, not silently approved target behavior.

## Residual risk

The following remain unverified until an authorized IBM environment is
available: middleware routing, COMMAREA and IMS message layouts, EBCDIC and
packed-decimal behavior, DB2 SQL and lock/rollback semantics, IMS call status
handling, CICS task/facility metadata, generated adapter mappings, security,
JCL execution, PL/I formatting/pagination, and operational deployment jobs.

## Gate result

The approved simulation substitute is complete for its declared scope:

- fixtures and responses cite their legacy basis;
- automated harness and HTTP contract checks pass;
- the original web UI was smoked against the simulator;
- affected workbook rows remain explicitly simulated/unverified;
- the real IBM-runtime blocker remains open.

Per the recorded owner fallback decision, work may proceed to Stage 4
requirements revision with outcome `partial-simulated`.
