# Stage 02 Review - Pass 003

## Metadata

- Date: 2026-07-21
- Stage: 02
- Pass: 003
- Agent/tool: OpenAI Codex, fresh independent review task
- Result: `findings`
- Repository baseline: `origin/main` at `092034e` (`Merge evidence path correction`)

## Independence Declaration

- [x] I did not create or edit any artifact in this review scope.
- [x] My current context does not contain the primary agent's working session.
- [x] I formed my own evidence inventory before reading prior conclusions.

I had no prior work or context on this repository or its parity map. Before
opening the filled workbook, reconnaissance, or a prior review, I read only
the Stage 2 governance inputs and inspected executable/deployable content under
`legacy/`.

## Frozen Code-First Inventory

The required command `Get-ChildItem legacy -Recurse -Force` was run first. A
sorted manifest of every forced-recursive file, with byte length and SHA-256,
was frozen outside the repository before comparison inputs were opened:

- files: 544
- hidden `legacy/.setup` files: 95
- manifest: `%TEMP%/z-bank-stage-02-pass-003-legacy-inventory.tsv`
- manifest SHA-256: `3c87ed1b4f128be007bb09a36ce6d9a8666f6bd86370a60d023d1f540caa767d`

The complete executable/deployable artifact inventory formed from that
enumeration follows. Documentation, images, editor settings, license files,
and generated static-analysis rules were retained in the 544-file manifest
but did not create behavioral scenarios.

### Frontend and Local Runtime

- Pages: `index.html`, `admin.html`, `customer-create.html`,
  `customer-delete.html`, `customer-details.html`, `account-create.html`,
  `account-delete.html`, `account-details.html`, `account-deposit.html`.
- Runtime/client code: `config.js`, `server.js`, `js/api.js`, `js/utils.js`,
  `js/init-side-nav.js`, `js/components/headers.js`, bundled
  `js/carbon-web-components.min.js`, four CSS files, `package.json`, and image
  assets served by the Node runtime.
- Observable scenarios: redirect to the control panel and navigate all eight
  tools; create a CICS customer with client/OpenAPI-aligned required-field,
  title, length, and date validation; search a prefixed CICS/IMS customer ID,
  reject malformed IDs, display customer/accounts/balances, and update the
  customer through the selected subsystem; expose name search as an explicit
  unsupported stub; search customer accounts, select or deep-link an account,
  and show balances; select a customer account and deposit a positive amount
  with six-digit sort code through the CICS or IMS route; expose account
  create/update/delete and customer delete as explicit unsupported stubs;
  surface validation, backend, network, success, and not-implemented states.
- The Node server serves static files, proxies `/api/`, IMS, customer, and
  account requests to `API_BASE_URL`, returns static 404/500 responses, emits
  502 JSON on proxy failure, and handles SIGTERM/SIGINT shutdown.

### OpenAPI, Operations, and z/OS Assets

- Contract: `src/api/src/main/api/openapi.yaml`; Gradle project and servlet
  runtime: `src/api/build.gradle`, `settings.gradle`, `src/main/web.xml`.
- CICS-bound operations: `POST /customers` -> `CRECUST`; `GET` and `PUT
  /customers/{customerId}` -> `INQCUST`/`UPDCUST`; `GET
  /customers/{customerId}/accounts` -> `INQACCCU`; `GET
  /accounts/{accountId}` and `/balances` -> `INQACC`; `POST
  /accounts/{accountId}/deposit` -> `DBCRFUN`.
- IMS-bound operations: `GET` and `PUT /ims/customers/{customerId}` ->
  `IBGCUDAT`/`IBSCUDAT`; `GET /ims/customers/{customerId}/accounts`, `GET
  /ims/accounts/{accountId}`, and its `/balances` endpoint -> `IBACSUM`; `POST
  /ims/accounts/{customerId}/{accountId}/deposit` -> `IBTRAN`.
- Contract-only/unbound paths: `GET /accounts`, `GET
  /accounts/{accountId}/transactions`, and `GET
  /accounts/{accountId}/transactions/{transactionId}` have only fallback 500
  mappings and no `operation.yaml`/zAsset binding.
- All request, success, error, and response-selection YAML files beneath the 16
  encoded operation directories were inventoried. All ten zAssets and their
  DAI, copybook, generated-copybook, and JSON-schema provider files were
  inventoried: `CRECUST`, `DBCRFUN`, `INQACC`, `INQACCCU`, `INQCUST`,
  `UPDCUST`, `IBACSUM`, `IBGCUDAT`, `IBSCUDAT`, and `IBTRAN`.
