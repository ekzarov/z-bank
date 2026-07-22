using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BankOfZ.IntegrationTests;

[Trait("Category", DatabaseIntegrationTestCategory.Name)]
public sealed class SessionApiTests(BankOfZTestsFixture fixture)
    : DatabaseIntegrationTestBase(fixture), IClassFixture<BankOfZTestsFixture>
{
    [Fact]
    public async Task Anonymous_Request_Returns_Problem_Details()
    {
        using var client = CreateSecureClient();

        var response = await client.GetAsync("/api/access/customer");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Authentication required", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Anonymous_Session_Has_No_Identity_Data()
    {
        using var client = CreateSecureClient();

        var session = await client.GetFromJsonAsync<JsonElement>("/api/session");

        Assert.False(session.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal(JsonValueKind.Null, session.GetProperty("userName").ValueKind);
        Assert.Empty(session.GetProperty("roles").EnumerateArray());
    }

    [Fact]
    public async Task Authenticated_Session_Returns_Channel_Neutral_Customer_Identity()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "customer", "Demo-Password-123!");

        var session = await client.GetFromJsonAsync<JsonElement>("/api/session");

        Assert.True(session.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal("customer", session.GetProperty("userName").GetString());
        Assert.Equal("1000000001", session.GetProperty("customerId").GetString());
        Assert.Equal("Customer", session.GetProperty("roles")[0].GetString());
    }

    [Fact]
    public async Task Customer_Login_Uses_Csrf_And_Enforces_Role()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "customer", "Demo-Password-123!");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/access/customer")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/access/operator")).StatusCode);
    }

    [Theory]
    [InlineData("operator", "/api/access/operator")]
    [InlineData("administrator", "/api/access/administrator")]
    public async Task Staff_Role_Can_Access_Its_Endpoint(string userName, string endpoint)
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, userName, "Demo-Password-123!");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(endpoint)).StatusCode);
    }

    [Theory]
    [InlineData("customer", "/api/access/administrator")]
    [InlineData("operator", "/api/access/administrator")]
    [InlineData("administrator", "/api/access/operator")]
    public async Task Wrong_Role_Returns_Problem_Details(string userName, string endpoint)
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, userName, "Demo-Password-123!");

        var response = await client.GetAsync(endpoint);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Access denied", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Logout_Revokes_The_Current_Session()
    {
        using var client = CreateSecureClient();
        await LoginAsync(client, "operator", "Demo-Password-123!");
        var token = await GetCsrfAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/logout")
        {
            Content = JsonContent.Create(new { })
        };
        request.Headers.Add("X-XSRF-TOKEN", token);
        var logout = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/access/operator")).StatusCode);
    }

    [Fact]
    public async Task Login_Without_Csrf_Is_Rejected()
    {
        using var client = CreateSecureClient();

        var response = await client.PostAsJsonAsync("/api/session/login", new
        {
            userName = "customer",
            password = "Demo-Password-123!",
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_User_And_Invalid_Password_Return_The_Same_Problem()
    {
        using var missingUserClient = CreateSecureClient();
        using var wrongPasswordClient = CreateSecureClient();

        var missingUser = await LoginResponseAsync(missingUserClient, "missing", "wrong");
        var wrongPassword = await LoginResponseAsync(wrongPasswordClient, "customer", "wrong");

        Assert.Equal(HttpStatusCode.Unauthorized, missingUser.StatusCode);
        Assert.Equal(await missingUser.Content.ReadAsStringAsync(), await wrongPassword.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Repeated_Failures_Lock_The_User()
    {
        using var client = CreateSecureClient();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var failed = await LoginResponseAsync(client, "customer", "wrong");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var locked = await LoginResponseAsync(client, "customer", "Demo-Password-123!");
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    private static async Task LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await LoginResponseAsync(client, userName, password);
        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateSecureClient() => Fixture.Factory.CreateClient(new()
    {
        BaseAddress = new Uri("https://localhost")
    });

    private static async Task<HttpResponseMessage> LoginResponseAsync(HttpClient client, string userName, string password)
    {
        var token = await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session/login")
        {
            Content = JsonContent.Create(new { userName, password, rememberMe = false })
        };
        request.Headers.Add("X-XSRF-TOKEN", token);
        return await client.SendAsync(request);
    }

    private static async Task<string> GetCsrfAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<JsonElement>("/api/session/csrf");
        return response.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("CSRF token was not returned.");
    }
}
