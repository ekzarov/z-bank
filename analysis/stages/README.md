# Migration Stage Records

This directory holds durable execution records produced by methodology stages
that are not independent control-pass reviews.

**Numbering.** Directory names use the stage numbering in effect when each
record was written. Everything below predates the 2026-07-24 renumbering
(Prototyping Stages 5-8 inserted; former Stages 5-10 became 9-14), so the old
numbers apply: `stage-05` here is SDD (Stage 9 today), `stage-08` is delivery
(Stage 12 today), `stage-09` is live revision (Stage 13 today). Records
written after 2026-07-24 use the new numbers. See the numbering note in
[`../migration_methodology.md`](../migration_methodology.md).

- [`stage-03/`](stage-03/) - live, simulated, or waived legacy walkthrough
  evidence (Stage 3, number unchanged).
- [`stage-04/`](stage-04/) - owner-approved requirement revisions and target
  decisions (Stage 4, number unchanged).
- [`stage-05/`](stage-05/) - SDD coverage and design-gate reports
  (old numbering; Stage 9 today).
- [`stage-08/`](stage-08/) - per-feature delivery and deployment evidence
  (old numbering; Stage 12 today).
- [`stage-09/`](stage-09/) - per-feature live revision and correction evidence
  (old numbering; Stage 13 today).

Independent Stage 2, 7, 10, and 14 reports remain under
[`../reviews/`](../reviews/README.md). The canonical process and current state
remain in [`../migration_methodology.md`](../migration_methodology.md) and
[`../migration_status.yaml`](../migration_status.yaml).
