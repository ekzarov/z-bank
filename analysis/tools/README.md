# Workbook tools

Invariant checker for `analysis/legacy_user_flows.xlsx` plus the shared helpers
every workbook-mutating script must use.

Read [`../../MIGRATION.md`](../../MIGRATION.md) and
[`../legacy_user_flows_template_instructions.md`](../legacy_user_flows_template_instructions.md)
before editing the workbook. The audit targets the filled Bank of Z map, not the
empty `analysis/legacy_user_flows_template.xlsx`.

## Run the audit (required before committing any workbook change)

```bash
npm --prefix analysis/tools ci   # first time or after dependency changes
npm --prefix analysis/tools run audit
```

When a workbook is exported by `@oai/artifact-tool`, normalize its XLSX package
metadata and restore detail-row outlines before the audit (the finalizer does
not change workbook cell values or status styles):

```bash
node analysis/tools/repair-artifact-xlsx.js analysis/legacy_user_flows.xlsx
```

Exit 0 / `AUDIT OK` means every invariant holds. Invariants checked:

- **A. Colour = status** on scenario rows: green ⇔ `Destination implemented? = Yes`;
  orange ⇒ `Deferred in SDD? = Yes` + a written reason; red ⇒ open with no decision.
- **B. Epic banner rows** reflect their children: all Yes → green + `Passed`;
  any red child → red; otherwise orange.
- **C. Revision coverage**: after target/SDD tracking begins, every open row is
  referenced by at least one `Rev N` sheet. A first-pass, code-only discovery
  map may contain no revision sheet while columns I:N are entirely blank.
- **D. Rev sheets**: a green (closed) finding carries `Implemented? = Yes`.
- **E. Rev finding types** use only `gap`, `decision`, or `deferred`.
- **F. Format stability**: scenario status fills are uniform across D:N; epic
  banners use one status fill and canonical `Carlito 11 bold` typography across
  A:N; every `Rev N` data row has one uniform fill across A:I. This prevents
  partial repainting and inconsistent epic headings after workbook exports.

## Run the target-surface audit

```bash
npm --prefix analysis/tools run audit:target
```

This gate reconciles Angular routes and navigation with
`analysis/inventories/target-surface-inventory.json`, requires a concrete useful action with
SDD/code/test evidence for every implemented surface, and requires test
evidence to identify the concrete test name after `#`. It rejects visible
gap/deferred destinations, rejects roles without a role-specific useful action,
and scans shipped UI source for governed placeholder markers. Route existence,
authorization, HTTP success, or a heading-only test cannot satisfy this gate.
Every referenced test title is bound with `@surface:<id>` and `@role:<role>`;
the audit checks those structural bindings. Stage 9/10 reviewers still execute
the action because static tags alone cannot prove test semantics.

## Writing mutation scripts

Use `lib.js` — it encodes the incident-born rules:

- `setFill(cell, argb)` — **never assign `cell.fill` directly**: ExcelJS shares
  style objects between cells; a direct assignment repaints unrelated cells
  (this corrupted the whole sheet once — PR #124 cleaned it up).
- `snapshot(ws)` / `diffAgainst(ws, snap)` — take a values+fills snapshot of every
  sheet before mutating and assert afterwards that only the intended cells changed.
  Auditing values alone is not enough; the #123 corruption passed a values-only audit.
- `cellText(cell)` — cells may hold rich text or hyperlink objects; read through this.
- `parseRefs('70-72, 94')` — row-reference parsing shared with the audit.
