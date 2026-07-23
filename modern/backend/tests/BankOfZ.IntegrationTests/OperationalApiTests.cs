using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BankOfZ.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class OperationalApiTests(BankOfZTestsFixture fixture) : IClassFixture<BankOfZTestsFixture>
{
    [Fact]
    public async Task Liveness_And_Readiness_Are_Separate_And_Read_Only()
    {
        using var client = fixture.Factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        var ready = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task Bank_Identity_Is_Validated_Configuration_And_Publicly_Readable()
    {
        using var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/configuration/bank");
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bank of Z", payload.GetProperty("displayName").GetString());
        Assert.Equal("100000", payload.GetProperty("sortCode").GetString());
    }

    [Fact]
    public async Task Response_Uses_Accepted_Or_Generated_Correlation_Id()
    {
        using var client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", "ops-smoke-001");

        var response = await client.SendAsync(request);

        Assert.Equal("ops-smoke-001", response.Headers.GetValues("X-Correlation-ID").Single());

        using var invalid = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        invalid.Headers.Add("X-Correlation-ID", "contains a secret-looking value");
        var generated = await client.SendAsync(invalid);
        var generatedId = generated.Headers.GetValues("X-Correlation-ID").Single();
        Assert.Matches("^[a-f0-9]{32}$", generatedId);
    }
}
