namespace SkillBot.Api.Models.Responses;

/// <summary>
/// Cache performance statistics response
/// </summary>
public class CacheStatisticsResponse
{
    /// <summary>
    /// Number of successful cache hits in L1 (memory) cache
    /// </summary>
    public long L1Hits { get; init; }

    /// <summary>
    /// Number of cache misses in L1 cache
    /// </summary>
    public long L1Misses { get; init; }

    /// <summary>
    /// Number of successful cache hits in L2 (persistent) cache
    /// </summary>
    public long L2Hits { get; init; }

    /// <summary>
    /// Number of cache misses in L2 cache
    /// </summary>
    public long L2Misses { get; init; }

    /// <summary>
    /// Total number of cached entries
    /// </summary>
    public int TotalEntries { get; init; }

    /// <summary>
    /// Total size of cached data in bytes
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Cache hit rate as a percentage (0-100)
    /// </summary>
    public double HitRatePercentage { get; init; }

    /// <summary>
    /// Total number of cache requests
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Total number of successful cache hits (L1 + L2)
    /// </summary>
    public long TotalHits { get; init; }

    /// <summary>
    /// Total number of cache misses (L1 + L2)
    /// </summary>
    public long TotalMisses { get; init; }

    /// <summary>
    /// Timestamp of the oldest cache entry
    /// </summary>
    public DateTime? OldestEntry { get; init; }

    /// <summary>
    /// Timestamp of the newest cache entry
    /// </summary>
    public DateTime? NewestEntry { get; init; }
}
