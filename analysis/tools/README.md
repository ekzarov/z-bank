# Workbook tools

Invariant checker for `analysis/legacy_user_flows.xlsx` plus the shared helpers
every workbook-mutating script must use.

## Run the audit (required before committing any workbook change)

```bash
cd analysis/tools
npm install   # first time only
npm run audit
```

Exit 0 / `AUDIT OK` means every invariant holds. Invariants checked:

- **A. Colour = status** on scenario rows: green ⇔ `Destination implemented? = Yes`;
  orange ⇒ `Deferred in SDD? = Yes` + a written reason; red ⇒ open with no decision.
- **B. Epic banner rows** reflect their children: all Yes → green + `Passed`;
  any red child → red; otherwise orange.
- **C. Revision coverage**: every open row is referenced by at least one `Rev N`
  sheet (this is the check that caught Rev 3 missing 33 rows).
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
