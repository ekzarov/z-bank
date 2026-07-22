using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankOfZ.IntegrationTests;

[Trait("Category", DatabaseIntegrationTestCategory.Name)]
[Collection(DatabaseTestCollection.Name)]
public sealed class CashTransactionApiTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Operator_Deposits_And_Withdraws_With_Atomic_History_And_Audit()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        await CreateAccountAsync(client);

        var deposit = await SendCashAsync(client, "10000001", "deposits", 125.25m, "cash-atomic-deposit");
        var withdrawal = await SendCashAsync(client, "10000001", "withdrawals", 25.25m, "cash-atomic-withdrawal");
        var depositBody = await deposit.Content.ReadFromJsonAsync<JsonElement>();
        var withdrawalBody = await withdrawal.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, deposit.StatusCode);
        Assert.Equal(125.25m, depositBody.GetProperty("actualBalance").GetDecimal());
        Assert.Equal(HttpStatusCode.OK, withdrawal.StatusCode);
        Assert.Equal(100m, withdrawalBody.GetProperty("actualBalance").GetDecimal());
        Assert.Equal("modern", withdrawalBody.GetProperty("sourceSystem").GetString());

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var account = await context.Accounts.SingleAsync();
        var bookings = await context.BookedTransactions.OrderBy(transaction => transaction.CreatedAt).ToArrayAsync();
        Assert.Equal(2, bookings.Length);
        Assert.Equal(account.LastTransactionReference, bookings[1].Reference);
        Assert.All(bookings, transaction => Assert.Equal(SourceSystem.Modern, transaction.SourceSystem));
        Assert.Equal(3, await context.AccountAuditEntries.CountAsync());
    }

    [Fact]
    public async Task Same_Idempotency_Request_Replays_And_Different_Payload_Conflicts()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        await CreateAccountAsync(client);

        var first = await SendCashAsync(client, "10000001", "deposits", 50m, "same-key");
        var replay = await SendCashAsync(client, "10000001", "deposits", 50.00m, "same-key");
        var conflict = await SendCashAsync(client, "10000001", "deposits", 51m, "same-key");
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();
        var conflictBody = await conflict.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(firstBody.GetProperty("reference").GetString(), replayBody.GetProperty("reference").GetString());
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("idempotency_conflict", conflictBody.GetProperty("code").GetString());

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Single(await context.BookedTransactions.ToArrayAsync());
        Assert.Equal(50m, (await context.Accounts.SingleAsync()).ActualBalance);
        Assert.Equal(2, await context.AccountAuditEntries.CountAsync());
    }

    [Fact]
    public async Task Missing_And_Oversized_Idempotency_Keys_Are_Rejected_Without_Booking()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        await CreateAccountAsync(client);

        var missing = await SendCashAsync(client, "10000001", "deposits", 50m, null);
        var oversized = await SendCashAsync(client, "10000001", "deposits", 50m, new string('x', 65));

        await AssertCodeAsync(missing, "idempotency_key_invalid");
        await AssertCodeAsync(oversized, "idempotency_key_invalid");
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>().BookedTransactions.ToArrayAsync());
    }

    [Fact]
    public async Task Concurrent_Cash_Requests_Are_Serialized_Without_Lost_Updates()
    {
        using var setupClient = CreateClient();
        await LoginAsync(setupClient, "operator");
        await CreateAccountAsync(setupClient);
        using var firstClient = CreateClient();
        using var secondClient = CreateClient();
        await LoginAsync(firstClient, "operator");
        await LoginAsync(secondClient, "operator");

        var responses = await Task.WhenAll(
            SendCashAsync(firstClient, "10000001", "deposits", 10m, "concurrent-one"),
            SendCashAsync(secondClient, "10000001", "deposits", 20m, "concurrent-two"));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Equal(30m, (await context.Accounts.SingleAsync()).ActualBalance);
        Assert.Equal(2, await context.BookedTransactions.CountAsync());
        Assert.Equal(3, await context.AccountAuditEntries.CountAsync());
    }

    [Fact]
    public async Task Customer_Can_Book_Own_Account_But_Foreign_Account_Is_Not_Disclosed()
    {
        using var operatorClient = CreateClient();
        await LoginAsync(operatorClient, "operator");
        await CreateAccountAsync(operatorClient);
        await AddForeignAccountAsync();
        using var customerClient = CreateClient();
        await LoginAsync(customerClient, "customer");

        var own = await SendCashAsync(customerClient, "10000001", "deposits", 10m, "customer-own");
        var foreign = await SendCashAsync(customerClient, "99999999", "deposits", 10m, "customer-foreign");
        var missing = await SendCashAsync(customerClient, "88888888", "deposits", 10m, "customer-missing");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>().BookedTransactions.ToArrayAsync());
    }

    [Fact]
    public async Task Invalid_Product_Status_Amount_And_Funds_Return_Stable_Codes_Without_Booking()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        await CreateAccountAsync(client);
        await CreateAccountAsync(client, "loan", 1m);
        var closedCreate = await CreateAccountAsync(client);
        var closedBody = await closedCreate.Content.ReadFromJsonAsync<JsonElement>();
        await SendMutationAsync(client, HttpMethod.Post, "/api/accounts/10000003/close", new
        {
            version = closedBody.GetProperty("version").GetString()
        });

        var zero = await SendCashAsync(client, "10000001", "deposits", 0m, "invalid-zero");
        var precision = await SendCashAsync(client, "10000001", "deposits", 1.001m, "invalid-precision");
        var lending = await SendCashAsync(client, "10000002", "deposits", 1m, "invalid-product");
        var closed = await SendCashAsync(client, "10000003", "deposits", 1m, "invalid-closed");
        var funds = await SendCashAsync(client, "10000001", "withdrawals", 500.01m, "invalid-funds");

        await AssertCodeAsync(zero, "cash_amount_invalid");
        await AssertCodeAsync(precision, "cash_amount_invalid");
        await AssertCodeAsync(lending, "cash_product_not_supported");
        await AssertCodeAsync(closed, "cash_account_inactive");
        await AssertCodeAsync(funds, "insufficient_funds");
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>().BookedTransactions.ToArrayAsync());
    }

    private HttpClient CreateClient() => Fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static Task<HttpResponseMessage> CreateAccountAsync(
        HttpClient client,
        string type = "current",
        decimal interestRate = 0) => SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/customers/1000000001/accounts",
            new
            {
                metadata = new { type, interestRate, overdraftLimit = 500, currency = "GBP" },
                sourceSystem = "modern"
            });

    private static async Task<HttpResponseMessage> SendCashAsync(
        HttpClient client,
        string accountId,
        string direction,
        decimal amount,
        string? idempotencyKey)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/session/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/accounts/{accountId}/{direction}")
        {
            Content = JsonContent.Create(new { amount })
        };
        request.Headers.Add("X-XSRF-TOKEN", csrf.GetProperty("token").GetString());
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        return await client.SendAsync(request);
    }

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

    private static async Task AssertCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
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