- Cross-cutting scenarios: global OAuth2 is declared but has placeholder
  authorization/token URLs; mappings distinguish declared 200/201, 400, 401,
  403, 404, and 500 shapes, although several selections are unconditional or
  fall through; deposit requires amount >= 0.01 and a six-digit sort code.

### CICS COBOL, BMS, and Data Programs

- All ten BMS maps were inventoried: `BNK1ACC`, `BNK1B2M`, `BNK1CAM`,
  `BNK1CCM`, `BNK1CDM`, `BNK1DAM`, `BNK1DCM`, `BNK1MAI`, `BNK1TFM`, and
  `BNK1UAM`.
- All 29 COBOL programs were inventoried: `ABNDPROC`, `BANKDATA`, `BNK1CAC`,
  `BNK1CCA`, `BNK1CCS`, `BNK1CRA`, `BNK1DAC`, `BNK1DCS`, `BNK1TFN`,
  `BNK1UAC`, `BNKMENU`, `CRDTAGY1` through `CRDTAGY5`, `CREACC`, `CRECUST`,
  `DBCRFUN`, `DELACC`, `DELCUS`, `GETCOMPY`, `GETSCODE`, `INQACC`,
  `INQACCCU`, `INQCUST`, `UPDACC`, `UPDCUST`, and `XFRFUN`.
- All 43 CICS copybooks under `src/base/cics/copy` and all provider copies
  under `src/api/src/main/zosAssets` were included as executable interfaces.
- 3270 scenarios: main-menu dispatch to customer display, account display,
  customer creation, account creation, account update, credit/debit, transfer,
  and accounts-by-customer; Enter-driven lookup/submit; PF5 update/delete paths
  where implemented; PF10 customer delete; PF3 exit to `OMEN`; PF12 cancel;
  Clear/reset; invalid AID and map I/O failures.
- Callable scenarios: create, inquire, update, and delete customer/account;
  list customer accounts; credit/debit an account; transfer between different
  accounts with positive amount, ordered locking, retry/error handling, and
  balance/history updates; retrieve company and sort-code control values;
  initialize DB2 seed data; write application-abend records; and run five
  delayed dummy credit-agency transactions. Failure branches include missing
  records, invalid titles/required values/types/amounts, SQL errors, CICS
  resource errors, duplicate/same-account transfer, insufficient/invalid
  balances, and Storm Drain/deadlock handling.

### IMS COBOL, PL/I, Java, DBD/PSB, and Loaders

- Online COBOL: `IBACSUM`, `IBGCUDAT`, `IBLOGIN1`, `IBLOGOUT`, `IBSCUDAT`,
  and `IBTRAN`; PL/I login variant: `IBLOGIN.pli`.
- Loader COBOL: `LOADACCT`, `LOADCUSA`, `LOADCUST`, `LOADHIST`, and
  `LOADTSTA`; all 12 files under `LoadData` were inventoried as load inputs.
- Java: `IMSBankHistory`, `InsertHist`, `QueryTransaction`,
  `TransactionDetail`, `TransactionService`, `InputMessage`,
  `TranHistoryDetail`, and `TranHistoryOutputMessage`, plus the Java 21 Gradle
  build. `IBTRAN` can insert IMS history or bridge through JNI to
  `InsertHist`/`TransactionService`, while `QueryTransaction` reads DB2 history
  by account, newest first, limited to 50, and returns up to 4,200 bytes.
  `IMSBankHistory` is a separate diagnostic main that disables TLS/hostname
  verification and queries account `1501` using system-property credentials.
- DBDs: `ACCOUNT`, `ACCTYPE`, `CUSTACCS`, `CUSTOMER`, `CUSTTYPE`, `HISTORY`,
  `TSTAT`, `TSTATTYP`, `TTYPE`. PSBs: `IB`, `IBACSUM`, `IBGCUDAT`, `IBLOAD`,
  `IBLOGIN`, `IBLOGOUT`, `IBSCUDAT`, `IBTRAN`.
- Observable scenarios: login success, unknown customer, invalid password,
  already logged in, and status/timestamp replacement; logout success/failure;
  retrieve/update customer; retrieve a customer's account summary and
  balances, including no-customer/no-account; deposit/withdraw with transaction
  type validation, account/customer association lookup, account balance and
  last-transaction update, history persistence, and result/error message;
  query transaction history; and bulk-load every IMS database family with
  status/error handling.

### Batch and JCL

- `src/base/batch/jcl/BNKSTMT.jcl` runs DB2 PL/I `BNKSTMT` through DSN using
  DATECARD month and SORTCODE input.
