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

## Orchestrated External Review

The primary agent may invoke the independent reviewer directly through an
external CLI under [`../agent_orchestration.md`](../agent_orchestration.md).
Transporting prompts and evidence does not make the primary agent the reviewer.
The external session must still be fresh, eligible, read-only, and isolated
from authoring context.

For an orchestrated pass, durably retain the review packet, checkpoints, and raw
structured responses under `analysis/reviews/evidence/<packet-id>/` after the
reviewer returns them, and record their digests in the immutable report. Record
every reviewer or orchestrator context reset. A missing batch,
lost scope acknowledgement, context overflow, timeout, repository mutation, or
incomplete raw response makes the pass `blocked`. The orchestrator may challenge
a factual finding with evidence, but the reviewer owns the final result and the
orchestrator cannot convert it to `clean`.

For formal passes, the reviewed state is a committed immutable ref in a clean
isolated worktree. The reviewer independently regenerates the diff and scope
from declared revisions. For pre-commit peer review, record the expected dirty
status and diff digest before invocation and require the exact same state after.
