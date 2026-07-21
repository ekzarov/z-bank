# Stage 02 Review - Pass 001

## Metadata

- Date: 2026-07-21
- Stage: 02
- Pass: 001
- Agent/tool: OpenAI Codex, independent review task in a dedicated workspace
- Result: `findings`

## Independence Declaration

- [x] I did not create or edit any artifact in this review scope.
- [x] My current context does not contain the primary agent's working session.
- [x] I formed my own evidence inventory before reading prior conclusions when
      the stage requires from-scratch discovery.

I am eligible for this pass. I had no prior involvement with this repository's
parity map or reconnaissance, and I did not read
`analysis/legacy_user_flows.xlsx` or
`analysis/legacy_reconnaissance.md` until after completing the independent
inventory from executable artifacts under `legacy/`.

## Scope and Inputs

- Repository revision: `3e777268f35a0b0ecb9f9d0e4933f706957467ce`.
- Initial code-only inventory: all frontend HTML/JavaScript/server files; all
  OpenAPI paths, operation mappings, response mappings, and z/OS assets; all
  CICS COBOL programs, BMS maps, helper programs, and copybook-backed
  interfaces; all IMS COBOL/PL/I transactions, Java message processors, DBD/PSB
  definitions, and five data loaders; the monthly statement PL/I/JCL; Docker,
  Liberty, DBB, and hidden `.setup` deployment/build/operator artifacts.
- Later comparison inputs: `analysis/legacy_user_flows.xlsx` (sheet `User
  Flows`, rows 7-128) and `analysis/legacy_reconnaissance.md`.
- Governance inputs: `MIGRATION.md`, constitution 0.4.0 draft,
  `analysis/migration_status.yaml`, Stage 2 methodology, workbook instructions,
  workbook tooling guide, and review protocol/template.
- No runtime URL or IBM CICS/IMS/DB2 environment was available. This was a
  static evidence review, as required for Stage 2.

## Method

I enumerated executable artifacts first, without reading Stage 1 conclusions.
For the browser channel I traced every page, event handler, validation branch,
API call, success/error state, redirect, and unsupported client method. For the
external API channel I reconciled all 13 OpenAPI operations with every generated
request/response mapping and z/OS asset. For CICS I traced every menu option,
attention key, map field, validation path, linked business program, SQL/data
mutation, rollback, and abend path. For IMS I traced login/logout, inquiry,
update, account summary, cash activity, both history stores, Java history
inquiry, message shapes, PSBs/DBDs, and all loaders. I also traced statement
batch input, output, pagination, SQL error handling, and operator deployment
automation.

After freezing that inventory, I read all 110 workbook detail rows and the
reconnaissance. I compared every row's requirement, status, expected result,
grouping, and evidence to executable code. I checked every explicit
`legacy/...` path and numeric line range mechanically; all such literal paths
exist and no numeric range exceeds its file. I then adversarially checked
shorthand/wildcard citations and cross-channel contradictions manually.

## Findings

### 1. High - IMS permits an account/customer mismatch that the map omits

Affected rows: 62-67 and 83 (`UF-006`, `UF-008`).

`IBTRAN` retrieves and mutates the account using only `IN-ACCID`
(`legacy/src/base/ims/cobol/IBTRAN.cbl:299-367`), then independently uses
`IN-CUSTID` only to build the returned portfolio
(`legacy/src/base/ims/cobol/IBTRAN.cbl:386-430`). The IMS deposit mapping passes
both path values without proving their relationship
(`legacy/src/api/src/main/operations/%2Fims%2Faccounts%2F%7BcustomerId%7D%2F%7BaccountId%7D%2Fdeposit/post/request.yaml`).
An external caller can therefore request one customer's path while mutating a
different existing account, after which the response summary is based on the
supplied customer. No row records this observable authorization/data-integrity
behavior.

Required action: add an atomic, code-proven scenario for mismatched IMS customer
and account identifiers, mark the security deviation explicitly, and keep it
separate from ordinary missing-account/customer handling.

### 2. High - Row 44 claims an old-account rule that is not executed

Affected row: 44 (`UF-004`).

The row is `Yes` and says customer IDs below 16 expose zero balances. The rule
body exists at `legacy/src/base/ims/cobol/IBACSUM.cbl:269-274`, but its only
invocation is commented out at lines 221-224. The active code moves the stored
balance directly to output. The cited evidence therefore contradicts the
claimed implemented behavior.

Required action: correct the row to describe the dormant/commented rule as
partial or non-executed evidence, or remove the scenario if it is not a legacy
behavior. Do not retain `Yes` or claim a zero result without runtime evidence or
an active call path.

### 3. High - Row 18 incorrectly says logout replacement failure is surfaced

Affected row: 18 (`UF-001`).

On a failed IMS `REPL`, `IBLOGOUT` first moves `BAD-STATUS` to `MSG-OUT`, but
then unconditionally overwrites it with `LOGOFF SUCCESSFUL`
(`legacy/src/base/ims/cobol/IBLOGOUT.cbl:203-212`). `FAILED UPDATE FOR LOGOFF`
is used for failure to retrieve the customer, not for failure to persist the
logout update. The row's requirement and expected result are unsupported.