- `src/base/batch/pli/BNKSTMT.pli` generates one paginated monthly statement
  per account for the sort code, defaults missing controls, derives the date
  range, retrieves customer and ordered transaction data, marks credits/debits,
  and prints customer/account detail, transactions, opening/closing/available
  balances, totals, transaction count, warnings, and completion/error output.

### Docker, Liberty, DBB, Deployment, and Operator Automation

- Root deploy/build inputs: `docker-compose.yaml`, `dbb-app.yaml`, `zapp.yaml`,
  `.zdx.json.template`, Zowe config/schema templates, and pre-commit/secret
  scanning configuration. Compose starts z/OS Connect Designer on 9080/9443
  and the Node frontend on 3001; it does not start CICS, IMS, or DB2.
- Liberty inputs: `server.xml`, `cics.xml`, `ims.xml`, `http-endpoint.xml`, and
  `web.xml`; these configure z/OS Connect, CICS IPIC, IMS connection factory,
  credentials, and HTTP endpoint. DBB builds COBOL, PL/I, BMS, DBD/PSB,
  OpenAPI WAR, frontend WAR, server configuration, and the IMS Java JAR, with
  special JNI link options for `IBTRAN`.
- Hidden `.setup` root scripts: `pipeline-common.sh`, `pipeline-local.sh`,
  `pipeline-remote.sh`, `setup-common.sh`, `setup-local.sh`, `setup-remote.sh`.
- Hidden `.setup/build`: `.zosattributes`, `datasets.yaml.j2`,
  `dbb-build.yaml`; Groovy `ImsJavaBuilder`, `ServerXmlPackager`,
  `VanillaFrontend`; language definitions `Assembler`, `BMS`, `Cobol`,
  `CobolTestcase`, `CPP`, `Db2Binds`, `Languages`, `LinkEdit`, `PLI`, `REXX`,
  and `Transfer`.
- Hidden `.setup/config` and libraries: `.gitignore`, `config.yaml`,
  `setenv.sh`, `colors.sh`, `config.py`, `prerequisites.sh`,
  `render_template.py`, `utilities.sh`.
- Hidden `.setup/deploy`: `db2_bind_package.jcl.j2`,
  `db2_bind_plan.jcl.j2`, `deployment-method.yml`, `Development.yml`,
  `ims_maclib.jcl.j2`, `ims_spoc.jcl.j2`, `types_pattern_mapping.yml`, and
  `zos_connect_app_config.xml.j2`.
- Hidden `.setup/jcl/cics`: `Db2-bind`, `Db2-create`, `Db2-drop`, `Db2-grant`,
  `Db2-insert`, `plt-create`, and `tcpip-create` Jinja JCL templates.
- Hidden `.setup/jcl/ims`: `Db2-bind`, `Db2-create`, `Db2-drop`, `Db2-insert`,
  `Db2-racf`, `Ims-deloldrecon`, `Ims-dynalloc`, four `Ims-load-*` jobs, and
  `Ims-recon`; JMP templates `CREDFSJVMAP`, `CREDFSJVMEV`, `CREDFSJVMMS`,
  `CREWORKDS`, `DFSJMP`, `dfsjvmpr.props`, `IMDOJMP`, `SIMLINK`, `STARTJMP`,
  `STOPJMP`, `VERIFYSPOC`; MPP templates `CRE_MPP_EXEC`, `DFSMPR1`,
  `DFSMPR2`, `STARTMPP`, `STOPMPP`.
- Hidden `.setup/setup`: `populate-db2-tables.sh`, `populate-ims-tables.sh`,
  `setup-cics-region.sh`, `setup-db2-tables.sh`, `setup-frontend-server.sh`,
  `setup-ims-bankz-regions.sh`, `setup-ims-region.sh`, `setup-ims-tables.sh`,
  `setup-zosconnect-server.sh`, `validate-install.sh`.
- Hidden `.setup/tasks`: `task-dbb-build.sh`, `task-wazi-deploy.sh`, and
  `task-zcodescan-static-scan.sh`.
- Hidden `.setup/zconfig`: `bank-of-z-definitions.yaml`, `cics-region.yaml`,
  `cics_region_0.6.0.schema.json`, `debug-definitions.yaml`, `ims-region.yaml`,
  `resource-model.yaml`, and `resource-schema.json`.
