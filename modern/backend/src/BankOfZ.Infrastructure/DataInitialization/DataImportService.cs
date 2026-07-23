using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankOfZ.Infrastructure.DataInitialization;

public sealed class DataImportService(BankOfZIdentityContext context)
{
    public async Task<ImportResult> ImportAsync(
        ReadOnlyMemory<byte> packageBytes,
        string @operator,
        string environment,
        CancellationToken cancellationToken = default)
    {
        ValidateInvocation(@operator, environment);
        var startedAt = DateTimeOffset.UtcNow;
        var fingerprint = Fingerprint(packageBytes.Span);
        var run = await context.ImportRuns.SingleOrDefaultAsync(
            value => value.InputFingerprint == fingerprint,
            cancellationToken);

        if (run?.Status == ImportRunStatus.Succeeded)
        {
            var alreadyAppliedAttempt = await StartAttemptAsync(
                run,
                @operator,
                environment,
                run.InputCount,
                startedAt,
                cancellationToken);
            alreadyAppliedAttempt.Succeed(
                0,
                run.MigrationVersion ?? "none",
                alreadyApplied: true,
                DateTimeOffset.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            return ToResult(run, alreadyAppliedAttempt);
        }

        if (run?.HasActiveLease(startedAt) == true)
        {
            var rejectedAttempt = await StartAttemptAsync(
                run,
                @operator,
                environment,
                run.InputCount,
                startedAt,
                cancellationToken);
            rejectedAttempt.Fail(
                ImportAttemptStatus.RejectedActive,
                "import_already_active",
                0,
                await CurrentMigrationAsync(cancellationToken),
                DateTimeOffset.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            throw new ImportPackageException(
                "import_already_active",
                ["An import with this fingerprint has an unexpired active lease."]);
        }

        if (packageBytes.Length > ImportPackageJson.MaximumPackageBytes)
        {
            await RecordRejectedInvocationAsync(
                run,
                fingerprint,
                @operator,
                environment,
                "unknown",
                $$"""{"packageBytes":{{packageBytes.Length}},"maximumBytes":{{ImportPackageJson.MaximumPackageBytes}}}""",
                "package_too_large",
                1,
                startedAt,
                cancellationToken);
            throw new ImportPackageException(
                "package_too_large",
                [$"package: maximum size is {ImportPackageJson.MaximumPackageBytes} bytes"]);
        }

        ImportPackage package;
        try
        {
            package = JsonSerializer.Deserialize<ImportPackage>(packageBytes.Span, ImportPackageJson.Options)
                ?? throw new JsonException("The package is empty.");
        }
        catch (JsonException)
        {
            await RecordRejectedInvocationAsync(
                run,
                fingerprint,
                @operator,
                environment,
                "unknown",
                """{"parse":"failed"}""",
                "package_json_invalid",
                1,
                startedAt,
                cancellationToken);
            throw new ImportPackageException("package_json_invalid", ["package: invalid JSON"]);
        }

        var errors = ImportPackageValidator.Validate(package);
        var manifest = CreateManifest(package);
        var inputCount = CountRecords(package);
        if (errors.Count > 0)
        {
            await RecordRejectedInvocationAsync(
                run,
                fingerprint,
                @operator,
                environment,
                package.SchemaVersion,
                manifest,
                "package_validation_failed",
                errors.Count,
                startedAt,
                cancellationToken);
            throw new ImportPackageException("package_validation_failed", errors);
        }

        run = PrepareRun(
            run,
            package.SchemaVersion,
            fingerprint,
            environment,
            manifest,
            inputCount,
            0,
            startedAt);
        var attempt = await StartAttemptAsync(
            run,
            @operator,
            environment,
            inputCount,
            startedAt,
            cancellationToken);

        try
        {
            await context.ImportStagedRecords
                .Where(record => record.ImportRunId == run.Id)
                .ExecuteDeleteAsync(cancellationToken);
            context.ImportStagedRecords.AddRange(CreateStagedRecords(run.Id, package));
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            var concurrentRun = await context.ImportRuns.SingleOrDefaultAsync(
                value => value.InputFingerprint == fingerprint,
                cancellationToken);
            if (concurrentRun is null)
            {
                throw;
            }

            var rejectedAttempt = await StartAttemptAsync(
                concurrentRun,
                @operator,
                environment,
                inputCount,
                startedAt,
                cancellationToken);
            rejectedAttempt.Fail(
                ImportAttemptStatus.RejectedActive,
                "import_already_active",
                0,
                await CurrentMigrationAsync(cancellationToken),
                DateTimeOffset.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            throw new ImportPackageException(
                "import_already_active",
                ["A concurrent import acquired this fingerprint."]);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            run.BeginPromotion(DateTimeOffset.UtcNow);
            attempt.BeginPromotion();
            await ApplyPackageAsync(package, cancellationToken);
            var migrationVersion = await CurrentMigrationAsync(cancellationToken) ?? "none";
            var completedAt = DateTimeOffset.UtcNow;
            run.Succeed(migrationVersion, completedAt);
            attempt.Succeed(inputCount, migrationVersion, alreadyApplied: false, completedAt);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResult(run, attempt);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            var failedRun = await context.ImportRuns.SingleAsync(value => value.Id == run.Id, cancellationToken);
            var failedAttempt = await context.ImportAttempts.SingleAsync(value => value.Id == attempt.Id, cancellationToken);
            var failureCode = ClassifyFailure(exception);
            var migrationVersion = await CurrentMigrationAsync(cancellationToken);
            var completedAt = DateTimeOffset.UtcNow;
            failedRun.Fail(failureCode, migrationVersion, completedAt);
            failedAttempt.Fail(
                ImportAttemptStatus.FailedPromotion,
                failureCode,
                0,
                migrationVersion,
                completedAt);
            await context.SaveChangesAsync(cancellationToken);
            throw new ImportPackageException(
                failureCode,
                [$"promotion: failed for fingerprint {fingerprint[..12]}"]);
        }
    }

    public async Task<ImportResult?> VerifyAsync(
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        var run = await context.ImportRuns.AsNoTracking().SingleOrDefaultAsync(
            value => value.InputFingerprint == fingerprint.ToLowerInvariant(),
            cancellationToken);
        if (run is null)
        {
            return null;
        }

        var attempt = await context.ImportAttempts.AsNoTracking()
            .Where(value => value.ImportRunId == run.Id)
            .OrderByDescending(value => value.AttemptNumber)
            .FirstAsync(cancellationToken);
        return ToResult(run, attempt);
    }

    private async Task ApplyPackageAsync(ImportPackage package, CancellationToken cancellationToken)
    {
        foreach (var value in package.Customers)
        {
            var existing = await context.Customers.SingleOrDefaultAsync(
                customer =>
                    customer.Id == value.Id ||
                    customer.SourceSystem == value.SourceSystem &&
                    customer.SourceIdentifier == value.SourceIdentifier,
                cancellationToken);
            if (existing is null)
            {
                context.Customers.Add(ToCustomer(value));
            }
            else
            {
                EnsureCustomerMatches(existing, value);
            }
        }
        await context.SaveChangesAsync(cancellationToken);

        foreach (var value in package.Accounts)
        {
            var existing = await context.Accounts.SingleOrDefaultAsync(
                account =>
                    account.Id == value.Id ||
                    account.SourceSystem == value.SourceSystem &&
                    account.SourceIdentifier == value.SourceIdentifier,
                cancellationToken);
            if (existing is null)
            {
                context.Accounts.Add(ToAccount(value));
            }
            else
            {
                EnsureAccountMatches(existing, value);
            }
        }
        await context.SaveChangesAsync(cancellationToken);

        foreach (var value in package.Transactions)
        {
            var existing = await context.BookedTransactions.SingleOrDefaultAsync(
                transaction => transaction.SourceSystem == SourceSystem.Ims &&
                               transaction.SourceIdentifier == value.SourceIdentifier,
                cancellationToken);
            if (existing is null)
            {
                context.BookedTransactions.Add(ToTransaction(value));
            }
            else
            {
                EnsureTransactionMatches(existing, value);
            }
        }

        foreach (var value in package.TransactionRuns)
        {
            var existing = await context.LegacyTransactionRuns.SingleOrDefaultAsync(
                item => item.SourceIdentifier == value.SourceIdentifier,
                cancellationToken);
            if (existing is null)
            {
                context.LegacyTransactionRuns.Add(LegacyTransactionRun.Import(
                    value.SourceIdentifier,
                    value.Status,
                    value.StartedAt,
                    value.StoppedAt,
                    value.CustomerId));
            }
            else if (existing.Status != value.Status.Trim().ToUpperInvariant() ||
                     existing.StartedAt != value.StartedAt ||
                     existing.StoppedAt != value.StoppedAt ||
                     existing.CustomerId != value.CustomerId)
            {
                throw Conflict("transaction_run_source_conflict");
            }
        }

        foreach (var value in package.ReferenceData)
        {
            var type = value.Type.Trim().ToUpperInvariant();
            var code = value.Code.Trim().ToUpperInvariant();
            var existing = await context.ImportReferenceValues.SingleOrDefaultAsync(
                reference => reference.Type == type && reference.Code == code,
                cancellationToken);
            if (existing is null)
            {
                context.ImportReferenceValues.Add(ImportReferenceValue.Import(
                    value.Type,
                    value.Code,
                    value.Description,
                    value.SourceIdentifier));
            }
            else if (existing.Description != value.Description.Trim() ||
                     existing.SourceIdentifier != value.SourceIdentifier.Trim())
            {
                throw Conflict("reference_source_conflict");
            }
        }
    }

    private async Task RecordRejectedInvocationAsync(
        ImportRun? run,
        string fingerprint,
        string @operator,
        string environment,
        string packageVersion,
        string manifest,
        string failureCode,
        int rejectedCount,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        run = PrepareRun(
            run,
            packageVersion,
            fingerprint,
            environment,
            manifest,
            0,
            rejectedCount,
            startedAt);
        var attempt = await StartAttemptAsync(
            run,
            @operator,
            environment,
            0,
            startedAt,
            cancellationToken);
        var migrationVersion = await CurrentMigrationAsync(cancellationToken);
        var completedAt = DateTimeOffset.UtcNow;
        run.Fail(failureCode, migrationVersion, completedAt);
        attempt.Fail(
            ImportAttemptStatus.FailedValidation,
            failureCode,
            rejectedCount,
            migrationVersion,
            completedAt);
        await context.SaveChangesAsync(cancellationToken);
    }

    private ImportRun PrepareRun(
        ImportRun? run,
        string packageVersion,
        string fingerprint,
        string environment,
        string manifest,
        int inputCount,
        int rejectedCount,
        DateTimeOffset startedAt)
    {
        if (run is null)
        {
            run = ImportRun.Stage(
                packageVersion,
                fingerprint,
                environment.Trim(),
                manifest,
                inputCount,
                rejectedCount,
                startedAt);
            context.ImportRuns.Add(run);
        }
        else
        {
            run.Restage(manifest, inputCount, rejectedCount, startedAt);
        }

        return run;
    }

    private async Task<ImportAttempt> StartAttemptAsync(
        ImportRun run,
        string @operator,
        string environment,
        int inputCount,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        var priorAttempts = await context.ImportAttempts.CountAsync(
            value => value.ImportRunId == run.Id,
            cancellationToken);
        var attempt = ImportAttempt.Start(
            run.Id,
            priorAttempts + 1,
            @operator.Trim(),
            environment.Trim(),
            inputCount,
            startedAt);
        context.ImportAttempts.Add(attempt);
        return attempt;
    }

    private static Customer ToCustomer(ImportCustomer value) => Customer.Import(
        value.Id,
        value.SortCode,
        new CustomerDetails(
            value.Title,
            value.FirstName,
            value.LastName,
            value.DateOfBirth,
            value.AddressLine1,
            value.AddressLine2,
            value.City,
            value.Region,
            value.PostalCode,
            value.CountryCode,
            value.Email,
            value.Phone),
        value.CreditScore,
        value.CreditReviewDate,
        value.Status,
        value.SourceSystem,
        value.SourceIdentifier,
        value.CreatedAt,
        value.UpdatedAt);

    private static Account ToAccount(ImportAccount value) => Account.Import(
        value.Id,
        value.CustomerId,
        value.SortCode,
        new AccountMetadata(value.Type, value.InterestRate, value.OverdraftLimit, value.Currency),
        value.ActualBalance,
        value.AvailableBalance,
        value.OpenedOn,
        value.LastStatementOn,
        value.NextStatementOn,
        value.Status,
        value.HasPendingWork,
        value.SourceSystem,
        value.SourceIdentifier,
        value.RawSourceType,
        value.LastTransactionReference,
        value.CreatedAt,
        value.UpdatedAt);

    private static BookedTransaction ToTransaction(ImportTransaction value)
    {
        var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(value.SourceIdentifier))[..16]);
        return BookedTransaction.Import(
            id,
            value.Reference,
            value.AccountId,
            value.CustomerId,
            value.Direction,
            value.Amount,
            value.Currency,
            value.ResultingActualBalance,
            value.ResultingAvailableBalance,
            value.SourceIdentifier,
            value.CreatedAt);
    }

    private static void EnsureCustomerMatches(Customer existing, ImportCustomer value)
    {
        if (existing.Id != value.Id ||
            existing.SortCode != value.SortCode ||
            existing.Title != value.Title.Trim() ||
            existing.FirstName != value.FirstName.Trim() ||
            existing.LastName != value.LastName.Trim() ||
            existing.DateOfBirth != value.DateOfBirth ||
            existing.AddressLine1 != value.AddressLine1.Trim() ||
            existing.AddressLine2 != Normalize(value.AddressLine2) ||
            existing.City != value.City.Trim() ||
            existing.Region != Normalize(value.Region) ||
            existing.PostalCode != value.PostalCode.Trim() ||
            existing.CountryCode != value.CountryCode.Trim().ToUpperInvariant() ||
            existing.Email != value.Email.Trim().ToLowerInvariant() ||
            existing.Phone != Normalize(value.Phone) ||
            existing.Status != value.Status ||
            existing.CreditScore != value.CreditScore ||
            existing.CreditReviewDate != value.CreditReviewDate ||
            existing.SourceSystem != value.SourceSystem ||
            existing.SourceIdentifier != value.SourceIdentifier.Trim() ||
            existing.CreatedAt != value.CreatedAt ||
            existing.UpdatedAt != value.UpdatedAt)
        {
            throw Conflict("customer_source_conflict");
        }
    }

    private static void EnsureAccountMatches(Account existing, ImportAccount value)
    {
        if (existing.Id != value.Id ||
            existing.CustomerId != value.CustomerId ||
            existing.SortCode != value.SortCode ||
            existing.Type != value.Type ||
            existing.InterestRate != value.InterestRate ||
            existing.OverdraftLimit != value.OverdraftLimit ||
            existing.Currency != value.Currency.Trim().ToUpperInvariant() ||
            existing.ActualBalance != value.ActualBalance ||
            existing.AvailableBalance != value.AvailableBalance ||
            existing.OpenedOn != value.OpenedOn ||
            existing.LastStatementOn != value.LastStatementOn ||
            existing.NextStatementOn != value.NextStatementOn ||
            existing.Status != value.Status ||
            existing.HasPendingWork != value.HasPendingWork ||
            existing.SourceSystem != value.SourceSystem ||
            existing.SourceIdentifier != value.SourceIdentifier.Trim() ||
            existing.RawSourceType != Normalize(value.RawSourceType) ||
            existing.LastTransactionReference != value.LastTransactionReference ||
            existing.CreatedAt != value.CreatedAt ||
            existing.UpdatedAt != value.UpdatedAt)
        {
            throw Conflict("account_source_conflict");
        }
    }

    private static void EnsureTransactionMatches(BookedTransaction existing, ImportTransaction value)
    {
        if (existing.Reference != value.Reference ||
            existing.AccountId != value.AccountId ||
            existing.CustomerId != value.CustomerId ||
            existing.Direction != value.Direction ||
            existing.Amount != value.Amount ||
            existing.Currency != value.Currency.Trim().ToUpperInvariant() ||
            existing.ResultingActualBalance != value.ResultingActualBalance ||
            existing.ResultingAvailableBalance != value.ResultingAvailableBalance ||
            existing.CreatedAt != value.CreatedAt)
        {
            throw Conflict("transaction_source_conflict");
        }
    }

    private static IEnumerable<ImportStagedRecord> CreateStagedRecords(Guid runId, ImportPackage package)
    {
        foreach (var value in package.Customers)
        {
            yield return Staged(runId, "customer", $"{value.SourceSystem}:{value.SourceIdentifier}", value);
        }
        foreach (var value in package.Accounts)
        {
            yield return Staged(runId, "account", $"{value.SourceSystem}:{value.SourceIdentifier}", value);
        }
        foreach (var value in package.Transactions)
        {
            yield return Staged(runId, "transaction", value.SourceIdentifier, value);
        }
        foreach (var value in package.TransactionRuns)
        {
            yield return Staged(runId, "transaction-run", value.SourceIdentifier, value);
        }
        foreach (var value in package.ReferenceData)
        {
            yield return Staged(runId, "reference", $"{value.Type}:{value.Code}", value);
        }
    }

    private static ImportStagedRecord Staged<T>(Guid runId, string type, string sourceKey, T value) =>
        ImportStagedRecord.Create(
            runId,
            type,
            sourceKey,
            Fingerprint(JsonSerializer.SerializeToUtf8Bytes(value, ImportPackageJson.Options)));

    private async Task<string?> CurrentMigrationAsync(CancellationToken cancellationToken) =>
        (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault();

    private static InvalidOperationException Conflict(string code) => new(code);

    private static string ClassifyFailure(Exception exception) =>
        exception.Message.EndsWith("_source_conflict", StringComparison.Ordinal)
            ? exception.Message
            : "promotion_failed";

    private static int CountRecords(ImportPackage package) =>
        package.Customers.Count +
        package.Accounts.Count +
        package.Transactions.Count +
        package.TransactionRuns.Count +
        package.ReferenceData.Count;

    private static string CreateManifest(ImportPackage package) => JsonSerializer.Serialize(
        new
        {
            package.SchemaVersion,
            package.Source,
            counts = new
            {
                customers = package.Customers.Count,
                accounts = package.Accounts.Count,
                transactions = package.Transactions.Count,
                transactionRuns = package.TransactionRuns.Count,
                referenceData = package.ReferenceData.Count
            }
        },
        ImportPackageJson.Options);

    private static ImportResult ToResult(ImportRun run, ImportAttempt attempt) =>
        new(
            run.Id,
            attempt.Id,
            attempt.AttemptNumber,
            run.InputFingerprint,
            run.Status,
            attempt.InputCount,
            attempt.PromotedCount,
            attempt.RejectedCount,
            attempt.DurationMilliseconds,
            attempt.AlreadyApplied,
            attempt.MigrationVersion);

    private static string Fingerprint(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateInvocation(string @operator, string environment)
    {
        if (string.IsNullOrWhiteSpace(@operator))
        {
            throw new ArgumentException("An authenticated operator identifier is required.", nameof(@operator));
        }
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("The target environment is required.", nameof(environment));
        }
    }
}
