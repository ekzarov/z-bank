# Feature 005 implementation review

## Scope

- Feature: `005-funds-transfers`
- Reviewer: Claude Code CLI, fresh read-only sessions
- Orchestrator: OpenAI Codex
- Reviewed candidate: `5c326aa`
- Result: clean after two backend rounds and two UI rounds

## Backend Round 1

- Packet: `FEATURE005-PEER-004-A`
- Result: findings
- Accepted:
  - add explicit product-eligibility, invalid-amount, currency, and closed
    source/destination coverage;
  - avoid the controller's duplicate current-user lookup.
- Rejected after source verification:
  - the claim that amount/status validation was absent. The validation already
    existed in `Account.ApplyCash`, outside the supplied focused diff.
- Corrections:
  - expanded unit and SQL Server integration coverage;
  - resolved the current user once per request.

## Backend Round 2

- Packet: `FEATURE005-PEER-005-A`
- Result: clean
- Verified: every Round 1 item was either corrected or closed with concrete
  source evidence; no new finding remained.

## UI Round 1

- Packet: `FEATURE005-PEER-006-B`
- Result: findings
- Accepted:
  - provision two customer-owned demo accounts so the customer transfer can be
    demonstrated;
  - add a customer Playwright transfer path, not only an operator path;
  - use scoped locators and reject sub-cent values client-side.
- Rejected after source verification:
  - the claimed response-model mismatch; `CashTransaction` already carries the
    returned balance/currency fields;
  - the claimed startup-password mutation; provisioning is an explicit setup
    command and normal API startup performs no seeding or migration.
- Clarified:
  - the transfer form is intentionally available to both customer and operator
    on eligible account detail pages; authorization remains server-enforced.

## UI Round 2

- Packet: `FEATURE005-PEER-007-B`
- Result: clean
- Verified: customer/operator happy paths, rejected funds, deterministic demo
  data, precision validation, and explicit-only provisioning.

## Invalid Invocations

Three attempts produced no usable review result: two timeouts and one malformed
PowerShell payload that serialized the diff as `System.Object[]`. Their scopes
were fully reassigned to fresh sessions and are counted as invalid, not clean.

## Gate

No peer-review finding remains. Feature 005 may proceed to delivery and formal
Stage 10 independent acceptance.
