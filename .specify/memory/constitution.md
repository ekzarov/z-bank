<!--
Sync Impact Report
0.7.0: Added target-surface completeness, useful-action evidence, placeholder
rejection, and role-by-role live click-through as hard acceptance gates.
0.6.1: Clarified self-contained reviewer packets and scoped retries after
orchestration-environment failures.
0.6.0: Added durable progress reporting, mandatory blocked-scope closure, and
manager-facing cross-agent finding/disposition logs.
0.5.0: Added owner-approved direct cross-agent orchestration, read-only external
review, deterministic context checkpoints, and fail-closed context handling.
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
Starting at Stage 7, one approved feature or a small, tightly related feature
group MUST pass through Stage 7 build, Stage 8 delivery, Stage 9 live revision,
and Stage 10 slice acceptance before the next slice starts. Findings return to
Stage 5 for SDD/task correction and re-verification, or to Stage 1 when the
legacy map is wrong, before that slice re-enters Stage 7. Implementing the
entire approved backlog in one batch or postponing verification until the end
is non-compliant. After every slice is accepted, Stage 10 performs one final
consolidated acceptance across the complete migrated system.

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
The primary agent MAY orchestrate an eligible external agent through its CLI
without owner message relay, but MUST follow
[`analysis/agent_orchestration.md`](../../analysis/agent_orchestration.md).
The reviewer is read-only and owns its conclusion; the primary agent validates
findings but MUST NOT rewrite `findings` or `blocked` as `clean`. Large reviews
MUST use deterministic batches and checkpoints. Context overflow, timeout,
missing scope acknowledgement, repository mutation, or an incomplete batch
MUST fail closed as `blocked`. Agreement between agents never replaces tests,
legacy evidence, or an owner gate.
Review packets MUST explicitly provide required runtime and dependency paths;
they cannot assume the reviewer inherits the orchestrator's shell environment.
After an orchestration-environment defect is corrected, an eligible fresh
session repeats the exact uncovered scope while immutable completed batches
remain valid unless their inputs changed.
Every blocked attempt remains immutable and records its exact lost scope. That
scope MUST be fully covered by later eligible fresh sessions, and a formal pass
MUST NOT close while `unresolved_blocked_scopes` is non-zero. The durable report
summarizes each external finding, the primary agent's evidence-based
accepted/rejected disposition, the correction, and the repeat-review result.
The same context checkpoint rule applies when the primary orchestrator compacts
or restarts. Review packets MUST NOT transmit credentials, personal data,
regulated data, or repository content to a service that the owner has not
authorized for that classification.
A blocked Stage 3 remains incomplete. If a complete live walkthrough cannot be
performed, the agent MUST ask the owner to choose traceable simulation or an
explicit waiver. The decision records approver, date, rationale, scope,
residual risk, and permitted next stage. Mock or emulator evidence MUST be
labeled simulated and MUST cite its legacy evidence basis. Neither simulation
nor waiver verifies the real legacy runtime; affected workbook rows remain
unverified until a real walkthrough confirms them.

### XIII. Every Shipped Surface Must Be Useful

A route, navigation item, role workspace, screen, API operation, or job is not
implemented merely because it exists, returns success, enforces authorization,
or displays the expected title. Every shipped surface MUST be recorded in
`analysis/target-surface-inventory.json` with at least one concrete useful
action or observable contract and traceable SDD, code, and automated-test
evidence. Automated evidence MUST name the concrete test case after `#`; a
reference to a test file alone is not proof of the action. Test titles MUST bind
their evidence to `@surface:<id>` and `@role:<role>`. These tags provide
structural traceability but do not prove semantics, so Stage 9/10 MUST still
execute the action. When a workbook row bundles several observable outcomes,
every outcome MUST be evidenced or explicitly split/deferred before the row
becomes green.

