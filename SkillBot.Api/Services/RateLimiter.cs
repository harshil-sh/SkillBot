using Microsoft.Extensions.Caching.Memory;

namespace SkillBot.Api.Services;

public class RateLimiter : IRateLimiter
{
    private readonly IMemoryCache _cache;
    private const int MaxRequestsPerHour = 100;
    private const long CacheEntrySize = 1;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromHours(1);

    public RateLimiter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<RateLimitResult> CheckRateLimitAsync(string userId, string endpoint)
    {
        var cacheKey = $"ratelimit:{userId}";

        var entry = _cache.GetOrCreate(cacheKey, cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = WindowDuration;
            cacheEntry.SetSize(CacheEntrySize);
            return new RateLimitEntry
            {
                Count = 0,
                WindowStart = DateTime.UtcNow
            };
        });

        if (entry!.Count >= MaxRequestsPerHour)
        {
            var retryAfter = entry.WindowStart.Add(WindowDuration) - DateTime.UtcNow;
            return Task.FromResult(new RateLimitResult
            {
                IsAllowed = false,
                RemainingRequests = 0,
                RetryAfter = retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero
            });
        }

        entry.Count++;
        _cache.Set(cacheKey, entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = WindowDuration
        }.SetSize(CacheEntrySize));

        return Task.FromResult(new RateLimitResult
        {
            IsAllowed = true,
            RemainingRequests = MaxRequestsPerHour - entry.Count,
            RetryAfter = TimeSpan.Zero
        });
    }

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}
