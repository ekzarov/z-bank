# Bank of Z Migration Agent Entry Point

This is the single entry point for any agent working on the Bank of Z
modernization. Start here even when the requested work appears to concern only
analysis, SDD, implementation, testing, deployment, or review.

## Bootstrap

Before changing any file:

1. Read the [project constitution](.specify/memory/constitution.md).
2. Read the [current migration status](analysis/migration_status.yaml) and
   identify the active stage, gate, and next action.
3. Read the applicable stage in the
   [migration methodology](analysis/migration_methodology.md).
4. If the work reads or changes either workbook, read the
   [workbook instructions](analysis/legacy_user_flows_template_instructions.md)
   and the [workbook tooling guide](analysis/tools/README.md).
5. Read only the SDD artifacts under `specs/` that the status file or workbook
   identifies as relevant. If they do not exist yet, do not invent them before
   the methodology reaches Stage 9.
6. Report any conflict, missing prerequisite, or owner gate before crossing it.

If the active stage is Stage 2, preserve independence in this order:

1. Read the constitution, status, methodology, workbook mechanics, and
   [review protocol](analysis/reviews/README.md), but do not initially read the
   filled map or `legacy_reconnaissance.md`.
2. Build a fresh behavior inventory directly from executable artifacts under
   `legacy/`.
3. Only then read the [filled map](analysis/legacy_user_flows.xlsx) and
   [legacy reconnaissance](analysis/legacy_reconnaissance.md), compare them
   with the independent inventory, and write the required review report.

Do not implement production code unless the relevant SDD is complete, the
independent design review is clean, and the owner has explicitly approved the
implementation.

## Sources of Truth

When documents disagree, use this precedence order:

1. The [constitution](.specify/memory/constitution.md) contains the project's
   non-negotiable rules.
2. Explicit, recorded owner decisions resolve business scope and approve gates.
3. The [methodology](analysis/migration_methodology.md) defines stage order,
   actors, gates, loops, and completion criteria.
4. The [workbook instructions](analysis/legacy_user_flows_template_instructions.md)
   define workbook structure, evidence, colors, revision sheets, and audit
   mechanics.
5. Approved SDD under `specs/` defines target behavior for a delivery slice.
6. The filled parity map records the evidence and status of this migration.

Do not silently choose one interpretation when two authoritative artifacts
conflict. Stop, cite both locations, and ask the owner to resolve the conflict.
Until the constitution is explicitly ratified, treat it as the mandatory
working draft; do not finalize SDD or cross an owner gate, and escalate any
conflict to the owner.

## Governed Artifacts

| Artifact | Purpose | May be edited when |
|---|---|---|
| [Filled parity map](analysis/legacy_user_flows.xlsx) | Bank of Z evidence, coverage, parity, and acceptance status | The active methodology stage requires a map update |
| [Empty workbook template](analysis/legacy_user_flows_template.xlsx) | Reusable starting point for a new legacy project | Only when deliberately improving the reusable template |
| [Workbook instructions](analysis/legacy_user_flows_template_instructions.md) | Rules for creating and maintaining parity maps | The workbook protocol itself changes |
| [Methodology](analysis/migration_methodology.md) | Canonical fourteen-stage migration process | The owner approves a process change |
| [Visual methodology](analysis/migration_methodology.html) | Human presentation of the methodology | It must remain synchronized with the Markdown methodology |
| [Migration status](analysis/migration_status.yaml) | Machine-readable checkpoint and next action | At every stage transition or gate decision |
| [Review protocol and records](analysis/reviews/README.md) | Auditable evidence for independent and control passes | Every Stage 2, 7, 10, or 14 pass, including clean and blocked passes |
| [Cross-agent orchestration](analysis/agent_orchestration.md) | Direct read-only review through an external agent CLI, including context checkpoints | Whenever agents review or challenge one another without owner message relay |
| `analysis/prototyping/` | Prototype record: Stage 5 form/style/palette decision (`decision.md`), exported wireframe catalog (`wireframes/`), screen manifest mapping screen → workbook rows → roles → states with export hashes (`screen-manifest.json`), and Stage 8 owner approval with the approved version/hash (`approval.md`) | During Stages 5-8 and when later SDD work changes the approved UI |
| [Target surface inventory](analysis/inventories/target-surface-inventory.json) | Every shipped route, role-visible destination, useful action, and its SDD/code/test evidence | Stage 9 design and every Stage 11-14 delivery loop |
| [Legacy reconnaissance](analysis/legacy_reconnaissance.md) | Stage 1 handoff and evidence boundary | New legacy evidence changes the handoff |
| [Legacy deployment overview](analysis/cobol_deployment_overview.md) | What can and cannot run without IBM infrastructure | Runtime prerequisites or deployment knowledge changes |
| [Constitution](.specify/memory/constitution.md) | Project governance and non-negotiable constraints | Through an explicit, documented amendment |
| `specs/NNN-<slug>/` | Approved feature requirements, plans, and tasks | During Stages 9-11 and later reconciliation loops |

Never replace the filled parity map with the empty template. The template has
no Bank of Z evidence; the filled workbook is the governed project record.

## Agent Operating Contract

- Work on a feature branch and keep `main` stable.
- Derive legacy behavior from code and executable artifacts. Narrative legacy
  documentation may guide discovery but is not parity evidence.
