using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BankOfZ.Application.Statements;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Statements;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankOfZ.IntegrationTests;

[Trait("Category", DatabaseIntegrationTestCategory.Name)]
[Collection(DatabaseTestCollection.Name)]
public sealed class StatementApiTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture)
{
    private static readonly DateTimeOffset July =
        new(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Generate_And_Read_Populated_Statement_With_Audit()
    {
        await AddAccountAsync("10000001");
        await AddBookingAsync("10000001", 'a', CashTransactionDirection.Deposit, 100, 100, July);
        await AddBookingAsync("10000001", 'b', CashTransactionDirection.Withdrawal, 40, 60, July.AddDays(1));
        using var client = CreateClient();
        await LoginAsync(client, "customer");

        var generatedResponse = await SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/accounts/10000001/statements",
            new { year = 2026, month = 7 });
        var generated = await generatedResponse.Content.ReadFromJsonAsync<JsonElement>();
        var generationId = generated.GetProperty("generationId").GetGuid();
        var found = await client.GetFromJsonAsync<JsonElement>(
            $"/api/accounts/10000001/statements/{generationId}");

        Assert.Equal(HttpStatusCode.Created, generatedResponse.StatusCode);
        Assert.Equal("Ms Jamie Customer", generated.GetProperty("customerName").GetString());
        Assert.Equal(0, generated.GetProperty("openingBalance").GetDecimal());
        Assert.Equal(100, generated.GetProperty("totalCredits").GetDecimal());
        Assert.Equal(40, generated.GetProperty("totalDebits").GetDecimal());
        Assert.Equal(60, generated.GetProperty("closingBalance").GetDecimal());
        Assert.Equal(2, generated.GetProperty("transactionCount").GetInt32());
        Assert.Equal(["aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"],
            generated.GetProperty("transactions").EnumerateArray()
                .Select(item => item.GetProperty("reference").GetString()));
        Assert.Equal(generationId, found.GetProperty("generationId").GetGuid());

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Equal(2, await context.StatementAuditEntries.CountAsync());
    }

    [Fact]
    public async Task Same_Data_Is_Idempotent_And_Empty_Period_Is_Valid()
    {
        await AddAccountAsync("10000001");
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var first = await SendMutationAsync(
            client, HttpMethod.Post, "/api/accounts/10000001/statements", new { year = 2026, month = 6 });
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        var second = await SendMutationAsync(
            client, HttpMethod.Post, "/api/accounts/10000001/statements", new { year = 2026, month = 6 });
        var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("true", second.Headers.GetValues("X-Statement-Reused").Single());
        Assert.Equal(firstBody.GetProperty("generationId").GetGuid(), secondBody.GetProperty("generationId").GetGuid());
        Assert.Equal(0, firstBody.GetProperty("transactionCount").GetInt32());
        Assert.Equal(0, firstBody.GetProperty("totalCredits").GetDecimal());
        Assert.Equal(0, firstBody.GetProperty("totalDebits").GetDecimal());
    }

    [Fact]
    public async Task Repository_Unique_Conflict_Rolls_Back_And_Returns_False()
    {
        await AddAccountAsync("10000001");
        using var client = CreateClient();
        await LoginAsync(client, "operator");
        var response = await SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/accounts/10000001/statements",
            new { year = 2026, month = 6 });
        response.EnsureSuccessStatusCode();

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var existing = await context.StatementSnapshots
            .AsNoTracking()
            .Include(statement => statement.Transactions)
            .SingleAsync();
        var duplicate = StatementSnapshot.Create(
            Guid.NewGuid(),
            existing.AccountId,
            existing.CustomerId,
            existing.Year,
            existing.Month,
            existing.PeriodStartUtc,
            existing.PeriodEndUtc,
            existing.GeneratedAt.AddSeconds(1),
            existing.DataAsOf,
            existing.DataVersion,
            existing.CustomerName,
            existing.CustomerAddress,
            existing.CustomerPhone,
            existing.SortCode,
            existing.AccountType,
            existing.Currency,
            existing.InterestRate,
            existing.OverdraftLimit,
            existing.OpeningBalance,
            existing.TotalCredits,
            existing.TotalDebits,
            existing.ClosingBalance,
            existing.AvailableBalance,
            existing.Transactions
                .OrderBy(transaction => transaction.Sequence)
                .Select(transaction => new StatementTransactionInput(
                    transaction.BookedAt,
                    transaction.Direction,
                    transaction.Reference,
                    transaction.Description,
                    transaction.Amount))
                .ToArray());
        var repository = scope.ServiceProvider.GetRequiredService<IStatementRepository>();

