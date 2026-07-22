using BankOfZ.Domain.Security;
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
    if (await users.FindByNameAsync(userName) is not null)
    {
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

static void EnsureSucceeded(IdentityResult result)
{
    if (!result.Succeeded)
    {
        throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}
