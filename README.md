# Bank of Z Legacy Modernization

This repository keeps the IBM Bank of Z legacy system and its future
replacement side by side.

## Repository layout

- `legacy/` - byte-for-byte snapshot of the upstream IBM Bank of Z repository.
- `modern/` - workspace reserved for the future replacement application.
- `analysis/` - reusable legacy-flow workbook template, instructions, and audit
  tooling copied from the XPlanner migration project.
- `.specify/` - Spec Kit governance and future feature specifications.

Implementation of the replacement must not begin until the legacy behavior has
been analyzed, recorded in the workbook and specifications, and explicitly
approved.

The current constitution was copied from XPlanner as a working template. It is
not yet adapted or ratified for Bank of Z and must be reviewed before the first
Bank of Z specification is created.

Legacy snapshot: IBM Bank of Z commit
`69a0bf9e162223c33d35468e9d708b591d9c8ec0`.

