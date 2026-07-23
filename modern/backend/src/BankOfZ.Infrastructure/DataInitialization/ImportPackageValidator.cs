using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Infrastructure.DataInitialization;

public static partial class ImportPackageValidator
{
    public static IReadOnlyList<string> Validate(ImportPackage package)
    {
        var errors = new List<string>();
        if (package.SchemaVersion != ImportPackageJson.CurrentSchemaVersion)
        {
            errors.Add("package.schemaVersion: unsupported version");
        }
        if (string.IsNullOrWhiteSpace(package.Source))
        {
            errors.Add("package.source: required");
        }
        if (package.Customers is null ||
            package.Accounts is null ||
            package.Transactions is null ||
            package.TransactionRuns is null ||
            package.ReferenceData is null)
        {
            errors.Add("package: all record collections are required");
            return errors;
        }

        CheckDuplicates(package.Customers, value => value.Id, "customers", errors);
        CheckDuplicates(
            package.Customers,
            value => $"{value.SourceSystem}:{value.SourceIdentifier}",
            "customers.source",
            errors);
        CheckDuplicates(package.Accounts, value => value.Id, "accounts", errors);
        CheckDuplicates(
            package.Accounts,
            value => $"{value.SourceSystem}:{value.SourceIdentifier}",
            "accounts.source",
            errors);
        CheckDuplicates(package.Transactions, value => value.SourceIdentifier, "transactions", errors);
        CheckDuplicates(package.TransactionRuns, value => value.SourceIdentifier, "transactionRuns", errors);
        CheckDuplicates(package.ReferenceData, value => $"{value.Type}:{value.Code}", "referenceData", errors);

        var customerIds = package.Customers.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var account in package.Accounts)
        {
            if (!customerIds.Contains(account.CustomerId))
            {
                errors.Add($"accounts[{Safe(account.Id)}].customerId: missing package customer");
            }
        }

        var accountIds = package.Accounts.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var transaction in package.Transactions)
        {
            if (!accountIds.Contains(transaction.AccountId))
            {
                errors.Add($"transactions[{Safe(transaction.SourceIdentifier)}].accountId: missing package account");
            }
            if (!customerIds.Contains(transaction.CustomerId))
            {
                errors.Add($"transactions[{Safe(transaction.SourceIdentifier)}].customerId: missing package customer");
            }
            if (transaction.Amount <= 0 || decimal.Round(transaction.Amount, 2) != transaction.Amount)
            {
                errors.Add($"transactions[{Safe(transaction.SourceIdentifier)}].amount: positive value with two decimals required");
            }
            if (transaction.Reference.Length != CashTransactionRules.ReferenceLength ||
                transaction.Reference.Any(character => !char.IsAsciiHexDigit(character)))
            {
                errors.Add(
                    $"transactions[{Safe(transaction.SourceIdentifier)}].reference: " +
                    $"{CashTransactionRules.ReferenceLength} hexadecimal characters required");
            }
        }

        foreach (var account in package.Accounts)
        {
            var history = package.Transactions
                .Where(transaction => transaction.AccountId == account.Id)
                .OrderBy(transaction => transaction.CreatedAt)
                .ThenBy(transaction => transaction.SourceIdentifier, StringComparer.Ordinal)
                .ToArray();
            var latest = history.LastOrDefault();
            if (latest is not null &&
                (latest.ResultingActualBalance != account.ActualBalance ||
                 latest.ResultingAvailableBalance != account.AvailableBalance))
            {
                errors.Add($"accounts[{Safe(account.Id)}].balances: latest history balance does not match account");
            }

            for (var index = 1; index < history.Length; index++)
            {
                var delta = history[index].Direction == CashTransactionDirection.Deposit
                    ? history[index].Amount
                    : -history[index].Amount;
                if (history[index].ResultingActualBalance != history[index - 1].ResultingActualBalance + delta ||
                    history[index].ResultingAvailableBalance != history[index - 1].ResultingAvailableBalance + delta)
                {
                    errors.Add(
                        $"transactions[{Safe(history[index].SourceIdentifier)}].balances: " +
                        "running balance does not follow prior history");
                }
            }
        }

        foreach (var run in package.TransactionRuns)
        {
            if (!customerIds.Contains(run.CustomerId))
            {
                errors.Add($"transactionRuns[{Safe(run.SourceIdentifier)}].customerId: missing package customer");
            }
            if (run.StoppedAt < run.StartedAt)
            {
                errors.Add($"transactionRuns[{Safe(run.SourceIdentifier)}].stoppedAt: cannot precede start");
            }
        }

        ValidateDomainObjects(package, errors);
        return errors;
    }

    private static void ValidateDomainObjects(ImportPackage package, ICollection<string> errors)
    {
        foreach (var value in package.Customers)
        {
            Capture($"customers[{Safe(value.Id)}]", errors, () => Customer.Import(
                value.Id,
                value.SortCode,
                new CustomerDetails(
                    value.Title,
                    value.FirstName,
                    value.LastName,
                    value.DateOfBirth,
                    value.AddressLine1,
                    value.AddressLine2,
                    value.City,
                    value.Region,
                    value.PostalCode,
                    value.CountryCode,
                    value.Email,
                    value.Phone),
                value.CreditScore,
                value.CreditReviewDate,
                value.Status,
                value.SourceSystem,
                value.SourceIdentifier,
                value.CreatedAt,
                value.UpdatedAt));
        }
        foreach (var value in package.Accounts)
        {
            Capture($"accounts[{Safe(value.Id)}]", errors, () => Account.Import(
                value.Id,
                value.CustomerId,
                value.SortCode,
                new AccountMetadata(value.Type, value.InterestRate, value.OverdraftLimit, value.Currency),
                value.ActualBalance,
                value.AvailableBalance,
                value.OpenedOn,
                value.LastStatementOn,
                value.NextStatementOn,
                value.Status,
                value.HasPendingWork,
                value.SourceSystem,
                value.SourceIdentifier,
                value.RawSourceType,
                value.LastTransactionReference,
                value.CreatedAt,
                value.UpdatedAt));
        }
    }

    private static void Capture(string source, ICollection<string> errors, Action action)
    {
        try
        {
            action();
        }
        catch (AccountValidationException exception)
        {
            foreach (var error in exception.Errors.SelectMany(
                         pair => pair.Value.Select(message => $"{source}.{pair.Key}: {message}")))
            {
                errors.Add(error);
            }
        }
        catch (CustomerValidationException exception)
        {
            foreach (var error in exception.Errors.SelectMany(
                         pair => pair.Value.Select(message => $"{source}.{pair.Key}: {message}")))
            {
                errors.Add(error);
            }
        }
        catch (ArgumentException exception)
        {
            errors.Add($"{source}: {exception.Message}");
        }
    }

    private static void CheckDuplicates<T>(
        IEnumerable<T> values,
        Func<T, string> keySelector,
        string source,
        ICollection<string> errors)
    {
        foreach (var duplicate in values.GroupBy(keySelector, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            errors.Add($"{source}[{Safe(duplicate.Key)}]: duplicate source key");
        }
    }

    private static string Safe(string value) =>
        value.Length <= 64 ? value : value[..64];
}
