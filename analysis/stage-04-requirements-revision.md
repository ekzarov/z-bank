# Stage 4 Requirements Revision

**Date:** 2026-07-21

**Participants:** project owner and primary implementation agent

**Outcome:** approved for Stage 5 design

## Owner Direction

The owner approved the primary agent's recommendations after the Stage 3
traceable simulation:

1. Correct unsafe and defective legacy behavior instead of preserving it.
2. Preserve business capabilities, not dead technical endpoints, missing
   generated mappings, or unsupported legacy UI placeholders.
3. Replace the separate CICS and IMS channels with one modern domain, API, and
   web application while retaining `SourceSystem` as provenance.
4. Use Angular, ASP.NET Core Web API, EF Core, and SQL Server on the latest
   approved stable/LTS major versions.

## Decisions

| ID | Legacy finding or inconsistency | Owner decision for the target |
|---|---|---|
| D-001 | CICS menus, PF keys, IMS commands, and two web channel selectors expose the same banking capabilities differently. | Replace them with one accessible Angular navigation model and one API. Do not reproduce terminal key semantics. |
| D-002 | The web control panel has no application authentication, the OAuth URLs are placeholders, and IMS session handling can report false logout success. | Require authenticated same-origin sessions, CSRF protection, reliable logout, and customer/operator/admin policies. Do not port the placeholder OAuth contract. |
| D-003 | A direct IMS transaction can mutate an account owned by another customer. | Always authorize customer-owned operations against the authenticated customer and account relationship. Return a non-disclosing not-found/forbidden result. |
| D-004 | Direct IMS messages accept zero and signed amounts; withdrawals and transfers can bypass consistent funds rules. | Require a strictly positive amount. Make direction explicit. Enforce sufficient available funds for withdrawals and transfers in every channel. |
| D-005 | PAYMENT and teller facilities enforce different restrictions, including loan/mortgage handling. | Use one documented product rule set. Cash deposits/withdrawals apply to transactional accounts; loan and mortgage movements use their supported account operations rather than a channel bypass. |
| D-006 | IMS history and account updates can expose partial-write risk; CICS transfer uses an atomic unit of work. | Persist every balance mutation and its immutable transaction/audit records atomically in SQL Server. |
| D-007 | CICS/IMS account type mappings disagree and IMS emits schema-invalid `CHECKING`. | Use one typed target `AccountType` vocabulary and explicit legacy-to-target mapping. Preserve the raw source value only as provenance where needed. |
| D-008 | Several published routes and generated error mappings are not deployable, while related business behavior exists elsewhere. | Design supported REST resources from target business capabilities. Do not reproduce unbound routes or missing generated files merely for path parity. |
| D-009 | Web account/customer create, update, and delete pages contain unsupported placeholders although the terminal business operations exist. | Implement coherent authorized customer and account lifecycle workflows in the modern UI/API; do not preserve `Feature Not Implemented` placeholders. |
| D-010 | Credit-score acquisition is a business step but no usable external provider is available in this repository. | Keep an explicit credit-assessment port. Use a deterministic simulation provider for demo/test and require configuration before any real provider integration. |
| D-011 | The CICS deposit page displays `N/A`, old-account logic is dormant, and a bank-to-bank screen is not deployed. | Correct the balance display. Do not port dormant or non-deployed surfaces. Record them as accepted deviations. |
| D-012 | The PL/I/JCL statement job provides useful reporting but IBM execution mechanics are platform-specific. | Preserve statement contents, period rules, totals, empty-history behavior, and operator invocation through a modern explicitly invoked job/API; do not reproduce JCL or fixed-width printer pagination. |
| D-013 | CICS/IMS loaders and DB2 setup create useful data, but their utilities and data layouts are IBM-specific. | Use versioned EF migrations plus an explicit idempotent import/demo-data command. Normal application startup remains read/write only and never migrates or seeds. |
| D-014 | IBM deployment, Wazi/DBB, 3270, Zowe, debug tooling, trust-all TLS, and unsecured management are operational legacy mechanics. | Replace them with Docker Compose, health checks, structured logging, HTTPS-ready same-origin routing, and secure configuration. Do not port IBM-only tooling or insecure settings. |
| D-015 | Full real-runtime behavior remains blocked without authorized IBM infrastructure. | Continue with the recorded `partial-simulated` evidence. Keep all runtime-dependent claims explicitly unverified and return to Stage 1 if a later real walkthrough contradicts the map. |

## Delivery Slices

Stage 5 must design the target as dependency-ordered slices. Starting at Stage
7, only one slice (or a very small tightly related group) may enter the
build-deliver-revise-accept loop at a time:

1. secure access and application shell;
2. customer management;
3. account management and balances;
4. deposits and withdrawals;
5. funds transfers;
6. transaction history;
7. monthly statements;
8. explicit data initialization/import;
9. deployment, resilience, and operational hardening.

## Gate Result

Every Stage 3 candidate has an explicit keep/change/do-not-port decision. The
target stack and the delivery-slice boundaries are approved for Stage 5 design.
No production implementation is authorized until Stage 5 artifacts pass an
eligible independent Stage 6 review and the owner approves implementation.
