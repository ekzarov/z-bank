# Plan: Account Management and Balances

## Domain and Persistence

Add `Account` with customer ownership, typed status/type/currency, decimal
balances and product terms, provenance, and concurrency token. Monetary columns
use explicit SQL precision. EF mappings live in separate configuration classes;
table names, lengths, precision, and discriminator values use centralized
constants.

Lifecycle commands run through application services and a transaction boundary.
Queries project DTOs without exposing persistence entities. Closure preserves
accounts/history and changes status.

## API and UI

Provide customer-scoped list/detail resources and operator create/update/close
commands. Angular adds account lists to customer detail, stable deep links,
balance presentation, product forms, and closure confirmation. Account type
labels come from the target enum, never raw source routing.

## Test Strategy

Unit-test mappings and eligibility. Categorized SQL Server tests prove FKs,
precision, uniqueness, concurrency, and closure behavior. API tests exercise
ownership and roles. Vitest covers forms and rendering; Playwright covers the
account lifecycle and customer self-view.
