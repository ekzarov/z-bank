namespace BankOfZ.Infrastructure.DataInitialization;

public sealed class ImportReferenceValue
{
    private ImportReferenceValue()
    {
    }

    public string Type { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string SourceIdentifier { get; private set; } = null!;

    public static ImportReferenceValue Import(
        string type,
        string code,
        string description,
        string sourceIdentifier)
    {
        if (string.IsNullOrWhiteSpace(type) ||
            string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(description) ||
            string.IsNullOrWhiteSpace(sourceIdentifier))
        {
            throw new ArgumentException("Reference type, code, description, and source identifier are required.");
        }

        return new()
        {
            Type = type.Trim().ToUpperInvariant(),
            Code = code.Trim().ToUpperInvariant(),
            Description = description.Trim(),
            SourceIdentifier = sourceIdentifier.Trim()
        };
    }
}
