# Stage 02 Review - Pass 004

## Metadata

- Date: 2026-07-21
- Stage: 02
- Pass: 004
- Agent/tool: OpenAI Codex, fresh independent review task
- Result: `findings`
- Repository baseline: `origin/main` at `550f566` (`Merge Stage 2 pass 003 corrections`)

## Independence Declaration

- [x] I did not create or edit any artifact in this review scope.
- [x] My current context does not contain the primary agent's working session.
- [x] I formed my own evidence inventory before reading prior conclusions.

I had no prior context or work on this repository or parity map. Before opening
the filled workbook, reconnaissance, or prior reports, I read only the required
governance inputs and then enumerated and traced the legacy artifacts directly.

## Frozen Code-First Inventory

The complete forced-recursive file inventory was frozen in the pre-comparison
task output with this command shape:

```powershell
Get-ChildItem -LiteralPath legacy -Recurse -Force -File | Sort-Object FullName
```

The output recorded every relative path, byte length, and file attributes:

- total files: 544
- hidden `legacy/.setup` files: 95
- repository state: clean `main`, with `HEAD == origin/main == 550f566`

The independent trace covered every executable/deployable family found by that
enumeration:

- all nine frontend pages, browser event/validation paths, API client methods,
  Node static server/proxy behavior, Compose services, and static Liberty WAR;
- all 16 OpenAPI operation directories, 13 `operation.yaml` bindings, request
  and response mappings, ten z/OS assets, generated provider interfaces, and
  CICS/IMS Liberty connections;
- all 29 CICS COBOL programs, ten BMS sources, 43 copybooks, nine deployed
  banking transactions, five credit-agency transactions, DB2/data helpers,
  error handling, and CICS resource definitions;
- all IMS online COBOL/PL/I programs, five loaders, eight Java sources, nine
  DBDs, eight PSBs, seven deployed transactions, load inputs, MPP/JMP setup,
  IMS/DB2 history paths, and operator JCL;
- the PL/I/DB2 monthly-statement batch and JCL inputs/output paths;
- DBB language/build/package tasks, Z Code Scan, Wazi Deploy, zconfig, CICS and
  IMS setup, DB2 jobs, native z/OS Connect setup/refresh, frontend setup,
  local/remote orchestration, installation validation, and VS Code task entry
  points under hidden `.setup`;
- visible pre-commit secret scanning, VSIX download/install scripts, zOpenDebug
  launch configuration, and the documentation TOC generator.

Documentation prose and images were not treated as parity evidence. Executable
scripts and deployable descriptors were traced even when they supported
developer or operator workflows rather than end-customer banking behavior.

## Scope and Inputs

- Initial code-only input: the forced-recursive legacy inventory and the full
  channel trace above. No filled analysis artifact or prior report was opened
  until that inventory was frozen.
- Later comparison inputs: `analysis/legacy_user_flows.xlsx` (`User Flows`,
  rows 7-143), `analysis/legacy_reconnaissance.md`, and Stage 2 pass 001-003
  reports.
- Governance inputs: `MIGRATION.md`, constitution 0.4.0 draft, Stage 2 of the
  methodology, migration status, workbook instructions/tooling guide, and the
  review protocol/template.
- Runtime boundary: no authorized CICS, IMS, DB2, 3270, or deployed z/OS
  environment was available. This was the required static Stage 2 control.

## Method

I first reconciled every independently discovered frontend, API, CICS, IMS,
batch, build, deployment, scan, and operator entry point to a prospective
observable scenario. After crossing the comparison boundary, I loaded all 125
workbook detail rows through ExcelJS and compared each requirement, status,
expected result, and evidence citation with the source. I then reversed the
comparison by mapping the complete artifact inventory back to workbook rows to
find uncaptured behavior.

All 173 literal `legacy/...` workbook references were checked mechanically;
none was missing and no numeric line reference exceeded its file. The two
wildcard DB2 JCL references on row 125 were resolved manually. Generated z/OS
Connect response selections were also checked against their sibling files,
which exposed finding 1.

## Pass-003 Correction Verification

All five pass-003 corrections are present and code-supported:

1. Rows 139-142 now capture DBB package/build modes, Z Code Scan, Wazi Deploy,
   and native z/OS Connect provisioning/refresh, including native, WAR, JAR,
   configuration, DBD, and PSB outputs.
2. Row 136 correctly says the frontend Liberty WAR is static, excludes
   `server.js`, calls z/OS Connect directly on port 9080, and depends on CORS.
3. Row 130 records `INQACCTY` as an enabled program definition with no supplied
   implementation or transaction binding, without inventing behavior.
4. Row 124 records that ACCTYPE, CUSTTYPE, TSTATTYP, and TTYPE inputs are staged
   while the supplied population script submits only five application loaders.
5. Reconnaissance now reports 125 scenarios, preserves blocked pass 002 and
   findings pass 003, names pass 004, removes the contradicted completeness
   claim, and records the corrected deployment/partial surfaces.

## Findings

### 1. Medium - Bound API response mappings reference missing artifacts

Affected rows: 87-95 (`UF-008`); missing partial/error-mapping scenario.

The generated response selections contain eight references to absent sibling
files. Five affect bound operations: CICS account inquiry, account balance, and
customer-account list select nonexistent `response_400.yaml` files; CICS
customer update selects nonexistent `response_401.yaml` and
`response_403.yaml` files. The other three are the already-unbound account
collection and transaction list/detail directories, whose fallback mappings
also reference nonexistent `response_500.yaml` files.

