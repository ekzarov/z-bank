# Legacy Migration Methodology

Agents enter the process through [`../MIGRATION.md`](../MIGRATION.md), then use
[`migration_status.yaml`](migration_status.yaml) to select the active stage.
This document defines the end-to-end process every agent must follow when
migrating a legacy system in this repository. It is stack-agnostic and applies
to any legacy source (web, 3270/COBOL terminal, API, batch). The visual
presentation of the same flow lives in
[`migration_methodology.html`](migration_methodology.html). The Markdown file
is canonical; the HTML is a human-oriented view and must not introduce rules
that are absent here.

Core principle: **every legacy behavior is a row in the parity map
([`legacy_user_flows.xlsx`](legacy_user_flows.xlsx)); every row is driven to green with
evidence. A migration is proven, not claimed.**

Workbook mechanics (columns, colors, finding types, audit tooling) are defined
in [`legacy_user_flows_template_instructions.md`](legacy_user_flows_template_instructions.md)
and are mandatory. The reusable blank workbook is
[`legacy_user_flows_template.xlsx`](legacy_user_flows_template.xlsx); it is not
the Bank of Z project record and must never replace the filled map. The
[project constitution](../.specify/memory/constitution.md) must be read before
any action and overrides convenience.

## Stage Control

- `analysis/migration_status.yaml` is the only current-stage checkpoint. Update
  it whenever a stage, gate, blocker, or owner decision changes.
- Its `progress` block is an estimate for orientation, not a gate. Update it at
  durable checkpoints (completed batch, correction, slice, deployment, or
  acceptance), not on polling noise. External-review counters distinguish
  started sessions, usable reports, historical failures, active sessions, and
  unresolved blocked scope.
- A stage starts only when its prerequisites and actor requirements are met.
- Automated checks prove technical invariants but never imply owner approval.
- Owner approval is recorded explicitly; it cannot be inferred from a merge,
  a successful test run, or an agent statement.
- If a stage cannot be executed, record the blocker and leave affected rows
  unverified. Skipping a stage does not count as completing it.
- Advancing beyond a blocked stage requires an explicit owner decision in
  `analysis/migration_status.yaml`, including approver, date, rationale, scope,
  residual risk, and permitted next stage. For an unavailable Stage 3, the
  owner chooses `simulate` or `waive`; neither completes the unobserved real
  scope or verifies affected rows.
- Stages 2, 7, 10, and 14 produce immutable reports under `analysis/reviews/`
  and append their result to `review_passes` in the status file. A clean result
  in chat is not a completed gate.

## Numbering note (2026-07-24)

The Prototyping phase (Stages 5–8) was inserted after Requirements on
2026-07-24; the former Stages 5–10 became Stages 9–14. Historical review
reports keep their original file names; the `review_passes` ledger in
`analysis/migration_status.yaml` is the authoritative stage-to-report
mapping. In this repository the pre-renumbering series are closed:
`stage-02-pass-001..007` (control reconnaissance, number unchanged),
`stage-06-pass-001..013` (design re-verification, now Stage 10), and
`stage-10-pass-001..015` (slice/final acceptance, now Stage 14). Passes
written after this date use the new stage numbers and continue at pass 016
for both affected stages — design re-verification as
`stage-10-pass-016.md` onward (014–015 are skipped because those file names
belong to the closed acceptance series) and acceptance as
`stage-14-pass-016.md` onward. Stage 7 (wireframe control) is new and starts
at `stage-07-pass-001.md`.

