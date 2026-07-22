You are one fresh, independent, read-only reviewer shard for Bank of Z Stage 6 Pass 004.

PACKET ACKNOWLEDGEMENT
- Packet: STAGE6-PASS004-B010
- Immutable revision: aaf050b
- Batch: B010 of 17
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

DECLARED SCOPE — GLOBAL QUALITY, AUTOMATED GATES, AND WORKBOOK VISUALS
1. Review all 27 spec.md/plan.md/tasks.md artifacts across slices 001-009 for internal consistency, dependency order, tests before implementation, categorized real SQL Server integration tests, security, explicit EF migrations/import, Docker Compose delivery, and the mandatory Stage 7-10 per-slice loop.
2. Run and record:
   npm --prefix analysis/tools run audit
   npm --prefix analysis/tools run audit:sdd
   npm --prefix analysis/tools run audit:evidence
3. Independently inspect workbook structure for User Flows and Rev 1: epic typography, lifecycle fills, collapsed detail groups, headers, widths/heights, fonts, and style consistency.
4. For visual inspection, copy analysis/legacy_user_flows.xlsx to a unique OS temp directory, open the copy read-only through installed Microsoft Excel automation, export User Flows and Rev 1 to PDF, and inspect the exported pages with the Read tool. Never save or repair the repository workbook. If rendering or visual reading cannot be completed, return BLOCKED.
5. Confirm the repository workbook hash and git status are unchanged after all checks.