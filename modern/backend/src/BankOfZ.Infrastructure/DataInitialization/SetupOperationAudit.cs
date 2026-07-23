namespace BankOfZ.Infrastructure.DataInitialization;

public sealed class SetupOperationAudit
{
    private SetupOperationAudit()
    {
    }

    public Guid Id { get; private set; }
    public string Operation { get; private set; } = null!;
    public string Operator { get; private set; } = null!;
    public string Environment { get; private set; } = null!;
    public string Result { get; private set; } = null!;
    public string? MigrationVersion { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }
    public long DurationMilliseconds { get; private set; }

    public static SetupOperationAudit Succeeded(
        string operation,
        string @operator,
        string environment,
        string? migrationVersion,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            Operation = operation,
            Operator = @operator,
            Environment = environment,
            Result = "Succeeded",
            MigrationVersion = migrationVersion,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            DurationMilliseconds = Math.Max(0, (long)(completedAt - startedAt).TotalMilliseconds)
        };
}
