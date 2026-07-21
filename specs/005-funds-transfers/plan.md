# Plan: Funds Transfers

## Design

Implement one transfer application command using the existing account,
transaction, audit, and idempotency model. Load accounts in deterministic ID
order to reduce deadlock risk, apply authorization/product/funds policies, and
commit both account mutations and paired entries in one SQL transaction.

External/interbank networks are out of scope because the legacy screen is not
deployed and no executable integration is evidenced. The API names the
operation internal transfer.

## API, UI, and Tests

Expose an authenticated internal-transfer command and a result containing
correlation ID, references, and permitted balance data. Angular provides an
owned-account transfer form and operator variant. Unit, categorized SQL Server,
API, Vitest, and tagged Playwright tests cover both success and rollback paths.
