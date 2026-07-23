namespace BankOfZ.Application.Transactions;

public sealed record TransactionHistoryPage(
    IReadOnlyList<TransactionHistoryView> Items,
    int PageSize,
    string? NextCursor);
