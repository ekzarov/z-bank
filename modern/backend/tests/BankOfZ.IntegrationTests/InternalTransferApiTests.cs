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
public sealed class InternalTransferApiTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Operator_Books_Atomic_Paired_Transfer_And_Safe_Replay()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        await CreateAccountAsync(client);
        await CreateAccountAsync(client);
        await SendCashAsync(client, "10000001", 100m, "fund-source");

        var first = await SendTransferAsync(client, "10000001", "10000002", 40m, "transfer-atomic");
        var replay = await SendTransferAsync(client, "10000001", "10000002", 40.00m, "transfer-atomic");
        var conflict = await SendTransferAsync(client, "10000001", "10000002", 41m, "transfer-atomic");
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(
            firstBody.GetProperty("correlationId").GetString(),
            replayBody.GetProperty("correlationId").GetString());
        Assert.NotEqual(
            firstBody.GetProperty("source").GetProperty("reference").GetString(),
            firstBody.GetProperty("destination").GetProperty("reference").GetString());
        await AssertCodeAsync(conflict, HttpStatusCode.Conflict, "idempotency_conflict");

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var accounts = await context.Accounts.OrderBy(account => account.Id).ToArrayAsync();
        Assert.Equal(60m, accounts[0].ActualBalance);
        Assert.Equal(40m, accounts[1].ActualBalance);
        var transferBookings = await context.BookedTransactions
            .Where(transaction => transaction.TransferCorrelationId != null)
            .ToArrayAsync();
        Assert.Equal(2, transferBookings.Length);
        Assert.Single(transferBookings.Select(transaction => transaction.TransferCorrelationId).Distinct());
        Assert.All(transferBookings, transaction => Assert.Equal(SourceSystem.Modern, transaction.SourceSystem));
        var transferAudits = await context.AccountAuditEntries
            .Where(entry => entry.Action == "InternalTransferDebited" || entry.Action == "InternalTransferCredited")
            .ToArrayAsync();
        Assert.Equal(2, transferAudits.Length);
        Assert.All(
            transferAudits,
            audit => Assert.Equal(transferBookings[0].TransferCorrelationId, audit.CorrelationId));
    }

    [Fact]
    public async Task Invalid_Transfer_Rolls_Back_All_Mutations_With_Stable_Codes()
    {
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        await CreateAccountAsync(client);
        await CreateAccountAsync(client, currency: "USD");
        await CreateAccountAsync(client, type: "loan", interestRate: 1m);
        var closedSource = await CreateAccountAsync(client);
        var closedDestination = await CreateAccountAsync(client);
        await CloseAccountAsync(client, "10000004", closedSource);
        await CloseAccountAsync(client, "10000005", closedDestination);
        await SendCashAsync(client, "10000001", 50m, "fund-invalid");

        await AssertCodeAsync(
            await SendTransferAsync(client, "10000001", "10000001", 1m, "same"),
            HttpStatusCode.BadRequest,
            "transfer_same_account");
        await AssertCodeAsync(
            await SendTransferAsync(client, "10000001", "10000002", 1m, "currency"),
            HttpStatusCode.BadRequest,
            "transfer_currency_mismatch");
        await AssertCodeAsync(
            await SendTransferAsync(client, "10000001", "10000003", 1m, "product"),
            HttpStatusCode.BadRequest,
            "cash_product_not_supported");
        await AssertCodeAsync(
            await SendTransferAsync(client, "10000001", "10000003", 1000m, "funds"),
            HttpStatusCode.BadRequest,
            "insufficient_funds");
        await AssertCodeAsync(
            await SendTransferAsync(client, "10000004", "10000001", 1m, "closed-source"),
            HttpStatusCode.BadRequest,
            "cash_account_inactive");
        await AssertCodeAsync(
            await SendTransferAsync(client, "10000001", "10000005", 1m, "closed-destination"),
            HttpStatusCode.BadRequest,
            "cash_account_inactive");

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Equal(50m, (await context.Accounts.SingleAsync(account => account.Id == "10000001")).ActualBalance);
        Assert.Empty(await context.BookedTransactions.Where(transaction => transaction.TransferCorrelationId != null).ToArrayAsync());
    }

    [Fact]
    public async Task Customer_Can_Transfer_Between_Own_Accounts_But_Foreign_Is_Not_Disclosed()
    {
        using var operatorClient = CreateClient();
        await LoginAsync(operatorClient, "operator");
        await CreateAccountAsync(operatorClient);
        await CreateAccountAsync(operatorClient);
        await SendCashAsync(operatorClient, "10000001", 50m, "fund-customer");
        await AddForeignAccountAsync();
        using var customer = CreateClient();
        await LoginAsync(customer, "customer");

        var own = await SendTransferAsync(customer, "10000001", "10000002", 10m, "own-transfer");
        var foreign = await SendTransferAsync(customer, "10000001", "99999999", 10m, "foreign-transfer");
        var missing = await SendTransferAsync(customer, "10000001", "88888888", 10m, "missing-transfer");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        await AssertCodeAsync(foreign, HttpStatusCode.NotFound, "transfer_account_not_found");
        await AssertCodeAsync(missing, HttpStatusCode.NotFound, "transfer_account_not_found");
    }

    [Fact]
    public async Task Opposite_Concurrent_Transfers_Use_Deterministic_Locks_Without_Lost_Update()
    {
        using var setup = CreateClient();
        await LoginAsync(setup, "operator");
        await CreateAccountAsync(setup);
        await CreateAccountAsync(setup);
        await SendCashAsync(setup, "10000001", 100m, "fund-one");
        await SendCashAsync(setup, "10000002", 100m, "fund-two");
        using var first = CreateClient();
        using var second = CreateClient();
        await LoginAsync(first, "operator");
        await LoginAsync(second, "operator");

        var responses = await Task.WhenAll(
            SendTransferAsync(first, "10000001", "10000002", 10m, "opposite-one"),
            SendTransferAsync(second, "10000002", "10000001", 10m, "opposite-two"));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var accounts = await context.Accounts.OrderBy(account => account.Id).ToArrayAsync();
        Assert.All(accounts, account => Assert.Equal(100m, account.ActualBalance));
        Assert.Equal(4, await context.BookedTransactions.CountAsync(transaction => transaction.TransferCorrelationId != null));
    }

    private HttpClient CreateClient() => Fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static Task<HttpResponseMessage> CreateAccountAsync(
        HttpClient client,
        string type = "current",
        decimal interestRate = 0,
        string currency = "GBP") => SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/customers/1000000001/accounts",
            new
            {
                metadata = new { type, interestRate, overdraftLimit = 0, currency },
                sourceSystem = "modern"
            });

    private static Task<HttpResponseMessage> SendCashAsync(
        HttpClient client,
        string accountId,
        decimal amount,
        string key) => SendMutationAsync(
            client,
            HttpMethod.Post,
            $"/api/accounts/{accountId}/deposits",
            new { amount },
            key);

    private static async Task CloseAccountAsync(
        HttpClient client,
        string accountId,
        HttpResponseMessage createResponse)
    {
        var account = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var response = await SendMutationAsync(
            client,
            HttpMethod.Post,
            $"/api/accounts/{accountId}/close",
            new { version = account.GetProperty("version").GetString() });
        response.EnsureSuccessStatusCode();
    }

    private static Task<HttpResponseMessage> SendTransferAsync(
        HttpClient client,
        string sourceAccountId,
        string destinationAccountId,
        decimal amount,
        string key) => SendMutationAsync(
            client,
            HttpMethod.Post,
            $"/api/accounts/{sourceAccountId}/transfers",
            new { destinationAccountId, amount },
            key);

    private async Task AddForeignAccountAsync()
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        context.Customers.Add(Customer.Create(
            "2000000002",
            "100000",
            new CustomerDetails(
                "Mr", "Foreign", "Customer", new DateOnly(1980, 1, 1), "2 Other Street", null,
                "London", null, "EC1A 1AB", "GB", "foreign@example.test", null),
            700,
            new DateOnly(2026, 8, 1),
            SourceSystem.Modern,
            "test",
            DateTimeOffset.UtcNow));
        context.Accounts.Add(Account.Create(
            "99999999",
            "2000000002",
            "100000",
            new AccountMetadata(AccountType.Current, 0, 0, "GBP"),
            SourceSystem.Modern,
            "test",
            null,
            DateTimeOffset.UtcNow));
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

    private static async Task<HttpResponseMessage> SendMutationAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object body,
        string? idempotencyKey = null)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/session/csrf");
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-XSRF-TOKEN", csrf.GetProperty("token").GetString());
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        return await client.SendAsync(request);
    }

    private static async Task AssertCodeAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
    }
}
