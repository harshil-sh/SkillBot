using Microsoft.AspNetCore.Mvc;
using SkillBot.Api.Models.Responses;
using SkillBot.Core.Interfaces;

namespace SkillBot.Api.Controllers;

/// <summary>
/// Cache management endpoints for monitoring and controlling cache performance
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
    /// Get cache performance statistics
    /// </summary>
    /// <remarks>
    /// Returns detailed cache statistics including:
    /// - Hit/miss counts for L1 (memory) and L2 (persistent) cache tiers
    /// - Overall hit rate percentage
    /// - Total number of cached entries and their size
    /// - Timestamps of oldest and newest cache entries
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns cache statistics with hit rate, total hits, total misses</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CacheStatisticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CacheStatisticsResponse>> GetStatistics(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving cache statistics");

            var stats = await _cacheManagement.GetStatisticsAsync(cancellationToken);

            var response = new CacheStatisticsResponse
            {
                L1Hits = stats.L1Hits,
                L1Misses = stats.L1Misses,
                L2Hits = stats.L2Hits,
                L2Misses = stats.L2Misses,
                TotalEntries = stats.TotalEntries,
                TotalSizeBytes = stats.TotalSizeBytes,
                HitRatePercentage = stats.HitRate,
                TotalRequests = stats.TotalRequests,
                TotalHits = stats.L1Hits + stats.L2Hits,
                TotalMisses = stats.L1Misses + stats.L2Misses,
                OldestEntry = stats.OldestEntry,
                NewestEntry = stats.NewestEntry
            };

            _logger.LogInformation(
                "Cache statistics: {TotalHits} hits, {TotalMisses} misses, {HitRate:F2}% hit rate",
                response.TotalHits,
                response.TotalMisses,
                response.HitRatePercentage);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache statistics");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to retrieve cache statistics",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get cache health status
    /// </summary>
    /// <remarks>
    /// Returns cache health information including:
    /// - Health status (healthy/unhealthy)
    /// - Current hit rate
    /// - Total size and entry count
    /// - Status message if applicable
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Returns cache health status</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(CacheHealth), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CacheHealth>> GetHealth(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking cache health");

            var health = await _cacheManagement.GetHealthAsync(cancellationToken);

            _logger.LogInformation(
                "Cache health: {IsHealthy}, Hit rate: {HitRate:F2}%, Entries: {TotalEntries}",
                health.IsHealthy,
                health.HitRate,
                health.TotalEntries);

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache health");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to check cache health",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Clear all cached items
    /// </summary>
    /// <remarks>
    /// Removes all entries from both L1 (memory) and L2 (persistent) cache tiers.
    /// This operation cannot be undone.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="204">Cache cleared successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ClearCache(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Cache clear requested via API");

            await _cacheManagement.ClearAllCacheAsync(cancellationToken);

            _logger.LogInformation("Cache cleared successfully");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to clear cache",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Invalidate cache entries by pattern
    /// </summary>
    /// <remarks>
    /// Removes all cache entries matching the specified pattern.
    ///
    /// Pattern examples:
    /// - "llm_response_*" - Clear all LLM response cache entries
    /// - "search:*" - Clear all search-related cache entries
    /// - "plugin:*" - Clear all plugin cache entries
    /// </remarks>
    /// <param name="pattern">Pattern to match cache keys (supports wildcards)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="204">Cache entries invalidated successfully</response>
    /// <response code="400">Invalid pattern</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("invalidate/{pattern}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InvalidateCache(
        string pattern,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "InvalidPattern",
                Message = "Pattern cannot be empty"
            });
        }

        try
        {
            _logger.LogInformation("Cache invalidation requested for pattern: {Pattern}", pattern);

            await _cacheManagement.InvalidateCacheAsync(pattern, cancellationToken);

            _logger.LogInformation("Cache entries invalidated successfully for pattern: {Pattern}", pattern);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache entries for pattern: {Pattern}", pattern);
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to invalidate cache entries",
                Details = ex.Message
            });
        }
    }
}
