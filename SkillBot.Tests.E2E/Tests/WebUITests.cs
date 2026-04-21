using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;
using FluentAssertions;

namespace SkillBot.Tests.E2E.Tests;

[TestFixture]
[Category("E2E")]
[Explicit("Requires live SkillBot.Web server at http://localhost:5000")]
public class WebUITests : PageTest
{
    private const string BaseUrl = "http://localhost:5000";

    [Test]
    public async Task RegistrationFlow_ValidData_RedirectsToLogin()
    {
        await Page.GotoAsync($"{BaseUrl}/register");
        await Page.FillAsync("[data-testid='username']", "testuser_e2e");
        await Page.FillAsync("[data-testid='email']", "e2e@test.com");
        await Page.FillAsync("[data-testid='password']", "TestPass123!");
        await Page.FillAsync("[data-testid='confirm-password']", "TestPass123!");
        await Page.ClickAsync("[data-testid='register-btn']");
        await Page.WaitForURLAsync($"{BaseUrl}/login");
        Page.Url.Should().Contain("/login");
    }

    [Test]
    public async Task LoginFlow_ValidCredentials_RedirectsToChat()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.FillAsync("[data-testid='email']", "e2e@test.com");
        await Page.FillAsync("[data-testid='password']", "TestPass123!");
        await Page.ClickAsync("[data-testid='login-btn']");
        await Page.WaitForURLAsync($"{BaseUrl}/chat");
        Page.Url.Should().Contain("/chat");
    }

    [Test]
    public async Task LoginFlow_InvalidCredentials_ShowsError()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.FillAsync("[data-testid='email']", "wrong@test.com");
        await Page.FillAsync("[data-testid='password']", "WrongPass123!");
        await Page.ClickAsync("[data-testid='login-btn']");
        var errorAlert = Page.Locator(".mud-alert");
        await errorAlert.WaitForAsync();
        (await errorAlert.IsVisibleAsync()).Should().BeTrue();
    }

    [Test]
    public async Task ChatFlow_SendMessage_DisplaysResponse()
    {
        // Assumes already logged in (share auth state or login first)
        await Page.GotoAsync($"{BaseUrl}/chat");
        // If redirected to login, login first
        if (Page.Url.Contains("/login"))
        {
            await Page.FillAsync("[data-testid='email']", "e2e@test.com");
            await Page.FillAsync("[data-testid='password']", "TestPass123!");
            await Page.ClickAsync("[data-testid='login-btn']");
            await Page.WaitForURLAsync($"{BaseUrl}/chat");
        }
        await Page.FillAsync("[data-testid='message-input']", "Hello, SkillBot!");
        await Page.ClickAsync("[data-testid='send-btn']");
        // Wait for AI response (up to 30s)
        await Page.WaitForSelectorAsync(".ai-message", new PageWaitForSelectorOptions { Timeout = 30000 });
        var messages = Page.Locator(".ai-message");
        (await messages.CountAsync()).Should().BeGreaterThan(0);
    }

    [Test]
    public async Task SettingsFlow_UnauthenticatedUser_RedirectsToLogin()
    {
        await Page.GotoAsync($"{BaseUrl}/settings");
        await Page.WaitForURLAsync($"{BaseUrl}/login");
        Page.Url.Should().Contain("/login");
    }

    [Test]
    public async Task Navigation_AllMainRoutes_LoadWithoutErrors()
    {
        // Public routes
        var routes = new[] { "/login", "/register" };
        foreach (var route in routes)
        {
            var response = await Page.GotoAsync($"{BaseUrl}{route}");
            response.Should().NotBeNull();
            response!.Status.Should().Be(200);
        }
    }
}
