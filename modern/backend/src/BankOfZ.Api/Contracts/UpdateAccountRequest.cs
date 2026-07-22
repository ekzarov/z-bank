namespace BankOfZ.Api.Contracts;

public sealed record UpdateAccountRequest(AccountMetadataRequest Metadata, string Version);
