# Legacy Migration Methodology

This document defines the end-to-end process every agent must follow when
migrating a legacy system in this repository. It is stack-agnostic and applies
to any legacy source (web, 3270/COBOL terminal, API, batch). The visual
presentation of the same flow lives in `analysis/migration_methodology.html`.

Core principle: **every legacy behavior is a row in the parity map
(`analysis/legacy_user_flows.xlsx`); every row is driven to green with
evidence. A migration is proven, not claimed.**

Workbook mechanics (columns, colors, finding types, audit tooling) are defined
in `analysis/legacy_user_flows_template_instructions.md` and are mandatory.
The project constitution at `.specify/memory/constitution.md` must be read
before any action and overrides convenience.

## Actors

- **Owner** — the human project owner. Only the owner approves SDD, merges
  PRs, and signs acceptance.
- **Primary agent** — does the stage's main work.
- **Independent agent** — a *different* agent (fresh context, no shared chat
  history) used for control and re-verification stages. An agent never
  verifies its own work.
- **Automated gate** — a check that must pass before the flow may continue
  (tests, workbook audit script, smoke test).

## Stages

### Stage 1 — Reconnaissance (legacy code → parity map)

- Extract behavior **from legacy code only** — not from documentation, not
  from memory. Narrative docs may hint where to look but are never evidence.
- Every user-visible scenario becomes one workbook row with concrete evidence
  (file/class/page/route), following the workbook instructions.
- Real systems hide secondary flows; treat the first pass as a draft.

### Stage 2 — Control reconnaissance (second agent)

- Once the map is built, a **second, independent agent repeats the same
  analysis from scratch** and diffs its result against the map: missed flows
  and scenarios, broken evidence references, wrong statuses.
- **Every finding loops back to Stage 1**: the map is corrected and extended,
  then the control pass is repeated.
- The stage is closed only by a control pass that produces **no new findings**.

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

### Stage 4 — Design: SDD (map → spec / plan / tasks)

- **This is where SDD (Spec-Driven Development) happens.** All further
  development is driven by the specifications written here.
- Entry point is always the workbook: first the rows themselves are brought up
  to date (wording, SDD columns: covered / deferred with reason), only then is
  each flow turned into SDD artifacts: `spec.md` → `plan.md` → `tasks.md`
  under `specs/NNN-*`.
- The map and SDD are linked through the coverage columns and must never
  disagree.
- **Not a single line of implementation code before the owner approves the
  SDD.**

### Stage 5 — Design re-verification (independent eyes)

- Before any build starts, a **different agent** cross-checks Stages 1–4:
  - map ↔ legacy: is every legacy flow captured (nothing lost in
    reconnaissance or the live walkthrough)?
  - map ↔ SDD: is every row covered by the specs or explicitly deferred with
    a written reason?
- Discrepancies return to Stage 4. Implementation does not start until this
  re-verification is clean. Passing it is the **gate to build**.

### Stage 6 — Build (code, tests, and documents in one PR)

- Branch first (`main` stays stable) → implement on the target stack.
- **Automated tests are a hard gate**: no PR ships on red.
- The same PR synchronizes code, SDD artifacts, and the workbook (rows greened
  with target evidence). A code-only PR that leaves SDD or the workbook stale
  is incomplete.
- **Merge is the owner's decision only.**

### Stage 7 — Delivery (one-command deploy)

- Deploy with a single command to a demo stand where **legacy and the new
  system run side by side**: the same flow can be shown in both worlds.
- A smoke test closes the delivery.

### Stage 8 — Live revision (map vs the living systems, every channel)

- An agent walks **both systems as a real user — through every channel the
  system has**: pages, links, and forms in a web UI; screens and transactions
  in a terminal (3270/COBOL/IMS); API routes; batch jobs — and compares what
  it sees against the map.
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

### Stage 9 — Final acceptance (someone else's hands)

- One **consolidated backlog** of everything still open; the automated audit
  proves completeness — an open row outside the backlog is impossible.
- The final checklist run (across all channels) and audit are given to **third-party agents of
  other vendors** (e.g. Google Antigravity, OpenAI Codex or equivalent): the
  agent that wrote the code never signs off on itself.
- The third-party agent receives only the instruction, the workbook, and the
  stand URL. When its results match the map, the acceptance is signed by the
  owner.

## Loops (return arrows)

| From | Back to | Trigger |
|---|---|---|
| Stage 2 (control reconnaissance) | Stage 1 | any hole or error found in the map |
| Stage 3 (live walkthrough) | Stage 1 | observed behavior missing from the map |
| Stage 5 (design re-verification) | Stage 4 | map/SDD coverage discrepancies |
| Stage 8 (live revision) | Stage 4 | gaps → map → SDD → code |
| Stage 9 (final acceptance) | Stage 4 | third-party findings |

Cycles repeat until acceptance is clean.

## Exit criterion

The migration is done when the parity map contains **no red and no gray**,
every orange row has an owner-approved reason, and an independent third-party
acceptance run matches the map. **Proven, not claimed.**
