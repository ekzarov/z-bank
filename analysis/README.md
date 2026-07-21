# Legacy Analysis Workspace

Start all migration work from [`../MIGRATION.md`](../MIGRATION.md). This
directory contains the governed Bank of Z analysis and reusable
reverse-engineering material:

- [`legacy_user_flows.xlsx`](legacy_user_flows.xlsx) - filled Bank of Z parity
  map and current evidence record.
- [`legacy_user_flows_template.xlsx`](legacy_user_flows_template.xlsx) - empty
  reusable workbook template. It must never overwrite the filled map.
- [`legacy_user_flows_template_instructions.md`](legacy_user_flows_template_instructions.md)
  - rules for evidence-based flow discovery, SDD mapping, implementation
  approval, and parity tracking.
- [`migration_methodology.md`](migration_methodology.md) - canonical ten-stage
  migration process.
- [`migration_methodology.html`](migration_methodology.html) - human
  presentation of that process; it is not an independent source of rules.
- [`migration_status.yaml`](migration_status.yaml) - current machine-readable
  stage, gate, blocker, pass history, waiver, and next action.
- [`reviews/`](reviews/README.md) - immutable control-pass reports and the
  shared report template for Stages 2, 6, and 10.
- [`legacy_reconnaissance.md`](legacy_reconnaissance.md) - Stage 1 evidence
  boundary and handoff.
- [`cobol_deployment_overview.md`](cobol_deployment_overview.md) - legacy
  runtime and IBM infrastructure limits.
- [`tools/`](tools/README.md) - workbook audit utility and its locked Node.js
  dependencies.

For a new project, copy the empty template. For Bank of Z, continue from the
filled map and preserve its evidence history.
