using Microsoft.AspNetCore.Identity;
using BankOfZ.Domain.Customers;

namespace BankOfZ.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
