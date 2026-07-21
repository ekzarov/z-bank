# Stage 02 Review - Pass 005

## Metadata

- Date: 2026-07-21
- Stage: 02
- Pass: 005
- Agent/tool: OpenAI Codex, fresh independent review task
- Result: `findings`
- Repository baseline: `origin/main` at `5929b14` (`Merge Stage 2 pass 004 corrections`)

## Independence Declaration

- [x] I did not create or edit any artifact in this review scope.
- [x] My current context does not contain the primary agent's working session.
- [x] I formed my own evidence inventory before reading prior conclusions.

I had no prior Bank of Z map, reconnaissance, or review context. I read only
the required governance inputs, then completed and froze the code-first legacy
inventory before opening the filled workbook, reconnaissance, or pass-004
report.

## Frozen Code-First Inventory

The complete visible and hidden inventory was frozen with:

```powershell
Get-ChildItem -Path legacy -Recurse -Force -File | Sort-Object FullName
```

For every file, the frozen output recorded relative path, byte length,
attributes, and SHA-256:

- files: 544, all tracked by Git
- bytes: 14,370,064
- canonical inventory SHA-256:
  `16bce7d8c0906e06465b2fa3a6ec56f169131d6a7cf84845b46df9006f086319`
- hidden `legacy/.setup` files: 95
- repository state: clean branch created from `origin/main == 5929b14`

The independent sweep covered all nine frontend pages, API client and proxy;
16 OpenAPI methods, 13 operation bindings, ten z/OS assets, generated provider
files, and Liberty configuration; 29 CICS COBOL programs, ten BMS sources, 38
CICS copybooks, resource definitions, DB2 operations, 3270 flows, and failure
handling; 11 IMS COBOL programs, the PL/I login, eight Java sources, nine DBDs,
eight PSBs, five loaders, MPP/JMP execution, and history behavior; the PL/I/JCL
monthly-statement batch; and all build, package, deployment, setup, validation,
remote orchestration, diagnostic, secret-scan, VSIX, debug, and documentation
tooling surfaces. Narrative documentation was not used as behavior evidence.

## Scope and Inputs

- Initial code-only input: all 544 forced-recursive legacy files at `5929b14`.
- Later comparison inputs: `analysis/legacy_user_flows.xlsx` (`User Flows`,
  rows 7-151), `analysis/legacy_reconnaissance.md`, and pass 004.
- Governance inputs: `MIGRATION.md`, constitution 0.4.0 draft, current status,
  Stage 2 methodology, workbook instructions/tooling, and review protocol.
- Runtime boundary: no authorized CICS, IMS, DB2, 3270, or deployed z/OS
  environment was available; this was the required static Stage 2 control.

## Method

I traced each executable/deployable family into an independent prospective
behavior inventory, including partial and operator-only surfaces. After the
comparison boundary, I reconciled that inventory in both directions against
all 133 workbook detail rows and the reconnaissance claims. I checked every
literal legacy evidence path and numeric line bound, recomputed generated API
response references, verified operation-to-z/OS-asset references, syntax-
checked the JavaScript executables, and inspected the newly mapped Python TOC
tool as executable code rather than accepting file presence as implementation.

## Pass-004 Correction Verification

All four pass-004 correction groups are present and code-supported:

1. Rows 87-88, 95-96 and reconnaissance distinguish complete API contracts
   from five missing bound-operation and three missing unbound-operation
   response artifacts.
2. Rows 141-142 cover remote workspace/setup orchestration and explicit DBB,
   ZOAU, zconfig, and Wazi Deploy prerequisite validation.
3. Rows 135-136 separate the trust-all TLS deviation from the fixed-account
   1501 diagnostic query and do not confuse it with IBGHIST.
4. Rows 147-150 cover detect-secrets, VSIX download/install, parked-session
   zOpenDebug, and documentation TOC tooling. Reconnaissance reports 133
   scenarios and preserves the pass history and partial-surface warnings.

## Findings

### 1. Medium - Documentation TOC generator is claimed working but is not executable

Affected workbook row: 150 (`UF-012`); affected reconnaissance tooling claim.

Row 150 marks `Source implemented?` as `Yes` and says the script rewrites the
TOC from discovered Markdown metadata or fails on invalid input. The supplied
script cannot reach that behavior:

- line 14 is a malformed dictionary entry with no key/value separator;
- lines 28-48 contain invalid indentation around `try`, `except`, and the
  function body;
- line 89 is also misindented; and
- the file ends at line 111 without calling `generate_toc()`.

Concrete evidence:
`legacy/docs/scripts/generate_toc_from_md.py:13-48,89,111`.

This is a contradiction, not merely an unavailable external prerequisite. Mark
the row `Partial` and state that the committed generator is syntactically
invalid and has no entry-point invocation, so it does not regenerate the TOC.
Update reconnaissance to preserve that limitation. Do not repair legacy code
during Stage 1 correction.

## Automated Gates

- Forced inventory: passed; 544 files, including all 95 hidden `.setup` files.
- Git/forced-inventory reconciliation: passed; 544 tracked = 544 forced files.
- Workbook extraction: passed; 12 epics and 133 detail rows (rows 7-151).
- Literal evidence check: passed; 190 references, zero missing exact paths,
  zero line references beyond EOF; both DB2 JCL wildcard families resolve.
- Generated-reference check: reproduced exactly 79 response selections and
  eight missing sibling response YAML files; 13 operation bindings all refer
  to present z/OS assets; no new generated-reference defect was found.
- JavaScript syntax checks: passed for the three VSIX scripts and frontend
  server.
- Documentation generator execution check: Python runtime was unavailable on
  this host; direct source inspection proves the syntax/entry-point defect.
- Workbook audit: passed with `AUDIT OK` (133 open scenario rows, 12 epics).
- Legacy modification check: passed; no file under `legacy/` changed.
- Evidence/generated-reference checks: passed apart from the recorded,
  already-mapped eight missing API response files and new finding above.
- `git diff --check`: passed with no output.

## Conclusion and Next Gate

Result: `findings`. Pass 005 independently verifies every pass-004 correction
and finds no new banking, API, deployment, setup, diagnostic, secret-scan,
VSIX, or debug omission. The TOC row and reconnaissance nevertheless overstate
a broken script as implemented, so this is not a dry pass. Stage 2 loops back
to Stage 1 for that map/reconnaissance correction. After `AUDIT OK`, a different
eligible fresh agent must perform Stage 2 pass 006.
