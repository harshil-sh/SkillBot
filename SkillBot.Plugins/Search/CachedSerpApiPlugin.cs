using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SkillBot.Core.Interfaces;
using SkillBot.Infrastructure.Configuration;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.Search;

/// <summary>
/// Cached decorator for SerpApiPlugin that caches search results.
/// </summary>
[Plugin(Name = "WebSearch", Description = "Search the web for current information using Google via SerpAPI (with caching)")]
public class CachedSerpApiPlugin
{
    private readonly SerpApiPlugin _innerPlugin;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyBuilder _keyBuilder;
    private readonly CachingOptions _options;
    private readonly ILogger<CachedSerpApiPlugin> _logger;

    public CachedSerpApiPlugin(
        SerpApiPlugin innerPlugin,
        ICacheService cacheService,
        ICacheKeyBuilder keyBuilder,
        CachingOptions options,
        ILogger<CachedSerpApiPlugin> logger)
    {
        _innerPlugin = innerPlugin;
        _cacheService = cacheService;
        _keyBuilder = keyBuilder;
        _options = options;
        _logger = logger;
    }

    [KernelFunction("search_web")]
    [Description("REQUIRED for current information: Search Google for recent news, latest developments, current events, or any time-sensitive information. Use this whenever the user asks about 'latest', 'recent', 'current', 'news', or requests up-to-date information.")]
    public async Task<string> SearchWebAsync(
        [Description("Search query - be specific and clear")] string query,
        [Description("Number of results to return (1-10, default 5)")] int count = 5)
    {
        var cacheKey = _keyBuilder.BuildSearchKey("web", query, count);

        // Try to get from cache
        var cached = await _cacheService.GetAsync<CachedSearchResult>(cacheKey);
        if (cached != null)
        {
            _logger.LogInformation("Web search cache hit for query: {Query}", query);
            return cached.Result;
        }

        // Cache miss - call inner plugin
        _logger.LogDebug("Web search cache miss for query: {Query}", query);
        var result = await _innerPlugin.SearchWebAsync(query, count);

        // Cache the result
        var cachedResult = new CachedSearchResult
        {
            Query = query,
            Result = result,
            CachedAt = DateTime.UtcNow
        };

        await _cacheService.SetAsync(cacheKey, cachedResult, _options.WebSearchTtl, "search:web");
        _logger.LogDebug("Cached web search result with TTL: {TTL}", _options.WebSearchTtl);

        return result;
    }

    [KernelFunction("search_news")]
    [Description("Search for recent news articles and breaking stories. Use this specifically for news, current events, or recent developments.")]
    public async Task<string> SearchNewsAsync(
        [Description("News search query")] string query,
        [Description("Number of results (1-10, default 5)")] int count = 5)
    {
        var cacheKey = _keyBuilder.BuildSearchKey("news", query, count);

        // Try to get from cache
        var cached = await _cacheService.GetAsync<CachedSearchResult>(cacheKey);
        if (cached != null)
        {
            _logger.LogInformation("News search cache hit for query: {Query}", query);
            return cached.Result;
        }

        // Cache miss - call inner plugin
        _logger.LogDebug("News search cache miss for query: {Query}", query);
        var result = await _innerPlugin.SearchNewsAsync(query, count);

        // Cache the result
        var cachedResult = new CachedSearchResult
        {
            Query = query,
            Result = result,
            CachedAt = DateTime.UtcNow
        };

        await _cacheService.SetAsync(cacheKey, cachedResult, _options.NewsSearchTtl, "search:news");
        _logger.LogDebug("Cached news search result with TTL: {TTL}", _options.NewsSearchTtl);

        return result;
    }

    [KernelFunction("search_images")]
    [Description("Search for images. Returns image URLs and descriptions.")]
    public async Task<string> SearchImagesAsync(
        [Description("Image search query")] string query,
        [Description("Number of results (1-10, default 5)")] int count = 5)
    {
        var cacheKey = _keyBuilder.BuildSearchKey("images", query, count);

        // Try to get from cache
        var cached = await _cacheService.GetAsync<CachedSearchResult>(cacheKey);
        if (cached != null)
        {
            _logger.LogInformation("Image search cache hit for query: {Query}", query);
            return cached.Result;
        }

        // Cache miss - call inner plugin
        _logger.LogDebug("Image search cache miss for query: {Query}", query);
        var result = await _innerPlugin.SearchImagesAsync(query, count);

        // Cache the result
        var cachedResult = new CachedSearchResult
        {
            Query = query,
            Result = result,
            CachedAt = DateTime.UtcNow
        };

        await _cacheService.SetAsync(cacheKey, cachedResult, _options.ImageSearchTtl, "search:images");
        _logger.LogDebug("Cached image search result with TTL: {TTL}", _options.ImageSearchTtl);

        return result;
    }

    private class CachedSearchResult
    {
        public string Query { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public DateTime CachedAt { get; set; }
    }
}
