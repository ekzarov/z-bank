# Stage 02 Review - Pass 006

## Metadata

- Date: 2026-07-21
- Stage: 02
- Pass: 006
- Agent/tool: OpenAI Codex, fresh independent review task
- Result: `findings`
- Repository baseline: `origin/main` at `68292ed` (`Merge Stage 2 pass 005 correction`)

## Independence Declaration

- [x] I did not create or edit any artifact in this review scope.
- [x] My current context does not contain the primary agent's working session.
- [x] I formed my own evidence inventory before reading prior conclusions.

I had no prior Bank of Z workbook, reconnaissance, or review context. I read
the required governance inputs, then completed and froze the code-first legacy
inventory before opening the filled workbook, reconnaissance, or prior reports.

## Frozen Code-First Inventory

The complete visible and hidden inventory was frozen with:

```powershell
Get-ChildItem -LiteralPath legacy -Force -Recurse -File | Sort-Object FullName
```

For every file, the frozen output included relative path, byte length,
attributes, and SHA-256:

- files: 544, all tracked by Git
- bytes: 14,370,064
- aggregate path/size/content manifest SHA-256:
  `3a1e4170b2671a93c95d6ed3cbaef1f5d03dc5b528151d626009f97a3837084a`
- files under dot-prefixed paths: 113
- files under hidden `legacy/.setup`: 95
- repository state: clean branch from `origin/main == 68292ed`

The independent sweep covered all frontend pages, browser and server behavior;
OpenAPI routes, operation mappings, generated responses, z/OS assets, and
Liberty configuration; CICS COBOL/BMS/copybooks/resources and DB2 paths; IMS
COBOL/PL/I/Java, DBD/PSB, loaders and message paths; monthly-statement batch;
and every visible or hidden build, scan, setup, deployment, validation,
diagnostic, secret-scan, VSIX, debug, repository-automation, and documentation
tooling/configuration surface. Narrative documentation and images were not
treated as parity evidence.

## Scope and Inputs

- Initial code-only input: all 544 forced-recursive legacy files at `68292ed`.
- Later comparison inputs: `analysis/legacy_user_flows.xlsx` (`User Flows`,
  rows 7-151), `analysis/legacy_reconnaissance.md`, and Stage 2 passes 001-005.
- Governance inputs: `MIGRATION.md`, constitution 0.4.0 draft, current status,
  Stage 2 methodology, workbook instructions/tooling, and review protocol.
- Runtime boundary: no authorized CICS, IMS, DB2, 3270, or deployed z/OS
  environment was available; this was the required static Stage 2 control.

## Method

I derived a prospective behavior inventory from every executable/deployable
family, including partial, failure, security, developer, and operator surfaces.
Only after freezing it did I extract the workbook read-only through ExcelJS and
compare all 133 detail scenarios in both directions against source and
reconnaissance. I then rechecked every earlier correction group, validated all
literal evidence paths and line bounds, recomputed generated API references,
verified operation-to-z/OS-asset links, syntax-checked JavaScript executables,
and adversarially swept root and hidden configuration for unmapped behavior.

## Earlier Correction Verification

All earlier corrected areas are present and code-supported:

1. Pass 001: row 11 separates invalid menu values; rows 19-20 separate logout
   retrieval failure and REPL false success; row 26 has the `Error` title; row
   46 marks the old-account helper dormant; rows 67-68 capture IMS ownership
   mismatch and DB2-then-IMS history writes; rows 69-70 split CICS/IMS deposit
   rendering; rows 76-78 capture signed/zero IMS amounts; and rows 110-112
   preserve route-specific account-type mappings.
2. Pass 002/003: row 68's Java evidence path exists; rows 131, 125, and 138
   correctly capture INQACCTY, staged reference data, and static Liberty WAR
   behavior; rows 143-146 cover DBB, Z Code Scan, Wazi Deploy, and native z/OS
   Connect provisioning; reconnaissance preserves the blocked pass and count.
3. Pass 004: rows 87-88 and 95-96 preserve all eight missing generated API
   responses; rows 141-142 cover setup orchestration and prerequisite checks;
   rows 135-136 separate TLS weakness from the fixed-account diagnostic; rows
   147-150 cover secret scan, VSIX tools, zOpenDebug, and TOC tooling.
4. Pass 005: row 150 is `Partial` and accurately states that the TOC generator
   has malformed syntax/indentation and no `generate_toc()` invocation.
   Reconnaissance states the same limitation and names pass 006.

## Findings

### 1. Medium - Deployable management-channel security settings are omitted

Affected scope: `UF-012`; missing security/operational scenarios and missing
reconnaissance security observations.

The workbook records the Java diagnostic's trust-all TLS behavior at row 135,
but two other executable/deployable security surfaces from the frozen inventory
are absent from all 133 scenarios and reconnaissance:

- `legacy/zowe.config.json:4-11` sets the shared Bank of Z Zowe profile to HTTPS
  with `rejectUnauthorized: false`. Setup and pipeline commands use Zowe CLI,
  and the zOpenDebug launch configuration names the same profile hierarchy.
  Certificate verification is therefore explicitly disabled for these
  management connections; it is not covered by the Java-only TLS row.
- `legacy/.setup/zconfig/cics-region.yaml:56-82` provisions the CICS region with
  `securetcpip: "NO"`, resource-security controls including `xtran`, `xcmd`,
  and `xuser` set to `"NO"`, and CMCI `authentication: "NO"` plus `ssl: "NO"`.
  `legacy/.setup/setup/setup-cics-region.sh:125-140` applies this exact file via
  `zconfig apply`, so these are active supplied deployment settings rather than
  unused examples.

Add atomic `Partial` security/operational scenarios for the Zowe certificate-
verification bypass and unauthenticated/non-TLS CICS management configuration,
and add both to reconnaissance and later owner security decisions. Preserve
them as legacy deviations; do not copy them into the target or repair legacy
code during the Stage 1 correction.

## Automated Gates

- Forced inventory: passed; 544 tracked files, 14,370,064 bytes, including all
  95 hidden `.setup` files and 113 files under dot-prefixed paths.
- Workbook extraction/comparison: passed; one sheet, 12 epics, all 133 detail
  scenarios compared.
- Evidence check: passed; 190 literal `legacy/...` references, zero missing
  paths, zero out-of-range line citations; both DB2 JCL wildcard families
  resolve.
- Generated-reference check: reproduced 79 response selections and the eight
  missing sibling responses already mapped; all 13 operation bindings point to
  present z/OS assets.
- JavaScript syntax checks: passed for three VSIX scripts and frontend server.
- TOC source check: confirmed malformed syntax/indentation and only the function
  definition; Python is unavailable on this host, so no runtime compile was run.
- Workbook audit: passed; 133 open rows, 12 epics, `AUDIT OK`.
- Legacy/governed-artifact modification check: passed before report/status edit;
  no legacy, workbook, reconnaissance, SDD, or implementation change.
- `git diff --check`: passed with no output after the report/status edits.

## Conclusion and Next Gate

Result: `findings`. Pass 006 verifies the pass-005 TOC correction and every
earlier corrected area, but the two omitted management-channel security
settings prevent a dry pass. Stage 2 loops back to Stage 1. The primary agent
must add the missing partial scenarios and reconnaissance security observations
without changing legacy or creating SDD/implementation, run the workbook audit
to `AUDIT OK`, and return control to a different fresh agent for pass 007.
