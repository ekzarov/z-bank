namespace BankOfZ.Api.Contracts;

public sealed record ChangeAdministrationLockoutRequest(bool Locked, string Version);
