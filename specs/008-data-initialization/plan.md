# Plan: Explicit Data Initialization and Import

## Design

Use EF Core migration bundles or `dotnet ef database update` as an explicit
operator action. Provide a separate .NET command-line import tool under
`modern/backend/tools` that references Application/Infrastructure but is not
hosted by the API. Accept a versioned documented UTF-8 JSON interchange format;
validate into durable normalized staging metadata before one atomic promotion
to trusted tables. Staging contains source keys and content hashes, not
PII-bearing source payloads.

The v1 format has an explicit 10 MiB package limit and is validated in memory;
streaming or larger imports are deferred to a later format/design. Trusted
promotion is atomic. A logical run ledger, immutable attempt records, leases,
and an input fingerprint support diagnosis, crash recovery, and resumable retry
without exposing partially trusted domain rows. Financial history validates
every running-balance step and avoids blind update: existing source keys must
match immutable values or the run fails.
Demo-data packages are separate from production import and require explicit
invocation. The destructive reset/generator preserves useful start/end/step/
seed controls but requires confirmation, authorized environment policy, and a
database role unavailable to normal API runtime.

## Verification

Categorize all migration/import tests as integration and run against disposable
real SQL Server. Verify empty-start behavior, migrations from zero/current,
duplicate runs, malformed input, FK/order errors, rollback, precision, and
audit/run summaries. A container smoke invokes migration and import commands
before starting the API.
