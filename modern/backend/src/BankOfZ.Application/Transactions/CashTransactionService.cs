using System.Globalization;
using BankOfZ.Application.Accounts;
using BankOfZ.Application.Common;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Application.Transactions;

public sealed class CashTransactionService(
    ICashTransactionRepository repository,
    IAccountAuditWriter auditWriter,
    IClock clock)
{
    public Task<CashTransactionView> DepositAsync(
        string accountId,
        decimal amount,
        string idempotencyKey,
        string actor,
        string correlationId,
        CancellationToken cancellationToken) => BookAsync(
            accountId, CashTransactionDirection.Deposit, amount, idempotencyKey, actor, correlationId, cancellationToken);

    public Task<CashTransactionView> WithdrawAsync(
        string accountId,
        decimal amount,
        string idempotencyKey,
        string actor,
        string correlationId,
        CancellationToken cancellationToken) => BookAsync(
            accountId, CashTransactionDirection.Withdrawal, amount, idempotencyKey, actor, correlationId, cancellationToken);

    private Task<CashTransactionView> BookAsync(
        string accountId,
        CashTransactionDirection direction,
        decimal amount,
        string idempotencyKey,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var key = NormalizeKey(idempotencyKey);
        var fingerprint = $"{direction}:{amount.ToString("0.############################", CultureInfo.InvariantCulture)}";
        return repository.ExecuteSerializableAsync(async () =>
        {
            if (!await repository.LockAccountAsync(accountId, cancellationToken))
            {
                throw new CashTransactionNotFoundException();
            }

            if (await repository.FindByIdempotencyAsync(accountId, key, cancellationToken) is { } existing)
            {
                if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    throw new CashTransactionConflictException(
                        "idempotency_conflict",
                        "The idempotency key was already used for a different cash request.");
                }
                return CashTransactionView.From(existing);
            }

            var account = await repository.FindAccountAsync(accountId, cancellationToken)
                ?? throw new CashTransactionNotFoundException();
            var now = clock.UtcNow;
            var reference = Guid.NewGuid().ToString("N");
            account.ApplyCash(direction, amount, reference, now);
            var transaction = BookedTransaction.Create(
                reference,
                account.Id,
                account.CustomerId,
                direction,
                amount,
                account.Currency,
                account.ActualBalance,
                account.AvailableBalance,
                key,
                fingerprint,
                now);
            repository.Add(transaction);
            auditWriter.Add(new AccountAuditEntry(
                actor,
                now,
                direction == CashTransactionDirection.Deposit ? "CashDeposited" : "CashWithdrawn",
                account.Id,
                account.CustomerId,
                correlationId));
            await repository.SaveChangesAsync(cancellationToken);
            return CashTransactionView.From(transaction);
        }, cancellationToken);
    }

    private static string NormalizeKey(string key)
    {
        var normalized = key.Trim();
        if (normalized.Length is 0 or > CashTransactionRules.IdempotencyKeyMaxLength ||
            normalized.Any(character => character is < '!' or > '~'))
        {
            throw new CashTransactionValidationException(
                "idempotency_key_invalid",
                new Dictionary<string, string[]> { ["idempotencyKey"] = ["Idempotency-Key must contain 1 to 64 visible ASCII characters."] });
        }
        return normalized;
    }
}
