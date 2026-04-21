using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using SkillBot.Api.Services;

namespace SkillBot.Tests.Unit.Services;

public class RateLimiterTests : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly RateLimiter _sut;

    public RateLimiterTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });
        _sut = new RateLimiter(_cache);
    }

    public void Dispose() => _cache.Dispose();

    [Fact]
    public async Task FirstRequest_IsAllowed()
    {
        var result = await _sut.CheckRateLimitAsync("user-1", "chat");

        result.IsAllowed.Should().BeTrue();
        result.RemainingRequests.Should().Be(99);
    }

    [Fact]
    public async Task UnderLimit_IsAllowed()
    {
        for (var i = 0; i < 50; i++)
            await _sut.CheckRateLimitAsync("user-under", "chat");

        var result = await _sut.CheckRateLimitAsync("user-under", "chat");

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task OverLimit_IsNotAllowed()
    {
        // Exhaust the 100-request limit
        for (var i = 0; i < 100; i++)
            await _sut.CheckRateLimitAsync("user-over", "chat");

        var result = await _sut.CheckRateLimitAsync("user-over", "chat");

        result.IsAllowed.Should().BeFalse();
        result.RemainingRequests.Should().Be(0);
        result.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task DifferentUsers_IndependentLimits()
    {
        // Exhaust limit for user A
        for (var i = 0; i < 100; i++)
            await _sut.CheckRateLimitAsync("user-a", "chat");

        // User B should still be allowed
        var resultB = await _sut.CheckRateLimitAsync("user-b", "chat");

        resultB.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task DifferentEndpoints_ShareSameUserBucket()
    {
        // The current RateLimiter uses only userId as key (not endpoint),
        // so different endpoints share the same bucket per user.
        for (var i = 0; i < 100; i++)
            await _sut.CheckRateLimitAsync("user-ep", "chat");

        var result = await _sut.CheckRateLimitAsync("user-ep", "settings");

        result.IsAllowed.Should().BeFalse();
    }
}
