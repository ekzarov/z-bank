using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BankOfZ.Application.Transactions;
using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.Persistence;
using BankOfZ.Infrastructure.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BankOfZ.IntegrationTests;

[Trait("Category", DatabaseIntegrationTestCategory.Name)]
[Collection(DatabaseTestCollection.Name)]
public sealed class FinancialFailureDiagnosticsTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Infrastructure_Failure_Rolls_Back_And_Emits_Safe_Correlated_Diagnostic()
    {
        using var setupClient = CreateClient(Fixture.Factory);
        await LoginAsync(setupClient);
        await CreateAccountAsync(setupClient);
        await CreateAccountAsync(setupClient);
        await DepositAsync(setupClient);

        var logs = new CaptureLoggerProvider();
        await using var failingFactory = new BankOfZApiFactory(
            Fixture.ConnectionString,
            configureServices: services =>
            {
                services.RemoveAll<IInternalTransferRepository>();
                services.AddScoped<IInternalTransferRepository>(provider =>
                    new SaveThenFailTransferRepository(
                        new InternalTransferRepository(
                            provider.GetRequiredService<BankOfZIdentityContext>())));
                services.AddSingleton<ILoggerProvider>(logs);
            });
        using var client = CreateClient(failingFactory);
        await LoginAsync(client);

        const string correlationId = "infra-failure-001";
        var response = await SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/accounts/10000001/transfers?password=must-not-be-logged",
            new { destinationAccountId = "10000002", amount = 1234.56m },
            "transfer-failure",
            correlationId);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var accounts = await context.Accounts.OrderBy(account => account.Id).ToArrayAsync();
        Assert.Equal(2000m, accounts[0].ActualBalance);
        Assert.Equal(0m, accounts[1].ActualBalance);
        Assert.Empty(await context.BookedTransactions
            .Where(transaction => transaction.TransferCorrelationId != null)
            .ToArrayAsync());
        Assert.Empty(await context.AccountAuditEntries
            .Where(entry => entry.Action == "InternalTransferDebited" ||
                entry.Action == "InternalTransferCredited")
            .ToArrayAsync());

        var failure = Assert.Single(logs.Records, record =>
            record.Level == LogLevel.Error &&
            record.Properties.TryGetValue("CorrelationId", out var value) &&
            Equals(value, correlationId));
        Assert.Equal("/api/accounts/10000001/transfers", failure.Properties["Path"]);
        Assert.DoesNotContain("must-not-be-logged", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("transfer-failure", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("1234.56", failure.Message, StringComparison.Ordinal);
    }

    private static HttpClient CreateClient(BankOfZApiFactory factory) =>
        factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static async Task LoginAsync(HttpClient client)
    {
        var response = await SendMutationAsync(client, HttpMethod.Post, "/api/session/login", new
        {
            userName = "operator",
            password = "Demo-Password-123!",
            rememberMe = false
        });
        response.EnsureSuccessStatusCode();
    }

    private static Task<HttpResponseMessage> CreateAccountAsync(HttpClient client) =>
        SendMutationAsync(client, HttpMethod.Post, "/api/customers/1000000001/accounts", new
        {
            metadata = new
            {
                type = "current",
                interestRate = 0,
                overdraftLimit = 0,
                currency = "GBP"
            },
            sourceSystem = "modern"
        });

    private static Task<HttpResponseMessage> DepositAsync(HttpClient client) =>
        SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/accounts/10000001/deposits",
            new { amount = 2000m },
            "failure-test-funding");

    private static async Task<HttpResponseMessage> SendMutationAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object body,
        string? idempotencyKey = null,
        string? correlationId = null)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/session/csrf");
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-XSRF-TOKEN", csrf.GetProperty("token").GetString());
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        if (correlationId is not null)
        {
            request.Headers.Add("X-Correlation-ID", correlationId);
        }
        return await client.SendAsync(request);
    }

    private sealed class SaveThenFailTransferRepository(IInternalTransferRepository inner)
        : IInternalTransferRepository
    {
        public Task<T> ExecuteSerializableAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken) =>
            inner.ExecuteSerializableAsync(operation, cancellationToken);

        public Task<bool> LockAccountAsync(string accountId, CancellationToken cancellationToken) =>
            inner.LockAccountAsync(accountId, cancellationToken);

        public Task<Account?> FindAccountAsync(string accountId, CancellationToken cancellationToken) =>
            inner.FindAccountAsync(accountId, cancellationToken);

        public Task<BookedTransaction?> FindByIdempotencyAsync(
            string sourceAccountId,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            inner.FindByIdempotencyAsync(sourceAccountId, idempotencyKey, cancellationToken);

        public Task<IReadOnlyList<BookedTransaction>> FindTransferAsync(
            string correlationId,
            CancellationToken cancellationToken) =>
            inner.FindTransferAsync(correlationId, cancellationToken);

        public void AddRange(BookedTransaction source, BookedTransaction destination) =>
            inner.AddRange(source, destination);

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await inner.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Injected infrastructure failure.");
        }
    }

    private sealed class CaptureLoggerProvider : ILoggerProvider
    {
        public ConcurrentBag<LogRecord> Records { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CaptureLogger(Records);

        public void Dispose()
        {
        }
    }

    private sealed class CaptureLogger(ConcurrentBag<LogRecord> records) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.Where(pair => pair.Key != "{OriginalFormat}")
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
                : [];
            records.Add(new(logLevel, formatter(state, exception), properties));
        }
    }

    private sealed record LogRecord(
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);
}
