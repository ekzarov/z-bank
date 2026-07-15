<!--
Sync Impact Report
1.4.0 amendment: §VII now states explicitly that authoring a migration and applying
it are two distinct steps — authoring an EF migration is MANDATORY in the same PR as
any model/DbContext change (verifiable via `has-pending-model-changes`), while
applying is the explicit `--migrate` command and never happens at startup. Resolves
ambiguity about when a migration is required vs why a table is "missing" (authored
but not applied). Also adds the agent expectation: after authoring a migration, the
agent applies it to the local dev DB (via `--migrate`) in the same step so the owner
can see and test the change immediately (explicit command, not startup mutation).
MINOR (new enforceable acceptance rule). Operating procedure
(legacy_user_flows_template_instructions.md) updated to match.
1.3.2 amendment (clarification): Development Workflow now names the authoritative
operating procedure — analysis/legacy_user_flows_template_instructions.md (Required
Workflow) — so agents know which step-by-step instructions govern the work.
1.3.1 amendment (clarification): §XI + Quality Gates — keep specs/NNN-*/tasks.md
checkboxes in sync with implementation/workbook-green status (tick [x] in the same
change; deferred tasks stay [ ]).
1.3.0 amendment: strengthen §VIII into a hard gate — backend unit tests are
mandatory and use xUnit, live in the solution test project (tests/XPlanner.Tests),
and a backend PR is not accepted without them + green `dotnet test`. Added the same
gate to Quality Gates. MINOR (new enforceable acceptance rule).
1.2.2 amendment (clarification): primary dev loop is local — backend via `dotnet run`
against a local SQL Server instance, frontend via `ng serve`; docker-compose is an
optional one-command full-stack run. No stack/engine change (still SQL Server behind
EF Core); schema/seed still applied via the explicit `--migrate` command (§VII).
1.2.1 amendment (clarification): add milestone-based delivery to the Development
Workflow — implementation proceeds in hybrid backend-then-UI milestones, the first
of which is an integrated "walking skeleton" (login + one protected screen running
end-to-end). The implementation-order source of truth is specs/ROADMAP.md. No
principle changed; this expands §V's incremental model with the milestone cadence.
Version change: 1.1.0 -> 1.2.0
1.2.0 amendment: consolidate ALL concrete technology choices into a single
"Technology Stack & Tooling" section (renamed from Technical Constraints) so they
are isolated and easy to change/remove; add the data layer explicitly — EF Core
latest (EF Core 10) as ORM with explicit migrations, default DB engine SQL Server
(swappable behind EF Core); keep Core Principles technology-neutral (removed
EF-Core-specific wording from §VII/§VIII).
1.1.0 amendment: pin the target technology stack and the (manually pre-created)
project layout — backend .NET 10 at backend/, frontend Angular 19 at frontend/,
legacy reference at legacy/; agents must not scaffold or re-version these without
an amendment.
Rationale (1.0.0): initial XPlanner migration constitution, seeded from the
PetStore migration constitution and adapted to the XPlanner+ -> .NET 10 /
Angular 19 work.
Principles carried over / adapted:
- Runnable legacy baseline, business-flow parity, evidence-before-replacement,
  incremental migration, branch-first workflow, automated verification,
  explicit database change control (no startup seeding).
Principles added for this project:
- Spec-driven & constitution-first; clean code / no magic values; security
  hardening over legacy; workbook-driven coverage tracking.
Templates requiring updates:
- TODO: add .specify/templates/{spec,plan,tasks}-template.md if/when adopted.
Follow-up items:
- Ratification by the project owner pending.
-->

# XPlanner Migration Constitution

Governs the migration of legacy **XPlanner+ v1.1a4** (Java / Struts / Hibernate)
to the new stack (**.NET 10 API + Angular 19 SPA**). It is authoritative: every
spec, plan, task, and implementation must comply or explicitly propose an
amendment.

## Core Principles

### I. Constitution-First, Spec-Driven, Approval-Gated
This constitution is ratified before specs/plans/tasks are accepted as final, and
no production code is written before the relevant `spec.md` (+ `plan.md` /
`tasks.md`) exists and the owner has explicitly approved moving to
implementation. Specs describe behavior, not code.

