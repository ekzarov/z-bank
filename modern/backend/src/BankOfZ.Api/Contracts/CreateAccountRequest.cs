using BankOfZ.Domain.Customers;

namespace BankOfZ.Api.Contracts;

public sealed record CreateAccountRequest(
    AccountMetadataRequest Metadata,
    SourceSystem SourceSystem = SourceSystem.Modern,
    string? SourceIdentifier = null,
    string? RawSourceType = null);
