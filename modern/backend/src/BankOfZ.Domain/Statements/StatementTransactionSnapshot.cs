using BankOfZ.Domain.Transactions;

namespace BankOfZ.Domain.Statements;

public sealed class StatementTransactionSnapshot
{
    private StatementTransactionSnapshot()
    {
    }

    public Guid Id { get; private set; }
    public Guid StatementId { get; private set; }
    public int Sequence { get; private set; }
    public DateTimeOffset BookedAt { get; private set; }
    public CashTransactionDirection Direction { get; private set; }
    public string Reference { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public decimal Amount { get; private set; }

    internal static StatementTransactionSnapshot Create(
        Guid statementId,
        int sequence,
        StatementTransactionInput input) => new()
        {
            Id = Guid.NewGuid(),
            StatementId = statementId,
            Sequence = sequence,
            BookedAt = input.BookedAt,
            Direction = input.Direction,
            Reference = input.Reference,
            Description = input.Description,
            Amount = input.Amount
        };
}