### II. Runnable Legacy Baseline
The legacy app MUST stay runnable via `legacy/docker-compose.yml` (Tomcat 9 /
JRE 8 / embedded HSQLDB) throughout the migration, so behavior can be observed
and compared. A change that breaks `docker compose up` for the legacy baseline is
non-compliant unless it explicitly replaces that baseline with a verified
equivalent.

### III. Evidence Before Replacement
Legacy behavior is captured as evidence before a .NET/Angular equivalent is
built. The source of truth for legacy behavior is
`analysis/legacy_user_flows.xlsx` (epics → flows → scenarios, with legacy code
evidence), not the code directly and not memory. Specs trace each requirement
back to workbook flows/rows.

### IV. Business-Flow Parity (with documented deviations)
Default goal is behavior parity for every workbook row. Intentional divergences
(security fixes, removed dead/debug code, modernized UX) MUST be called out in the
spec as a **Deviation** with rationale and recorded in the workbook destination
notes. They are never silent.

### V. Incremental Migration Over Big Bang
The target system is delivered in small, independently testable slices — features
in implementation/dependency order, processed in small batches (≈3 features per
batch). No whole-system rewrite without per-feature parity checkpoints.

### VI. Branch-First, main-Stable Workflow
All work happens on feature branches; `main` stays the usable integration
baseline. One PR per scoped change; keep unrelated refactors, generated churn,
and exploratory edits out of the scoped PR. (This repo's default branch is
`main`.)

### VII. Explicit Database Change Control (no startup mutation)
The .NET app MUST NOT create, migrate, seed, or otherwise mutate the database
during normal application startup. Schema changes and reference/demo/seed data
(including the initial roles and sysadmin account) MUST be applied via explicit
database migrations or an explicitly invoked operator/developer command (see
Technology Stack & Tooling). Startup may validate required configuration and fail
fast, but MUST NOT repair or populate the database implicitly (no auto-migrate /
ensure-created / hosted seed services at boot).

**Authoring a migration and applying it are two distinct steps — do not conflate
them:**

1. **Author (ALWAYS, in the same PR as the change).** Any change to the EF model
   or `DbContext` — a new/changed/removed entity, property, key, index, column
   facet, or relationship — MUST ship a generated EF Core migration committed in
   the **same PR**. The model snapshot and the migrations must stay in sync; a PR
   that changes the model without its migration is incomplete. This is
   non-optional and not a judgement call: if `DbContext`/an entity changed, run
   `dotnet ef migrations add <Name>` and commit the result. (Verifiable:
   `dotnet ef migrations has-pending-model-changes` reports none.)
2. **Apply (EXPLICIT, never at startup).** A migration only reaches a database
   when an operator/developer runs the explicit command
   `dotnet run --project backend -- --migrate` (which runs `MigrateAsync` + seed),
   never during normal app startup. **Authoring ≠ applying:** after pulling
   changes that add migrations, the new tables/columns do **not** exist in a
   database until `--migrate` is run once. If a table looks "missing," the
   migration was almost certainly authored but not yet applied — run `--migrate`,
   don't re-add the migration.
   - **Agent expectation:** whenever an agent authors a migration, it MUST also
     apply it to the **local development database** in the same step (run
     `--migrate`) so the schema change is immediately visible and testable by the
     owner. This is the explicit operator command — it is *not* startup mutation,
     so it does not violate the no-startup-mutation rule above. (Applying to
     shared/staging/production databases remains a deliberate, separately-run
     operator action, never automatic.)

### VIII. Automated Verification at the Lowest Useful Level
New or materially changed backend behavior MUST ship automated tests: unit tests
for pure domain/validation/mapping/permission logic; integration/contract tests
for database/ORM, API contract, serialization, and configuration boundaries. Tests needing a real database or external dependency MUST be
categorized so they can be included/excluded explicitly. Frontend features MUST
have component/service specs for their user-visible behavior. Any deferral is
documented in the spec/plan with a follow-up task and rationale.

