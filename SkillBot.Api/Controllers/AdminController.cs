using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillBot.Api.Services;
using SkillBot.Infrastructure.Data;

namespace SkillBot.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly SkillBotDbContext _db;
    private readonly ITokenUsageService _usageService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        SkillBotDbContext db,
        ITokenUsageService usageService,
        ILogger<AdminController> logger)
    {
        _db = db;
        _usageService = usageService;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetStats(CancellationToken ct)
    {
        try
        {
            var totalUsers = await _db.Users.CountAsync(ct);
            var totalConversations = await _db.Conversations
                .Select(c => c.ConversationId)
                .Distinct()
                .CountAsync(ct);
            var usageStats = await _usageService.GetUsageStatsAsync();

            return Ok(new
            {
                totalUsers,
                totalConversations,
                totalTokensUsed = (long)usageStats.TotalTokens
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin stats");
            return StatusCode(500, new ErrorResponse { Error = "InternalError", Message = "Failed to retrieve stats" });
        }
    }

    [HttpGet("users")]
    public async Task<ActionResult> GetUsers(CancellationToken ct)
    {
        try
        {
            var users = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    email = u.Email,
                    createdAt = u.CreatedAt,
                    lastActive = (DateTime?)null,
                    isActive = u.IsActive
                })
                .ToListAsync(ct);

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin users");
            return StatusCode(500, new ErrorResponse { Error = "InternalError", Message = "Failed to retrieve users" });
        }
    }

    [HttpDelete("users/{userId}")]
    public async Task<ActionResult> DeleteUser(string userId, CancellationToken ct)
    {
        try
        {
            var user = await _db.Users.FindAsync(new object[] { userId }, ct);
            if (user is null)
                return NotFound(new ErrorResponse { Error = "NotFound", Message = "User not found" });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return StatusCode(500, new ErrorResponse { Error = "InternalError", Message = "Failed to delete user" });
        }
    }
}
