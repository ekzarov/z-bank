# Bank of Z Legacy Reconnaissance

## Purpose

This is the Stage 1 handoff to the later control, requirements-revision, and SDD
stages. The governed behavioral inventory is
[`legacy_user_flows.xlsx`](legacy_user_flows.xlsx); this note records how it was
produced, its runtime limits, and the decisions that must not be guessed during
Stages 4-5.

## Evidence boundary

Requirements were derived from executable or deployable artifacts under
`legacy/`: COBOL and PL/I programs, BMS maps, JCL, IMS Java integration, OpenAPI
and z/OS Connect operation mappings, frontend HTML/JavaScript/server code,
Docker Compose, and CICS/IMS deployment definitions. Narrative files under
`legacy/docs/` were not used as requirement evidence.

The analyzed snapshot is upstream IBM Bank of Z commit
`69a0bf9e162223c33d35468e9d708b591d9c8ec0`.

## System shape discovered from code

- CICS 3270 operator channel for customer/account lifecycle, cash transactions,
  transfers, account lookup, and shared failure handling.
- IMS transaction channel for login/logout, customer inquiry/update, account
  summaries, deposits/withdrawals, and history persistence.
- DB2-backed CICS data plus IMS databases and an IMS transaction path that
  attempts DB2 history through Java before also inserting IMS history.
- z/OS Connect REST facade over selected CICS and IMS programs.
- Static Carbon-based web control panel that routes C-prefixed IDs to CICS and
  I-prefixed IDs to IMS.
- PL/I/JCL monthly statement batch and separate CICS/IMS data loaders.
- Deployment automation for DB2 objects/plans and CICS/IMS resources.

The parity map contains 12 epics and 119 atomic, checkable scenarios. All target
and SDD columns are intentionally empty because target design has not started.

## Proven partial or unavailable legacy surfaces

- Web customer name search is displayed but the JS API explicitly throws
  `not supported`.
- Web customer deletion is displayed but the JS API explicitly throws
  `not supported`.
- Web account create/update/delete pages exist but their JS API operations
  explicitly throw `not supported`.
- OpenAPI declares account collection and transaction list/detail routes, but
  those operation directories contain no `operation.yaml` mapping.
- OAuth2 is declared using `auth.bankofz.example.com` placeholder endpoints;
  the code does not prove a production identity provider.
- A Java history utility disables TLS certificate and hostname verification.
- Transfer overdraft enforcement is not clear from static code and remains an
  inferred behavior requiring executable observation or owner decision.
- IMS cash activity does not validate that the supplied customer owns the
  supplied account; account mutation and returned customer portfolio use the
  two identifiers independently.
- Direct IMS cash messages accept signed and zero amounts. Negative values
  reverse deposit/withdrawal direction, while zero still advances history and
  last-transaction processing. Web/OpenAPI validation is a separate boundary.
- IMS logout can return `LOGOFF SUCCESSFUL` after a failed replacement because
  the failure message is overwritten unconditionally.
- CICS web deposits succeed but display `New balance: $N/A` because the mapping
  returns scalar balances while the page's display branch expects arrays.
- Account-type mapping is route-specific: the CICS account list passes raw
  values and IMS can emit `CHECKING`, which is absent from the OpenAPI enum.

## Runtime constraint

`legacy/docker-compose.yaml` starts the web frontend and IBM z/OS Connect
Designer only. It requires externally supplied CICS and IMS hosts/credentials
and does not provide CICS, IMS, or DB2. Full legacy execution therefore requires
an authorized IBM z/OS/Wazi (or verified compatible) environment. Until one is
available, runtime-only behavior must remain unverified rather than assumed.

## Adversarial completeness passes

After the first map draft, the source inventory was rechecked by channel and
program family. The second pass added three missed operational behaviors:

1. loading IMS customer-account relationship segments (`LOADCUSA.cbl`);
2. loading IMS transaction-status segments (`LOADTSTA.cbl`);
3. supplying shared CICS company name and sort code (`GETCOMPY.cbl`,
   `GETSCODE.cbl`).

The final pass reconciled all CICS COBOL program families, all IMS COBOL loaders
and transactions, every frontend page, all declared OpenAPI paths, all existing
z/OS Connect operation mappings, the monthly statement job, and deployment
definitions against at least one workbook row. Helper copybooks/data mappings
are evidence for parent flows rather than independent user flows.

Stage 2 pass 001 then found ten concrete map defects. Stage 1 was reopened and
the workbook was corrected by splitting logout retrieval/replacement failures,
adding invalid CICS menu input, IMS ownership mismatch, signed/zero IMS cash
activity, route-specific deposit presentation, and route-specific account-type
mapping. It also corrected the dormant old-account helper, history dual-write,
name-search presentation, and the mechanically recalculated scenario count.
The evidence and required corrections are recorded in
[`reviews/stage-02-pass-001.md`](reviews/stage-02-pass-001.md). A different
independent agent must complete Stage 2 pass 002 before this inventory is clean.

## Stage 4-5 owner decisions

Before specs/plans/tasks are final, the owner must decide:

- target backend, UI, persistence, messaging/batch, and deployment stack;
- whether CICS and IMS remain separate business channels or become one target
  domain with compatibility identifiers;
- which partial web/API surfaces are desired requirements versus legacy dead UI;
- authentication/authorization scope, especially the placeholder OAuth contract
  and IMS login/logout behavior;
- transfer overdraft rule and treatment of dormant IMS old-account code;
- whether target security closes the IMS account/customer ownership gap and
  logout false-success behavior while documenting intentional parity deviation;
- whether signed/zero direct IMS cash behavior is rejected or preserved behind
  a compatibility boundary;
- whether the PL/I monthly statement is MVP, later milestone, or approved deferment;
- how full parity will be observed without an available CICS/IMS/DB2 environment.

No target implementation should begin until the constitution is ratified and
the relevant feature SDD is approved.
