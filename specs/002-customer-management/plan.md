# Plan: Customer Management

## Domain and Data

Add `Customer` as an aggregate with immutable ID, status, contact/address value
objects, provenance, concurrency token, and audit timestamps. Use separate EF
configuration classes and centralized table/length/precision constants. Use a
filtered normalized-name index and enforce unique IDs in SQL Server.

`ICreditAssessmentProvider` belongs to Application; Infrastructure supplies a
deterministic demo adapter configured by explicit options. No network provider
is implied. The application fans out to five configured provider instances,
averages successful scores, and calculates the review date with a controllable
clock. Customer retirement checks accounts through an application policy
inside the same transaction.

## API and UI

Expose role-protected target resources for exact lookup, paged name search,
create, update, and retirement. Return typed DTOs and Problem Details. Angular
provides a searchable customer workspace, details/edit form, guarded retirement
confirmation, and customer self-profile. Failed searches clear stale state and
terminate loading. Read-only account summaries are composed from Feature 003;
cash actions remain Feature 004 links rather than Customer-domain behavior.

## Test Strategy

Unit-test value objects, domain transitions, name normalization, and credit
port outcomes. Categorized integration tests use real SQL Server for indexes,
constraints, concurrency, retirement relationships, and audit atomicity. UI
tests cover validation and state transitions; Playwright covers the operator
happy path.

## Migration

Ship the Customer schema and deterministic demo-provider configuration with
this slice. Do not seed customers at startup; test/demo customers are applied
only through Feature 008's explicit command once available, or test fixtures.
