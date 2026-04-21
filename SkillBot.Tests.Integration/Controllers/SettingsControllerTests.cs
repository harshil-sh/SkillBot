using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SkillBot.Tests.Integration.Controllers;

public class SettingsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SettingsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSettings_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateApiKey_Unauthenticated_ReturnsUnauthorized()
    {
        var content = JsonContent.Create(new { provider = "openai", apiKey = "sk-test" });
        var response = await _client.PutAsync("/api/settings/api-key", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProvider_Unauthenticated_ReturnsUnauthorized()
    {
        var content = JsonContent.Create(new { provider = "openai" });
        var response = await _client.PutAsync("/api/settings/provider", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
