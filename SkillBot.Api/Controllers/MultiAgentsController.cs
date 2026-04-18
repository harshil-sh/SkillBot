using Microsoft.AspNetCore.Mvc;
using SkillBot.Api.Models.Requests;
using SkillBot.Api.Models.Responses;
using SkillBot.Api.Services;
using SkillBot.Core.Interfaces;

namespace SkillBot.Api.Controllers;

/// <summary>
/// Multi-agent orchestration endpoints
/// </summary>
[ApiController]
[Route("api/multi-agent")]
[Produces("application/json")]
public class MultiAgentController : ControllerBase
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IConversationService _conversationService;
    private readonly ILogger<MultiAgentController> _logger;

    public MultiAgentController(
        IAgentOrchestrator orchestrator,
        IConversationService conversationService,
        ILogger<MultiAgentController> logger)
    {
        _orchestrator = orchestrator;
        _conversationService = conversationService;
        _logger = logger;
    }

    /// <summary>
    /// Execute a task using multi-agent coordination
    /// </summary>
    /// <param name="request">Multi-agent request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Coordinated response from multiple agents</returns>
    /// <response code="200">Successfully processed task</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(MultiAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MultiAgentResponse>> ExecuteTask(
        [FromBody] MultiAgentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Task))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "InvalidRequest",
                Message = "Task cannot be empty"
            });
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Get or create conversation
            var conversationId = request.ConversationId 
                ?? await _conversationService.CreateConversationAsync();
            
            _logger.LogInformation(
                "Processing multi-agent request for conversation {ConversationId}",
                conversationId);

            // Execute orchestrated task
            var orchestratedResponse = await _orchestrator.ExecuteTaskAsync(
                request.Task,
                cancellationToken);

            // Save to conversation
            await _conversationService.SaveMessageAsync(
                conversationId,
                "user",
                request.Task);
            
            await _conversationService.SaveMessageAsync(
                conversationId,
                "assistant",
                orchestratedResponse.FinalResponse);

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var response = new MultiAgentResponse
            {
                FinalResponse = orchestratedResponse.FinalResponse,
                ConversationId = conversationId,
                Strategy = GetStrategyFromMetadata(orchestratedResponse),
                TotalExecutionTimeMs = executionTime,
                AgentsUsed = orchestratedResponse.AgentResults.Select(ar => new AgentExecutionInfo
                {
                    AgentId = ar.AgentId,
                    AgentName = ar.AgentId, // Could be improved with agent name lookup
                    Result = ar.Result,
                    ExecutionTimeMs = ar.ExecutionTime.TotalMilliseconds,
                    Success = ar.IsSuccess
                }).ToList()
            };

            _logger.LogInformation(
                "Multi-agent task completed in {ExecutionTime}ms using {AgentCount} agents",
                executionTime,
                response.AgentsUsed.Count);

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Multi-agent request cancelled");
            return StatusCode(499, new ErrorResponse
            {
                Error = "RequestCancelled",
                Message = "Request was cancelled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing multi-agent request");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "An error occurred while processing your request",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get list of available specialized agents
    /// </summary>
    /// <returns>List of available agents</returns>
    [HttpGet("agents")]
    [ProducesResponseType(typeof(List<AgentStatusResponse>), StatusCodes.Status200OK)]
    public ActionResult<List<AgentStatusResponse>> GetAgents()
    {
        try
        {
            var agents = _orchestrator.GetAgents();
            
            var response = agents.Select(agent => new AgentStatusResponse
            {
                AgentId = agent.AgentId,
                Name = agent.Name,
                Description = agent.Description,
                Specializations = agent.Specializations.ToList(),
                Status = agent.GetStatus().ToString()
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agents");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to retrieve agents"
            });
        }
    }

    private string GetStrategyFromMetadata(SkillBot.Core.Models.OrchestratedResponse response)
    {
        if (response.Metadata?.TryGetValue("strategy", out var strategy) == true)
        {
            return strategy?.ToString() ?? "unknown";
        }
        
        // Infer strategy from agent count
        return response.AgentResults.Count switch
        {
            1 => "single",
            _ => "sequential" // Default assumption
        };
    }
}
