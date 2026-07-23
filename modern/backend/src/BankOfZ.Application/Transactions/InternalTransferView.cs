namespace BankOfZ.Application.Transactions;

public sealed record InternalTransferView(
    string CorrelationId,
    CashTransactionView Source,
    CashTransactionView Destination)
{
    public static InternalTransferView From(
        string correlationId,
        BookedTransactionPair pair) => new(
            correlationId,
            CashTransactionView.From(pair.Source),
            CashTransactionView.From(pair.Destination));
}

public sealed record BookedTransactionPair(
    Domain.Transactions.BookedTransaction Source,
    Domain.Transactions.BookedTransaction Destination);
