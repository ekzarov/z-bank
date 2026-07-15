# Workbook tools

Invariant checker for `analysis/legacy_user_flows.xlsx` plus the shared helpers
every workbook-mutating script must use.

## Run the audit (required before committing any workbook change)

```bash
cd analysis/tools
npm install   # first time only
npm run audit
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
