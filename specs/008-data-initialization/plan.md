# Plan: Explicit Data Initialization and Import

## Design

Use EF Core migration bundles or `dotnet ef database update` as an explicit
operator action. Provide a separate .NET command-line import tool under
`modern/backend/tools` that references Application/Infrastructure but is not
hosted by the API. Accept a versioned documented JSON/CSV interchange format;
validate into a staging schema before one atomic promotion to trusted tables.

Large-file parsing uses bounded batches only inside staging; trusted promotion
is atomic. A run ledger and input fingerprint support diagnosis and resumable
retry without exposing partially trusted domain rows. Financial history avoids
blind update: existing source keys must match immutable values or the run fails.
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