**Maturity.** The Prototyping phase is normative from 2026-07-24, but it is
deliberately young: its rules are expected to be extended and refined as the
first projects run through it (the presentation marks the block as "in
progress, ~50%"). Until then the stages apply as written — maturity is a
statement about future additions, never a permission to skip or improvise.

## Actors

- **Owner** — the human project owner. Only the owner approves SDD, merges
  PRs, and signs acceptance.
- **Human-in-the-loop stages** — Stages 4, 5, 8, and 14 require live human
  participation (marked with a person badge on the presentation diagrams).
  On reaching these points the agent **stops and asks the owner for explicit
  confirmation**; it must not proceed on its own and must not assume, infer,
  or simulate the human decision.
- **Primary agent** — does the stage's main work.
- **Independent agent** — a *different* agent (fresh context, no shared chat
  history) used for control and re-verification stages. An agent never
  verifies its own work. Before every Stage 2, 7, 10, or 14 pass, it checks
  its eligibility: if its current context contains creation or editing of an
  artifact in scope, it self-disqualifies and requests a fresh agent. Every
  iteration uses an eligible fresh agent.
- **Orchestrator** — the primary agent transporting deterministic review
  packets and challenge responses to an external agent CLI. Orchestration
  removes owner message relay but does not transfer the independent reviewer's
  conclusion to the primary agent and does not grant approval authority.
- **Automated gate** — a check that must pass before the flow may continue
  (tests, workbook audit script, smoke test).

## Working principle: depth over speed

At every stage, agents work in **deep-research mode**: full sweeps instead of
sampling, adversarial re-checks, independent second opinions. This is slower —
and that is accepted. Never trade completeness for speed: a hole caught early
is always cheaper than one found at acceptance.

## Stop criterion for re-checks: the dry pass

Every executable return loop — Stages 2, 3, 4 → Stage 1; Stages 7, 8 →
Stage 6; and Stages 10, 13, 14 → Stage 9 — closes not after a fixed number of
repetitions but on a **dry pass**: a full pass that yields not a single new
finding (in practice 2–3 iterations). Stages 2, 7, 10, and 14 require a fresh
independent agent for every pass. Working iterations in Stages 3, 4, and 13
may use the responsible primary agent unless a stage-specific decision
requires otherwise. When Stage 3 cannot be executed, its recorded `simulate`
or `waive` owner decision replaces the dry-pass gate only for progression; the
unobserved real scope remains incomplete.

## Phase groups

For presentation and planning, the fourteen stages are grouped into phases:
**Requirements** (Stages 1–4), **Prototyping** (Stages 5–8: application form
and style, wireframes, their control and approval), **Architecture**
(Stages 9–10: SDD and its re-verification), **Coding** (Stage 11),
**Deployment** (Stage 12), and **QA** (Stages 13–14). Grouping changes no
rules: loops, gates, and actors stay per-stage; the cross-phase return
QA → Architecture is the Stage 13/14 → Stage 9 loop.

## Iterative delivery loop from Stage 11

Stages 11-14 are not one big-bang pass over the whole approved backlog. Select
one approved feature or a small, tightly related group as a **delivery slice**
and repeat this loop:

1. **Stage 11 — Build:** implement and test only the selected slice;
   synchronize its SDD, tasks, and workbook rows in the same PR. After tests
   pass, run a read-only external-agent peer review of the requirements and
   diff; verify every finding and use at most two evidence-based discussion
   rounds.
2. **Stage 12 — Delivery:** deploy that slice and close its smoke test.
3. **Stage 13 — Live revision:** compare that slice with the legacy baseline
   and map through every affected channel. For every role, open every visible
   destination and complete at least one useful action or observable contract;
   a successful login, HTTP status, route, or heading alone is not evidence of
   a completed surface.
4. **Stage 14 — Slice acceptance:** an eligible independent agent checks the
   delivered slice. Findings return to Stage 9 (or Stage 1 for a map error),
   then the corrected slice repeats the loop. A clean accepted slice releases
   selection of the next slice.

The whole approved backlog MUST NOT be implemented before this feedback is
collected. When every slice is accepted, Stage 14 runs once more as the final
consolidated acceptance of the complete migrated system.

## Cross-agent execution and context safety

The primary agent invokes Claude Code, Antigravity, Codex, or an equivalent
approved external CLI directly for the mandatory slice peer review, so the
owner does not relay review messages. Every such
invocation follows
[`agent_orchestration.md`](agent_orchestration.md): deterministic review packet,
new session, read-only isolated revision, structured findings, independent
validation by the primary agent, and no more than two discussion rounds.

Large scopes are divided into explicit batches with persisted checkpoints.
After `/compact` or a fresh-session continuation, the reviewer must acknowledge
the packet identifier, revision, completed scope, and remaining scope before
continuing. Context overflow, timeout, lost acknowledgement, repository
mutation, or incomplete batch coverage makes the review `blocked`. A formal
Stage 2, 7, 10, or 14 conclusion still belongs to the eligible external reviewer;
model agreement never replaces automated evidence or owner approval.
No packet may send credentials, personal data, regulated data, or repository
content not approved for the selected service. If complete safe evidence cannot
be sent, the review is blocked until the owner selects an authorized reviewer
or environment.

Review worktrees and scratch directories are disposable execution state.
Durable reports and the minimum auditable prompt/response/checkpoint packet are
consolidated under `analysis/reviews/`; duplicate checkouts, rendered copies,
dependency folders, and external `*-review`/`*-evidence` directories are
removed before the review is considered operationally complete.

## Stages

### Stage 1 — Reconnaissance (legacy code → parity map)

- Extract behavior **from legacy code only** — not from documentation, not
  from memory. Narrative docs may hint where to look but are never evidence.
- Every user-visible scenario becomes one workbook row with concrete evidence
  (file/class/page/route), following the workbook instructions.
- Real systems hide secondary flows; treat the first pass as a draft.

### Stage 2 — Control reconnaissance (second agent)

- Once the map is built, a **second, independent agent repeats the same
  analysis from scratch**. It first inventories behavior from executable
  artifacts without reading the filled map or `legacy_reconnaissance.md`; only
  then does it diff its result against those artifacts for missed flows and
  scenarios, broken evidence references, wrong statuses, and unsupported
  claims.
- Every pass, including a clean or blocked pass, is written to the next
  `analysis/reviews/stage-02-pass-NNN.md` declared by the status file.
- **Every finding loops back to Stage 1**: the map is corrected and extended,
  then the control pass is repeated by another eligible fresh agent.
- The stage is closed only when a report records **no new findings**, the pass
  is appended to status history, and the workbook audit succeeds after any map
  edits.

### Stage 3 — Live legacy walkthrough (deploy and walk everything)

- The map built from code is a hypothesis. Deploy the legacy system and walk
  it as a real user would, whatever the technology:
  - web UI — click through **every page, link, and form**;
  - terminal (3270/COBOL/IMS) — execute **every screen and transaction**;
  - API — call **every route**;
  - batch — run the jobs.
- Compare everything observed against the map. Behavior with no row → add the
  row (**loop back to Stage 1**). A row that cannot be observed or deployed
  stays explicitly *unverified* in the map — never "assumed working".
- If a complete live walkthrough is unavailable, too costly, or impractical,
  the agent MUST stop and ask the owner to choose one of two fallback modes. It
  must not select a mode or build mocks on its own:
  - **simulate** — build the smallest useful emulator, stub, contract harness,
    or mock from traceable legacy code/contracts, then execute the observable
    flows against it;
  - **waive** — perform no substitute run and continue based on static evidence
    after the owner explicitly accepts the additional migration risk.
- Record one of three outcomes in the status and workbook evidence:
  - `live-verified` — real legacy behavior was observed for the declared scope;
  - `partial-simulated` — some behavior was exercised through mocks or
    emulation; simulated evidence is labeled and real runtime behavior remains
    unverified;
  - `blocked-waived` — the owner authorized progression without a run; the
    skipped scope remains blocked and unverified.
- A simulation proves the consistency of the team's interpretation, not the
  behavior of the real legacy runtime. Its fixtures and responses must cite the
  code, contracts, traces, or owner decisions from which they were derived.
- `partial-simulated` and `blocked-waived` allow progression to Stage 4 only
  through the recorded owner decision. Stage 3 remains incomplete for the
  unobserved real scope and must be resumed if access later becomes available;
  any difference found then loops back to Stage 1.

### Stage 4 — Requirements revision (consistency check, with the business)

- Before any design, the agent audits the map itself as a set of requirements
  — **actively hunting for contradictions**, not just re-reading:
  - **contradictions / inconsistencies** between flows and channels (e.g. a
    login exists in one channel and is absent in another; validation or
    balance rules differ between channels for the same operation);
  - **obsolete requirements** — behavior kept only for historical reasons;
    cross-check the legacy documentation for signs a rule is outdated
    (documentation is a signal here, still never parity evidence);
  - **unreasonable or strange requirements** — behavior that makes no
    business sense today.
- Every flagged row is reviewed **together with the business (owner) and a
  developer**: keep as-is / change / do not port. The decision is recorded in
  the map (decision status, deviation notes) **before SDD starts**.
- This is a **human-in-the-loop checkpoint**: the agent stops here and
  requests the owner's explicit confirmation of every keep / change /
  do-not-port decision. It must not proceed to Stage 5, and must not assume
  or simulate the business decision, until the owner has confirmed.
- If a "contradiction" turns out to be a mapping error, that is a map hole —
  **loop back to Stage 1**.

### Stage 5 — Application form and style (human decision)

- Before a single screen is drawn, decide **what the application will be as a
  whole**:
  - **form** — web, mobile, desktop, terminal/console, several channels
    (multi-channel), or **no interactive UI at all** (a pure API/batch
    system), and which channel is primary. The methodology is stack-agnostic;
    the option list is open, not limited to web/mobile.
  - **style** — e.g. minimalism, classic corporate, Material-like, or another
    direction;
  - **color scheme** — palette, accent colors, light/dark theme.
- The agent prepares the decision material: channels and scenarios from the
  parity map (which channels the legacy has, who uses them, where each flow
  lives), style options with references, and 2–3 candidate palettes.
- This is a **human-in-the-loop checkpoint**: the agent stops and asks the
  owner for an explicit decision. The chosen form, style, and palette are
  recorded in `analysis/prototyping/decision.md` (approver, date, options
  considered) before wireframing starts; the agent must not pick them itself.
- If the owner selects **no interactive UI**, Stages 6–8 shrink to the
  owner-approved minimum (e.g. wireframes only for an ops/admin console, or an
  explicit recorded owner waiver of the remaining prototyping scope).

### Stage 6 — Wireframes (agent via Google Stitch or Figma)

- The agent builds **wireframes for every screen** of the future application
  for the form chosen in Stage 5, driven by the parity-map rows.
- **"Every screen" is defined, not felt.** The wireframe set covers:
  - every screen **for every role** that can see it;
  - screen **states**: loading, empty, error, forbidden/no-access, success;
  - **forms with their validation states** (pristine, invalid with messages,
    submitted);
  - **dialogs, wizard steps, and the transitions** between screens;
  - **desktop and mobile variants** when the Stage 5 decision includes both;
  - **target-only screens** (owner-approved surfaces with no legacy
    predecessor).
- Tooling: **Google Stitch or Figma**. The owner performs a one-time setup of
  access — an API key or a locally configured MCP server for the chosen tool —
  before this stage can run. **Secrets never enter the repository**: keys and
  MCP configuration live only in the owner's local environment; Git, review
  reports, and manifests record only the fact that access is configured,
  never the credential itself.
- The stage produces a durable, reviewable record under
  `analysis/prototyping/`:
  - `wireframes/` — the exported wireframe catalog (images/exports, not only
    links into the SaaS tool);
  - `screen-manifest.json` — a machine-readable manifest mapping
    **screen → workbook rows → roles → states**, plus the export
    version/hash of each screen.
- Every wireframe screen is linked to the workbook rows it covers: a screen
  without a row is as impossible as a ported row without a screen (rows
  decided as "do not port" in Stage 4 are excluded).

### Stage 7 — Wireframe control (second agent against the map)

- When the wireframes are ready, a **second, independent agent** walks them
  screen by screen against the parity map: is every ported flow covered, is
  anything missed, and did anything appear on the screens that no requirement
  asks for.
- The pass verifies, over the exported catalog and `screen-manifest.json`
  (never over live SaaS links alone):
  - every ported UI-facing workbook row is covered by at least one screen;
  - no screen exists without a backing requirement or owner-approved
    target-only decision;
  - the declared roles, states, forms/validation, dialogs/wizard steps, and
    channel variants from the Stage 6 definition are all present;
  - every manifest link (screen → rows → roles → states) resolves and the
    export hashes match the delivered files.
- These manifest invariants are checked by an **automated prototype audit**
  under `analysis/tools` (added together with the first prototyping run);
  until that script exists, the pass performs and records the same checks
  manually in the report, item by item.
- Every finding loops back to **Stage 6**: the wireframes are corrected, then
  the control pass is repeated. The stage closes only on a **dry pass** — a
  full pass without a single new finding.
- Every pass is recorded as `analysis/reviews/stage-07-pass-NNN.md` and
  appended to `review_passes` in the status file; each pass requires an
  eligible fresh agent.
- If a wireframe finding turns out to be a hole in the map itself, that is a
  Stage 1 return, handled like any other map error.

### Stage 8 — Wireframe approval (human decision)

- The verified wireframes are shown to the **owner**: is this how the
  application should look, is the set of screens and transitions right.
- Remarks return the work to Stage 6; the control pass of Stage 7 is repeated
  after material changes.
- This is a **human-in-the-loop checkpoint**: the agent stops and waits for
  the owner's explicit approval. Design (SDD) does not start until the
  wireframes are approved; the approved wireframes then serve as the visual
  companion to the specifications.
- The approval is recorded in `analysis/prototyping/approval.md`: approver,
  date, and the **version/hash of the approved wireframe export set** (from
  `screen-manifest.json`), so later stages can prove exactly which prototype
  was approved. An unrecorded approval does not exist.

### Stage 9 — Design: SDD (map → spec / plan / tasks)

- **This is where SDD (Spec-Driven Development) happens.** All further
  development is driven by the specifications written here.
- Entry point is always the workbook: first the rows themselves are brought up
  to date (wording, SDD columns: covered / deferred with reason), only then is
  each flow turned into SDD artifacts: `spec.md` → `plan.md` → `tasks.md`
  under `specs/NNN-*`.
- The map and SDD are linked through the coverage columns and must never
  disagree.
- Build or update `analysis/inventories/target-surface-inventory.json`. Every target route,
  menu item, role workspace, screen, API operation, and job in the slice maps
  to a concrete user-visible action or observable contract plus its SDD
  requirement. A target-only role or screen with no legacy predecessor needs
  an explicit owner-approved target requirement; it cannot inherit completion
  from a similarly named legacy file or page. Automated evidence names the
  concrete test case after `#` and binds the test title to the surface and role
  with `@surface:<id>` and `@role:<role>`. The binding is structurally audited;
  Stage 13/14 must still execute the action because metadata cannot prove test
  semantics.
- Decompose bundled workbook outcomes before claiming coverage. If one row says
  that a control panel exposes seven actions, navigation-shell evidence does
  not cover the row: each action must be traceable to SDD and planned
  verification, or the row must be split/deferred explicitly.
- **Not a single line of implementation code before the owner approves the
  SDD.**

### Stage 10 — Design re-verification (independent eyes)

- Before any build starts, a **different agent** cross-checks Stages 1–9:
  - map ↔ legacy: is every legacy flow captured (nothing lost in
    reconnaissance or the live walkthrough)?
  - map ↔ decisions: are the requirements-revision decisions reflected?
  - map ↔ SDD: is every row covered by the specs or explicitly deferred with
    a written reason?
  - target surface ↔ SDD: does every declared role and destination have a
    concrete useful action and acceptance evidence, rather than a placeholder
    heading or access probe?
  - wireframes ↔ SDD: does every screen of the approved prototype (per the
    approved version/hash in `analysis/prototyping/approval.md`) trace to the
    specs, and does the SDD's UI scope still match the approved prototype? If
    SDD work after Stage 8 changed the UI, the change **returns to Stage 6**:
    the wireframes are updated, Stage 7 control and Stage 8 approval are
    repeated for the changed scope. (Skipped when the owner waived
    prototyping for the reviewed scope.)
- The check is not paper-only: following the **source code evidence** columns,
  the agent goes back into the legacy sources row by row — opening the cited
  files and lines and verifying that the requirement and the spec really match
  the code, catching omissions and understatements.
- Discrepancies return to Stage 9. Implementation does not start until this
  re-verification is clean. For every workbook row, the reviewer checks the
  cited legacy evidence and traces the row to an approved requirement, decision,
  or explicit deferral; sampling is not sufficient.
- Every pass is recorded as `analysis/reviews/stage-10-pass-NNN.md`
  (passes 001–013 predate the renumbering and live in
  `stage-06-pass-001..013.md`; new passes continue at 016 per the numbering
  note). Findings
  require a Stage 9 correction and another eligible fresh-agent pass. The gate
  to build opens only after a clean report, recorded pass history, a successful
  workbook audit, and explicit owner approval of the relevant SDD.
- A blocked or invalid session remains immutable history. Its exact unchecked
  scope MUST be assigned to later eligible fresh sessions and covered in full.
  The pass cannot close until `unresolved_blocked_scopes` is zero and a fresh
  consolidator verifies the closure mapping. The final report records each
  external finding, the primary agent's accepted/rejected disposition, the
  correction, and the repeat-review outcome.

### Stage 11 — Build (code, tests, and documents in one PR)

- Select one approved feature or a small, tightly related feature group. Do not
  pull the entire approved backlog into one implementation batch.
- Branch first (`main` stays stable) → implement on the target stack.
- **Automated tests are a hard gate**: no PR ships on red.
- A new visible route or role destination ships only with a completed
  target-surface inventory entry and an automated test that performs or
  observes its useful action. Route existence, `200 OK`, authorization success,
  and heading-only assertions are necessary checks but never sufficient
  acceptance evidence.
- After tests pass, the primary agent sends the slice requirements and diff to
  a fresh read-only external reviewer. It validates every response rather than
  accepting it automatically. Confirmed findings are fixed and tests rerun;
  material unresolved disagreement blocks the slice and goes to the owner.
- The same PR synchronizes code, SDD artifacts, and the workbook (rows greened
  with target evidence). A code-only PR that leaves SDD or the workbook stale
  is incomplete.
- **Merge is the owner's decision only.**

### Stage 12 — Delivery (one-command deploy)

- Deliver the current slice before implementation starts on the next slice.
- Deploy with a single command to a demo stand where **legacy and the new
  system run side by side**: the same flow can be shown in both worlds.
- The two systems have different delivery rhythms: **legacy is deployed to the
  stand once** and then simply lives there as the comparison baseline, while
  **the new system is redeployed after every delivered feature**.
- A smoke test closes every delivery.
- The smoke must fail on visible placeholders and must execute one meaningful
  action for each changed role-visible destination.

### Stage 13 — Live revision (map vs the living systems, every channel)

- Revise the current delivered slice before selecting the next one.
- An agent walks **both systems as a real user — through every channel the
  system has**: pages, links, and forms in a web UI; screens and transactions
  in a terminal (3270/COBOL/IMS); API routes; batch jobs — and compares what
  it sees against the map.
- Before walking, enumerate routes, navigation items, roles, screens, API
  operations, and jobs from the deployed target and reconcile that inventory
  with `analysis/inventories/target-surface-inventory.json`. For every applicable role,
  click every visible navigation item and complete the listed useful action.
  A page that only authenticates, loads, returns `200`, or displays its title is
  recorded as a gap when the inventory or SDD promises functionality.
- Scan the deployed and source UI for placeholder destinations (for example
  "next migration slice", "coming soon", "not implemented", or an empty generic
  workspace). A deferred surface may remain only when it is explicitly
  owner-approved and hidden from production navigation.
- Every finding updates the map **and** the SDD in the same change set.
- **Unverified = not done (red).** Every gap records its provenance:
  confirmed by observation, or not-checked (the check is then the first step
  of closing it).
- Exactly three finding types (per the workbook instructions):
  - **gap** (red) — claimed/expected but absent → back into the cycle:
    map → SDD → code;
  - **decision** (gray) — differs from legacy with a recommendation → owner
    approves the deviation or orders the work;
  - **deferred** (orange) — consciously postponed, reason recorded → final
    acceptance backlog.
- The workbook audit script (`node analysis/tools/workbook-audit.js`) must
  pass before any workbook commit.
- The target-surface audit
  (`npm --prefix analysis/tools run audit:target`) must pass before Stage 13
  can close.
- Stage 13 closes for a delivered slice only when every observed difference is
  represented in the workbook and SDD, all gaps have looped back to Stage 9 or
  have an explicit owner decision, and the workbook audit passes. Stage 13 does
  not require a fresh independent agent for each working iteration.

### Stage 14 — Slice and final acceptance (someone else's hands)

- Every delivery slice receives an independent acceptance pass. A clean slice
  releases the next slice; findings return the current slice to Stage 9 and it
  repeats Stages 11-14 after correction.
- One **consolidated backlog** of everything still open; the automated audit
  proves completeness — an open row outside the backlog is impossible.
- The final checklist run (across all channels) and audit are given to **third-party agents of
  other vendors** (e.g. Google Antigravity, OpenAI Codex or equivalent): the
  agent that wrote the code never signs off on itself.
- The third-party agent receives only the instruction, the workbook, and the
  stand URL, plus the target-surface inventory. The reviewer independently
  enumerates deployed navigation/routes and compares them with that inventory.
  It must exercise useful actions for every role-visible destination; title,
  route, access-probe, and HTTP-status-only checks cannot close a surface. When
  its results match the map and surface inventory, the acceptance is signed by
  the owner.
- This is a **human-in-the-loop checkpoint**: the agent stops and requests
  the owner's explicit sign-off. Acceptance cannot be inferred from a clean
  report, a merge, or an agent statement; without the owner's recorded
  confirmation the slice (or the final system) remains unaccepted.
- Every acceptance attempt is recorded as
  `analysis/reviews/stage-14-pass-NNN.md` (passes 001–015 predate the
  renumbering and live in `stage-10-pass-001..015.md`; new passes continue at
  016 per the numbering note). A finding returns to Stage 9 and the
  next attempt requires another eligible fresh agent. Final acceptance requires
  a clean report, a complete consolidated backlog, passing workbook, SDD, and
  target-surface audits, and the owner's recorded signature. No visible
  placeholder or role without a useful accepted action may remain. After all
  slices are accepted, repeat this stage once across the complete system for
  final consolidated acceptance.

## Loops (return arrows)

| From | Back to | Trigger |
|---|---|---|
| Stage 2 (control reconnaissance) | Stage 1 | any hole or error found in the map |
| Stage 3 (live walkthrough) | Stage 1 | observed behavior missing from the map |
| Stage 4 (requirements revision) | Stage 1 | a flagged contradiction turns out to be a mapping error |
| Stage 7 (wireframe control) | Stage 6 | screens missing a mapped flow, or screens with no requirement (Stage 1 for a map error) |
| Stage 8 (wireframe approval) | Stage 6 | owner remarks on the wireframes |
| Stage 10 (design re-verification) | Stage 9 | map/SDD coverage discrepancies |
| Stage 10 (design re-verification) | Stage 6 | the SDD's UI scope diverges from the approved wireframes |
| Stage 13 (live revision) | Stage 9 | gaps → map → SDD → code |
| Stage 14 (final acceptance) | Stage 9 | third-party findings |

Cycles repeat until every loop ends in a dry pass and the final acceptance is
clean.

## Exit criterion

The migration is done when the parity map contains **no red and no gray**,
every orange row has an owner-approved reason, and an independent third-party
acceptance run matches the map. **Proven, not claimed.**
