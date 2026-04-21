using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SkillBot.Api.Models.Auth;
using SkillBot.Infrastructure.Data;

namespace SkillBot.Tests.Integration.Controllers;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidData_ReturnsToken()
    {
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Username = "newuser",
            Password = "StrongPass123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.UserId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        var request = new RegisterRequest
        {
            Email = "dup@example.com",
            Username = "dupuser",
            Password = "StrongPass123!"
        };

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        const string email = "login_valid@example.com";
        const string password = "StrongPass123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email, Username = "loginvalid", Password = password
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        const string email = "login_bad@example.com";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email, Username = "loginbad", Password = "StrongPass123!"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "WrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "ghost@example.com", Password = "AnyPass123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
