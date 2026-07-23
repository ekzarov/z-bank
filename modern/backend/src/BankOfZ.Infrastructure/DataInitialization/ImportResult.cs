namespace BankOfZ.Infrastructure.DataInitialization;

public sealed record ImportResult(
    Guid RunId,
    Guid AttemptId,
    int AttemptNumber,
    string Fingerprint,
    ImportRunStatus Status,
    int InputCount,
    int PromotedCount,
    int RejectedCount,
    long DurationMilliseconds,
    bool AlreadyApplied,
    string? MigrationVersion);
