using System.Text.Json;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.Identity;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankOfZ.IntegrationTests;

public sealed class BankOfZTestsFixture : IAsyncLifetime
{
    public BankOfZTestsFixture()
    {
        ConnectionString = ReadConnectionString();
        Factory = new BankOfZApiFactory(ConnectionString);
    }

    public string ConnectionString { get; }
    public BankOfZApiFactory Factory { get; }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<BankOfZIdentityContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        await using var context = new BankOfZIdentityContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        context.Customers.Add(CreateTestCustomer());
        await context.SaveChangesAsync();
        await ProvisionTestIdentitiesAsync();
    }

    private async Task ProvisionTestIdentitiesAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in BankRoles.All)
        {
            var roleResult = await roles.CreateAsync(new IdentityRole<Guid>(role));
            EnsureSucceeded(roleResult);
        }

        await CreateUserAsync(users, "customer", "customer@example.test", BankRoles.Customer, "1000000001");
        await CreateUserAsync(users, "operator", "operator@example.test", BankRoles.Operator, null);
        await CreateUserAsync(users, "administrator", "administrator@example.test", BankRoles.Administrator, null);
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> users,
        string userName,
        string email,
        string role,
        string? customerId)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            CustomerId = customerId
        };
        EnsureSucceeded(await users.CreateAsync(user, "Demo-Password-123!"));
        EnsureSucceeded(await users.AddToRoleAsync(user, role));
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }

    private static Customer CreateTestCustomer() => Customer.Create(
        "1000000001",
        "100000",
        new CustomerDetails(
            "Ms",
            "Jamie",
            "Customer",
            new DateOnly(1990, 5, 12),
            "1 Test Street",
            null,
            "London",
            null,
            "EC1A 1AA",
            "GB",
            "customer@example.test",
            "+44 20 0000 0000"),
        720,
        new DateOnly(2026, 8, 12),
        SourceSystem.Modern,
        "test-fixture",
        new DateTimeOffset(2026, 7, 22, 0, 0, 0, TimeSpan.Zero));

    private static string ReadConnectionString()
    {
        var environmentValue = Environment.GetEnvironmentVariable("ConnectionStrings__BankOfZ");
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty("ConnectionStrings").GetProperty("BankOfZ").GetString()
            ?? throw new InvalidOperationException("Test connection string 'BankOfZ' is required.");
    }
}
