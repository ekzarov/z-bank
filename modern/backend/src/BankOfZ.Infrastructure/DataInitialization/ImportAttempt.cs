namespace BankOfZ.Infrastructure.DataInitialization;

public sealed class ImportAttempt
{
    private ImportAttempt()
    {
    }

    public Guid Id { get; private set; }
    public Guid ImportRunId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string Operator { get; private set; } = null!;
    public string Environment { get; private set; } = null!;
    public ImportAttemptStatus Status { get; private set; }
    public int InputCount { get; private set; }
    public int PromotedCount { get; private set; }
    public int RejectedCount { get; private set; }
    public string? FailureCode { get; private set; }
    public string? MigrationVersion { get; private set; }
    public bool AlreadyApplied { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public long DurationMilliseconds { get; private set; }

    public static ImportAttempt Start(
        Guid importRunId,
        int attemptNumber,
        string @operator,
        string environment,
        int inputCount,
        DateTimeOffset startedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            ImportRunId = importRunId,
            AttemptNumber = attemptNumber,
            Operator = @operator,
            Environment = environment,
            Status = ImportAttemptStatus.Validating,
            InputCount = inputCount,
            StartedAt = startedAt
        };

    public void BeginPromotion() => Status = ImportAttemptStatus.Promoting;

    public void Succeed(int promotedCount, string migrationVersion, bool alreadyApplied, DateTimeOffset completedAt)
    {
        Status = ImportAttemptStatus.Succeeded;
        PromotedCount = promotedCount;
        MigrationVersion = migrationVersion;
        AlreadyApplied = alreadyApplied;
        Complete(completedAt);
    }

    public void Fail(
        ImportAttemptStatus status,
        string failureCode,
        int rejectedCount,
        string? migrationVersion,
        DateTimeOffset completedAt)
    {
        if (status is not (ImportAttemptStatus.FailedValidation or
            ImportAttemptStatus.FailedPromotion or
            ImportAttemptStatus.RejectedActive))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
        PromotedCount = 0;
        RejectedCount = rejectedCount;
        FailureCode = failureCode;
        MigrationVersion = migrationVersion;
        Complete(completedAt);
    }

    private void Complete(DateTimeOffset completedAt)
    {
        CompletedAt = completedAt;
        DurationMilliseconds = Math.Max(0, (long)(completedAt - StartedAt).TotalMilliseconds);
    }
}
