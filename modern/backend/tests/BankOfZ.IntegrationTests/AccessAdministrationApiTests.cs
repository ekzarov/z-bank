using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BankOfZ.Application.AccessAdministration;
using BankOfZ.Infrastructure.AccessAdministration;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BankOfZ.IntegrationTests;

[Trait("Category", DatabaseIntegrationTestCategory.Name)]
[Collection(DatabaseTestCollection.Name)]
public sealed class AccessAdministrationApiTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Administration_Requires_Administrator_And_Returns_Safe_User_Page()
    {
        using var anonymous = CreateClient();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/administration/users")).StatusCode);

        using var customer = CreateClient();
        await LoginAsync(customer, "customer");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await customer.GetAsync("/api/administration/users")).StatusCode);

        using var administrator = CreateClient();
        await LoginAsync(administrator, "administrator");
        var response = await administrator.GetAsync(
            "/api/administration/users?query=operator&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var page = JsonDocument.Parse(json).RootElement;

        Assert.Equal(1, page.GetProperty("total").GetInt32());
        Assert.Equal("operator", page.GetProperty("items")[0].GetProperty("userName").GetString());
        Assert.Equal("Operator", page.GetProperty("items")[0].GetProperty("role").GetString());
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Administrator_Creates_And_Changes_A_User_With_Validation_And_No_Delete_Surface()
    {
        using var client = CreateClient();
        await LoginAsync(client, "administrator");

        var created = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/administration/users", new
        {
            userName = "auditor-one",
            email = "auditor-one@example.test",
            password = "Temporary-Password-123!",
            role = "Operator",
            customerId = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = user.GetProperty("id").GetGuid();
        var version = user.GetProperty("version").GetString()!;

        var invalidCustomerRole = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            $"/api/administration/users/{id}/role",
            new { role = "Customer", customerId = (string?)null, version });
        Assert.Equal(HttpStatusCode.BadRequest, invalidCustomerRole.StatusCode);

        var changed = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            $"/api/administration/users/{id}/role",
            new { role = "Administrator", customerId = (string?)null, version });
        changed.EnsureSuccessStatusCode();
        var changedUser = await changed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Administrator", changedUser.GetProperty("role").GetString());

        var stale = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            $"/api/administration/users/{id}/role",
            new { role = "Operator", customerId = (string?)null, version });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var missingVersion = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            $"/api/administration/users/{id}/lockout",
            new { locked = true });
        Assert.Equal(HttpStatusCode.BadRequest, missingVersion.StatusCode);

        var delete = await SendWithCsrfAsync(
            client,
            HttpMethod.Delete,
            $"/api/administration/users/{id}",
            new { });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, delete.StatusCode);
    }

    [Fact]
    public async Task Lockout_Revokes_Existing_Session_And_Unlock_Resets_State()
    {
        using var operatorClient = CreateClient();
        await LoginAsync(operatorClient, "operator");
        Assert.Equal(
            HttpStatusCode.OK,
            (await operatorClient.GetAsync("/api/access/operator")).StatusCode);

        using var administrator = CreateClient();
        await LoginAsync(administrator, "administrator");
        var operatorUser = await FindUserAsync(administrator, "operator");
        var id = operatorUser.GetProperty("id").GetGuid();

        var locked = await SendWithCsrfAsync(
            administrator,
            HttpMethod.Put,
            $"/api/administration/users/{id}/lockout",
            new { locked = true, version = operatorUser.GetProperty("version").GetString() });
        locked.EnsureSuccessStatusCode();
        var lockedUser = await locked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(lockedUser.GetProperty("isLockedOut").GetBoolean());
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await operatorClient.GetAsync("/api/access/operator")).StatusCode);

        var unlocked = await SendWithCsrfAsync(
            administrator,
            HttpMethod.Put,
            $"/api/administration/users/{id}/lockout",
            new { locked = false, version = lockedUser.GetProperty("version").GetString() });
        unlocked.EnsureSuccessStatusCode();
        var unlockedUser = await unlocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(unlockedUser.GetProperty("isLockedOut").GetBoolean());
        Assert.Equal(0, unlockedUser.GetProperty("accessFailedCount").GetInt32());
    }

    [Fact]
    public async Task Administrator_Cannot_Change_Its_Own_Access_And_Rejections_Are_Audited()
    {
        using var client = CreateClient();
        await LoginAsync(client, "administrator");
        var administrator = await FindUserAsync(client, "administrator");
        var id = administrator.GetProperty("id").GetGuid();

        var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            $"/api/administration/users/{id}/lockout",
            new { locked = true, version = administrator.GetProperty("version").GetString() });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var audit = await client.GetFromJsonAsync<JsonElement>(
            $"/api/administration/security-audit?eventName=user-locked&actorOrSubject={id}&succeeded=false");
        Assert.True(audit.GetProperty("total").GetInt32() >= 1);
        Assert.Equal("conflict", audit.GetProperty("items")[0].GetProperty("outcome").GetString());
    }

    [Fact]
    public async Task Concurrent_Administrator_Lockouts_Preserve_An_Unlocked_Administrator()
    {
        using var bootstrap = CreateClient();
        await LoginAsync(bootstrap, "administrator");
        await CreateAdministratorAsync(bootstrap, "administrator-a");
        await CreateAdministratorAsync(bootstrap, "administrator-b");

        using var first = CreateClient();
        using var second = CreateClient();
        (await LoginResponseAsync(first, "administrator-a", "Temporary-Password-123!"))
            .EnsureSuccessStatusCode();
        (await LoginResponseAsync(second, "administrator-b", "Temporary-Password-123!"))
            .EnsureSuccessStatusCode();

        var original = await FindUserAsync(first, "administrator");
        var lockOriginal = await SendWithCsrfAsync(
            first,
            HttpMethod.Put,
            $"/api/administration/users/{original.GetProperty("id").GetGuid()}/lockout",
            new { locked = true, version = original.GetProperty("version").GetString() });
        lockOriginal.EnsureSuccessStatusCode();

        var firstUser = await FindUserAsync(second, "administrator-a");
        var secondUser = await FindUserAsync(first, "administrator-b");
        var attempts = await Task.WhenAll(
            SendWithCsrfAsync(
                first,
                HttpMethod.Put,
                $"/api/administration/users/{secondUser.GetProperty("id").GetGuid()}/lockout",
                new { locked = true, version = secondUser.GetProperty("version").GetString() }),
            SendWithCsrfAsync(
                second,
                HttpMethod.Put,
                $"/api/administration/users/{firstUser.GetProperty("id").GetGuid()}/lockout",
                new { locked = true, version = firstUser.GetProperty("version").GetString() }));

        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(attempts, response =>
            response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Unauthorized);

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var administratorRole = await context.Roles
            .SingleAsync(role => role.Name == "Administrator");
        var unlockedAdministrators = await (
            from userRole in context.UserRoles
            join user in context.Users on userRole.UserId equals user.Id
            where userRole.RoleId == administratorRole.Id
                && (!user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow)
            select user.Id)
            .CountAsync();
        Assert.True(unlockedAdministrators >= 1);
    }

    [Fact]
    public async Task Security_Audit_Is_Persistent_Filtered_Newest_First_And_Append_Only()
    {
        using var client = CreateClient();
        await LoginAsync(client, "administrator");
        await LoginResponseAsync(CreateClient(), "missing", "Wrong-Password-123!");
        using var operatorClient = CreateClient();
        await LoginAsync(operatorClient, "operator");
        var logout = await SendWithCsrfAsync(
            operatorClient,
            HttpMethod.Post,
            "/api/session/logout",
            new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var page = await client.GetFromJsonAsync<JsonElement>(
            "/api/administration/security-audit?eventName=login&page=1&pageSize=20");
        Assert.True(page.GetProperty("total").GetInt32() >= 2);
        var items = page.GetProperty("items").EnumerateArray().ToArray();
        Assert.All(items, item => Assert.Equal("login", item.GetProperty("eventName").GetString()));
        Assert.True(items.Zip(items.Skip(1), (left, right) =>
            left.GetProperty("occurredAt").GetDateTimeOffset() >=
            right.GetProperty("occurredAt").GetDateTimeOffset()).All(value => value));

        var logoutPage = await client.GetFromJsonAsync<JsonElement>(
            "/api/administration/security-audit?eventName=logout&succeeded=true");
        Assert.True(logoutPage.GetProperty("total").GetInt32() >= 1);
        Assert.Equal(
            "session-revoked",
            logoutPage.GetProperty("items")[0].GetProperty("outcome").GetString());

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();
        var record = await context.SecurityAuditEntries.FirstAsync();
        record.Outcome = "tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Audit_Failure_Rolls_Back_User_Creation()
    {
        await using var factory = new BankOfZApiFactory(
            Fixture.ConnectionString,
            configureServices: services =>
            {
                services.RemoveAll<ISecurityAuditWriter>();
                services.AddScoped<ISecurityAuditWriter, ThrowingAuditWriter>();
            });
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAccessAdministrationService>();
        var administrator = await scope.ServiceProvider
            .GetRequiredService<BankOfZIdentityContext>()
            .Users.SingleAsync(user => user.UserName == "administrator");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(
            new(
                "rollback-user",
                "rollback-user@example.test",
                "Temporary-Password-123!",
                "Operator",
                null),
            administrator.Id,
            "rollback-test"));

        await using var verify = new BankOfZIdentityContext(
            new DbContextOptionsBuilder<BankOfZIdentityContext>()
                .UseSqlServer(Fixture.ConnectionString)
                .Options);
        Assert.False(await verify.Users.AnyAsync(user => user.UserName == "rollback-user"));
    }

    private HttpClient CreateClient() => Fixture.Factory.CreateClient(new()
    {
        BaseAddress = new Uri("https://localhost")
    });

    private static async Task<JsonElement> FindUserAsync(HttpClient client, string query)
    {
        var page = await client.GetFromJsonAsync<JsonElement>(
            $"/api/administration/users?query={Uri.EscapeDataString(query)}");
        return page.GetProperty("items")[0];
    }

    private static async Task LoginAsync(HttpClient client, string userName)
    {
        var response = await LoginResponseAsync(client, userName, "Demo-Password-123!");
        response.EnsureSuccessStatusCode();
    }

    private static async Task CreateAdministratorAsync(HttpClient client, string userName)
    {
        var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/administration/users",
            new
            {
                userName,
                email = $"{userName}@example.test",
                password = "Temporary-Password-123!",
                role = "Administrator",
                customerId = (string?)null
            });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> LoginResponseAsync(
        HttpClient client,
        string userName,
        string password) =>
        await SendWithCsrfAsync(client, HttpMethod.Post, "/api/session/login", new
        {
            userName,
            password,
            rememberMe = false
        });

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/session/csrf");
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-XSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }

    private sealed class ThrowingAuditWriter : ISecurityAuditWriter
    {
        public Task WriteAsync(
            SecurityAuditEntry entry,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected audit failure.");
    }
}
