namespace BankOfZ.Infrastructure.DataInitialization;

public sealed class ImportRun
{
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(15);

    private ImportRun()
    {
    }

    public Guid Id { get; private set; }
    public string PackageVersion { get; private set; } = null!;
    public string InputFingerprint { get; private set; } = null!;
    public string Environment { get; private set; } = null!;
    public ImportRunStatus Status { get; private set; }
    public string StagedManifest { get; private set; } = null!;
    public string? MigrationVersion { get; private set; }
    public int InputCount { get; private set; }
    public int PromotedCount { get; private set; }
    public int RejectedCount { get; private set; }
    public string? FailureCode { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset LeaseExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public long DurationMilliseconds { get; private set; }

    public static ImportRun Stage(
        string packageVersion,
        string inputFingerprint,
        string environment,
        string stagedManifest,
        int inputCount,
        int rejectedCount,
        DateTimeOffset startedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            PackageVersion = packageVersion,
            InputFingerprint = inputFingerprint,
            Environment = environment,
            Status = ImportRunStatus.Validated,
            StagedManifest = stagedManifest,
            InputCount = inputCount,
            RejectedCount = rejectedCount,
            StartedAt = startedAt,
            LeaseExpiresAt = startedAt.Add(LeaseDuration)
        };

    public bool HasActiveLease(DateTimeOffset now) =>
        Status is ImportRunStatus.Validated or ImportRunStatus.Promoting && LeaseExpiresAt > now;

    public void BeginPromotion(DateTimeOffset now)
    {
        if (Status != ImportRunStatus.Validated)
        {
            throw new InvalidOperationException("Only a validated import can be promoted.");
        }

        Status = ImportRunStatus.Promoting;
        LeaseExpiresAt = now.Add(LeaseDuration);
    }

    public void Restage(string stagedManifest, int inputCount, int rejectedCount, DateTimeOffset startedAt)
    {
        var expiredRecoverableRun =
            Status is ImportRunStatus.Validated or ImportRunStatus.Promoting &&
            !HasActiveLease(startedAt);
        if (Status != ImportRunStatus.Failed && !expiredRecoverableRun)
        {
            throw new InvalidOperationException("Only a failed or expired import can be restaged.");
        }

        Status = ImportRunStatus.Validated;
        StagedManifest = stagedManifest;
        InputCount = inputCount;
        PromotedCount = 0;
        RejectedCount = rejectedCount;
        FailureCode = null;
        StartedAt = startedAt;
        LeaseExpiresAt = startedAt.Add(LeaseDuration);
        CompletedAt = null;
        DurationMilliseconds = 0;
    }

    public void Succeed(string migrationVersion, DateTimeOffset completedAt)
    {
        if (Status != ImportRunStatus.Promoting)
        {
            throw new InvalidOperationException("Only a promoting import can succeed.");
        }

        Status = ImportRunStatus.Succeeded;
        PromotedCount = InputCount;
        MigrationVersion = migrationVersion;
        Complete(completedAt);
    }

    public void Fail(string failureCode, string? migrationVersion, DateTimeOffset completedAt)
    {
        if (Status is ImportRunStatus.Succeeded or ImportRunStatus.Failed)
        {
            throw new InvalidOperationException("A completed import cannot be changed.");
        }

        Status = ImportRunStatus.Failed;
        PromotedCount = 0;
        FailureCode = failureCode;
        MigrationVersion = migrationVersion;
        Complete(completedAt);
    }

    private void Complete(DateTimeOffset completedAt)
    {
        if (completedAt < StartedAt)
        {
            throw new InvalidOperationException("Completion cannot precede import start.");
        }

        CompletedAt = completedAt;
        DurationMilliseconds = (long)(completedAt - StartedAt).TotalMilliseconds;
    }
}
