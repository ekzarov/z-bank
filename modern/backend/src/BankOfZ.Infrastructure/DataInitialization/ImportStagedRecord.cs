namespace BankOfZ.Infrastructure.DataInitialization;

public sealed class ImportStagedRecord
{
    private ImportStagedRecord()
    {
    }

    public Guid ImportRunId { get; private set; }
    public string RecordType { get; private set; } = null!;
    public string SourceKey { get; private set; } = null!;
    public string ContentHash { get; private set; } = null!;

    public static ImportStagedRecord Create(
        Guid importRunId,
        string recordType,
        string sourceKey,
        string contentHash) =>
        new()
        {
            ImportRunId = importRunId,
            RecordType = recordType,
            SourceKey = sourceKey,
            ContentHash = contentHash
        };
}