- Record uncertainty as inferred, partial, or unverified; never promote it to a
  proven requirement without evidence or an owner decision.
- If a complete Stage 3 live walkthrough cannot be performed, stop and ask the
  owner to choose `simulate` or `waive`. Do not choose silently and do not build
  mocks before that decision. Record the mode, rationale, approver, date, scope,
  and residual risk in `analysis/migration_status.yaml`.
- Label mock/emulator results as simulated evidence. They may unblock design but
  never prove real legacy runtime behavior; affected workbook rows remain
  unverified until a real walkthrough confirms them.
- A primary agent does not sign off its own control, design, or final review.
  Independent stages require a different agent with fresh context for every
  pass. If the agent's current context includes creating or editing an artifact
  in the review scope, it MUST self-disqualify, stop, and tell the owner that a
  fresh agent is required.
- Every Stage 2, 7, 10, or 14 pass writes an immutable report under
  `analysis/reviews/`, even when its result is clean or blocked. Chat history is
  not sufficient evidence that a pass occurred. Reports written before the
  2026-07-24 renumbering keep their original stage-numbered file names (see the
  methodology's numbering note).
- The primary agent MAY communicate directly with an external agent CLI under
  the [cross-agent orchestration protocol](analysis/agent_orchestration.md).
  The external reviewer remains read-only; the primary agent verifies every
  finding, and no model consensus replaces evidence or an owner gate.
- Large external reviews MUST use deterministic batches and context
  checkpoints. Context overflow, a lost scope acknowledgement, a timeout, or a
  missing batch makes the review `blocked`; it can never be interpreted as a
  partial `clean` result.
- Every blocked attempt remains in history and its exact scope MUST be covered
  by later eligible fresh sessions. A formal pass cannot close while
  `unresolved_blocked_scopes` is non-zero. The immutable report summarizes each
  external finding, the primary agent's accepted/rejected disposition, the
  correction, and the repeat-review outcome.
- Keep code, tests, SDD, tasks, and the filled workbook synchronized in the
  same implementation PR.
- A route, menu item, role workspace, screen, API operation, or job is not
  complete merely because it exists, returns success, or shows the expected
  heading. Every shipped surface MUST appear in
  `analysis/inventories/target-surface-inventory.json` with at least one concrete useful
  action or observable contract and SDD, code, and automated-test evidence.
  Test evidence MUST identify the concrete test name, not merely a test file.
  Its title MUST include `@surface:<id>` and `@role:<role>` bindings. These
  bindings are structural evidence only; Stage 13/14 still executes and
  independently checks the useful action.
  Visible placeholders, "coming soon" destinations, and deferred surfaces left
  in navigation fail the target-surface gate.
- Stage 13 MUST exercise every visible destination for every applicable role
  and demonstrate its useful action, not only authentication and navigation.
  Stage 14 reviewers receive the target-surface inventory and fail closed when
  a surface is missing, placeholder-only, or supported only by a title/HTTP
  status assertion.
- Starting at Stage 11, deliver iteratively. Select one approved feature or a
  small, tightly related group of features as a delivery slice, then take only
  that slice through Stage 11 build, Stage 12 delivery, Stage 13 live revision,
  and Stage 14 slice acceptance. Findings return the slice to Stage 9 (or to
  Stage 1 when the legacy map is wrong); an accepted slice releases the next
  slice. Do not implement the entire approved backlog in one batch. After all
  slices are accepted, run the consolidated Stage 14 final acceptance.
- Run every automated gate named by the active stage. After any filled-workbook
  edit, run `npm --prefix analysis/tools run audit` and require `AUDIT OK`.
  After target routes, navigation, roles, or user-facing screens change, run
  `npm --prefix analysis/tools run audit:target` and require
  `TARGET SURFACE AUDIT OK`.
- Update `analysis/migration_status.yaml` when a stage, gate, blocker, or owner
  decision changes. Do not mark approval merely because code was merged.
- Stop at owner gates. State what was completed, show evidence, and name the
  exact approval needed to continue.

## Before Stopping

Before ending any working session:

1. Write or update the artifact required by the active stage. A control pass
   must produce its review report.
2. Run the applicable automated gates and record their exact results.
3. Update `analysis/migration_status.yaml` with the pass history, current gate,
   blocker or waiver, durable progress estimate, external-review counters,
   unresolved blocked scopes, and next action. Do not erase prior history.
4. Confirm that no uncommitted claim of completion exists only in chat.
5. Tell the owner what changed, what remains open, which gate stopped progress,
   and the exact action or approval needed next.
6. Consolidate external-agent evidence under
   `analysis/reviews/evidence/<packet-id>/`, then remove clean review worktrees
   and disposable sibling scratch/evidence directories. Do not leave duplicate
   repository checkouts, renders, dependency folders, or raw temporary files
   beside the project.

## Current Project

The current stage and next action are deliberately not duplicated here. Read
[analysis/migration_status.yaml](analysis/migration_status.yaml), which is the
only current-state checkpoint.

The legacy snapshot is under `legacy/`; the future implementation belongs
under `modern/`. Full legacy execution requires authorized IBM infrastructure,
as documented in the
[COBOL deployment overview](analysis/cobol_deployment_overview.md).
