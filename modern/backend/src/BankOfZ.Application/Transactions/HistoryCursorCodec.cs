using System.Text;
using System.Text.Json;

namespace BankOfZ.Application.Transactions;

public sealed class HistoryCursorCodec
{
    private const int CurrentVersion = 1;

    public string Encode(
        string accountId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        DateTimeOffset bookedAt,
        string reference)
    {
        var payload = new CursorPayload(
            CurrentVersion,
            accountId,
            Ticks(from),
            Ticks(to),
            bookedAt.UtcDateTime.Ticks,
            reference);
        return Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
    }

    public HistoryCursor Decode(
        string value,
        string accountId,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<CursorPayload>(Base64UrlDecode(value))
                ?? throw new JsonException();
            if (payload.Version != CurrentVersion ||
                !string.Equals(payload.AccountId, accountId, StringComparison.Ordinal) ||
                payload.FromUtcTicks != Ticks(from) ||
                payload.ToUtcTicks != Ticks(to) ||
                payload.BookedAtUtcTicks <= 0 ||
                string.IsNullOrWhiteSpace(payload.Reference))
            {
                throw InvalidCursor();
            }

            return new HistoryCursor(
                new DateTimeOffset(payload.BookedAtUtcTicks, TimeSpan.Zero),
                payload.Reference);
        }
        catch (TransactionHistoryValidationException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is FormatException or JsonException or ArgumentOutOfRangeException)
        {
            throw InvalidCursor();
        }
    }

    private static long? Ticks(DateTimeOffset? value) => value?.UtcDateTime.Ticks;

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized += new string('=', (4 - normalized.Length % 4) % 4);
        return Convert.FromBase64String(normalized);
    }

    private static TransactionHistoryValidationException InvalidCursor() => new(
        "invalid_history_cursor",
        new Dictionary<string, string[]> { ["cursor"] = ["The history cursor is invalid or belongs to a different query."] });

    private sealed record CursorPayload(
        int Version,
        string AccountId,
        long? FromUtcTicks,
        long? ToUtcTicks,
        long BookedAtUtcTicks,
        string Reference);
}
