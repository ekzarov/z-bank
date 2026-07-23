using System.Globalization;
using BankOfZ.Application.Accounts;
using BankOfZ.Application.Common;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Application.Transactions;

public sealed class InternalTransferService(
    IInternalTransferRepository repository,
    IAccountAuditWriter auditWriter,
    IClock clock)
{
    public Task<InternalTransferView> TransferAsync(
        string sourceAccountId,
        string destinationAccountId,
        decimal amount,
        string idempotencyKey,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!ValidAccountId(sourceAccountId) || !ValidAccountId(destinationAccountId))
        {
            throw Validation(
                "transfer_identifier_invalid",
                "accountId",
                $"Account identifiers must contain exactly {AccountRules.IdLength} digits.");
        }
        if (string.Equals(sourceAccountId, destinationAccountId, StringComparison.Ordinal))
        {
            throw Validation("transfer_same_account", "destinationAccountId", "Source and destination accounts must be different.");
        }

        var key = NormalizeKey(idempotencyKey);
        var fingerprint = $"transfer:{destinationAccountId}:{amount.ToString("0.############################", CultureInfo.InvariantCulture)}";
        return repository.ExecuteSerializableAsync(async () =>
        {
            foreach (var accountId in new[] { sourceAccountId, destinationAccountId }.Order(StringComparer.Ordinal))
            {
                if (!await repository.LockAccountAsync(accountId, cancellationToken))
                {
                    throw new CashTransactionNotFoundException();
                }
            }

            if (await repository.FindByIdempotencyAsync(sourceAccountId, key, cancellationToken) is { } existing)
            {
                if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal) ||
                    existing.TransferCorrelationId is null)
                {
                    throw new CashTransactionConflictException(
                        "idempotency_conflict",
                        "The idempotency key was already used for a different request.");
                }

                var replay = await repository.FindTransferAsync(existing.TransferCorrelationId, cancellationToken);
                if (replay.Count != 2)
                {
                    throw new CashTransactionConflictException(
                        "transfer_history_conflict",
                        "The paired transfer history is incomplete.");
                }
                return ToView(existing.TransferCorrelationId, replay, sourceAccountId);
            }

            var source = await repository.FindAccountAsync(sourceAccountId, cancellationToken)
                ?? throw new CashTransactionNotFoundException();
            var destination = await repository.FindAccountAsync(destinationAccountId, cancellationToken)
                ?? throw new CashTransactionNotFoundException();
            if (!string.Equals(source.Currency, destination.Currency, StringComparison.Ordinal))
            {
                throw Validation("transfer_currency_mismatch", "destinationAccountId", "Accounts must use the same currency.");
            }

            var now = clock.UtcNow;
            var transferCorrelationId = Guid.NewGuid().ToString("N");
            var sourceReference = Guid.NewGuid().ToString("N");
            var destinationReference = Guid.NewGuid().ToString("N");
            source.ApplyCash(CashTransactionDirection.Withdrawal, amount, sourceReference, now);
            destination.ApplyCash(CashTransactionDirection.Deposit, amount, destinationReference, now);

            var sourceBooking = CreateBooking(
                source, CashTransactionDirection.Withdrawal, amount, sourceReference, key, fingerprint, now, transferCorrelationId);
            var destinationBooking = CreateBooking(
                destination, CashTransactionDirection.Deposit, amount, destinationReference,
                transferCorrelationId, fingerprint, now, transferCorrelationId);
            repository.AddRange(sourceBooking, destinationBooking);
            auditWriter.Add(new AccountAuditEntry(
                actor, now, "InternalTransferDebited", source.Id, source.CustomerId, transferCorrelationId));
            auditWriter.Add(new AccountAuditEntry(
                actor, now, "InternalTransferCredited", destination.Id, destination.CustomerId, transferCorrelationId));
            await repository.SaveChangesAsync(cancellationToken);
            return InternalTransferView.From(transferCorrelationId, new(sourceBooking, destinationBooking));
        }, cancellationToken);
    }

    private static BookedTransaction CreateBooking(
        Domain.Accounts.Account account,
        CashTransactionDirection direction,
        decimal amount,
        string reference,
        string idempotencyKey,
        string fingerprint,
        DateTimeOffset now,
        string correlationId) => BookedTransaction.Create(
            reference,
            account.Id,
            account.CustomerId,
            direction,
            amount,
            account.Currency,
            account.ActualBalance,
            account.AvailableBalance,
            idempotencyKey,
            fingerprint,
            now,
            correlationId);

    private static InternalTransferView ToView(
        string correlationId,
        IReadOnlyList<BookedTransaction> transactions,
        string sourceAccountId)
    {
        var source = transactions.Single(transaction => transaction.AccountId == sourceAccountId);
        var destination = transactions.Single(transaction => transaction.AccountId != sourceAccountId);
        return InternalTransferView.From(correlationId, new(source, destination));
    }

    private static string NormalizeKey(string key)
    {
        var normalized = key.Trim();
        if (normalized.Length is 0 or > CashTransactionRules.IdempotencyKeyMaxLength ||
            normalized.Any(character => character is < '!' or > '~'))
        {
            throw Validation(
                "idempotency_key_invalid",
                "idempotencyKey",
                "Idempotency-Key must contain 1 to 64 visible ASCII characters.");
        }
        return normalized;
    }

    private static CashTransactionValidationException Validation(string code, string field, string message) =>
        new(code, new Dictionary<string, string[]> { [field] = [message] });

    private static bool ValidAccountId(string accountId) =>
        accountId.Length == AccountRules.IdLength && accountId.All(char.IsAsciiDigit);
}
