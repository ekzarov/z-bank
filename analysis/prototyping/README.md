# Prototyping Record (Stages 5-8)

Durable, reviewable output of the Prototyping phase. For the completed Bank
of Z migration the owner waived retroactive execution on 2026-07-24 (see
`owner_gates.prototyping_retroactive` in
[`../migration_status.yaml`](../migration_status.yaml)), so this directory
holds only the reusable scaffold. The first project or feature that runs
Stages 5-8 fills it in.

## Contents

| File | Stage | Purpose |
|---|---|---|
| `decision.md` | 5 | Owner decision on application form, style, and color scheme (copy `templates/decision-template.md`) |
| `wireframes/` | 6 | Exported wireframe catalog — image/export files, never only links into the SaaS tool |
| `screen-manifest.json` | 6 | Machine-readable manifest mapping screen → workbook rows → roles → states, with export file hashes (start from `templates/screen-manifest.example.json`) |
| `approval.md` | 8 | Owner approval with the approved `export_set_version` (copy `templates/approval-template.md`) |

## Rules

- **Secrets never enter this directory or Git.** Stitch/Figma API keys and
  MCP configuration live only in the owner's local environment. Record the
  fact that access is configured, never the credential.
- Every screen in `wireframes/` appears in `screen-manifest.json` and vice
  versa; every manifest entry lists the workbook rows it covers (or is marked
  `target_only` with an owner-approved requirement reference).
- "Every screen" is defined in the methodology Stage 6: every role, the
  loading/empty/error/forbidden/success states, forms with validation states,
  dialogs and wizard steps, channel variants, and target-only screens.
- An approval without a recorded `export_set_version` does not exist. If SDD
  work later changes the UI, the change returns to Stage 6 and the control
  (Stage 7) and approval (Stage 8) repeat for the changed scope.

## Automated audit

Run after any change to this directory:

```bash
npm --prefix analysis/tools run audit:prototype
```

For the Stage 8 approval gate and any later stage that relies on the approved
prototype (Stages 10, 13, 14), run the strict mode, which additionally
requires `approval.md` pinning the manifest's exact `export_set_version`:

```bash
npm --prefix analysis/tools run audit:prototype:approved
```

The audit is **fail-closed and scope-aware**: a missing `screen-manifest.json`
is an error unless `analysis/migration_status.yaml` records an explicit owner
waiver (`owner_gates.prototyping_retroactive.status: waived`) **whose
`applies_to` list contains the audited scope** — only then it prints
`PROTOTYPE AUDIT SKIPPED` with exit code 0. The audited scope is
`--scope=<feature-or-project-id>` when given, otherwise `next_action.feature`
from the status file; a waiver recorded for the completed migration never
silences the gate for a new feature slug. With a manifest present it
verifies that `decision.md` exists and contains no unfilled template
placeholders, manifest structure, unique screen ids, non-empty roles /
states / rows (or an explicit `target_only` justification), that every listed
export file exists with a matching SHA-256 hash, that no file in `wireframes/`
is missing from the manifest, that `approval.md` (when present, or required in
strict mode) names the manifest's exact `export_set_version`, and that no
token-shaped credential appears in any text-based file of the record
(including `.svg`/`.html` exports). It must print `PROTOTYPE AUDIT OK`
before Stage 7 can close.

Regression tests: `npm --prefix analysis/tools run test:prototype-audit`.

Coverage of the parity map (is every ported UI row covered by at least one
screen, and does every screen trace to a real requirement) is the Stage 7
reviewer's responsibility per the review checklist in
[`../reviews/README.md`](../reviews/README.md); wiring that comparison into
the audit script is a planned extension.
