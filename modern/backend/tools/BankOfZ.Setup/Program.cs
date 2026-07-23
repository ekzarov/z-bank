using BankOfZ.Domain.Security;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Accounts;
using BankOfZ.Infrastructure.Identity;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var command = args.SingleOrDefault()
    ?? throw new ArgumentException("Use 'migrate' or 'provision-demo'.");

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetSection("ConnectionStrings")["BankOfZ"]
    ?? throw new InvalidOperationException("Connection string 'BankOfZ' is required.");

builder.Services.AddDbContext<BankOfZIdentityContext>(options => options.UseSqlServer(connectionString));
builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<BankOfZIdentityContext>();

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();

if (string.Equals(command, "migrate", StringComparison.OrdinalIgnoreCase))
{
    await scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>().Database.MigrateAsync();
    Console.WriteLine("Database migrations applied.");
    return;
}

if (!string.Equals(command, "provision-demo", StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException($"Unknown setup command '{command}'.");
}

var password = Environment.GetEnvironmentVariable("BANKOFZ_DEMO_PASSWORD")
    ?? throw new InvalidOperationException("BANKOFZ_DEMO_PASSWORD is required for demo provisioning.");
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var database = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();

await EnsureDemoCustomerAsync(database);
await EnsureDemoAccountsAsync(database);

foreach (var role in BankRoles.All)
{
    if (!await roleManager.RoleExistsAsync(role))
    {
        EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole<Guid>(role)));
    }
}

await EnsureUserAsync(userManager, "customer", "customer@bankofz.demo", BankRoles.Customer, "1000000001", password);
await EnsureUserAsync(userManager, "operator", "operator@bankofz.demo", BankRoles.Operator, null, password);
await EnsureUserAsync(userManager, "administrator", "administrator@bankofz.demo", BankRoles.Administrator, null, password);
Console.WriteLine("Demo identities provisioned explicitly.");

static async Task EnsureUserAsync(
    UserManager<ApplicationUser> users,
    string userName,
    string email,
    string role,
    string? customerId,
    string password)
{
    if (await users.FindByNameAsync(userName) is { } existing)
    {
        foreach (var validator in users.PasswordValidators)
        {
            EnsureSucceeded(await validator.ValidateAsync(users, existing, password));
        }
        existing.CustomerId = customerId;
        existing.PasswordHash = users.PasswordHasher.HashPassword(existing, password);
        existing.SecurityStamp = Guid.NewGuid().ToString();
        EnsureSucceeded(await users.UpdateAsync(existing));
        if (!await users.IsInRoleAsync(existing, role))
        {
            EnsureSucceeded(await users.AddToRoleAsync(existing, role));
        }
        return;
    }

    var user = new ApplicationUser
    {
        Id = Guid.NewGuid(),
        UserName = userName,
        Email = email,
        EmailConfirmed = true,
        CustomerId = customerId
    };
    EnsureSucceeded(await users.CreateAsync(user, password));
    EnsureSucceeded(await users.AddToRoleAsync(user, role));
}

static async Task EnsureDemoCustomerAsync(BankOfZIdentityContext context)
{
    if (await context.Customers.AnyAsync(customer => customer.Id == "1000000001"))
    {
        return;
    }

    context.Customers.Add(Customer.Create(
        "1000000001",
        "100000",
        new CustomerDetails(
            "Ms",
            "Jamie",
            "Customer",
            new DateOnly(1990, 5, 12),
            "1 Demo Street",
            null,
            "London",
            null,
            "EC1A 1AA",
            "GB",
            "customer@bankofz.demo",
            "+44 20 0000 0000"),
        720,
        new DateOnly(2026, 8, 12),
        SourceSystem.Modern,
        "demo-provisioning",
        DateTimeOffset.UtcNow));
    await context.SaveChangesAsync();
}

static async Task EnsureDemoAccountsAsync(BankOfZIdentityContext context)
{
    var demoAccounts = new[]
    {
        (Id: "10000000", Type: AccountType.Current, Interest: 0.25m, Overdraft: 500, RawType: "CURRENT"),
        (Id: "10000099", Type: AccountType.Saving, Interest: 1.50m, Overdraft: 0, RawType: "SAVING")
    };

    var now = DateTimeOffset.UtcNow;
    foreach (var demo in demoAccounts)
    {
        if (await context.Accounts.AnyAsync(account => account.Id == demo.Id))
        {
            continue;
        }

        context.Accounts.Add(Account.Create(
            demo.Id,
            "1000000001",
            "100000",
            new AccountMetadata(demo.Type, demo.Interest, demo.Overdraft, "GBP"),
            SourceSystem.Modern,
            "demo-provisioning",
            demo.RawType,
            now));
        context.AccountAuditEntries.Add(new AccountAuditRecord
        {
            Actor = "setup",
            Timestamp = now,
            Action = "AccountCreated",
            AccountId = demo.Id,
            CustomerId = "1000000001",
            Result = "Succeeded",
            CorrelationId = "demo-provisioning"
        });
    }
    await context.SaveChangesAsync();
}

static void EnsureSucceeded(IdentityResult result)
{
    if (!result.Succeeded)
    {
        throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}
