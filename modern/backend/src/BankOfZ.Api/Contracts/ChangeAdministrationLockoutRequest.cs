using System.ComponentModel.DataAnnotations;

namespace BankOfZ.Api.Contracts;

public sealed record ChangeAdministrationLockoutRequest(
    bool Locked,
    [Required] string Version);
