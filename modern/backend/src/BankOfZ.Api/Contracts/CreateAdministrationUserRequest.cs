using System.ComponentModel.DataAnnotations;

namespace BankOfZ.Api.Contracts;

public sealed record CreateAdministrationUserRequest(
    [Required, MaxLength(256)] string UserName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(256)] string Password,
    [Required, MaxLength(32)] string Role,
    [MaxLength(10)] string? CustomerId);
