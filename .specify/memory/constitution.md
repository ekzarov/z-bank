<!--
Sync Impact Report
0.3.0: Made independent reviews auditable through per-pass eligibility checks,
immutable reports, and status history; added explicit owner-waiver control for
blocked stages; and required deterministic session handoffs. The ten-stage
manager-facing process and visual diagram are unchanged.

Follow-up before Stage 5 artifacts are final:
- project owner ratifies this constitution;
- target architecture, languages, persistence, authentication, and deployment
  model are selected in an approved planning decision;
- testing tools are selected consistently with that target stack.
-->

# Bank of Z Modernization Constitution

Governs the evidence-led modernization of IBM Bank of Z. It applies to legacy
analysis, SDD artifacts, target implementation, tests, deployment, and parity
verification. A spec or implementation that conflicts with it must be revised
or propose an explicit amendment for owner approval.

Every agent starts with [`MIGRATION.md`](../../MIGRATION.md). That entry point
routes the agent to this constitution, the current stage, the methodology, and
artifact-specific instructions. This constitution remains the highest
repository authority when those documents conflict.

## Core Principles

### I. Constitution-First, SDD-Driven, Approval-Gated

The project follows the ten stages in
[`analysis/migration_methodology.md`](../../analysis/migration_methodology.md):
reconnaissance, independent control, live legacy walkthrough, requirements
revision, SDD, independent design verification, build, delivery, live revision,
and final acceptance. Production code MUST NOT be written before the relevant
SDD exists, independent design verification is clean, and the owner explicitly
approves implementation.

### II. Preserve an Honest Legacy Baseline

The exact upstream legacy source remains under `legacy/` and MUST not be altered
to make parity easier. The repository's Docker Compose starts the frontend and
z/OS Connect Designer, but it does not contain CICS, IMS, or DB2 runtimes.
Therefore agents MUST NOT claim a standalone local legacy system is runnable.
Executable comparison requires an authorized IBM z/OS/Wazi environment (or a
verified compatible environment) with CICS, IMS, DB2, and configured endpoints.
Until that exists, code evidence is authoritative and UI/runtime observations
are marked unverified.

### III. Evidence Before Replacement

Legacy requirements come from source code, screens, API/transaction mappings,
batch jobs, data definitions, deployment descriptors, configuration, and tests.
Project prose and agent memory are supporting context, not proof. The governed
evidence map is
[`analysis/legacy_user_flows.xlsx`](../../analysis/legacy_user_flows.xlsx);
every row cites concrete legacy evidence and marks inference or partial
implementation explicitly. The reusable
[`analysis/legacy_user_flows_template.xlsx`](../../analysis/legacy_user_flows_template.xlsx)
is an empty starting point for new projects and MUST NOT replace the filled
Bank of Z map.

### IV. Business-Flow Parity with Explicit Decisions

The default is observable behavior parity for every workbook row. Dead code,
insecure behavior, unavailable integrations, and intentionally modernized UX
may differ only through an owner-approved decision recorded in both the SDD and
the workbook. No feature is silently dropped and no inferred behavior is
silently promoted to a requirement.

### V. Incremental, Vertically Verifiable Delivery

The target is delivered in small dependency-ordered slices. Each slice includes
its contract/domain behavior, required data changes, user or external-system
surface, automated tests, SDD updates, and workbook updates in one scoped PR.
Large rewrites without per-flow checkpoints are non-compliant.

### VI. Branch-First, Main-Stable Workflow

All scoped work happens on feature branches; `main` remains the integration
baseline. One PR contains one coherent analysis, SDD, or implementation change.
Unrelated refactors and generated churn are excluded.

### VII. Explicit Data Change Control

Normal application startup MUST NOT create, migrate, seed, repair, or otherwise
mutate a database. Schema and reference/demo data changes use versioned migration
artifacts or a documented, explicitly invoked operator command selected by the
approved target architecture. The migration and its verification ship with the
model change. Shared/staging/production application is always deliberate.

### VIII. Automated Verification at the Lowest Useful Level

New or materially changed behavior MUST have automated tests appropriate to the
chosen stack: unit tests for domain/validation/mapping/authorization logic;
integration/contract tests for persistence, APIs, serialization, queues, batch,
and external boundaries; UI tests for user-visible behavior. Tests requiring a
real database, mainframe, browser, or external service are categorized so they
can be selected explicitly. Tool/framework names are decided in the approved
plan after the target stack is chosen.

### IX. Maintainable Domain Vocabulary

