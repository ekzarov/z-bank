You are one fresh, independent, read-only reviewer shard for Bank of Z Stage 6 Pass 004.

PACKET ACKNOWLEDGEMENT
- Packet: STAGE6-PASS004-B003A
- Immutable revision: aaf050b
- Batch: B003A of 17
- Worktree: C:\Work\Legacy\z-bank-pass004-review

Before substantive work, echo the packet, revision, batch, and whether you are eligible. You are eligible only if this fresh session has no prior creation or editing of the parity map, Stage 4/5 artifacts, specs, methodology, or earlier Stage 6 reviews. If ineligible, return BLOCKED immediately.

SAFETY
- Read only. Do not edit, create, delete, stage, commit, push, open a PR, or mutate any repository file.
- Temporary render/output files may be created only below the OS temp directory, never in the repository.
- NODE_PATH already points to the external dependency store. Never run npm install, npm ci, or any command that writes dependencies or caches inside the repository. Verify ExcelJS with node -p "require.resolve('exceljs')"; return BLOCKED if it is unavailable.
- Do not read Stage 6 Pass 001-003 before forming your own inventory and completing this batch's substantive checks.
- Do not claim full Pass 004 completion; you own only this declared batch.
- Verify every declared row/artifact without sampling. Missing access, timeout, lost context, or incomplete scope is BLOCKED.
- Run git status --porcelain=v1 --untracked-files=all --ignored before and after. Any tracked, untracked, or ignored repository entry is BLOCKED.

AUTHORITIES
Start with MIGRATION.md, .specify/memory/constitution.md, analysis/migration_status.yaml, Stage 6 in analysis/migration_methodology.md, analysis/agent_orchestration.md, analysis/reviews/README.md, and analysis/legacy_user_flows_template_instructions.md.

OUTPUT
Return Markdown with: acknowledgement and eligibility; exact scope; method; rows/artifacts/decisions checked; concrete findings with severity and file/line/row evidence; commands and results; repository status before/after; completed and remaining scope; and exactly one final result CLEAN, FINDINGS, or BLOCKED. CLEAN means only that this batch is complete and has no finding.

DECLARED SCOPE — SLICE 003 account-management
- Workbook sheet User Flows rows (Excel row numbers): 41, 42, 43, 44
- SDD artifacts: specs/003-account-management/spec.md, plan.md, tasks.md
- Cross-artifacts: analysis/stage-04-requirements-revision.md, analysis/stage-05-sdd-coverage.json, specs/traceability.md

REQUIRED COMPLETE CHECK
1. Use ExcelJS from analysis/tools/node_modules to independently extract the header and every declared workbook row. Do not rely on a packet transcription of row content.
2. For each row, parse every Source code evidence segment and open every cited legacy file/range. Verify the functional requirement, business description, expected user-visible result, Source implemented classification (Yes/Partial/Inferred), runtime/simulation label, and negative evidence against actual source semantics. Do not merely check path existence.
3. Trace each row to concrete requirements in the slice spec, architecture in plan, implementation/test tasks, Stage 4 decisions, coverage JSON, and specs/traceability.md. Verify Destination notes and every applicable D-001 through D-015 decision agree.
4. Review all three SDD artifacts in full for contradictions, missing edge cases, security problems, infeasible dependencies, tests-before-implementation, and exact coverage of this batch's rows.
5. Report each row number explicitly as checked. If one row or evidence segment cannot be semantically verified, return BLOCKED or FINDINGS as appropriate.

This batch does not own workbook rows outside the declared list.