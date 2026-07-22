using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BankOfZ.Application.Customers;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BankOfZ.IntegrationTests;

[Trait("Category", DatabaseIntegrationTestCategory.Name)]
[Collection(DatabaseTestCollection.Name)]
public sealed class CustomerApiTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Operator_Creates_Customer_With_Id_Sort_Code_Provenance_And_Audit()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "operator");
        var options = Fixture.Factory.Services.GetRequiredService<CustomerOptions>();
        Assert.Equal(5, options.CreditProviders.Length);
        Assert.Equal(5, options.CreditProviders.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var response = await SendMutationAsync(client, HttpMethod.Post, "/api/customers", CreateRequest());
        var customer = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("1000000002", customer.GetProperty("id").GetString());
        Assert.Equal("100000", customer.GetProperty("sortCode").GetString());
        Assert.Equal("modern", customer.GetProperty("sourceSystem").GetString());
        Assert.NotEmpty(customer.GetProperty("version").GetString()!);

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var audit = await context.CustomerAuditEntries.SingleAsync(entry => entry.CustomerId == "1000000002");
        Assert.Equal("operator", audit.Actor);
        Assert.Equal("CustomerCreated", audit.Action);
        Assert.NotEmpty(audit.CorrelationId);
    }

    [Fact]
    public async Task Operator_Search_Is_Case_Insensitive_And_Exact_Id_Preserves_Zeroes()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "operator");

        var search = await client.GetFromJsonAsync<JsonElement>("/api/customers?name=jAmIe");
        var exact = await client.GetFromJsonAsync<JsonElement>("/api/customers/1000000001");

        Assert.Single(search.EnumerateArray());
        Assert.Equal("1000000001", exact.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Customer_Can_View_Only_Associated_Profile()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "customer");

        var profile = await client.GetFromJsonAsync<JsonElement>("/api/customers/me");
        var operatorSearch = await client.GetAsync("/api/customers?name=Jamie");

        Assert.Equal("1000000001", profile.GetProperty("id").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, operatorSearch.StatusCode);
    }

    [Fact]
    public async Task Missing_Customer_Returns_Generic_Not_Found()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "operator");

        var response = await client.GetAsync("/api/customers/9999999999");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Customer not found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Invalid_Create_Is_Rejected_Without_Customer_Or_Audit()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "operator");
        var invalid = CreateRequest() with
        {
            Details = CreateRequest().Details with { CountryCode = "GBR" }
        };

        var response = await SendMutationAsync(client, HttpMethod.Post, "/api/customers", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Equal(1, await context.Customers.CountAsync());
        Assert.Empty(await context.CustomerAuditEntries.ToArrayAsync());
    }

    [Fact]
    public async Task Total_Credit_Provider_Failure_Returns_Unavailable_Without_Customer_Persistence()
    {
        await using var unavailableFactory = new BankOfZApiFactory(
            Fixture.ConnectionString,
            new CustomerOptions
            {
                CreditProviders = CustomerOptions.DefaultCreditProviders.ToArray(),
                FailedCreditProviders = CustomerOptions.DefaultCreditProviders.ToArray()
            });
        using var client = unavailableFactory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, "operator");

        var response = await SendMutationAsync(client, HttpMethod.Post, "/api/customers", CreateRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Equal(1, await context.Customers.CountAsync());
        Assert.Empty(await context.CustomerAuditEntries.ToArrayAsync());
    }

    [Fact]
    public async Task Stale_Update_Returns_Conflict_And_Does_Not_Add_Audit()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "operator");
        var original = await client.GetFromJsonAsync<JsonElement>("/api/customers/1000000001");
        var version = original.GetProperty("version").GetString()!;
        var update = new
        {
            details = Details(lastName: "Updated"),
            version
        };

        var first = await SendMutationAsync(client, HttpMethod.Put, "/api/customers/1000000001", update);
        var stale = await SendMutationAsync(client, HttpMethod.Put, "/api/customers/1000000001", update);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Single(await context.CustomerAuditEntries.Where(entry => entry.Action == "CustomerUpdated").ToArrayAsync());
    }

    [Fact]
    public async Task Retirement_Is_Audited_Soft_State_Transition()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "operator");
        var original = await client.GetFromJsonAsync<JsonElement>("/api/customers/1000000001");

        var response = await SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/customers/1000000001/retire",
            new { version = original.GetProperty("version").GetString() });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var customer = await context.Customers.SingleAsync(entity => entity.Id == "1000000001");
        Assert.Equal("Retired", customer.Status.ToString());
        Assert.Equal("Jamie", customer.FirstName);
        Assert.Contains(await context.CustomerAuditEntries.ToArrayAsync(), entry => entry.Action == "CustomerRetired");
    }

    [Fact]
    public async Task Retirement_Is_Rejected_When_Accounts_Block_It()
    {
        await using var blockedFactory = Fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICustomerAccountStatusReader>();
                services.AddSingleton<ICustomerAccountStatusReader>(new BlockingAccountStatusReader());
            }));
        using var client = blockedFactory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, "operator");
        var original = await client.GetFromJsonAsync<JsonElement>("/api/customers/1000000001");

        var response = await SendMutationAsync(
            client,
            HttpMethod.Post,
            "/api/customers/1000000001/retire",
            new { version = original.GetProperty("version").GetString() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        Assert.Equal("Active", (await context.Customers.SingleAsync()).Status.ToString());
        Assert.Empty(await context.CustomerAuditEntries.ToArrayAsync());
    }

    [Fact]
    public async Task Identity_Customer_Relationship_Is_Enforced_By_Sql_Server()
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var user = await context.Users.SingleAsync(entity => entity.UserName == "operator");
        user.CustomerId = "9999999999";

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private HttpClient CreateSecureClient() => Fixture.Factory.CreateClient(new()
    {
        BaseAddress = new Uri("https://localhost")
    });

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

    private static CreateCustomerTestRequest CreateRequest() => new(
        Details(),
        "modern",
        "integration-test");

    private static CustomerDetailsTestRequest Details(string lastName = "Example") => new(
        "Ms",
        "Alex",
        lastName,
        new DateOnly(1992, 3, 14),
        "42 Test Avenue",
        null,
        "Leeds",
        "West Yorkshire",
        "LS1 1AA",
        "GB",
        "alex@example.test",
        "+44 113 000 0000");

    private sealed record CreateCustomerTestRequest(
        CustomerDetailsTestRequest Details,
        string SourceSystem,
        string SourceIdentifier);

    private sealed record CustomerDetailsTestRequest(
        string Title,
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string? Region,
        string PostalCode,
        string CountryCode,
        string Email,
        string? Phone);

    private sealed class BlockingAccountStatusReader : ICustomerAccountStatusReader
    {
        public Task<CustomerAccountStatus> GetAsync(string customerId, CancellationToken cancellationToken) =>
            Task.FromResult(new CustomerAccountStatus(true, false));
    }
}