**Backend unit tests are mandatory and use xUnit.** They live in the solution's
test project (`tests/XPlanner.Tests`, referenced by `XPlanner.slnx`). A pull request
that adds or changes backend code is **not accepted** unless it includes xUnit unit
tests covering the new/changed logic and `dotnet test` is green. This is a hard
gate, not advisory: the only exception is a PR that touches no backend logic
(docs/specs/workbook/frontend-only), and any rare deferral must be called out
explicitly in the PR for the owner to accept.

### IX. Clean Code, No Magic Values
No magic strings or numbers in committed code. Permission names, resource-type
keys, role names, status/disposition values, config keys, and similar vocabulary
MUST be defined once as named constants / enums / strongly-typed options and
reused. Shared domain vocabulary (e.g. the legacy permission and resource-type
strings) lives in a single typed source, not scattered literals.

### X. Security Hardening Over Legacy
Where legacy is insecure, the target MUST improve it and document the deviation:
passwords stored only as salted strong hashes (never MD5, never reversible);
no password or reversible secret in any cookie; default/admin credentials never
displayed in the UI; parameterized queries only; secrets from configuration, not
source. Such deviations are recorded against the relevant workbook rows.

### XI. Workbook-Driven Coverage Tracking
Each spec sets the workbook SDD columns for its rows (`Covered in SDD? = Yes`, or
`Deferred in SDD? = Yes` with reason; `SDD evidence` = spec path). Row color
follows: red `Not Passed - Missed` → orange `Not Passed - Deferred/Partial` once
covered/deferred by a spec → green `Passed` once the target implements it with
passing tests and parity (or the documented deviation) holds. Destination columns
are filled at implementation time.

**Task-list sync.** Whenever a flow is implemented and greened in the workbook, the
corresponding `specs/NNN-*/tasks.md` checkbox(es) MUST be ticked (`[x]`) in the same
change. `tasks.md` always reflects actual implementation status — a green workbook
row with an unchecked task, or a checked task without delivered+tested code, is a
defect to fix. Deferred tasks stay `[ ]` with their deferral noted.

## Technology Stack & Tooling (technical details — isolated; change here)

> ALL concrete technology choices live in this single section so they can be
> changed, swapped, or removed in one place. The Core Principles above are kept
> technology-neutral; when a principle needs a specific tool/version, it points
> here. The target projects already **exist (created manually)** — agents work
> *inside* them and MUST NOT scaffold new projects, relocate them, or change the
> stack/major versions without a constitution amendment.

### Project layout
- **Backend:** ASP.NET Core Web API on **.NET 10** (`net10.0`), at **`backend/`**
  (project `backend/XPlanner.csproj`).
- **Frontend:** **Angular 19** SPA (project `xplanner-frontend`), at
  **`frontend/`**.
- **Legacy reference:** original XPlanner+ under **`legacy/`** (Tomcat 9 / JRE 8 /
  HSQLDB via `legacy/docker-compose.yml`), kept runnable per Principle II.
- **SDD artifacts:** specs at `specs/NNN-<slug>/`; this constitution at
  `.specify/memory/constitution.md`.

### Data & persistence
- **ORM / data access:** **Entity Framework Core — latest (EF Core 10, aligned
  with .NET 10)**, code-first with **explicit migrations**.
- **Database engine (default):** **Microsoft SQL Server** (SQL Server / LocalDB
  for local development). The engine is isolated behind EF Core and can be
  swapped by changing the provider here (e.g. **PostgreSQL** via Npgsql) — this
  is the one place to change it. *(confirmable / easily changed)*
- Schema and seed/reference data are applied via EF Core migrations or an
  explicit operator command — never at startup (Principle VII).

### Backend specifics
- **Auth:** JWT access token + rotating refresh token, over HTTPS.
- **Password hashing:** salted strong hash (PBKDF2 by default; bcrypt/argon2
  acceptable) — never MD5, never reversible.
- **Validation:** FluentValidation. **API error contract:** RFC 7807
  ProblemDetails (message codes + params).