Concrete evidence:

- `legacy/src/api/src/main/operations/%2Faccounts%2F%7BaccountId%7D/get/response_mapping.yaml:7`
- `legacy/src/api/src/main/operations/%2Faccounts%2F%7BaccountId%7D%2Fbalances/get/response_mapping.yaml:7`
- `legacy/src/api/src/main/operations/%2Fcustomers%2F%7BcustomerId%7D%2Faccounts/get/response_mapping.yaml:7`
- `legacy/src/api/src/main/operations/%2Fcustomers%2F%7BcustomerId%7D/put/response_mapping.yaml:8-10`
- the three contract-only `response_mapping.yaml` files under `/accounts`,
  `/accounts/{accountId}/transactions`, and transaction detail

Row 95 says standard failures are exposed, while rows 87-88 describe the bound
operations without this deployable mapping defect. Add an atomic `Partial`
scenario for missing generated response artifacts and update the affected API
rows so declared OpenAPI responses are not confused with complete deployable
operation mappings.

### 2. Medium - Top-level setup and installation validation are unmapped

Affected scope: `UF-012` (rows 126-143); missing operational scenarios.

`legacy/.setup/setup-local.sh:22-50` initializes or interactively preserves/
recreates a remote USS workspace and uses Zowe CLI to launch remote setup.
`legacy/.setup/setup-remote.sh:24-68` executes validation, environment setup,
and baseline installation in order. `legacy/.setup/setup-common.sh:466-590`
exposes the `validate-prereqs`, `environment`, and `install-bank-of-z` entry
points, provisions all middleware/application components, runs build/deploy,
and populates DB2 and IMS. `legacy/.setup/setup/validate-install.sh:73-253`
checks minimum DBB, ZOAU, zconfig, and Wazi Deploy installations and emits a
pass/fail summary with a failing exit status.

Rows 127-142 cover many component results and the later pipeline, but none
captures the operator-facing workspace/setup orchestrator, its preserve/delete
choice, ordered all-system installation, or the explicit installation
validation result. Add atomic operational scenarios for remote workspace/setup
orchestration and installation validation, with environment-dependent execution
left unverified.

### 3. Low - History diagnostic behavior is reduced to its TLS defect

Affected row: 134 (`UF-012`).

Row 134 records only the trust-all TLS behavior of `IMSBankHistory`. The same
executable main reads DB2 host, port, location, username, and password from
system properties, queries all `IMSBANK.HISTORY` columns for hard-coded account
`1501`, prints every returned column/value, and commits the read transaction
(`legacy/src/base/ims/java/src/main/java/nazare/jmp/controller/IMSBankHistory.java:61-109`).

Add a separate `Partial` diagnostic-query scenario, or expand the row without
losing the security deviation. Do not represent this fixed-account diagnostic
as the parameterized IBGHIST customer-facing history flow.

### 4. Low - Visible executable developer/operator tools are absent

Affected scope: `UF-012`; missing operational scenarios.

The code-first inventory found additional executable workflows with observable
operator effects that are absent from both the workbook and reconnaissance:

- `legacy/.pre-commit-config.yaml:14-21` runs IBM detect-secrets against the
  committed baseline and fails on unaudited findings;
- `legacy/scripts/download-vsix.js:176-238` downloads the configured extension
  set and reports per-item/final success or failure;
- `legacy/scripts/install-vscode-vsix.js:133-227` and
  `install-bobide-vsix.js:132-225` install every discovered VSIX and return a
  failing status when prerequisites or installations fail;
- `legacy/.vscode/launch.json:6-15` exposes a zOpenDebug connection to a parked
  Bank of Z debug session;
- `legacy/docs/scripts/generate_toc_from_md.py:42-102` regenerates the
  deployable documentation site's TOC from Markdown metadata.

Add operational rows for these executable tool surfaces, grouped where one
workflow has multiple launch wrappers. If any is deliberately outside migration
scope, record that as an explicit owner decision rather than silently omitting
an executable artifact.

## Automated Gates

- Required forced enumeration: passed; 544 files, including 95 under hidden
  `legacy/.setup`.
- Read-only workbook extraction: passed; one `User Flows` sheet, 12 epics and
  125 detail rows (rows 7-143).
- Literal evidence check: passed for 173 exact `legacy/...` references; no
  missing path and no line reference beyond EOF. Both row-125 wildcard families
  resolve.
- Generated operation response-integrity check: found the eight missing files
  recorded in finding 1.
- `npm --prefix analysis/tools run audit`: passed; `scenario rows: 125 (closed
  0, open 125); epics: 12; rev-covered open rows: 0/125`; `AUDIT OK`.
- Legacy modification check: passed; no file under `legacy/` is modified.
- `git diff --check`: passed with no output after the report/status edits.

## Conclusion and Next Gate

Result: `findings`. Every pass-003 correction is verified, but four new
coverage/contradiction findings prevent a dry pass. Stage 2 loops back to Stage
1. The primary agent must correct the workbook and reconnaissance without
changing legacy code, run the workbook audit to `AUDIT OK`, and update the
scenario count mechanically. Stage 2 then requires pass 005 by another eligible
fresh independent agent.
