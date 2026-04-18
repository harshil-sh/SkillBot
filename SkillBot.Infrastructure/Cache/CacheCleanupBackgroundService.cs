using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Interfaces;
using SkillBot.Infrastructure.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkillBot.Infrastructure.Cache;

/// <summary>
/// Background service that periodically cleans up expired cache entries.
/// </summary>
public class CacheCleanupBackgroundService : BackgroundService
{
    private readonly ICacheService _cacheService;
    private readonly CachingOptions _options;
    private readonly ILogger<CacheCleanupBackgroundService> _logger;

    public CacheCleanupBackgroundService(
        ICacheService cacheService,
        CachingOptions options,
        ILogger<CacheCleanupBackgroundService> logger)
    {
        _cacheService = cacheService;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache cleanup service started with interval: {Interval}", _options.CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.CleanupInterval, stoppingToken);

                _logger.LogDebug("Running cache cleanup...");
                await _cacheService.CleanupExpiredAsync(stoppingToken);

                var stats = _cacheService.GetStatistics();
                _logger.LogInformation(
                    "Cache cleanup completed. Stats - Entries: {Entries}, Hit rate: {HitRate}%, Size: {SizeMB}MB",
                    stats.TotalEntries,
                    stats.HitRate,
                    stats.TotalSizeBytes / 1024 / 1024);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        _logger.LogInformation("Cache cleanup service stopped");
    }
}
