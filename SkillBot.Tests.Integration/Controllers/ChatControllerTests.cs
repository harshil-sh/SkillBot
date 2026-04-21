using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SkillBot.Api.Models.Auth;

namespace SkillBot.Tests.Integration.Controllers;

public class ChatControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChatControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Chat_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/chat",
            new { message = "Hello" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHistory_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/chat/history");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Chat_InvalidInput_ReturnsBadRequest()
    {
        var token = await RegisterAndGetTokenAsync("chatinvalid@example.com", "chatinvalid");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Empty message
        var response = await _client.PostAsJsonAsync("/api/chat",
            new { message = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    private async Task<string> RegisterAndGetTokenAsync(string email, string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Username = username,
            Password = "StrongPass123!"
        });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }
}
