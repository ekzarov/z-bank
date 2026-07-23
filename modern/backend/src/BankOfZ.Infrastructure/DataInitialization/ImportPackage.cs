using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;

namespace BankOfZ.Infrastructure.DataInitialization;

public sealed record ImportPackage(
    string SchemaVersion,
    string Source,
    IReadOnlyList<ImportCustomer> Customers,
    IReadOnlyList<ImportAccount> Accounts,
    IReadOnlyList<ImportTransaction> Transactions,
    IReadOnlyList<ImportLegacyTransactionRun> TransactionRuns,
    IReadOnlyList<ImportReference> ReferenceData);

public sealed record ImportCustomer(
    string Id,
    string SortCode,
    string Title,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string PostalCode,
    string CountryCode,
    string Email,
    string? Phone,
    CustomerStatus Status,
    decimal CreditScore,
    DateOnly CreditReviewDate,
    SourceSystem SourceSystem,
    string SourceIdentifier,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ImportAccount(
    string Id,
    string CustomerId,
    string SortCode,
    AccountType Type,
    decimal InterestRate,
    int OverdraftLimit,
    string Currency,
    decimal ActualBalance,
    decimal AvailableBalance,
    DateOnly OpenedOn,
    DateOnly LastStatementOn,
    DateOnly NextStatementOn,
    AccountStatus Status,
    bool HasPendingWork,
    SourceSystem SourceSystem,
    string SourceIdentifier,
    string? RawSourceType,
    string? LastTransactionReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ImportTransaction(
    string SourceIdentifier,
    string Reference,
    string AccountId,
    string CustomerId,
    CashTransactionDirection Direction,
    decimal Amount,
    string Currency,
    decimal ResultingActualBalance,
    decimal ResultingAvailableBalance,
    DateTimeOffset CreatedAt);

public sealed record ImportLegacyTransactionRun(
    string SourceIdentifier,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? StoppedAt,
    string CustomerId);

public sealed record ImportReference(
    string Type,
    string Code,
    string Description,
    string SourceIdentifier);
