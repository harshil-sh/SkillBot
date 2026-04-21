using FluentAssertions;
using System.Net.Http.Json;

namespace SkillBot.Tests.E2E.Tests;

/// <summary>
/// Integration tests that verify frontend → API communication.
/// Requires the SkillBot.Api server running at http://localhost:5188.
/// </summary>
[TestFixture]
[Category("ApiIntegration")]
[Explicit("Requires live SkillBot.Api server at http://localhost:5188")]
public class ApiIntegrationTests
{
    private HttpClient _client = null!;
    private const string ApiBase = "http://localhost:5188";

    [SetUp]
    public void Setup()
    {
        _client = new HttpClient { BaseAddress = new Uri(ApiBase) };
    }

    [TearDown]
    public void TearDown() => _client.Dispose();

    [Test]
    public async Task Auth_Register_ReturnsToken()
    {
        var request = new { username = "apitestuser", email = $"apitest_{Guid.NewGuid():N}@test.com", password = "TestPass123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<dynamic>();
        ((object?)body).Should().NotBeNull();
    }

    [Test]
    public async Task Auth_Login_WithInvalidCredentials_Returns401()
    {
        var request = new { email = "nonexistent@test.com", password = "WrongPass123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Test]
    public async Task Chat_UnauthenticatedRequest_Returns401()
    {
        var request = new { message = "Hello" };
        var response = await _client.PostAsJsonAsync("/api/chat", request);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Test]
    public async Task Health_Endpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task Settings_UnauthenticatedRequest_Returns401()
    {
        var response = await _client.GetAsync("/api/settings");
        ((int)response.StatusCode).Should().Be(401);
    }
}
