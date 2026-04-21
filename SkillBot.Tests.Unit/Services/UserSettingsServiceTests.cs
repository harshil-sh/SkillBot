using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SkillBot.Api.Services;
using SkillBot.Infrastructure.Repositories;
using SkillBot.Tests.Unit.Helpers;

namespace SkillBot.Tests.Unit.Services;

public class UserSettingsServiceTests
{
    private static UserSettingsService CreateSut(IUserRepository repo)
        => new(repo, NullLogger<UserSettingsService>.Instance);

    // ── GetSettings ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSettings_ValidUser_ReturnsSettings()
    {
        await using var ctx = TestDbContextHelper.CreateInMemoryContext(nameof(GetSettings_ValidUser_ReturnsSettings));
        await TestDbContextHelper.SeedTestDataAsync(ctx);
        var repo = new SqliteUserRepository(ctx);
        var sut = CreateSut(repo);

        var settings = await sut.GetSettingsAsync("test-user-1");

        settings.PreferredProvider.Should().Be("openai");
        settings.HasOpenAiKey.Should().BeTrue();
        settings.HasClaudeKey.Should().BeFalse();
    }

    [Fact]
    public async Task GetSettings_InvalidUser_ThrowsKeyNotFoundException()
    {
        await using var ctx = TestDbContextHelper.CreateInMemoryContext(nameof(GetSettings_InvalidUser_ThrowsKeyNotFoundException));
        var repo = new SqliteUserRepository(ctx);
        var sut = CreateSut(repo);

        var act = () => sut.GetSettingsAsync("no-such-user");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateApiKey ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("openai",  "sk-openai-new")]
    [InlineData("claude",  "sk-claude-new")]
    [InlineData("gemini",  "sk-gemini-new")]
    [InlineData("serpapi", "sk-serpapi-new")]
    public async Task UpdateApiKey_ValidProvider_Updates(string provider, string newKey)
    {
        var dbName = $"{nameof(UpdateApiKey_ValidProvider_Updates)}_{provider}";
        await using var ctx = TestDbContextHelper.CreateInMemoryContext(dbName);
        await TestDbContextHelper.SeedTestDataAsync(ctx);
        var repo = new SqliteUserRepository(ctx);
        var sut = CreateSut(repo);

        await sut.UpdateApiKeyAsync("test-user-1", provider, newKey);

        var user = await repo.GetByIdAsync("test-user-1");
        var stored = provider.ToLowerInvariant() switch
        {
            "openai"  => user!.OpenAiApiKey,
            "claude"  => user!.ClaudeApiKey,
            "gemini"  => user!.GeminiApiKey,
            "serpapi" => user!.SerpApiKey,
            _         => null
        };
        stored.Should().Be(newKey);
    }

    [Fact]
    public async Task UpdateApiKey_InvalidProvider_ThrowsArgumentException()
    {
        await using var ctx = TestDbContextHelper.CreateInMemoryContext(nameof(UpdateApiKey_InvalidProvider_ThrowsArgumentException));
        await TestDbContextHelper.SeedTestDataAsync(ctx);
        var repo = new SqliteUserRepository(ctx);
        var sut = CreateSut(repo);

        var act = () => sut.UpdateApiKeyAsync("test-user-1", "badprovider", "key");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── UpdateProvider ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("openai")]
    [InlineData("claude")]
    [InlineData("gemini")]
    public async Task UpdateProvider_ValidProvider_Updates(string provider)
    {
        var dbName = $"{nameof(UpdateProvider_ValidProvider_Updates)}_{provider}";
        await using var ctx = TestDbContextHelper.CreateInMemoryContext(dbName);
        await TestDbContextHelper.SeedTestDataAsync(ctx);
        var repo = new SqliteUserRepository(ctx);
        var sut = CreateSut(repo);

        await sut.UpdateProviderAsync("test-user-1", provider);

        var user = await repo.GetByIdAsync("test-user-1");
        user!.PreferredProvider.Should().Be(provider);
    }

    [Fact]
    public async Task UpdateProvider_InvalidProvider_ThrowsArgumentException()
    {
        await using var ctx = TestDbContextHelper.CreateInMemoryContext(nameof(UpdateProvider_InvalidProvider_ThrowsArgumentException));
        await TestDbContextHelper.SeedTestDataAsync(ctx);
        var repo = new SqliteUserRepository(ctx);
        var sut = CreateSut(repo);

        var act = () => sut.UpdateProviderAsync("test-user-1", "serpapi");

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
