using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SkillBot.Core.Interfaces;
using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.MultiAgent;

/// <summary>
/// Uses an LLM to intelligently route tasks to the most appropriate agent(s).
/// </summary>
public class LlmTaskRouter : ITaskRouter
{
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<LlmTaskRouter> _logger;

    public LlmTaskRouter(
        IChatCompletionService chatService,
        ILogger<LlmTaskRouter> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TaskRoutingDecision> RouteTaskAsync(
        string userRequest,
        IReadOnlyList<ISpecializedAgent> availableAgents,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Routing task: {Request}", userRequest);

        if (availableAgents.Count == 0)
        {
            return new TaskRoutingDecision
            {
                Strategy = "single",
                SelectedAgentIds = new List<string>(),
                Reasoning = "No agents available"
            };
        }

        // Build agent descriptions for LLM
        var agentDescriptions = availableAgents.Select(a =>
            $"- {a.AgentId}: {a.Name} - {a.Description} (Specializations: {string.Join(", ", a.Specializations)})"
        );

        var routingPrompt = $$"""
            You are a task routing system. Analyze the user request and decide which agent(s) should handle it.

            Available agents:
            {{string.Join("\n", agentDescriptions)}}

            User request: {{userRequest}}

            Decide:
            1. Which agent(s) should handle this task?
            2. What strategy should be used? (single, parallel, or sequential)
               - single: One agent handles the entire task
               - parallel: Multiple agents work simultaneously on different aspects
               - sequential: Multiple agents work in order, each building on the previous

            Respond ONLY with valid JSON in this exact format:
            {
              "strategy": "single|parallel|sequential",
              "agentIds": ["agent1", "agent2"],
              "reasoning": "Brief explanation"
            }
            """;

        try
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a task routing assistant. Always respond with valid JSON only.");
            chatHistory.AddUserMessage(routingPrompt);

            var response = await _chatService.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            var responseText = response.Content ?? "{}";
            
            // Extract JSON from response (in case LLM added extra text)
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                responseText = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            var routingData = JsonSerializer.Deserialize<RoutingResponse>(responseText);

            if (routingData == null || routingData.AgentIds == null || routingData.AgentIds.Count == 0)
            {
                // Fallback: use first available agent
                return new TaskRoutingDecision
                {
                    Strategy = "single",
                    SelectedAgentIds = new List<string> { availableAgents[0].AgentId },
                    Reasoning = "LLM routing failed, using fallback"
                };
            }

            _logger.LogInformation(
                "Routed to {Count} agent(s) using {Strategy} strategy: {Reasoning}",
                routingData.AgentIds.Count,
                routingData.Strategy,
                routingData.Reasoning);

            return new TaskRoutingDecision
            {
                Strategy = routingData.Strategy ?? "single",
                SelectedAgentIds = routingData.AgentIds,
                Reasoning = routingData.Reasoning
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing task with LLM, using fallback");

            // Fallback to first available agent
            return new TaskRoutingDecision
            {
                Strategy = "single",
                SelectedAgentIds = new List<string> { availableAgents[0].AgentId },
                Reasoning = $"Error during routing: {ex.Message}"
            };
        }
    }

    private class RoutingResponse
    {
        public string? Strategy { get; set; }
        public List<string>? AgentIds { get; set; }
        public string? Reasoning { get; set; }
    }
}
