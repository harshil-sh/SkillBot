using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillBot.Api.Models.Responses;
using SkillBot.Api.Models.Settings;
using SkillBot.Api.Services;
using System.Security.Claims;

namespace SkillBot.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Produces("application/json")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IUserSettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IUserSettingsService settingsService, ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSettingsResponse>> GetSettings()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        try
        {
            var settings = await _settingsService.GetSettingsAsync(userId);
            return Ok(settings);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Error = "NotFound", Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settings for user {UserId}.", userId);
            return StatusCode(500);
        }
    }

    [HttpPut("api-key")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApiKey([FromBody] UpdateApiKeyRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return BadRequest(new ErrorResponse { Error = "InvalidRequest", Message = "ApiKey cannot be empty." });

        try
        {
            await _settingsService.UpdateApiKeyAsync(userId, request.Provider, request.ApiKey);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Error = "InvalidProvider", Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Error = "NotFound", Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API key for user {UserId}.", userId);
            return StatusCode(500);
        }
    }

    [HttpPut("provider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProvider([FromBody] UpdateProviderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        try
        {
            await _settingsService.UpdateProviderAsync(userId, request.Provider);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Error = "InvalidProvider", Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Error = "NotFound", Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider for user {UserId}.", userId);
            return StatusCode(500);
        }
    }
}
