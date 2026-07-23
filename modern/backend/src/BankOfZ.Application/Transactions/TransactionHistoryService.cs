using System.Globalization;

namespace BankOfZ.Application.Transactions;

public sealed class TransactionHistoryService(
    ITransactionHistoryRepository repository,
    HistoryCursorCodec cursors)
{
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 200;

    public async Task<TransactionHistoryPage> ListAsync(
        string accountId,
        string? from,
        string? to,
        int pageSize,
        string? cursor,
        CancellationToken cancellationToken)
    {
        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw Validation(
                "invalid_history_page_size",
                "pageSize",
                $"Page size must be between 1 and {MaximumPageSize}.");
        }

        var fromUtc = ParseFilter(from, "from");
        var toUtc = ParseFilter(to, "to");
        if (fromUtc >= toUtc)
        {
            throw Validation(
                "invalid_history_range",
                "to",
                "The to filter must be later than the from filter.");
        }

        var position = string.IsNullOrWhiteSpace(cursor)
            ? null
            : cursors.Decode(cursor, accountId, fromUtc, toUtc);
        var records = await repository.ListAsync(
            accountId,
            fromUtc,
            toUtc,
            position?.BookedAt,
            position?.Reference,
            pageSize + 1,
            cancellationToken);
        var items = records.Take(pageSize).Select(TransactionHistoryView.From).ToArray();
        var nextCursor = records.Count > pageSize && items.Length > 0
            ? cursors.Encode(accountId, fromUtc, toUtc, items[^1].BookedAt, items[^1].Reference)
            : null;
        return new(items, pageSize, nextCursor);
    }

    public async Task<TransactionHistoryView> FindAsync(
        string accountId,
        string reference,
        CancellationToken cancellationToken) =>
        TransactionHistoryView.From(
            await repository.FindAsync(accountId, reference, cancellationToken)
            ?? throw new TransactionHistoryNotFoundException());

    private static DateTimeOffset? ParseFilter(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            throw Validation("invalid_history_range", field, $"{field} must be a valid UTC date or timestamp.");
        }
        return parsed.ToUniversalTime();
    }

    private static TransactionHistoryValidationException Validation(
        string code,
        string field,
        string message) => new(
            code,
            new Dictionary<string, string[]> { [field] = [message] });
}
