# PROCESS-SURFACE-GATE-R1 Interaction Summary

## Packet

- Base revision: `1f79fce`
- Initial reviewed revision: `3a24d35`
- Mode: fresh read-only Claude Code CLI peer review
- Sessions started: 2
- Usable reports: 2
- Discussion rounds completed: 2

## Reviewer Findings And Disposition

| ID | Claude finding | Primary-agent disposition | Correction |
|---|---|---|---|
| F1 | Actions, roles, and tests were not bound; Operator evidence could be relabeled as Administrator evidence. | Accepted, high severity. | Inventory actions and tests are now role-bound. Test titles carry `@surface:<id>` and `@role:<role>`, and the audit checks the exact referenced test title. |
| F2 | A deferred surface could be declared hidden while its direct Angular route remained reachable. | Accepted, high severity. | Hidden/deferred inventory entries now fail when the Angular route still exists. |
| F3 | Exposed roles were derived only from navigation and not checked against route guards. | Accepted, medium severity. | Route roles are read from Angular `data.role`, reconciled with inventory roles, and included in role-action completeness. |
| F4 | Placeholder detection used a narrow four-string denylist and scanned non-rendered code. | Accepted, medium severity. | The marker vocabulary was expanded and scanning was restricted to external HTML and inline Angular component templates. Placeholder scanning remains supplemental to action/test/live gates. |
| F5 | Regex route/navigation parsing missed property reordering, extra fields, double quotes, and nested routes. | Accepted, medium severity. | Parsing now uses the TypeScript AST, supports property reordering and nested routes, and fails closed on unsupported entries. |
| F6 | Normative text overstated what static metadata can prove about test semantics. | Accepted, low severity. | Governance now describes tags as structural traceability; Stage 9 and Stage 10 must still execute the useful action. |

## Verification After Corrections

- `npm --prefix analysis/tools run test:target-audit`:
  `5 scenarios passed`.
- `npm --prefix analysis/tools run audit`, `audit:sdd`, and `audit:evidence`:
  passed.
- Angular production build under Node `24.15.0`: passed.
- Angular unit tests: `17` files and `49` tests passed.
- Playwright discovery: `17` E2E tests listed, including the new
  Operator statement surface check.
- `npm --prefix analysis/tools run audit:target`: expected failure with four
  Administrator violations. This red result is the intended reopened project
  gate, not a process-tool failure.

## Repeat Review

Fresh packet `PROCESS-SURFACE-GATE-R2` reviewed immutable revision `0d7a279`
and returned `CLEAN`. It confirmed F1-F6 were materially fixed and found no
remaining high- or medium-severity bypass.

The reviewer recorded four low-risk future-hardening observations:

- enforce token boundaries when matching test anchors and tags;
- model inherited route roles if nested role-guarded routes are introduced;
- scan interpolated inline Angular templates;
- add more unit tests for individual negative evidence branches.

These do not allow the current visible actionless Administrator destination to
pass. They remain explicit hardening backlog rather than silent findings.

The reviewer statically predicted three Administrator violations because it
omitted the template-marker result from its count. The primary agent reran the
actual command: `audit:target` reports four violations, including the visible
`next migration slice` marker. This factual counting discrepancy does not
change the reviewer's `CLEAN` conclusion.
