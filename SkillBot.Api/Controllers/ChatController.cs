using Microsoft.AspNetCore.Mvc;
using SkillBot.Api.Models.Requests;
using SkillBot.Api.Models.Responses;
using SkillBot.Api.Services;
using SkillBot.Core.Interfaces;

namespace SkillBot.Api.Controllers;

/// <summary>
/// Chat endpoints for single-agent interactions with improved token tracking
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IAgentEngine _engine;
    private readonly IConversationService _conversationService;
    private readonly ITokenUsageService _usageService;
    private readonly ILogger<ChatController> _logger;
    private readonly IConfiguration _configuration;

    public ChatController(
        IAgentEngine engine,
        IConversationService conversationService,
        ITokenUsageService usageService,
        ILogger<ChatController> logger,
        IConfiguration configuration)
    {
        _engine = engine;
        _conversationService = conversationService;
        _usageService = usageService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Send a message to the AI agent
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "InvalidRequest",
                Message = "Message cannot be empty"
            });
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Get or create conversation
            var conversationId = request.ConversationId 
                ?? await _conversationService.CreateConversationAsync();
            
            _logger.LogInformation(
                "Processing chat request for conversation {ConversationId}", 
                conversationId);

            // Execute agent
            var agentResponse = await _engine.ExecuteAsync(
                request.Message, 
                cancellationToken);

            // Save to conversation
            await _conversationService.SaveMessageAsync(
                conversationId, 
                "user", 
                request.Message);
            
            await _conversationService.SaveMessageAsync(
                conversationId, 
                "assistant", 
                agentResponse.Content);

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Track token usage (with estimation fallback)
            var tokensUsed = agentResponse.TokensUsed;
            
            // If no tokens reported, estimate based on text length
            if (tokensUsed <= 0)
            {
                tokensUsed = EstimateTokens(request.Message, agentResponse.Content);
                _logger.LogWarning(
                    "No token count from API, estimated {Tokens} tokens", 
                    tokensUsed);
            }

            var model = _configuration["SkillBot:Model"] ?? "gpt-4";
            await _usageService.TrackUsageAsync(
                conversationId,
                tokensUsed,
                model);

            var response = new ChatResponse
            {
                Message = agentResponse.Content,
                ConversationId = conversationId,
                ExecutionTimeMs = executionTime,
                TokensUsed = tokensUsed > 0 ? tokensUsed : null,
                ToolCalls = agentResponse.ToolCalls.Select(tc => new ToolCallInfo
                {
                    PluginName = tc.PluginName,
                    FunctionName = tc.FunctionName,
                    Arguments = tc.Arguments,
                    ExecutionTimeMs = tc.ExecutionTime.TotalMilliseconds
                }).ToList()
            };

            _logger.LogInformation(
                "Chat completed in {ExecutionTime}ms with {ToolCount} tool calls, {Tokens} tokens",
                executionTime,
                response.ToolCalls.Count,
                tokensUsed);

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Chat request cancelled");
            return StatusCode(499, new ErrorResponse
            {
                Error = "RequestCancelled",
                Message = "Request was cancelled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "An error occurred while processing your request",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get conversation history
    /// </summary>
    [HttpGet("{conversationId}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConversationResponse>> GetConversation(string conversationId)
    {
        try
        {
            var conversation = await _conversationService.GetConversationAsync(conversationId);
            
            if (conversation == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "NotFound",
                    Message = $"Conversation {conversationId} not found"
                });
            }

            return Ok(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation {ConversationId}", conversationId);
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to retrieve conversation"
            });
        }
    }

    /// <summary>
    /// Delete a conversation
    /// </summary>
    [HttpDelete("{conversationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteConversation(string conversationId)
    {
        try
        {
            var deleted = await _conversationService.DeleteConversationAsync(conversationId);
            
            if (!deleted)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "NotFound",
                    Message = $"Conversation {conversationId} not found"
                });
            }

            _logger.LogInformation("Deleted conversation {ConversationId}", conversationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to delete conversation"
            });
        }
    }

    /// <summary>
    /// Estimate tokens based on text length
    /// Rough approximation: 1 token ≈ 4 characters
    /// </summary>
    private static int EstimateTokens(string input, string output)
    {
        var totalChars = input.Length + output.Length;
        return (int)Math.Ceiling(totalChars / 4.0);
    }
}