namespace BankOfZ.Infrastructure.DataInitialization;

public sealed class ImportPackageException(string code, IReadOnlyList<string> errors)
    : Exception($"Import package failed validation ({code}).")
{
    public string Code { get; } = code;
    public IReadOnlyList<string> Errors { get; } = errors;
}
