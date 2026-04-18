using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkillBot.Core.Interfaces;

/// <summary>
/// Defines the contract for a caching service with two-tier caching support.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached value by key.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Stores a value in the cache with the specified TTL.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, string type, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a specific cache entry by key.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes entries matching the specified pattern (e.g., "llm:*", "search:web:*").
    /// </summary>
    Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current cache statistics.
    /// </summary>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Represents cache performance statistics.
/// </summary>
public class CacheStatistics
{
    public long L1Hits { get; set; }
    public long L1Misses { get; set; }
    public long L2Hits { get; set; }
    public long L2Misses { get; set; }
    public int TotalEntries { get; set; }
    public long TotalSizeBytes { get; set; }
    public double HitRate => TotalRequests > 0
        ? (double)(L1Hits + L2Hits) / TotalRequests * 100
        : 0;
    public long TotalRequests => L1Hits + L1Misses;
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
}
