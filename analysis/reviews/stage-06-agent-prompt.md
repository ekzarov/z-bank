# Prompt for the Independent Stage 6 Agent

Work in the `ekzarov/z-bank` repository from the latest `main` commit. Perform
only methodology Stage 6 (independent design re-verification). Do not implement
the modern application and do not repair findings yourself.

Start with `MIGRATION.md` and follow every linked authority. In particular read:

- `.specify/memory/constitution.md`;
- `analysis/migration_status.yaml`;
- Stage 6 in `analysis/migration_methodology.md`;
- `analysis/reviews/README.md` and `review_template.md`;
- `analysis/legacy_user_flows_template_instructions.md`.

Before reading the artifacts under review, evaluate eligibility. If your current
context includes creating or editing the Stage 4 report, Stage 5 report,
`analysis/legacy_user_flows.xlsx`, `analysis/stage-05-sdd-coverage.json`, or any
file under `specs/`, you are not independent. Create the required blocked report
and stop.

If eligible, create `analysis/reviews/stage-06-pass-001.md` from the template and
check all of the following without sampling:

1. For each of the 135 detail rows in `analysis/legacy_user_flows.xlsx`, open
   every cited legacy source under `legacy/` and verify that the requirement,
   expected behavior, `Yes`/`Partial`/`Inferred` classification, and simulation
   label are supported. Narrative legacy documentation is not parity evidence.
2. For every row affected by D-001 through D-015, verify that
   `analysis/stage-04-requirements-revision.md`, Destination notes, the relevant
   `spec.md`, and `specs/traceability.md` agree. Flag any silent behavior loss,
   unapproved deviation, or security defect accidentally preserved.
3. Verify every row maps exactly once through
   `analysis/stage-05-sdd-coverage.json` to one delivery slice and to concrete,
   testable SDD requirements. Confirm all business capabilities are covered and
   only dead/platform-specific mechanics are intentionally not ported.
4. Review all nine `spec.md`, `plan.md`, and `tasks.md` sets for internal
   consistency, dependency order, feasible vertical slicing, tests before
   implementation, real categorized SQL Server integration coverage, security,
   explicit migrations/import, and the mandatory Stage 7-10 per-slice loop.
5. Verify the selected Angular 22 / .NET 10 / EF Core 10 / SQL Server / Docker
   Compose architecture is consistent across the constitution, plans, tasks,
   workbook, status, and `modern/README.md`. Confirm no production code or
   completed task is claimed before Stage 7.
6. Run and record:

   ```text
   npm --prefix analysis/tools run audit
   npm --prefix analysis/tools run audit:sdd
   ```

Record concrete findings with severity, workbook rows, SDD requirements, legacy
file/line evidence, and required correction. Use `findings` for any actionable
issue, `blocked` if the complete sweep cannot be performed, and `clean` only
after all 135 rows and all 27 slice artifacts have been checked.

Append the immutable pass entry to `analysis/migration_status.yaml` and set the
next gate correctly:

- `findings`: return to Stage 5 and name the correction report/action;
- `blocked`: keep Stage 6 blocked and record the prerequisite;
- `clean`: keep implementation approval pending and require explicit owner
  approval before Feature 001 may enter Stage 7.

Commit and push only the review report and status/checkpoint changes on a fresh
review branch. Create a PR if available. Do not merge it and do not write target
implementation code.
