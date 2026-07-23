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
public sealed class TransactionHistoryApiTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture)
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Operator_History_Is_Newest_First_Stable_Pageable_Filtered_And_Empty()
    {
        await AddAccountAsync("10000001", "1000000001");
        await AddAccountAsync("10000002", "1000000001");
        await AddBookingAsync("10000001", 'a', Now.AddDays(-2));
        await AddBookingAsync("10000001", 'b', Now.AddDays(-1));
        await AddBookingAsync("10000001", 'c', Now);
        await AddBookingAsync("10000001", 'd', Now);
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var first = await client.GetFromJsonAsync<JsonElement>(
            "/api/accounts/10000001/transactions?pageSize=2");
        var firstItems = first.GetProperty("items").EnumerateArray().ToArray();
        var cursor = first.GetProperty("nextCursor").GetString();
        var second = await client.GetFromJsonAsync<JsonElement>(
            $"/api/accounts/10000001/transactions?pageSize=2&cursor={Uri.EscapeDataString(cursor!)}");
        var secondItems = second.GetProperty("items").EnumerateArray().ToArray();
        var filtered = await client.GetFromJsonAsync<JsonElement>(
            "/api/accounts/10000001/transactions?from=2026-07-22T00%3A00%3A00Z&to=2026-07-24T00%3A00%3A00Z");
        var empty = await client.GetFromJsonAsync<JsonElement>(
            "/api/accounts/10000002/transactions");

        Assert.Equal([new string('d', 32), new string('c', 32)],
            firstItems.Select(item => item.GetProperty("reference").GetString()));
        Assert.Equal([new string('b', 32), new string('a', 32)],
            secondItems.Select(item => item.GetProperty("reference").GetString()));
        Assert.Equal(3, filtered.GetProperty("items").GetArrayLength());
        Assert.Equal(0, empty.GetProperty("items").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("nextCursor").ValueKind);

        await AssertCodeAsync(
            await client.GetAsync("/api/accounts/10000001/transactions?pageSize=201"),
            HttpStatusCode.BadRequest,
            "invalid_history_page_size");
        await AssertCodeAsync(
            await client.GetAsync("/api/accounts/10000001/transactions?cursor=broken"),
            HttpStatusCode.BadRequest,
            "invalid_history_cursor");
        await AssertCodeAsync(
            await client.GetAsync("/api/accounts/10000001/transactions?from=2026-08-01&to=2026-07-01"),
            HttpStatusCode.BadRequest,
            "invalid_history_range");
    }

    [Fact]
    public async Task Detail_Contains_Related_Transfer_And_History_Is_Read_Only()
    {
        await AddAccountAsync("10000001", "1000000001");
        await AddAccountAsync("10000002", "1000000001");
        var sourceReference = new string('e', 32);
        var destinationReference = new string('f', 32);
        await AddTransferBookingsAsync(sourceReference, destinationReference);
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var detail = await client.GetFromJsonAsync<JsonElement>(
            $"/api/accounts/10000001/transactions/{sourceReference}");
        var put = await client.PutAsJsonAsync(
            $"/api/accounts/10000001/transactions/{sourceReference}",
            new { amount = 1 });
        var delete = await client.DeleteAsync(
            $"/api/accounts/10000001/transactions/{sourceReference}");

        Assert.Equal("Internal transfer sent", detail.GetProperty("description").GetString());
        Assert.Equal("booked", detail.GetProperty("status").GetString());
        Assert.Equal(destinationReference, detail.GetProperty("relatedTransferReference").GetString());
        Assert.Equal("modern", detail.GetProperty("sourceSystem").GetString());
        Assert.Equal(HttpStatusCode.MethodNotAllowed, put.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, delete.StatusCode);
    }

    [Fact]
    public async Task Imported_Unpaired_Transfer_Retains_Provenance_Without_Failing_History()
    {
        await AddAccountAsync("10000001", "1000000001");
        await AddBookingAsync(
            "10000001",
            'i',
            Now,
            new string('u', 32),
            SourceSystem.Cics,
            "CICS-TRN-00042");
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var response = await client.GetAsync("/api/accounts/10000001/transactions");
        var history = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = Assert.Single(history.GetProperty("items").EnumerateArray());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("cics", item.GetProperty("sourceSystem").GetString());
        Assert.Equal("CICS-TRN-00042", item.GetProperty("sourceIdentifier").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("relatedTransferReference").ValueKind);
    }

    [Fact]
    public async Task Customer_Ownership_And_Non_Disclosure_Apply_To_List_And_Detail()
    {
        await AddAccountAsync("10000001", "1000000001");
        await AddBookingAsync("10000001", '1', Now);
        await AddForeignAccountAsync();
        await AddBookingAsync("99999999", '9', Now);
        using var customer = CreateClient();
        await LoginAsync(customer, "customer");

        Assert.Equal(
            HttpStatusCode.OK,
            (await customer.GetAsync("/api/accounts/10000001/transactions")).StatusCode);
        var foreignList = await customer.GetAsync("/api/accounts/99999999/transactions");
        var missingList = await customer.GetAsync("/api/accounts/88888888/transactions");
        var foreignDetail = await customer.GetAsync(
            $"/api/accounts/99999999/transactions/{new string('9', 32)}");
        var missingDetail = await customer.GetAsync(
            $"/api/accounts/10000001/transactions/{new string('8', 32)}");

        await AssertCodeAsync(foreignList, HttpStatusCode.NotFound, "history_account_not_found");
        await AssertCodeAsync(missingList, HttpStatusCode.NotFound, "history_account_not_found");
        await AssertCodeAsync(foreignDetail, HttpStatusCode.NotFound, "transaction_not_found");
        await AssertCodeAsync(missingDetail, HttpStatusCode.NotFound, "transaction_not_found");

        using var administrator = CreateClient();
        await LoginAsync(administrator, "administrator");
        await AssertCodeAsync(
            await administrator.GetAsync("/api/accounts/10000001/transactions"),
            HttpStatusCode.NotFound,
            "history_account_not_found");
    }

    private HttpClient CreateClient() =>
        Fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private async Task AddAccountAsync(string accountId, string customerId)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        context.Accounts.Add(Account.Create(
            accountId,
            customerId,
            "100000",
            new AccountMetadata(AccountType.Current, 0, 0, "GBP"),
            SourceSystem.Modern,
            "history-test",
            null,
            Now));
        await context.SaveChangesAsync();
    }

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
            "history-test",
            Now));
        context.Accounts.Add(Account.Create(
            "99999999",
            "2000000002",
            "100000",
            new AccountMetadata(AccountType.Current, 0, 0, "GBP"),
            SourceSystem.Modern,
            "history-test",
            null,
            Now));
        await context.SaveChangesAsync();
    }

    private async Task AddBookingAsync(
        string accountId,
        char reference,
        DateTimeOffset bookedAt,
        string? transferCorrelationId = null,
        SourceSystem sourceSystem = SourceSystem.Modern,
        string? sourceIdentifier = null)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var account = await context.Accounts.SingleAsync(candidate => candidate.Id == accountId);
        context.BookedTransactions.Add(BookedTransaction.Create(
            new string(reference, 32),
            account.Id,
            account.CustomerId,
            CashTransactionDirection.Deposit,
            10,
            account.Currency,
            10,
            10,
            $"history-{reference}",
            $"history:{reference}",
            bookedAt,
            transferCorrelationId,
            sourceSystem,
            sourceIdentifier));
        await context.SaveChangesAsync();
    }

    private async Task AddTransferBookingsAsync(string sourceReference, string destinationReference)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var correlation = new string('c', 32);
        context.BookedTransactions.AddRange(
            BookedTransaction.Create(
                sourceReference, "10000001", "1000000001", CashTransactionDirection.Withdrawal,
                25, "GBP", 75, 75, "transfer-source", "transfer:10000002:25", Now, correlation),
            BookedTransaction.Create(
                destinationReference, "10000002", "1000000001", CashTransactionDirection.Deposit,
                25, "GBP", 25, 25, correlation, "transfer:10000002:25", Now, correlation));
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
        object body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/session/csrf");
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-XSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }

    private static async Task AssertCodeAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
    }
}
