using System.Threading;
using System.Threading.Tasks;

namespace SkillBot.Core.Interfaces;

/// <summary>
/// Provides cache management operations including statistics and invalidation.
/// </summary>
public interface ICacheManagementService
{
    /// <summary>
    /// Gets current cache statistics.
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries matching the specified pattern.
    /// </summary>
    Task InvalidateCacheAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    Task ClearAllCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache health information.
    /// </summary>
    Task<CacheHealth> GetHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the health status of the cache.
/// </summary>
public class CacheHealth
{
    public bool IsHealthy { get; set; }
    public double HitRate { get; set; }
    public long TotalSizeBytes { get; set; }
    public int TotalEntries { get; set; }
    public string? Message { get; set; }
}
