using Microsoft.AspNetCore.Mvc;
using SkillBot.Core.Interfaces;

namespace SkillBot.Api.Controllers;

/// <summary>
/// API endpoints for cache management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly ICacheManagementService _cacheManagement;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheManagementService cacheManagement,
        ILogger<CacheController> logger)
    {
        _cacheManagement = cacheManagement;
        _logger = logger;
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<CacheStatistics>> GetStatistics()
    {
        var stats = await _cacheManagement.GetStatisticsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Gets cache health status.
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<CacheHealth>> GetHealth()
    {
        var health = await _cacheManagement.GetHealthAsync();
        return Ok(health);
    }

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCache()
    {
        _logger.LogWarning("Cache clear requested via API");
        await _cacheManagement.ClearAllCacheAsync();
        return Ok(new { message = "Cache cleared successfully" });
    }

    /// <summary>
    /// Invalidates cache entries matching the specified pattern.
    /// </summary>
    /// <param name="pattern">Pattern to match (e.g., "llm:*", "search:web:*")</param>
    [HttpDelete("invalidate/{pattern}")]
    public async Task<IActionResult> InvalidateCache(string pattern)
    {
        _logger.LogInformation("Cache invalidation requested for pattern: {Pattern}", pattern);
        await _cacheManagement.InvalidateCacheAsync(pattern);
        return Ok(new { message = $"Cache entries matching '{pattern}' invalidated successfully" });
    }
}