Every role exposed by the target MUST have at least one owner-approved useful
action. A target-only role or surface cannot derive business meaning from a
similarly named legacy file. Visible placeholders, generic empty workspaces,
"coming soon" text, and future-slice destinations are release-blocking gaps.
An approved deferred surface MUST be hidden from production navigation.

Stage 9 walks every visible destination for every applicable role and performs
the listed useful action. Stage 10 independently reconciles deployed
routes/navigation with the target-surface inventory. Login, `200 OK`, access
probes, route guards, and heading-only assertions cannot by themselves close a
surface.

## Repository Layout and Decisions

- `legacy/`: immutable IBM Bank of Z source snapshot.
- `modern/backend/`: .NET 10 LTS ASP.NET Core Web API, application/domain code,
  EF Core 10 persistence, versioned migrations, and xUnit test projects.
- `modern/frontend/`: Angular 22 standalone application with the Angular CLI
  application builder, Vitest tests, and Playwright end-to-end tests.
- `analysis/`: governed workbook, methodology, audit tooling, and analysis notes.
- `MIGRATION.md`: mandatory agent entry point and artifact routing contract.
- `analysis/migration_status.yaml`: current stage, gates, blockers, and next action.
- `.specify/memory/constitution.md`: this constitution.
- `specs/NNN-<slug>/`: Stage 5 feature artifacts; implementation requires the
  Stage 6 clean review and explicit owner approval.

The owner-approved target uses .NET 10 LTS, ASP.NET Core Web API, EF Core 10,
SQL Server, and Angular 22. The browser and API use secure same-origin HTTP-only
cookie sessions with CSRF protection and explicit customer/operator/admin
authorization policies. CICS and IMS business concepts are unified in one
domain model; `SourceSystem` records provenance without recreating separate
runtime channels. Deployment uses Docker Compose with independently versioned
API, Angular/nginx, and SQL Server services. Schema and demo/reference data are
applied only by explicit migration/operator commands, never at application
startup.

Backend unit tests use xUnit; API tests use `WebApplicationFactory`; database
integration tests run against a real categorized SQL Server test database.
Angular unit/component tests use the Angular CLI's Vitest setup. Playwright
covers critical cross-application happy paths. Concrete package patch versions
are pinned when each delivery slice enters implementation and may move only
within the approved major versions through a tested dependency update.

## Required Workflow

The authoritative entry point is [`MIGRATION.md`](../../MIGRATION.md), and the
canonical stage sequence is
[`analysis/migration_methodology.md`](../../analysis/migration_methodology.md).
Workbook-specific mechanics are governed by
[`analysis/legacy_user_flows_template_instructions.md`](../../analysis/legacy_user_flows_template_instructions.md).

Agents MUST read `analysis/migration_status.yaml`, perform only work allowed by
the active stage, and update that checkpoint when a stage, gate, blocker, or
owner decision changes. Its progress estimates and external-review counters
are updated only at durable checkpoints and never substitute for gates. An
agent MUST NOT mark its own work independently
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
- Every visible target surface passes the target-surface audit and has
  useful-action evidence; no placeholder destination or actionless role is
  accepted.
- Each implementation slice receives a fresh read-only external peer review
  after tests pass. Findings are independently validated by the primary agent;
  no more than two discussion rounds are allowed, and unresolved material
  findings block delivery.
- A delivery slice is not complete until it has passed Stages 7-10; the next
  slice does not start before that acceptance is recorded.
- Every independent gate has an eligible-agent declaration, immutable report,
  status-history entry, and stage-specific clean result.
- Every transition past a blocked Stage 3 has an explicit owner fallback
  decision; neither simulation nor waiver converts unobserved real behavior
  into completed or live-verified behavior.

## Governance

This document uses semantic versioning: MAJOR for principle removal or
redefinition, MINOR for a new enforceable principle/section, PATCH for wording
clarification. Amendments state rationale and impact. Ratification and owner
approval cannot be inferred from an agent action or from merge alone.

**Version**: 0.6.1 | **Ratified**: 2026-07-21 by project owner | **Last Amended**: 2026-07-22