- **Testing:** xUnit for unit + integration; integration tests that need a real
  database/external dependency are categorized (traits) so they can be
  included/excluded.

### Frontend specifics
- **Angular 19**; runtime i18n library (transloco or ngx-translate) for in-session
  localization; unit tests via Jasmine/Karma.

### Authorization model
- Mirrors the legacy project-scoped role/permission model
  (viewer/editor/admin/sysadmin; positive/negative permissions; `%` prefix and
  project/resource wildcards) unless a spec explicitly deviates.

### Version & change policy
- Keep current within these majors. A major **.NET**, **Angular**, or **EF Core**
  upgrade, or a **database-engine** change, is an amendment-level decision (edit
  this section + bump the constitution version), not an ad-hoc change.

## Development Workflow

- **Operating procedure (authoritative):** agents follow the step-by-step
  **Required Workflow** in
  [`analysis/legacy_user_flows_template_instructions.md`](../../analysis/legacy_user_flows_template_instructions.md)
  — legacy-evidence → re-analysis → SDD → approval-gated implementation →
  parity verification → SDD↔workbook reconciliation. That document operationalizes
  this constitution; if the two ever conflict, the constitution wins and the
  instructions are corrected.
- Ratify/refresh this constitution before specs/plans/tasks for a batch are
  treated as final.
- Process features in implementation/dependency order, in batches of ≈3: write
  `spec.md` + `plan.md` + `tasks.md`, mark the workbook SDD columns + recolor,
  open a PR; implement only after the owner approves.
- Implementation proceeds in **hybrid, milestone-batched** slices: build a backend
  chunk to a usable point, then the UI chunk that integrates with it, then the next
  backend chunk, and so on. The **first milestone is an integrated walking
  skeleton** (login + one protected screen running end-to-end via
  `docker compose up`) so integration is proven early, not deferred. The
  implementation-order source of truth is [`specs/ROADMAP.md`](../../specs/ROADMAP.md);
  milestone ordering is process and is **not** recorded in the workbook.
- **Local-first dev loop:** develop and debug by running the backend with
  `dotnet run` against a **local SQL Server** instance and the SPA with `ng serve`;
  schema/seed is applied via the explicit `dotnet run -- --migrate` command (§VII),
  never at app startup. `docker-compose.yml` (DB + migrator + backend + frontend) is
  an **optional** one-command full-stack run for onboarding/demo/CI — not required
  for day-to-day work. This is a workflow choice; the stack/engine is unchanged.
- Clarify ambiguous scope/behavior before planning; define the automated test
  strategy in the plan before generating implementation tasks.
- Put required reference/seed data in migrations or a documented operator
  command — never in startup seeders.
- Record important discoveries in specs/docs so future work does not depend on
  chat history.

## Quality Gates

- Legacy baseline still deploys via `legacy/docker compose up`.
- **No backend PR is accepted without accompanying xUnit unit tests** for the
  new/changed logic, and `dotnet test` is green (hard gate, §VIII).
- Each implemented feature has passing unit + integration tests covering its
  Happy / Alternative / Operational scenarios from the workbook; DB-dependent
  integration tests are categorized.
- Database schema and seed/reference data changes are represented by EF Core
  migrations, with integration coverage proving the data is present after
  `MigrateAsync` (no startup seeding).
- No magic strings/numbers in committed code; constants/enums are used (Principle
  IX) — checked in review.
- Security deviations (Principle X) are implemented and documented where legacy is
  insecure.
- Workbook updated: SDD columns at spec time; destination columns + color at
  implementation time. The matching `tasks.md` checkboxes are ticked in the same
  change (task-list sync, §XI).
- Generated/tool-added files are reviewed and kept separate from business changes.

## Governance

This constitution guides all XPlanner migration specs, plans, and tasks. If a
spec conflicts with it, the spec must be revised or must explicitly propose an
amendment (rationale, expected impact, and an update to this file with a version
bump). Versioning is semantic: MAJOR for principle removals/redefinitions, MINOR
for new principles/sections, PATCH for clarifications.

**Version**: 1.4.0 | **Ratified**: 2026-06-24 | **Last Amended**: 2026-06-30
