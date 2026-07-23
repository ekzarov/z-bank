namespace BankOfZ.Application.AccessAdministration;

public sealed record ChangeAdministrationLockoutCommand(bool Locked, string Version);
