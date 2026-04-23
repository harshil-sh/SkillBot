using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillBot.Infrastructure.Data;
using System.Security.Claims;

namespace SkillBot.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly SkillBotDbContext _db;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(SkillBotDbContext db, ILogger<ConversationsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetConversations(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        try
        {
            var rows = await _db.Conversations
                .Where(c => c.UserId == userId && c.ConversationId != null)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct);

            var conversations = rows
                .GroupBy(c => c.ConversationId!)
                .Select(g => new
                {
                    id = g.Key,
                    title = Truncate(g.First().Message, 50),
                    lastMessage = Truncate(g.Last().Response, 100),
                    timestamp = g.Max(c => c.CreatedAt)
                })
                .OrderByDescending(c => c.timestamp)
                .ToList();

            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations for user {UserId}", userId);
            return StatusCode(500, new ErrorResponse { Error = "InternalError", Message = "Failed to retrieve conversations" });
        }
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult> GetMessages(string conversationId, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        try
        {
            var rows = await _db.Conversations
                .Where(c => c.UserId == userId && c.ConversationId == conversationId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct);

            var messages = rows.SelectMany(r => new[]
            {
                new { role = "user",      content = r.Message,  timestamp = r.CreatedAt },
                new { role = "assistant", content = r.Response, timestamp = r.CreatedAt }
            }).ToList();

            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for conversation {ConversationId}", conversationId);
            return StatusCode(500, new ErrorResponse { Error = "InternalError", Message = "Failed to retrieve messages" });
        }
    }

    [HttpDelete("{conversationId}")]
    public async Task<ActionResult> DeleteConversation(string conversationId, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        try
        {
            var rows = await _db.Conversations
                .Where(c => c.UserId == userId && c.ConversationId == conversationId)
                .ToListAsync(ct);

            if (rows.Count == 0)
                return NotFound(new ErrorResponse { Error = "NotFound", Message = "Conversation not found" });

            _db.Conversations.RemoveRange(rows);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
            return StatusCode(500, new ErrorResponse { Error = "InternalError", Message = "Failed to delete conversation" });
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
