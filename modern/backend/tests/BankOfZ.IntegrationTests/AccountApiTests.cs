using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankOfZ.IntegrationTests;

[Trait("Category", DatabaseIntegrationTestCategory.Name)]
[Collection(DatabaseTestCollection.Name)]
public sealed class AccountApiTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Operator_Creates_Account_With_System_Defaults_And_Atomic_Audit()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var response = await CreateAccountAsync(client);
        var account = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        Assert.Equal("10000001", account.GetProperty("id").GetString());
        Assert.Equal("100000", account.GetProperty("sortCode").GetString());
        Assert.Equal(0, account.GetProperty("actualBalance").GetDecimal());
        Assert.Equal(0, account.GetProperty("availableBalance").GetDecimal());
        Assert.Equal("active", account.GetProperty("status").GetString());
        Assert.Equal("2026-07-22", account.GetProperty("openedOn").GetString());
        Assert.Equal("2026-08-22", account.GetProperty("nextStatementOn").GetString());

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var audit = await context.AccountAuditEntries.SingleAsync();
        Assert.Equal("AccountCreated", audit.Action);
        Assert.Equal("operator", audit.Actor);
    }

    [Fact]
    public async Task Customer_Sees_Own_Complete_Paged_Portfolio_And_Not_Foreign_Account()
    {
        using var operatorClient = CreateClient();
        await LoginAsync(operatorClient, "operator");
        for (var index = 0; index < 7; index++)
        {
            (await CreateAccountAsync(operatorClient)).EnsureSuccessStatusCode();
        }

        await AddForeignAccountAsync();
        using var customerClient = CreateClient();
        await LoginAsync(customerClient, "customer");

        var first = await customerClient.GetFromJsonAsync<JsonElement>("/api/customers/1000000001/accounts?page=1&pageSize=3");
        var third = await customerClient.GetFromJsonAsync<JsonElement>("/api/customers/1000000001/accounts?page=3&pageSize=3");
        var foreign = await customerClient.GetAsync("/api/accounts/99999999");
        var foreignPortfolio = await customerClient.GetAsync("/api/customers/2000000002/accounts");

        Assert.Equal(7, first.GetProperty("total").GetInt32());
        Assert.Equal(3, first.GetProperty("items").GetArrayLength());
        Assert.Single(third.GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignPortfolio.StatusCode);
    }

    [Fact]
    public async Task Eleventh_Active_Account_Is_Rejected_Without_Partial_Account_Or_Audit()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        for (var index = 0; index < AccountRules.MaximumAccountsPerCustomer; index++)
        {
            (await CreateAccountAsync(client)).EnsureSuccessStatusCode();
        }

        var rejected = await CreateAccountAsync(client);

        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Equal(10, await context.Accounts.CountAsync());
        Assert.Equal(10, await context.AccountAuditEntries.CountAsync());
    }

    [Fact]
    public async Task Concurrent_Tenth_Account_Creation_Cannot_Exceed_The_Limit()
    {
        using var firstClient = CreateClient();
        using var secondClient = CreateClient();
        await LoginAsync(firstClient, "operator");
        await LoginAsync(secondClient, "operator");
        for (var index = 0; index < AccountRules.MaximumAccountsPerCustomer - 1; index++)
        {
            (await CreateAccountAsync(firstClient)).EnsureSuccessStatusCode();
        }

        var responses = await Task.WhenAll(CreateAccountAsync(firstClient), CreateAccountAsync(secondClient));

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Equal(10, await context.Accounts.CountAsync());
        Assert.Equal(10, await context.AccountAuditEntries.CountAsync());
    }

    [Fact]
    public async Task Customer_Cannot_Create_Account()
    {
        using var client = CreateClient();
        await LoginAsync(client, "customer");

        var response = await CreateAccountAsync(client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>().Accounts.ToArrayAsync());
    }

    [Fact]
    public async Task Metadata_Update_Preserves_Balances_And_Stale_Version_Conflicts()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        var createdResponse = await CreateAccountAsync(client);
        var created = await createdResponse.Content.ReadFromJsonAsync<JsonElement>();
        var version = created.GetProperty("version").GetString()!;
        var update = new
        {
            metadata = new { type = "isa", interestRate = 4.25m, overdraftLimit = 0, currency = "GBP" },
            version
        };

        var first = await SendMutationAsync(client, HttpMethod.Put, "/api/accounts/10000001", update);
        var stale = await SendMutationAsync(client, HttpMethod.Put, "/api/accounts/10000001", update);
        var account = await first.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(0, account.GetProperty("actualBalance").GetDecimal());
        Assert.Equal("2026-08-22", account.GetProperty("nextStatementOn").GetString());
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Single(await context.AccountAuditEntries.Where(entry => entry.Action == "AccountUpdated").ToArrayAsync());
    }

    [Fact]
    public async Task Operator_Closes_Eligible_Account_Without_Deleting_History()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        var createdResponse = await CreateAccountAsync(client);
        var created = await createdResponse.Content.ReadFromJsonAsync<JsonElement>();

        var response = await SendMutationAsync(client, HttpMethod.Post, "/api/accounts/10000001/close", new
        {
            version = created.GetProperty("version").GetString()
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Equal(AccountStatus.Closed, (await context.Accounts.SingleAsync()).Status);
        Assert.Contains(await context.AccountAuditEntries.ToArrayAsync(), entry => entry.Action == "AccountClosed");
    }

    [Fact]
    public async Task Sql_Server_Enforces_Customer_Ownership_And_Account_Constraints()
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        context.Accounts.Add(Account.Create(
            "12345678",
            "9999999999",
            "100000",
            new AccountMetadata(AccountType.Current, 0, 0, "GBP"),
            SourceSystem.Modern,
            null,
            null,
            DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        Assert.Empty(await context.Accounts.ToArrayAsync());
    }

    [Fact]
    public async Task Sql_Server_Rejects_Invalid_Account_Type_And_Maps_Exact_Precision()
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var account = Account.Create(
            "12345678", "1000000001", "100000",
            new AccountMetadata(AccountType.Current, 0, 0, "GBP"),
            SourceSystem.Modern, null, null, DateTimeOffset.UtcNow);
        context.Accounts.Add(account);
        context.Entry(account).Property(entity => entity.Type).CurrentValue = (AccountType)99;

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        Assert.Empty(await context.Accounts.ToArrayAsync());
        var entity = context.Model.FindEntityType(typeof(Account))!;
        Assert.Equal(18, entity.FindProperty(nameof(Account.ActualBalance))!.GetPrecision());
        Assert.Equal(2, entity.FindProperty(nameof(Account.ActualBalance))!.GetScale());
        Assert.Equal(6, entity.FindProperty(nameof(Account.InterestRate))!.GetPrecision());
        Assert.Equal(2, entity.FindProperty(nameof(Account.InterestRate))!.GetScale());
    }

    [Fact]
    public async Task Failed_Audit_Insert_Rolls_Back_Account_And_Audit_Together()
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        context.Accounts.Add(Account.Create(
            "12345678", "1000000001", "100000",
            new AccountMetadata(AccountType.Current, 0, 0, "GBP"),
            SourceSystem.Modern, null, null, DateTimeOffset.UtcNow));
        context.AccountAuditEntries.Add(new AccountAuditRecord
        {
            Actor = new string('x', CatalogModelConstants.Lengths.Actor + 1),
            Timestamp = DateTimeOffset.UtcNow,
            Action = "AccountCreated",
            AccountId = "12345678",
            CustomerId = "1000000001",
            Result = "Succeeded",
            CorrelationId = "forced-failure"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        Assert.Empty(await context.Accounts.ToArrayAsync());
        Assert.Empty(await context.AccountAuditEntries.ToArrayAsync());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123456789")]
    public async Task Invalid_Deep_Link_Returns_Problem_Details(string id)
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var response = await client.GetAsync($"/api/accounts/{id}");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Identifier is invalid", problem.GetProperty("title").GetString());
    }

    private HttpClient CreateClient() => Fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static Task<HttpResponseMessage> CreateAccountAsync(HttpClient client) => SendMutationAsync(
        client,
        HttpMethod.Post,
        "/api/customers/1000000001/accounts",
        new
        {
            metadata = new { type = "current", interestRate = 0m, overdraftLimit = 500, currency = "GBP" },
            sourceSystem = "modern",
            sourceIdentifier = "integration-test"
        });

    private async Task AddForeignAccountAsync()
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        context.Customers.Add(Customer.Create(
            "2000000002", "100000",
            new CustomerDetails("Mr", "Foreign", "Customer", new DateOnly(1980, 1, 1), "2 Other Street", null,
                "London", null, "EC1A 1AB", "GB", "foreign@example.test", null),
            700, new DateOnly(2026, 8, 1), SourceSystem.Modern, "test", DateTimeOffset.UtcNow));
        context.Accounts.Add(Account.Create(
            "99999999", "2000000002", "100000",
            new AccountMetadata(AccountType.Current, 0, 0, "GBP"), SourceSystem.Modern, "test", null, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
    }

    private static async Task LoginAsync(HttpClient client, string userName)
    {
        var response = await SendMutationAsync(client, HttpMethod.Post, "/api/session/login", new
        {
            userName,
            password = "Demo-Password-123!",
            rememberMe = false
        });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> SendMutationAsync(HttpClient client, HttpMethod method, string path, object body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/session/csrf");
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-XSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }
}
