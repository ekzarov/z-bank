# Plan: Deposits and Withdrawals

## Domain and Transaction Boundary

Introduce immutable `Transaction` and `AuditEvent` records plus deposit and
withdrawal application commands. The application loads the account under the
authorization scope, evaluates one product/funds policy, and commits account,
transaction, audit, and idempotency data in one EF/SQL transaction. Concurrency
tokens prevent lost updates.

Use decimal money with explicit currency and SQL precision; never use floating
point. Stable error codes are target-owned and mapped to Problem Details.

## API and UI

Expose customer/account-scoped deposit and withdrawal commands with required
idempotency header. Angular supplies separate explicit commands, positive money
validation, confirmation, and resulting balances. UI validation improves
feedback but does not replace API/domain validation.

## Test Strategy

Unit tests enumerate legacy edge cases and approved corrections. Categorized
SQL Server tests force FK, concurrency, rollback, and duplicate-key paths. API
tests verify authorization and serialization. Vitest covers form states and
result rendering; Playwright covers the critical money happy path.
