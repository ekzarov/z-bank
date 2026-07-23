namespace BankOfZ.Api.Contracts;

public sealed record InternalTransferRequest(string DestinationAccountId, decimal Amount);
