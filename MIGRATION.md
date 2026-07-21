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
   the methodology reaches Stage 5.
6. Report any conflict, missing prerequisite, or owner gate before crossing it.

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

## Governed Artifacts

| Artifact | Purpose | May be edited when |
|---|---|---|
| [Filled parity map](analysis/legacy_user_flows.xlsx) | Bank of Z evidence, coverage, parity, and acceptance status | The active methodology stage requires a map update |
| [Empty workbook template](analysis/legacy_user_flows_template.xlsx) | Reusable starting point for a new legacy project | Only when deliberately improving the reusable template |
| [Workbook instructions](analysis/legacy_user_flows_template_instructions.md) | Rules for creating and maintaining parity maps | The workbook protocol itself changes |
| [Methodology](analysis/migration_methodology.md) | Canonical ten-stage migration process | The owner approves a process change |
| [Visual methodology](analysis/migration_methodology.html) | Human presentation of the methodology | It must remain synchronized with the Markdown methodology |
| [Migration status](analysis/migration_status.yaml) | Machine-readable checkpoint and next action | At every stage transition or gate decision |
| [Legacy reconnaissance](analysis/legacy_reconnaissance.md) | Stage 1 handoff and evidence boundary | New legacy evidence changes the handoff |
| [Legacy deployment overview](analysis/cobol_deployment_overview.md) | What can and cannot run without IBM infrastructure | Runtime prerequisites or deployment knowledge changes |
| [Constitution](.specify/memory/constitution.md) | Project governance and non-negotiable constraints | Through an explicit, documented amendment |
| `specs/NNN-<slug>/` | Approved feature requirements, plans, and tasks | During Stages 5-7 and later reconciliation loops |

Never replace the filled parity map with the empty template. The template has
no Bank of Z evidence; the filled workbook is the governed project record.

## Agent Operating Contract

- Work on a feature branch and keep `main` stable.
- Derive legacy behavior from code and executable artifacts. Narrative legacy
  documentation may guide discovery but is not parity evidence.
- Record uncertainty as inferred, partial, or unverified; never promote it to a
  proven requirement without evidence or an owner decision.
- A primary agent does not sign off its own control, design, or final review.
  Independent stages require a different agent with fresh context.
- Keep code, tests, SDD, tasks, and the filled workbook synchronized in the
  same implementation PR.
- Run every automated gate named by the active stage. After any filled-workbook
  edit, run `npm --prefix analysis/tools run audit` and require `AUDIT OK`.
- Update `analysis/migration_status.yaml` when a stage, gate, blocker, or owner
  decision changes. Do not mark approval merely because code was merged.
- Stop at owner gates. State what was completed, show evidence, and name the
  exact approval needed to continue.

## Current Project

The current stage and next action are deliberately not duplicated here. Read
[analysis/migration_status.yaml](analysis/migration_status.yaml), which is the
only current-state checkpoint.

The legacy snapshot is under `legacy/`; the future implementation belongs
under `modern/`. Full legacy execution requires authorized IBM infrastructure,
as documented in the
[COBOL deployment overview](analysis/cobol_deployment_overview.md).

