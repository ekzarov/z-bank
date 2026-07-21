# Migration Review Records

This directory contains the auditable output of methodology control passes.
Every pass leaves a report, including a clean or blocked pass. Chat text alone
is not a review record.

Start from [`../../MIGRATION.md`](../../MIGRATION.md), then copy
[`review_template.md`](review_template.md) to the next path declared in
[`../migration_status.yaml`](../migration_status.yaml).

## Naming

Use `stage-NN-pass-NNN.md`, for example:

- `stage-02-pass-001.md` for control reconnaissance;
- `stage-06-pass-001.md` for independent design verification;
- `stage-10-pass-001.md` for independent final acceptance.

Pass numbers are monotonically increasing within a stage. Never overwrite or
renumber an earlier report.

## Results

Each report has exactly one result:

- `clean`: the declared scope was fully checked and no new finding was found;
- `findings`: one or more actionable findings were recorded;
- `blocked`: the declared scope could not be completed, with prerequisites
  recorded in the report.

After writing the report, append the pass to `review_passes` in
`analysis/migration_status.yaml` and set the next action. A `clean` report does
not advance the stage unless every stage-specific gate also passes.

## Independence

Stages 2, 6, and 10 require a fresh independent agent for every pass. Before
reading the artifact under review, the agent must complete the eligibility
declaration in the report. If its current context contains creation or editing
of any artifact in scope, it is disqualified and must stop without performing
the review.

