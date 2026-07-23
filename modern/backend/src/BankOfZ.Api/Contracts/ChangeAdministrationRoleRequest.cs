using System.ComponentModel.DataAnnotations;

namespace BankOfZ.Api.Contracts;

public sealed record ChangeAdministrationRoleRequest(
    [Required, MaxLength(32)] string Role,
    [MaxLength(10)] string? CustomerId,
    [Required] string Version);
