using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SkillBot.Api.Models.Requests;
using SkillBot.Api.Models.Responses;
using SkillBot.Api.Services;
using SkillBot.Core.Interfaces;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SkillBot.Api.Controllers;

/// <summary>
/// Chat endpoints for single-agent interactions with improved token tracking
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IAgentEngine _engine;
    private readonly IConversationService _conversationService;
    private readonly ITokenUsageService _usageService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ChatController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IInputValidator _inputValidator;
    private readonly IContentSafetyService _contentSafetyService;
    private readonly IRateLimiter _rateLimiter;

    public ChatController(
        IAgentEngine engine,
        IConversationService conversationService,
        ITokenUsageService usageService,
        ICacheService cacheService,
        ILogger<ChatController> logger,
        IConfiguration configuration,
        IInputValidator inputValidator,
        IContentSafetyService contentSafetyService,
        IRateLimiter rateLimiter)
    {
        _engine = engine;
        _conversationService = conversationService;
        _usageService = usageService;
        _cacheService = cacheService;
        _logger = logger;
        _configuration = configuration;
        _inputValidator = inputValidator;
        _contentSafetyService = contentSafetyService;
        _rateLimiter = rateLimiter;
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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

            // Check rate limit
            var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(userId, "chat");
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded. Retry after: {RetryAfter}", rateLimitResult.RetryAfter);
                return StatusCode(429, new ErrorResponse
                {
                    Error = "RateLimitExceeded",
                    Message = $"Rate limit exceeded. Retry after {rateLimitResult.RetryAfter.TotalSeconds:F0} seconds"
                });
            }

            // Validate input
            var validationResult = await _inputValidator.ValidateInputAsync(request.Message);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Input validation failed: {Error}", validationResult.ErrorMessage);
                return BadRequest(new ErrorResponse
                {
                    Error = "ValidationFailed",
                    Message = validationResult.ErrorMessage
                });
            }

            // Check content safety
            var safetyResult = await _contentSafetyService.CheckContentAsync(request.Message);
            if (!safetyResult.IsSafe)
            {
                _logger.LogWarning("Content safety check failed: {Category} - {Reason}",
                    safetyResult.Category, safetyResult.Reason);
                return BadRequest(new ErrorResponse
                {
                    Error = "UnsafeContent",
                    Message = safetyResult.Reason ?? "Content failed safety check"
                });
            }

            _logger.LogInformation("Request from user: {UserId}", userId);

            var startTime = DateTime.UtcNow;

            // Get or create conversation
            var conversationId = request.ConversationId
                ?? await _conversationService.CreateConversationAsync();

            _logger.LogInformation(
                "Processing chat request for conversation {ConversationId}",
                conversationId);

            // Generate cache key from message hash
            var messageHash = GenerateSHA256Hash(request.Message);
            var cacheKey = $"llm_response_{messageHash}";

            // Check cache first
            var cachedResponse = await _cacheService.GetAsync<string>(cacheKey);

            string agentContent;
            int tokensUsed = 0;
            List<ToolCallInfo> toolCalls = new();

            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for message hash: {Hash}", messageHash);
                agentContent = cachedResponse;

                // For cached responses, we don't have token info or tool calls
                tokensUsed = EstimateTokens(request.Message, agentContent);
            }
            else
            {
                _logger.LogInformation("Cache miss, calling LLM for message hash: {Hash}", messageHash);

                // Execute agent
                var agentResponse = await _engine.ExecuteAsync(
                    request.Message,
                    cancellationToken);

                agentContent = agentResponse.Content;
                tokensUsed = agentResponse.TokensUsed;
                toolCalls = agentResponse.ToolCalls.Select(tc => new ToolCallInfo
                {
                    PluginName = tc.PluginName,
                    FunctionName = tc.FunctionName,
                    Arguments = tc.Arguments,
                    ExecutionTimeMs = tc.ExecutionTime.TotalMilliseconds
                }).ToList();

                // Cache the response for 1 hour
                await _cacheService.SetAsync(cacheKey, agentContent, TimeSpan.FromHours(1), "llm_response", cancellationToken);
            }

            // Save to conversation
            await _conversationService.SaveMessageAsync(
                conversationId,
                "user",
                request.Message);

            await _conversationService.SaveMessageAsync(
                conversationId,
                "assistant",
                agentContent);

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // If no tokens reported, estimate based on text length
            if (tokensUsed <= 0)
            {
                tokensUsed = EstimateTokens(request.Message, agentContent);
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
                Message = agentContent,
                ConversationId = conversationId,
                ExecutionTimeMs = executionTime,
                TokensUsed = tokensUsed > 0 ? tokensUsed : null,
                ToolCalls = toolCalls
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

    /// <summary>
    /// Generate SHA256 hash of a string
    /// </summary>
    private static string GenerateSHA256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}