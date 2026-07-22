# Plan: Transaction History

## Design

Reuse the immutable transaction model introduced by Feature 004. Provide
read-only application queries with keyset pagination over booking timestamp and
ID. Project directly to DTOs, apply ownership filtering before materialization,
and expose provenance without creating CICS/IMS-specific routes. Cursors are
opaque/versioned; page size defaults to 50 and caps at 200. Optional UTC date
filters use inclusive-from/exclusive-to semantics.

The target contract replaces the legacy OpenAPI paths whose operation mappings
were missing. It preserves the evidenced business outcome from Java/IMS history
services: recent, newest-first account activity.

## Verification

Unit-test malformed/stale cursor handling, date ranges, and mapping. Categorized SQL Server tests verify
query ordering and pagination at equal timestamps. API tests verify security and
errors. Vitest and tagged Playwright cover customer/operator browsing, details,
authorized and denied scope, filters, and empty history.
