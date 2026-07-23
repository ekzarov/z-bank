namespace BankOfZ.Infrastructure.DataInitialization;

public sealed class LegacyTransactionRun
{
    private LegacyTransactionRun()
    {
    }

    public string SourceIdentifier { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? StoppedAt { get; private set; }
    public string CustomerId { get; private set; } = null!;

    public static LegacyTransactionRun Import(
        string sourceIdentifier,
        string status,
        DateTimeOffset startedAt,
        DateTimeOffset? stoppedAt,
        string customerId)
    {
        if (string.IsNullOrWhiteSpace(sourceIdentifier) || string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Legacy transaction run source identifier and status are required.");
        }
        if (stoppedAt < startedAt)
        {
            throw new ArgumentException("Legacy transaction run stop time cannot precede start time.");
        }

        return new()
        {
            SourceIdentifier = sourceIdentifier.Trim(),
            Status = status.Trim().ToUpperInvariant(),
            StartedAt = startedAt,
            StoppedAt = stoppedAt,
            CustomerId = customerId
        };
    }
}
