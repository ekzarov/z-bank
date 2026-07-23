using BankOfZ.Domain.Transactions;

namespace BankOfZ.Domain.Statements;

public sealed record StatementTransactionInput(
    DateTimeOffset BookedAt,
    CashTransactionDirection Direction,
    string Reference,
    string Description,
    decimal Amount);
