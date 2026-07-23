using BankOfZ.Domain.Statements;

namespace BankOfZ.Application.Statements;

public interface IStatementRepository
{
    Task<StatementSourceData?> LoadSourceAsync(
        string accountId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListAccountIdsAsync(
        string sortCode,
        CancellationToken cancellationToken);

    Task<StatementSnapshot?> FindByVersionAsync(
        string accountId,
        int year,
        int month,
        string dataVersion,
        CancellationToken cancellationToken);

    Task<StatementSnapshot?> FindAsync(
        string accountId,
        Guid statementId,
        CancellationToken cancellationToken);

    Task<bool> TryAddWithAuditAsync(
        StatementSnapshot statement,
        string actor,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);

    Task WriteAuditAsync(
        Guid? statementId,
        string? accountId,
        string actor,
        string action,
        string result,
        string? diagnostics,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);
}
