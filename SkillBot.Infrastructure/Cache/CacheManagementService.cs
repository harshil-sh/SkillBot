using Microsoft.Extensions.Logging;
using SkillBot.Core.Interfaces;
using SkillBot.Infrastructure.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace SkillBot.Infrastructure.Cache;

/// <summary>
/// Provides cache management operations.
/// </summary>
public class CacheManagementService : ICacheManagementService
{
    private readonly ICacheService _cacheService;
    private readonly CachingOptions _options;
    private readonly ILogger<CacheManagementService> _logger;

    public CacheManagementService(
        ICacheService cacheService,
        CachingOptions options,
        ILogger<CacheManagementService> logger)
    {
        _cacheService = cacheService;
        _options = options;
        _logger = logger;
    }

    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = _cacheService.GetStatistics();
        _logger.LogDebug("Retrieved cache statistics: {Entries} entries, {HitRate}% hit rate", stats.TotalEntries, stats.HitRate);
        return Task.FromResult(stats);
    }

    public async Task InvalidateCacheAsync(string pattern, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating cache entries matching pattern: {Pattern}", pattern);
        await _cacheService.InvalidateByPatternAsync(pattern, cancellationToken);
    }

    public async Task ClearAllCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Clearing all cache entries");
        await _cacheService.ClearAsync(cancellationToken);
    }

    public Task<CacheHealth> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var stats = _cacheService.GetStatistics();
        var maxSizeBytes = (long)_options.MaxCacheSizeMb * 1024 * 1024;

        var health = new CacheHealth
        {
            IsHealthy = stats.TotalSizeBytes < maxSizeBytes && stats.HitRate >= 0,
            HitRate = stats.HitRate,
            TotalSizeBytes = stats.TotalSizeBytes,
            TotalEntries = stats.TotalEntries,
            Message = stats.TotalSizeBytes >= maxSizeBytes
                ? $"Cache size ({stats.TotalSizeBytes / 1024 / 1024}MB) exceeds maximum ({_options.MaxCacheSizeMb}MB)"
                : "Healthy"
        };

        _logger.LogDebug("Cache health check: {IsHealthy}, Hit rate: {HitRate}%", health.IsHealthy, health.HitRate);
        return Task.FromResult(health);
    }
}
