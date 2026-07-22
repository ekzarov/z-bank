using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Domain.Accounts;

public sealed class Account
{
    private Account()
    {
    }

    public string Id { get; private set; } = null!;
    public string CustomerId { get; private set; } = null!;
    public string SortCode { get; private set; } = null!;
    public AccountType Type { get; private set; }
    public decimal InterestRate { get; private set; }
    public int OverdraftLimit { get; private set; }
    public string Currency { get; private set; } = null!;
    public decimal ActualBalance { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public DateOnly OpenedOn { get; private set; }
    public DateOnly LastStatementOn { get; private set; }
    public DateOnly NextStatementOn { get; private set; }
    public AccountStatus Status { get; private set; }
    public bool HasPendingWork { get; private set; }
    public SourceSystem SourceSystem { get; private set; }
    public string? SourceIdentifier { get; private set; }
    public string? RawSourceType { get; private set; }
    public string? LastTransactionReference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] Version { get; private set; } = [];

    public static Account Create(
        string id,
        string customerId,
        string sortCode,
        AccountMetadata metadata,
        SourceSystem sourceSystem,
        string? sourceIdentifier,
        string? rawSourceType,
        DateTimeOffset now)
    {
        ValidateDigits(id, AccountRules.IdLength, nameof(id));
        ValidateDigits(customerId, CustomerRules.IdLength, nameof(customerId));
        ValidateDigits(sortCode, AccountRules.SortCodeLength, nameof(sortCode));
        ValidateMetadata(metadata);
        ValidateOptional(sourceIdentifier, AccountRules.SourceIdentifierMaxLength, nameof(sourceIdentifier));
        ValidateOptional(rawSourceType, AccountRules.RawSourceValueMaxLength, nameof(rawSourceType));

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        return new Account
        {
            Id = id,
            CustomerId = customerId,
            SortCode = sortCode,
            Type = metadata.Type,
            InterestRate = metadata.InterestRate,
            OverdraftLimit = metadata.OverdraftLimit,
            Currency = metadata.Currency.Trim().ToUpperInvariant(),
            ActualBalance = 0,
            AvailableBalance = 0,
            OpenedOn = today,
            LastStatementOn = today,
            NextStatementOn = today.AddMonths(1),
            Status = AccountStatus.Active,
            SourceSystem = sourceSystem,
            SourceIdentifier = Normalize(sourceIdentifier),
            RawSourceType = Normalize(rawSourceType),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateMetadata(AccountMetadata metadata, DateTimeOffset now)
    {
        EnsureActive();
        ValidateMetadata(metadata);
        Type = metadata.Type;
        InterestRate = metadata.InterestRate;
        OverdraftLimit = metadata.OverdraftLimit;
        Currency = metadata.Currency.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }

    public void Close(DateTimeOffset now)
    {
        EnsureActive();
        if (ActualBalance != 0 || AvailableBalance != 0 || HasPendingWork)
        {
            throw Validation(nameof(Status), "Only a zero-balance account without pending work can be closed.");
        }

        Status = AccountStatus.Closed;
        UpdatedAt = now;
    }

    public void ApplyCash(
        CashTransactionDirection direction,
        decimal amount,
        string transactionReference,
        DateTimeOffset now)
    {
        if (Status != AccountStatus.Active)
        {
            throw CashValidation("cash_account_inactive", nameof(Status), "Cash activity requires an active account.");
        }
        if (Type is AccountType.Loan or AccountType.Mortgage)
        {
            throw CashValidation("cash_product_not_supported", nameof(Type), "Cash activity is not supported for loan or mortgage accounts.");
        }
        if (amount <= 0 || amount > CashTransactionRules.MaximumAmount || decimal.Round(amount, 2) != amount)
        {
            throw CashValidation("cash_amount_invalid", nameof(amount), "Amount must be positive and have at most two decimal places.");
        }
        if (transactionReference.Length != CashTransactionRules.ReferenceLength ||
            transactionReference.Any(character => !char.IsAsciiHexDigit(character)))
        {
            throw CashValidation("cash_reference_invalid", nameof(transactionReference), "Transaction reference is invalid.");
        }

        var delta = direction switch
        {
            CashTransactionDirection.Deposit => amount,
            CashTransactionDirection.Withdrawal when amount <= AvailableBalance + OverdraftLimit => -amount,
            CashTransactionDirection.Withdrawal => throw CashValidation(
                "insufficient_funds", nameof(amount), "Available funds and overdraft are insufficient."),
            _ => throw CashValidation("cash_direction_invalid", nameof(direction), "Cash direction is not supported.")
        };

        ActualBalance += delta;
        AvailableBalance += delta;
        LastTransactionReference = transactionReference;
        UpdatedAt = now;
    }

    private static void ValidateMetadata(AccountMetadata metadata)
    {
        if (!Enum.IsDefined(metadata.Type))
        {
            throw Validation(nameof(metadata.Type), "Account type is not supported.");
        }
        if (metadata.InterestRate < 0 || metadata.InterestRate > AccountRules.MaximumInterestRate ||
            decimal.Round(metadata.InterestRate, 2) != metadata.InterestRate)
        {
            throw Validation(nameof(metadata.InterestRate), "Interest rate must be between 0 and 9999.99 with at most two decimals.");
        }
        if (metadata.Type is AccountType.Loan or AccountType.Mortgage && metadata.InterestRate == 0)
        {
            throw Validation(nameof(metadata.InterestRate), "Loan and mortgage accounts require a non-zero interest rate.");
        }
        if (metadata.OverdraftLimit < 0)
        {
            throw Validation(nameof(metadata.OverdraftLimit), "Overdraft limit cannot be negative.");
        }
        var currency = metadata.Currency.Trim();
        if (currency.Length != AccountRules.CurrencyLength ||
            currency.Any(character => !char.IsAsciiLetter(character)))
        {
            throw Validation(nameof(metadata.Currency), "Currency must be a three-letter code.");
        }
    }

    private void EnsureActive()
    {
        if (Status != AccountStatus.Active)
        {
            throw Validation(nameof(Status), "Closed accounts cannot be changed.");
        }
    }

    private static void ValidateDigits(string value, int length, string field)
    {
        if (value.Length != length || value.Any(character => !char.IsAsciiDigit(character)))
        {
            throw Validation(field, $"{field} must contain exactly {length} digits.");
        }
    }

    private static void ValidateOptional(string? value, int maxLength, string field)
    {
        if (value?.Trim().Length > maxLength)
        {
            throw Validation(field, $"{field} exceeds {maxLength} characters.");
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AccountValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static CashTransactionValidationException CashValidation(string code, string field, string message) =>
        new(code, new Dictionary<string, string[]> { [field] = [message] });
}
