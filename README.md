# Bank of Z Legacy Modernization

This repository keeps the IBM Bank of Z legacy system and its future
replacement side by side.

Agents and contributors must start with [MIGRATION.md](MIGRATION.md). It links
the constitution, current stage, ten-stage methodology, filled parity map,
empty workbook template, SDD artifacts, and required verification tooling.

## Repository layout

- `legacy/` - byte-for-byte snapshot of the upstream IBM Bank of Z repository.
- `modern/` - workspace reserved for the future replacement application.
- `analysis/` - the filled Bank of Z parity map, reusable empty template,
  migration methodology, current status, instructions, and audit tooling.
- `.specify/` - Spec Kit governance and future feature specifications.

Implementation of the replacement must not begin until the legacy behavior has
been analyzed, recorded in the workbook and specifications, and explicitly
approved.

The constitution is adapted for Bank of Z but remains a draft until the owner
explicitly ratifies it. See `analysis/migration_status.yaml` for the active
stage and pending gates.

Legacy snapshot: IBM Bank of Z commit
`69a0bf9e162223c33d35468e9d708b591d9c8ec0`.