Business statuses, account/customer identifiers, transaction types, roles,
configuration keys, limits, and similar values are defined once using the
target stack's typed constants/enums/options mechanisms. Public types live in
clear ownership boundaries and committed code avoids unexplained magic values.

### X. Security Hardening Over Legacy

The target MUST improve insecure legacy behavior and document each intentional
deviation. At minimum: no trust-all TLS or disabled hostname verification; no
placeholder or hard-coded credentials/endpoints in production configuration;
no reversible or weak password storage; least-privilege authorization;
parameterized persistence; secrets outside source; and protected audit data.
The OAuth placeholder and Java trust-all TLS behavior discovered in Stage 1 are
not approved target patterns.

### XI. Workbook and SDD Stay in Lockstep

At SDD time, affected workbook rows receive SDD coverage/deferment and evidence.
At implementation time, destination status and code/test evidence are updated.
Completed tasks are checked in `tasks.md` in the same PR that delivers and tests
them. A green row with an unchecked task, a checked task without delivered code,
or code that contradicts the spec is a defect.

### XII. Auditable Independent Reviews and Handoffs

Stages 2, 6, and 10 require a different agent with fresh context for every
pass. An agent whose current context includes creating or editing an artifact
in scope MUST self-disqualify. Every clean, findings, or blocked pass produces
an immutable report under
[`analysis/reviews/`](../../analysis/reviews/README.md) and an entry in
[`analysis/migration_status.yaml`](../../analysis/migration_status.yaml); chat
history is not evidence of completion.
A blocked stage remains incomplete. Progress beyond it requires an explicit
owner waiver recording approver, date, rationale, and permitted next stage,
while affected workbook rows remain unverified.

## Repository Layout and Decisions

- `legacy/`: immutable IBM Bank of Z source snapshot.
- `modern/`: reserved target implementation root; currently no stack selected.
- `analysis/`: governed workbook, methodology, audit tooling, and analysis notes.
- `MIGRATION.md`: mandatory agent entry point and artifact routing contract.
- `analysis/migration_status.yaml`: current stage, gates, blockers, and next action.
- `.specify/memory/constitution.md`: this constitution.
- `specs/NNN-<slug>/`: Stage 5 feature artifacts; implementation requires the
  Stage 6 clean review and explicit owner approval.

Target language/runtime, frontend, database, messaging, authentication,
deployment topology, and concrete test frameworks are **Pending Decision**.
Agents MUST NOT scaffold or select them implicitly. An approved architecture
decision amends this section before implementation tasks are treated as final.

## Required Workflow

The authoritative entry point is [`MIGRATION.md`](../../MIGRATION.md), and the
canonical stage sequence is
[`analysis/migration_methodology.md`](../../analysis/migration_methodology.md).
Workbook-specific mechanics are governed by
[`analysis/legacy_user_flows_template_instructions.md`](../../analysis/legacy_user_flows_template_instructions.md).

Agents MUST read `analysis/migration_status.yaml`, perform only work allowed by
the active stage, and update that checkpoint when a stage, gate, blocker, or
owner decision changes. An agent MUST NOT mark its own work independently
verified. Before stopping, it records the required artifact and checks, updates
the status without erasing history, and names the next gate. Stages 2, 6, and
10 follow the
[review protocol](../../analysis/reviews/README.md).

## Quality Gates

- Legacy snapshot remains byte-for-byte attributable to its recorded upstream
  commit; analysis and target files do not modify it.
- Every workbook row has concrete evidence or is explicitly marked inferred or
  partial; target/SDD columns are not pre-filled during discovery.
- The workbook audit exits successfully after every workbook edit.
- SDD coverage is traceable to workbook rows before implementation approval.
- New behavior has passing automated tests at the lowest useful level; external
  tests are categorized and their environment prerequisites documented.
- Database mutation never occurs as a side effect of normal startup.
- Security deviations from legacy are explicit and tested.
- A feature is not complete until code, tests, SDD, tasks, and workbook agree.
- Every independent gate has an eligible-agent declaration, immutable report,
  status-history entry, and stage-specific clean result.
- Every transition past a blocked stage has an explicit owner waiver; no waiver
  converts blocked or unverified behavior into completed behavior.

## Governance

This document uses semantic versioning: MAJOR for principle removal or
redefinition, MINOR for a new enforceable principle/section, PATCH for wording
clarification. Amendments state rationale and impact. Ratification and owner
approval cannot be inferred from an agent action or from merge alone.

**Version**: 0.3.0 (Draft) | **Ratified**: Pending owner approval | **Last Amended**: 2026-07-21
