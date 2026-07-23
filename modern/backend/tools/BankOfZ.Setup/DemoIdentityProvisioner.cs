using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace BankOfZ.Setup;

internal static class DemoIdentityProvisioner
{
    public static async Task ProvisionAsync(
        RoleManager<IdentityRole<Guid>> roles,
        UserManager<ApplicationUser> users,
        string password)
    {
        foreach (var role in BankRoles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roles.CreateAsync(new IdentityRole<Guid>(role)));
            }
        }

        await EnsureUserAsync(users, "customer", "customer@bankofz.demo", BankRoles.Customer, "1000000001", password);
        await EnsureUserAsync(users, "operator", "operator@bankofz.demo", BankRoles.Operator, null, password);
        await EnsureUserAsync(users, "administrator", "administrator@bankofz.demo", BankRoles.Administrator, null, password);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> users,
        string userName,
        string email,
        string role,
        string? customerId,
        string password)
    {
        var user = await users.FindByNameAsync(userName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                CustomerId = customerId
            };
            EnsureSucceeded(await users.CreateAsync(user, password));
        }
        else
        {
            user.CustomerId = customerId;
            user.PasswordHash = users.PasswordHasher.HashPassword(user, password);
            user.SecurityStamp = Guid.NewGuid().ToString();
            EnsureSucceeded(await users.UpdateAsync(user));
        }

        if (!await users.IsInRoleAsync(user, role))
        {
            EnsureSucceeded(await users.AddToRoleAsync(user, role));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
