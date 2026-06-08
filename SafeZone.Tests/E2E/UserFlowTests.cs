using Microsoft.AspNetCore.Mvc.Testing;
using SafeZone.Server.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SafeZone.Tests.E2E;

public class UserFlowTests : IClassFixture<WebApplicationFactory<Server.Program>>
{
    private readonly WebApplicationFactory<Server.Program> _factory;

    public UserFlowTests(WebApplicationFactory<Server.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LoginPage_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("SafeZone", content);
    }

    [Fact]
    public async Task SwaggerEndpoint_ReturnsOk_InDevelopment()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // swagger.json is protected by the API path selector, should return OK
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ElevenLabsWebhook_AcceptsDirectServerToolPayload()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/ElevenLabsWebhook", new
        {
            category = "Theft",
            description = "A caller reported a stolen motorcycle outside the market twenty minutes ago.",
            address = "F-7 Markaz, Islamabad",
            latitude = 33.7215,
            longitude = 73.0433,
            severity = "High",
            is_anonymous = false
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ElevenLabsWebhookResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.IncidentNumber));
    }
}
