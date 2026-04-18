using System;

namespace SkillBot.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the caching system.
/// </summary>
public class CachingOptions
{
    /// <summary>
    /// Whether caching is enabled globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Path to the SQLite cache database file.
    /// </summary>
    public string CacheDatabasePath { get; set; } = "skillbot-cache.db";

    /// <summary>
    /// Memory cache size limit in MB.
    /// </summary>
    public int MemoryCacheSizeMb { get; set; } = 100;

    /// <summary>
    /// Maximum total cache size in MB.
    /// </summary>
    public int MaxCacheSizeMb { get; set; } = 500;

    /// <summary>
    /// TTL for routing LLM calls.
    /// </summary>
    public TimeSpan RoutingCacheTtl { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// TTL for agent execution LLM calls.
    /// </summary>
    public TimeSpan AgentCacheTtl { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// TTL for general LLM calls.
    /// </summary>
    public TimeSpan GeneralCacheTtl { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// TTL for web search results.
    /// </summary>
    public TimeSpan WebSearchTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// TTL for news search results.
    /// </summary>
    public TimeSpan NewsSearchTtl { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// TTL for image search results.
    /// </summary>
    public TimeSpan ImageSearchTtl { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// Interval for automatic cache cleanup.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Whether to enable automatic background cleanup.
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;
}
