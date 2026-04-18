using Microsoft.AspNetCore.Mvc;
using SkillBot.Api.Services;

namespace SkillBot.Api.Controllers;

/// <summary>
/// Token usage and cost tracking endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsageController : ControllerBase
{
    private readonly ITokenUsageService _usageService;
    private readonly ILogger<UsageController> _logger;

    public UsageController(
        ITokenUsageService usageService,
        ILogger<UsageController> logger)
    {
        _usageService = usageService;
        _logger = logger;
    }

    /// <summary>
    /// Get overall usage statistics
    /// </summary>
    /// <param name="since">Optional: Only include usage since this date (ISO 8601)</param>
    /// <returns>Usage statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(TokenUsageStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenUsageStats>> GetStats([FromQuery] DateTime? since = null)
    {
        try
        {
            var stats = await _usageService.GetUsageStatsAsync(since: since);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage stats");
            return StatusCode(500, new { error = "Failed to retrieve usage stats" });
        }
    }

    /// <summary>
    /// Get usage statistics for a specific conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>Usage statistics for the conversation</returns>
    [HttpGet("stats/{conversationId}")]
    [ProducesResponseType(typeof(TokenUsageStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenUsageStats>> GetConversationStats(string conversationId)
    {
        try
        {
            var stats = await _usageService.GetUsageStatsAsync(conversationId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation stats for {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to retrieve conversation stats" });
        }
    }

    /// <summary>
    /// Get top conversations by token usage
    /// </summary>
    /// <param name="limit">Number of top conversations to return (default 10)</param>
    /// <returns>List of top conversations</returns>
    [HttpGet("top-conversations")]
    [ProducesResponseType(typeof(List<ConversationUsage>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConversationUsage>>> GetTopConversations([FromQuery] int limit = 10)
    {
        try
        {
            var topConversations = await _usageService.GetTopConversationsAsync(Math.Min(limit, 100));
            return Ok(topConversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top conversations");
            return StatusCode(500, new { error = "Failed to retrieve top conversations" });
        }
    }

    /// <summary>
    /// Reset all usage statistics
    /// </summary>
    /// <returns>No content on success</returns>
    [HttpDelete("stats")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetStats()
    {
        try
        {
            await _usageService.ResetStatsAsync();
            _logger.LogInformation("Usage stats reset");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting usage stats");
            return StatusCode(500, new { error = "Failed to reset stats" });
        }
    }
}
