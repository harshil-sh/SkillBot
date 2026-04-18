using Microsoft.Extensions.Caching.Memory;
using SkillBot.Core.Models;

namespace SkillBot.Api.Services;

/// <summary>
/// Interface for token usage tracking
/// </summary>
public interface ITokenUsageService
{
    Task TrackUsageAsync(string conversationId, int tokensUsed, string model, double? cost = null);
    Task<TokenUsageStats> GetUsageStatsAsync(string? conversationId = null, DateTime? since = null);
    Task<List<ConversationUsage>> GetTopConversationsAsync(int limit = 10);
    Task ResetStatsAsync();
}

/// <summary>
/// Tracks and analyzes token usage and costs
/// </summary>
public class TokenUsageService : ITokenUsageService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenUsageService> _logger;
    private const string UsageKey = "token_usage_data";

    // Pricing per 1M tokens (as of Jan 2025)
    private static readonly Dictionary<string, (double Input, double Output)> ModelPricing = new()
    {
        ["gpt-4"] = (30.0, 60.0),
        ["gpt-4-turbo"] = (10.0, 30.0),
        ["gpt-4o"] = (2.5, 10.0),
        ["gpt-4o-mini"] = (0.15, 0.6),
        ["gpt-3.5-turbo"] = (0.5, 1.5),
    };

    public TokenUsageService(IMemoryCache cache, ILogger<TokenUsageService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task TrackUsageAsync(string conversationId, int tokensUsed, string model, double? cost = null)
    {
        var usageData = GetOrCreateUsageData();

        var usage = new UsageEntry
        {
            ConversationId = conversationId,
            TokensUsed = tokensUsed,
            Model = model,
            Timestamp = DateTimeOffset.UtcNow,
            EstimatedCost = cost ?? EstimateCost(tokensUsed, model)
        };

        usageData.Entries.Add(usage);

        // Update cache with 30-day retention
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromDays(30))
            .SetSize(1); // Each usage data entry counts as 1 unit

        _cache.Set(UsageKey, usageData, cacheOptions);

        _logger.LogDebug(
            "Tracked {Tokens} tokens for conversation {ConversationId} using {Model}",
            tokensUsed,
            conversationId,
            model);

        return Task.CompletedTask;
    }

    public Task<TokenUsageStats> GetUsageStatsAsync(string? conversationId = null, DateTime? since = null)
    {
        var usageData = GetOrCreateUsageData();
        var entries = usageData.Entries.AsEnumerable();

        // Filter by conversation if specified
        if (!string.IsNullOrEmpty(conversationId))
        {
            entries = entries.Where(e => e.ConversationId == conversationId);
        }

        // Filter by date if specified
        if (since.HasValue)
        {
            entries = entries.Where(e => e.Timestamp >= since.Value);
        }

        var entryList = entries.ToList();

        var stats = new TokenUsageStats
        {
            TotalTokens = entryList.Sum(e => e.TokensUsed),
            TotalCost = entryList.Sum(e => e.EstimatedCost),
            TotalRequests = entryList.Count,
            AverageTokensPerRequest = entryList.Any() ? (int)entryList.Average(e => e.TokensUsed) : 0,
            ConversationCount = entryList.Select(e => e.ConversationId).Distinct().Count(),
            ModelBreakdown = entryList
                .GroupBy(e => e.Model)
                .ToDictionary(
                    g => g.Key,
                    g => new ModelStats
                    {
                        Tokens = g.Sum(e => e.TokensUsed),
                        Cost = g.Sum(e => e.EstimatedCost),
                        Requests = g.Count()
                    }),
            Since = since ?? entryList.MinBy(e => e.Timestamp)?.Timestamp.DateTime,
            Until = DateTimeOffset.UtcNow.DateTime
        };

        return Task.FromResult(stats);
    }

    public Task<List<ConversationUsage>> GetTopConversationsAsync(int limit = 10)
    {
        var usageData = GetOrCreateUsageData();

        var topConversations = usageData.Entries
            .GroupBy(e => e.ConversationId)
            .Select(g => new ConversationUsage
            {
                ConversationId = g.Key,
                TotalTokens = g.Sum(e => e.TokensUsed),
                TotalCost = g.Sum(e => e.EstimatedCost),
                RequestCount = g.Count(),
                FirstRequest = g.Min(e => e.Timestamp).DateTime,
                LastRequest = g.Max(e => e.Timestamp).DateTime
            })
            .OrderByDescending(c => c.TotalTokens)
            .Take(limit)
            .ToList();

        return Task.FromResult(topConversations);
    }

    public Task ResetStatsAsync()
    {
        _cache.Remove(UsageKey);
        _logger.LogInformation("Token usage stats reset");
        return Task.CompletedTask;
    }

    private UsageData GetOrCreateUsageData()
    {
        if (_cache.TryGetValue<UsageData>(UsageKey, out var data))
        {
            return data;
        }

        var newData = new UsageData();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromDays(30))
            .SetSize(1); // Each usage data entry counts as 1 unit

        _cache.Set(UsageKey, newData, cacheOptions);
        return newData;
    }

    private double EstimateCost(int tokens, string model)
    {
        if (!ModelPricing.TryGetValue(model.ToLower(), out var pricing))
        {
            // Default to GPT-4 pricing if unknown
            pricing = ModelPricing["gpt-4"];
        }

        // Rough estimate: assume 50/50 input/output split
        var inputCost = (tokens * 0.5 / 1_000_000) * pricing.Input;
        var outputCost = (tokens * 0.5 / 1_000_000) * pricing.Output;

        return inputCost + outputCost;
    }

    // Internal data models
    private class UsageData
    {
        public List<UsageEntry> Entries { get; set; } = new();
    }

    private class UsageEntry
    {
        public required string ConversationId { get; init; }
        public int TokensUsed { get; init; }
        public required string Model { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public double EstimatedCost { get; init; }
    }
}

// Response models
public class TokenUsageStats
{
    public int TotalTokens { get; init; }
    public double TotalCost { get; init; }
    public int TotalRequests { get; init; }
    public int AverageTokensPerRequest { get; init; }
    public int ConversationCount { get; init; }
    public Dictionary<string, ModelStats> ModelBreakdown { get; init; } = new();
    public DateTime? Since { get; init; }
    public DateTime Until { get; init; }
}

public class ModelStats
{
    public int Tokens { get; init; }
    public double Cost { get; init; }
    public int Requests { get; init; }
}

public class ConversationUsage
{
    public required string ConversationId { get; init; }
    public int TotalTokens { get; init; }
    public double TotalCost { get; init; }
    public int RequestCount { get; init; }
    public DateTime FirstRequest { get; init; }
    public DateTime LastRequest { get; init; }
}