- Operator scenarios: validate tools/configuration; initialize a remote USS
  workspace; provision/stop CICS and IMS regions; create/drop/bind/grant/load
  CICS and IMS DB2 resources; define CICS maps/programs/transactions, IPIC,
  DB2, TCP/IP, and PLT resources; create IMS RECON/dynamic allocation, DBD/PSB
  libraries, databases, MPP/JMP procedures and JVM settings; load IMS data;
  install/start/stop frontend and z/OS Connect Liberty; perform full or impact
  DBB builds; package and deploy LOAD/CICSLOAD/MAPLOAD/DBRM/JCL/IMSLOAD,
  DBDLOAD/PSBLOAD, WAR, JAR, and configuration artifacts; bind DB2; activate
  CICS and IMS resources; refresh Liberty configuration/apps; run Z Code Scan;
  and validate installation. `Development.yml` intentionally excludes
  `BNK1B2M` from default deployment.

This inventory was frozen before the filled workbook, reconnaissance, and
prior review reports were intentionally opened. Subsequent sections record
comparison only; they do not redefine this inventory.

## Scope and Inputs

- Repository revision: `092034e` (`origin/main`, after PR #10).
- Initial code-only inventory: the complete forced-recursive legacy manifest
  and every executable/deployable family detailed above. No filled workbook,
  reconnaissance, or prior review was intentionally opened until that
  inventory was frozen.
- Later comparison inputs: `analysis/legacy_user_flows.xlsx` (`User Flows`,
  rows 7-137), `analysis/legacy_reconnaissance.md`, and Stage 2 pass 001/002
  reports.
- Governance inputs: `MIGRATION.md`, constitution 0.4.0 draft, Stage 2 of the
  methodology, migration status, workbook instructions/tooling guide, and the
  review protocol/template.
- No authorized CICS, IMS, DB2, 3270, or deployed z/OS runtime was available.
  This was the required static evidence control pass.

## Method

I traced each artifact family before comparison: frontend event handlers and
API calls; every OpenAPI path and generated operation/response mapping; all
CICS menu/AID/map/program/data paths; all IMS online programs, Java history
paths, DBDs, PSBs, and loaders; monthly statement input/SQL/output; and hidden
setup, build, deploy, activation, and operator scripts.

After freezing that result, I loaded all 119 workbook detail rows through
ExcelJS. Each scenario was compared with the frozen inventory for presence,
channel, active/dead/partial status, requirement, expected result, and cited
evidence. I also reconciled all programs, maps, API bindings, pages, jobs,
loaders, and operator entry points in the frozen inventory back to a workbook
scenario to search for omissions.

All ten pass-001 corrections were independently rechecked and are correct:

1. row 11 separates invalid menu values from unsupported AIDs;
2. rows 19-20 separate logout retrieval failure from REPL false success;
3. row 26 reports the name-search notification title as `Error`;
4. row 46 marks the old-account zero-balance helper dormant;
5. row 67 records the IMS account/customer ownership mismatch;
6. row 68 records DB2-then-IMS dual-write and account replacement order;
7. rows 69-70 split CICS `$N/A` and IMS balance presentation;
8. rows 76-78 cover negative deposit, negative withdrawal, and zero amount;
9. rows 109-111 split single-account normalization, raw CICS list values, and
   schema-invalid IMS `CHECKING`;
10. the workbook/reconnaissance count is mechanically 119 scenarios.

The pass-002 IMS history correction is also correct: row 68 now cites
`legacy/src/base/ims/java/src/main/java/nazare/jmp/history/TransactionService.java:140`,
which exists and begins the parameterized DB2 history insert.

## Findings

### 1. Medium - Build, scan, and deployment operator flows are absent

Affected scope: `UF-012` (rows 125-137) and missing scenarios.

No workbook row contains DBB, Wazi Deploy, or Z Code Scan evidence. Yet
`legacy/.setup/pipeline-common.sh:25-98,128-247` exposes scan, full/impact or
preview build, deploy, and combined pipeline commands.
`legacy/.setup/tasks/task-dbb-build.sh` validates DBB status and publishes build
lists/packages/logs; `task-zcodescan-static-scan.sh` previews the build and
publishes scan results; `task-wazi-deploy.sh` generates and executes a
deployment plan with evidence. `legacy/dbb-app.yaml:276-370` builds the
frontend WAR, z/OS Connect API, server configuration, IMS Java JAR, and native
artifacts. `legacy/.setup/deploy/deployment-method.yml:304-501` binds DB2,
activates CICS and IMS, deploys WAR/JAR/config artifacts, and refreshes z/OS
Connect. `legacy/.setup/setup/setup-zosconnect-server.sh:31-167` separately
creates and starts the native server and its CICS/IMS/CORS configuration.

Rows 128-136 cover selected resulting resources, but not these operator entry
points, outputs, no-change/preview behavior, or failure evidence. Add atomic
operational scenarios for scan/build/package/deploy and native z/OS Connect
provisioning/refresh, including frontend WAR, API WAR/config, IMS Java JAR,
native load modules, and DBD/PSB activation. Mark environment-dependent claims
unverified where runtime observation is unavailable.

### 2. Medium - Row 134 incorrectly claims frontend Liberty proxies API traffic

Affected row: 134 (`UF-012`).

The row says FEBANKZ "proxies API traffic to z/OS Connect." The generated
Liberty `server.xml` only serves `bank-frontend-vanilla.war`
(`legacy/.setup/setup/setup-frontend-server.sh:68-102`). Packaging explicitly
removes `server.js` from that WAR
(`legacy/.setup/build/groovy/VanillaFrontend.groovy:124-135`). On port 9081,
`legacy/src/frontend/config.js:14-20` directs the browser straight to
`http://<host>:9080/api`; z/OS Connect's setup adds CORS for that browser origin
(`setup-zosconnect-server.sh:139-157`). Only the separate Node deployment
proxies, as already captured by row 113.

Correct row 134 to describe static WAR service plus direct browser-to-z/OS
Connect calls and CORS. Do not infer a Liberty reverse proxy from the setup
script's stale header comment.

### 3. Low - Enabled CICS INQACCTY definition is an omitted partial surface

Affected scope: `UF-004`/`UF-012`; missing scenario.

`legacy/.setup/zconfig/bank-of-z-definitions.yaml:520-529` defines and enables
program `INQACCTY`, but no `INQACCTY.cbl` or other implementation exists under
`legacy/src`. The workbook correctly records the analogous missing `BNK1B2B`
surface at row 85 but never records `INQACCTY`.

Add an atomic Partial scenario stating that the program resource is enabled by
deployment configuration while its executable implementation and transaction
binding are absent. Do not claim account-type inquiry behavior from the name.

### 4. Low - Four staged IMS reference-data inputs are not mapped

Affected scope: `UF-011` (rows 119-123); missing scenario.

`legacy/.setup/setup/populate-ims-tables.sh:27-33` creates input datasets for
every `.data` file, including `ACCTYPE`, `CUSTTYPE`, `TSTATTYP`, and `TTYPE`.
Lines 36-52 then submit only the five application loaders already represented
by rows 119-123 (`ACCOUNT`, `CUSTOMER`, `CUSTACCS`, `HISTORY`, `TSTAT`). No
loader job for those four reference inputs is submitted.

Add a Partial operational scenario for the staged-but-not-loaded reference
datasets, or cite executable loading evidence if another active path exists.

### 5. Low - Reconnaissance contains stale and contradicted control claims

Affected artifact: `analysis/legacy_reconnaissance.md`.

The handoff still says a fresh agent "must complete Stage 2 pass 002," although
pass 002 is an immutable blocked report and status required pass 003. It also
claims all deployment definitions were reconciled to a row, contradicted by
findings 1, 3, and 4.

Update the handoff after correcting the workbook: preserve pass-002 history,
name pass 004 as the next independent control, refresh the scenario count, and
remove the unsupported deployment-completeness claim.

## Automated Gates

- Required forced enumeration: passed; 544 files, including 95 under hidden
  `legacy/.setup`; frozen manifest SHA-256 recorded above.
- `npm --prefix analysis/tools run audit`: passed; `scenario rows: 119 (closed
  0, open 119); epics: 12; rev-covered open rows: 0/119`; `AUDIT OK`.
- Read-only ExcelJS extraction: passed; one `User Flows` sheet, rows 1-137,
  12 epic rows and 119 detail rows.
- Literal evidence check: passed for 157 exact `legacy/...` references; no
  missing literal path and no numeric line reference beyond EOF. Row 68's
  corrected Java reference was also inspected directly.
- Wildcard/shorthand evidence: manually checked while reconciling every
  scenario; row 124's CICS/IMS `Db2-*.j2` families resolve. Findings above are
  semantic/coverage defects, not missing literal paths.
- Legacy modification check: passed; no file under `legacy/` is modified.
- `git diff --check`: passed with no output after the report/status edits.

## Conclusion and Next Gate

Result: `findings`. The ten pass-001 corrections and the pass-002 Java evidence
path are verified, but the complete hidden deployment inventory exposes four
new map/reconnaissance defects and one incorrect existing claim. Stage 2 is not
clean and loops to Stage 1.

The primary agent must correct findings 1-5 in the workbook and reconnaissance
without changing legacy code, run the workbook audit to `AUDIT OK`, and update
the mechanically calculated scenario count. Stage 2 then requires pass 004 by
another eligible fresh independent agent; this reviewer is not eligible for
that pass.