Required action: split retrieval failure from replacement failure and record
the actual false-success behavior as a security/data-consistency deviation.

### 4. High - IMS accepts zero and negative amounts; the map misses operation inversion

Affected rows: 62-64 and 71 (`UF-006`).

`IBTRAN` validates only transaction type (`d`/`w`), converts `IN-AMOUNT` with
`NUMVAL`, and performs arithmetic without a positive/non-zero check
(`legacy/src/base/ims/cobol/IBTRAN.cbl:294-343`). A negative deposit subtracts
funds, a negative withdrawal adds funds, and zero still advances transaction
history/last-transaction processing. Row 71 covers insufficient-funds absence,
not signed or zero amount behavior.

Required action: add separate atomic scenarios for signed/zero IMS amounts and
distinguish direct IMS message behavior from OpenAPI schema validation.

### 5. Medium - Row 65 describes alternative history stores, but the active path writes both

Affected row: 65 (`UF-006`).

The row says history is stored in IMS "or, when Java is primed, in DB2" and that
the runtime can use either path. In the active transaction path Java is primed,
DB2 history is attempted, and IMS history is then inserted unconditionally
before the account replacement
(`legacy/src/base/ims/cobol/IBTRAN.cbl:326-367`). This is dual-write behavior,
with different error/transaction semantics, not an either/or implementation.

Required action: rewrite or split the row to record both writes, their order,
and the observable partial-failure risk.

### 6. Medium - Row 66 groups CICS and IMS web deposit results that render differently

Affected row: 66 (`UF-006`).

The CICS response mapping returns scalar `availableBalance` and `accountId`
(`.../%2Faccounts%2F%7BaccountId%7D%2Fdeposit/post/response_201.yaml`). The web
page only derives `balanceDisplay` when `result.availableBalance` is an array,
so a successful CICS deposit displays `New balance: $N/A`
(`legacy/src/frontend/account-deposit.html:353-371`). IMS returns portfolio
arrays and follows a different branch. The grouped row claims both channels show
the resulting balance.

Required action: split CICS and IMS web deposit presentation or correct the
expected result/status for CICS.

### 7. Medium - Row 102 has an incorrect mapping claim and unusable evidence reference

Affected row: 102 (`UF-010`).

The row says API account-type mapping normalizes values to `SAVINGS`, `LOAN`, or
`CURRENT`, citing `legacy/src/api/src/main/operations response.yaml`, which is
not a concrete file or resolvable evidence path. The IMS account mappings emit
`CHECKING` for type `c`
(`.../%2Fims%2Fcustomers%2F%7BcustomerId%7D%2Faccounts/get/response_200.yaml:16-20`),
while the OpenAPI `Account.accountType` enum does not include `CHECKING`. The
CICS list mapping also passes raw account types rather than applying the
single-account normalization.

Required action: replace the broken citation with exact mapping files and split
mapping behavior by route/channel, including schema-invalid `CHECKING` and raw
CICS list values.

### 8. Low - Row 24 states the wrong visible title for unsupported name search

Affected row: 24 (`UF-002`).

The API method throws the unsupported message, but the page catches it and
renders an inline notification titled `Error`, not `Feature Not Implemented`
(`legacy/src/frontend/customer-details.html:207-229`). Other unsupported pages
do use the `Feature Not Implemented` modal, which appears to have been
incorrectly generalized to name search.

Required action: correct the expected result or separate the common unsupported
API state from each page's actual presentation.

### 9. Low - Invalid menu choice is missing and grouped with a different key path

Affected rows: 8-10 (`UF-001`).

Row 10 covers unsupported attention keys (`Invalid key pressed.`). Entering an
invalid menu value is a different input branch with the message `You must enter
a valid value (1-7 or A).` and no transaction dispatch
(`legacy/src/base/cics/cobol/BNKMENU.cbl:363-378`). It is absent as an atomic
scenario.

Required action: add an invalid-menu-selection alternative path rather than
grouping it with unsupported terminal attention keys.

### 10. Low - Reconnaissance reports the wrong scenario count

Affected artifact: `analysis/legacy_reconnaissance.md`.

The handoff claims 12 epics and 98 atomic scenarios. The workbook has 12 epic
banner rows and 110 detail/scenario rows (rows 7-128). This makes the stated
completeness count stale and unsupported.

Required action: update the reconnaissance count after correcting the map and
recalculate it mechanically.

## Automated Gates

- Read-only ExcelJS workbook load and row extraction: passed; one `User Flows`
  sheet, rows 1-128, 12 epic rows, 110 detail rows.
- Read-only explicit evidence-path/range check: passed for literal
  `legacy/...` citations; no missing literal path and no line range beyond EOF.
- Filled workbook audit: not run because this pass did not edit the workbook,
  as directed by the Stage 2 request and workbook protocol.
- `git diff --check`: passed with no errors after report/status edits.

## Conclusion and Next Gate

Result: `findings`. This is not a clean Stage 2 pass. The process loops back to
Stage 1 so the primary agent can correct the parity map and reconnaissance,
including all ten findings above. Any workbook edit must be followed by
`npm --prefix analysis/tools run audit` with `AUDIT OK`. After those corrections
are committed, Stage 2 requires pass 002 by another eligible fresh independent
agent; this reviewer must not perform that pass.
