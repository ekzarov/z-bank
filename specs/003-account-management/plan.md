# Plan: Account Management and Balances

## Domain and Persistence

Add `Account` with customer ownership, typed status/type/currency, decimal
balances and product terms, provenance, and concurrency token. Monetary columns
use explicit SQL precision. EF mappings live in separate configuration classes;
table names, lengths, precision, and discriminator values use centralized
constants.

Lifecycle commands run through application services and a transaction boundary.
Account creation enforces the ten-account-per-customer limit. Identifier
allocation, account persistence, and audit persistence share one transaction;
identifier gaps are allowed but partial entities are not. Statement dates are
system-managed rather than editable metadata. Queries project DTOs without
exposing persistence entities. Closure preserves accounts/history and changes
status instead of reproducing the unconditional legacy hard delete.

Portfolio queries use bounded pagination and remove channel-specific six/ten/
twenty display caps. Account number, configured bank sort code, opening date,
and zero opening balances are assigned inside the transaction. Metadata update
DTOs contain no balance fields. This slice owns balance read models while
Feature 004 exclusively owns balance mutation.

## API and UI

Provide customer-scoped list/detail resources and operator create/update/close
commands. Angular adds account lists to customer detail, stable deep links,
balance presentation, product forms, closure confirmation, and direct deep-link
loading with explicit invalid/not-found states. Account type
labels come from the target enum, never raw source routing.

## Test Strategy

Unit-test mappings and eligibility. Categorized SQL Server tests prove FKs,
precision, configured sort code, portfolio pagination, uniqueness, concurrency,
closure behavior, and allocation/account/audit rollback. API tests exercise
ownership, roles, target Problem Details, and
direct-link outcomes. Vitest covers forms and rendering; Playwright covers the
account lifecycle and customer self-view.
