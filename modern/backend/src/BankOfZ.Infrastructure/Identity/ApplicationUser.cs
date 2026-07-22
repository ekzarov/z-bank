using Microsoft.AspNetCore.Identity;

namespace BankOfZ.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? CustomerId { get; set; }
}
