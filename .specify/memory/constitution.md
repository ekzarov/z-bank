<!--
Sync Impact Report
0.1.0: Replaced the copied XPlanner-specific constitution with a Bank of Z
modernization draft. Removed unapproved .NET/Angular/EF/SQL Server choices and
paths. Preserved evidence-first, SDD-first, approval-gated delivery, explicit
data change control, automated verification, security hardening, and workbook
traceability. Adapted the legacy-baseline rule to the actual IBM infrastructure
dependency: the repository does not contain standalone CICS/IMS/DB2 runtimes.

Follow-up before Stage 2 artifacts are final:
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

## Core Principles

### I. Constitution-First, SDD-Driven, Approval-Gated

The project proceeds in this order: legacy evidence map, owner-reviewed SDD
(`spec.md` -> clarification -> `plan.md` -> `tasks.md`), implementation, parity
verification, and owner acceptance. Production code MUST NOT be written before
the relevant SDD exists and the owner explicitly approves implementation.

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
evidence map is `analysis/legacy_user_flows.xlsx`; every row cites concrete
legacy evidence and marks inference or partial implementation explicitly.

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

## Repository Layout and Decisions

- `legacy/`: immutable IBM Bank of Z source snapshot.
- `modern/`: reserved target implementation root; currently no stack selected.
- `analysis/`: governed workbook, methodology, audit tooling, and analysis notes.
- `.specify/memory/constitution.md`: this constitution.
- `specs/NNN-<slug>/`: Stage 2 feature artifacts after owner approval.

Target language/runtime, frontend, database, messaging, authentication,
deployment topology, and concrete test frameworks are **Pending Decision**.
Agents MUST NOT scaffold or select them implicitly. An approved architecture
decision amends this section before implementation tasks are treated as final.

## Required Workflow

The authoritative operating procedure is
`analysis/legacy_user_flows_template_instructions.md`:

1. Derive legacy behavior from code and executable artifacts.
2. Build and adversarially re-audit the parity map.
3. Ratify this constitution and create/clarify/plan/task SDD from the map.
4. Obtain explicit owner approval before implementation.
5. Implement a small slice with required automated tests.
6. Update SDD and workbook in the same PR.
7. Compare legacy and target through available APIs/UI/runtime and record proof.
8. Obtain independent final review and owner acceptance.

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

## Governance

This document uses semantic versioning: MAJOR for principle removal or
redefinition, MINOR for a new enforceable principle/section, PATCH for wording
clarification. Amendments state rationale and impact. Ratification and owner
approval cannot be inferred from an agent action or from merge alone.

**Version**: 0.1.0 (Draft) | **Ratified**: Pending owner approval | **Last Amended**: 2026-07-15