        var added = await repository.TryAddWithAuditAsync(
            duplicate,
            "operator",
            July,
            default);

        Assert.False(added);
        Assert.Single(await context.StatementSnapshots.AsNoTracking().ToArrayAsync());
        Assert.Single(await context.StatementAuditEntries.AsNoTracking().ToArrayAsync());
    }

    [Fact]
    public async Task Prior_Period_Booking_Defines_Opening_Balance()
    {
        await AddAccountAsync("10000001");
        await AddBookingAsync(
            "10000001",
            'p',
            CashTransactionDirection.Deposit,
            75,
            75,
            new DateTimeOffset(2026, 6, 30, 23, 59, 59, TimeSpan.Zero));
        await AddBookingAsync(
            "10000001",
            'q',
            CashTransactionDirection.Deposit,
            25,
            100,
            July);
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var response = await SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/accounts/10000001/statements",
            new { year = 2026, month = 7 });
        var statement = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(75, statement.GetProperty("openingBalance").GetDecimal());
        Assert.Equal(100, statement.GetProperty("closingBalance").GetDecimal());
        Assert.Equal(1, statement.GetProperty("transactionCount").GetInt32());
    }

    [Fact]
    public async Task Customer_Ownership_And_Validation_Are_Enforced()
    {
        await AddAccountAsync("10000001");
        await AddForeignCustomerAndAccountAsync();
        using var customer = CreateClient();
        await LoginAsync(customer, "customer");

        Assert.Equal(
            HttpStatusCode.Created,
            (await SendMutationAsync(
                customer,
                HttpMethod.Post,
                "/api/accounts/10000001/statements",
                new { year = 2026, month = 7 })).StatusCode);
        await AssertCodeAsync(
            await SendMutationAsync(
                customer,
                HttpMethod.Post,
                "/api/accounts/99999999/statements",
                new { year = 2026, month = 7 }),
            HttpStatusCode.NotFound,
            "statement_account_not_found");
        await AssertCodeAsync(
            await SendMutationAsync(
                customer,
                HttpMethod.Post,
                "/api/accounts/10000001/statements",
                new { year = 2026, month = 13 }),
            HttpStatusCode.BadRequest,
            "invalid_statement_period");
        await AssertCodeAsync(
            await SendMutationAsync(
                customer,
                HttpMethod.Post,
                "/api/accounts/10000001/statements",
                new { year = 2026, month = 8 }),
            HttpStatusCode.BadRequest,
            "invalid_statement_period");

        using var anonymous = CreateClient();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync(
                "/api/accounts/10000001/statements/11111111-1111-1111-1111-111111111111")).StatusCode);

        using var administrator = CreateClient();
        await LoginAsync(administrator, "administrator");
        await AssertCodeAsync(
            await SendMutationAsync(
                administrator,
                HttpMethod.Post,
                "/api/accounts/10000001/statements",
                new { year = 2026, month = 7 }),
            HttpStatusCode.NotFound,
            "statement_account_not_found");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await SendMutationAsync(
                customer,
                HttpMethod.Post,
                "/api/statements/bulk",
                new { year = 2026, month = 7 })).StatusCode);
    }

    [Fact]
    public async Task Operator_Bulk_Isolates_Failed_Account_And_Retry_Reuses_Success()
    {
        await AddAccountAsync("10000001");
        await AddAccountAsync("10000002");
        await AddBookingAsync("10000001", 'a', CashTransactionDirection.Deposit, 10, 10, July);
        await AddBookingAsync("10000002", 'b', CashTransactionDirection.Deposit, 10, 10, July);
        await AddBookingAsync("10000002", 'c', CashTransactionDirection.Deposit, 10, 99, July.AddMinutes(1));
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var firstResponse = await SendMutationAsync(
            client, HttpMethod.Post, "/api/statements/bulk", new { year = 2026, month = 7 });
        var first = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var successfulGenerationId = first.GetProperty("accounts")
            .EnumerateArray()
            .Single(account => account.GetProperty("accountId").GetString() == "10000001")
            .GetProperty("generationId")
            .GetGuid();

        await using (var repairScope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var repairContext = repairScope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
            var invalidBooking = await repairContext.BookedTransactions.SingleAsync(
                transaction => transaction.Reference == new string('c', 32));
            repairContext.BookedTransactions.Remove(invalidBooking);
            await repairContext.SaveChangesAsync();
        }

        var secondResponse = await SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/statements/bulk",
            new { year = 2026, month = 7, accountIds = new[] { "10000002" } });
        var second = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(2, first.GetProperty("total").GetInt32());
        Assert.Equal(1, first.GetProperty("succeeded").GetInt32());
        Assert.Equal(1, first.GetProperty("failed").GetInt32());
        Assert.Equal(1, second.GetProperty("total").GetInt32());
        Assert.Equal("10000002", second.GetProperty("accounts")[0].GetProperty("accountId").GetString());
        Assert.True(second.GetProperty("accounts")[0].GetProperty("succeeded").GetBoolean());

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var snapshots = await context.StatementSnapshots.ToArrayAsync();
        Assert.Equal(2, snapshots.Length);
        Assert.Contains(snapshots, statement =>
            statement.AccountId == "10000001" && statement.Id == successfulGenerationId);
        Assert.Contains(snapshots, statement => statement.AccountId == "10000002");
    }

    [Fact]
    public async Task Operator_Bulk_Rejects_Account_Outside_Configured_Sort_Code()
    {
        await AddAccountAsync("10000003", "999999");
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var response = await SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/statements/bulk",
            new { year = 2026, month = 7, accountIds = new[] { "10000003" } });

        await AssertCodeAsync(response, HttpStatusCode.BadRequest, "invalid_statement_scope");
    }

    [Fact]
    public async Task Operator_Bulk_Includes_Retained_Closed_Account()
    {
        await AddAccountAsync("10000004");
        await using (var closeScope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var closeContext = closeScope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
            var account = await closeContext.Accounts.SingleAsync(item => item.Id == "10000004");
            account.Close(July.AddDays(1));
            await closeContext.SaveChangesAsync();
        }
        using var client = CreateClient();
        await LoginAsync(client, "operator");

        var response = await SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/statements/bulk",
            new { year = 2026, month = 7 });
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(result.GetProperty("accounts").EnumerateArray(), account =>
            account.GetProperty("accountId").GetString() == "10000004" &&
            account.GetProperty("succeeded").GetBoolean());
    }

    private HttpClient CreateClient() =>
        Fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private async Task AddAccountAsync(string accountId, string sortCode = "100000")
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        context.Accounts.Add(Account.Create(
            accountId,
            "1000000001",
            sortCode,
            new AccountMetadata(AccountType.Current, 1.25m, 100, "GBP"),
            SourceSystem.Modern,
            "statement-test",
            null,
            July));
        await context.SaveChangesAsync();
    }

    private async Task AddForeignCustomerAndAccountAsync()
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        context.Customers.Add(Customer.Create(
            "2000000002",
            "100000",
            new CustomerDetails(
                "Mr", "Other", "Customer", new DateOnly(1980, 1, 1), "2 Other Street", null,
                "London", null, "EC1A 1AB", "GB", "other@example.test", null),
            700,
            new DateOnly(2026, 8, 1),
            SourceSystem.Modern,
            "statement-test",
            July));
        context.Accounts.Add(Account.Create(
            "99999999",
            "2000000002",
            "100000",
            new AccountMetadata(AccountType.Current, 0, 0, "GBP"),
            SourceSystem.Modern,
            "statement-test",
            null,
            July));
        await context.SaveChangesAsync();
    }

    private async Task AddBookingAsync(
        string accountId,
        char reference,
        CashTransactionDirection direction,
        decimal amount,
        decimal resultingBalance,
        DateTimeOffset bookedAt)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        context.BookedTransactions.Add(BookedTransaction.Create(
            new string(reference, 32),
            accountId,
            "1000000001",
            direction,
            amount,
            "GBP",
            resultingBalance,
            resultingBalance,
            $"statement-{reference}",
            $"statement:{reference}",
            bookedAt));
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
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
    }
}
