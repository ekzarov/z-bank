namespace BankOfZ.Application.Transactions;

public sealed class CashTransactionConflictException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}
