using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Interfaces;
using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.MultiAgent;

/// <summary>
/// Coordinates multiple specialized agents to work together on tasks.
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ConcurrentDictionary<string, ISpecializedAgent> _agents;
    private readonly ITaskRouter _taskRouter;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        ITaskRouter taskRouter,
        ILogger<AgentOrchestrator> logger)
    {
        _taskRouter = taskRouter ?? throw new ArgumentNullException(nameof(taskRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agents = new ConcurrentDictionary<string, ISpecializedAgent>();
    }

    public void RegisterAgent(ISpecializedAgent agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        if (_agents.TryAdd(agent.AgentId, agent))
        {
            _logger.LogInformation(
                "Registered agent: {AgentId} ({Name}) - Specializations: {Specializations}",
                agent.AgentId,
                agent.Name,
                string.Join(", ", agent.Specializations));
        }
        else
        {
            _logger.LogWarning("Agent {AgentId} is already registered", agent.AgentId);
        }
    }

    public bool UnregisterAgent(string agentId)
    {
        var removed = _agents.TryRemove(agentId, out _);
        
        if (removed)
        {
            _logger.LogInformation("Unregistered agent: {AgentId}", agentId);
        }
        
        return removed;
    }

    public IReadOnlyList<ISpecializedAgent> GetAgents()
    {
        return _agents.Values.ToList();
    }

    public async Task<OrchestratedResponse> ExecuteTaskAsync(
        string userRequest,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Orchestrator received request: {Request}", userRequest);

        try
        {
            // Route the task to appropriate agent(s)
            var routingDecision = await _taskRouter.RouteTaskAsync(
                userRequest,
                GetAgents(),
                cancellationToken);

            _logger.LogInformation(
                "Routing strategy: {Strategy}, Selected agents: {Agents}",
                routingDecision.Strategy,
                string.Join(", ", routingDecision.SelectedAgentIds));

            // Execute based on routing strategy
            var results = routingDecision.Strategy.ToLowerInvariant() switch
            {
                "single" => await ExecuteSingleAgentAsync(userRequest, routingDecision, cancellationToken),
                "parallel" => await ExecuteParallelAsync(userRequest, routingDecision, cancellationToken),
                "sequential" => await ExecuteSequentialAsync(userRequest, routingDecision, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown routing strategy: {routingDecision.Strategy}")
            };

            // Synthesize final response
            var finalResponse = SynthesizeResponse(results, routingDecision);

            stopwatch.Stop();

            return new OrchestratedResponse
            {
                FinalResponse = finalResponse,
                AgentResults = results,
                TotalExecutionTime = stopwatch.Elapsed,
                IsSuccess = results.All(r => r.IsSuccess),
                Metadata = new Dictionary<string, object>
                {
                    ["RoutingStrategy"] = routingDecision.Strategy,
                    ["AgentsUsed"] = routingDecision.SelectedAgentIds.Count,
                    ["Reasoning"] = routingDecision.Reasoning ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Error orchestrating task");

            return new OrchestratedResponse
            {
                FinalResponse = "I encountered an error while coordinating the agents to handle your request.",
                TotalExecutionTime = stopwatch.Elapsed,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OrchestratedResponse> ExecuteWorkflowAsync(
        AgentWorkflow workflow,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<AgentTaskResult>();
        var context = new Dictionary<string, object>(workflow.InitialContext ?? new());

        _logger.LogInformation(
            "Executing workflow: {WorkflowId} with {StepCount} steps",
            workflow.WorkflowId,
            workflow.Steps.Count);

        try
        {
            // Execute steps in order, respecting dependencies
            var executedSteps = new HashSet<string>();

            foreach (var step in workflow.Steps)
            {
                // Wait for dependencies
                if (step.DependsOn != null)
                {
                    foreach (var dependency in step.DependsOn)
                    {
                        while (!executedSteps.Contains(dependency))
                        {
                            await Task.Delay(100, cancellationToken);
                        }
                    }
                }

                // Find the agent for this step
                var agent = step.RequiredAgentId != null
                    ? _agents.GetValueOrDefault(step.RequiredAgentId)
                    : null;

                if (agent == null)
                {
                    _logger.LogWarning("No agent found for step: {StepId}", step.StepId);
                    continue;
                }

                // Create task with context
                var task = new AgentTask
                {
                    TaskId = step.StepId,
                    Description = step.Description,
                    UserRequest = step.Description,
                    Context = new Dictionary<string, object>(context)
                };

                // Execute step
                var result = await agent.ExecuteAsync(task, cancellationToken);
                results.Add(result);

                // Update context with results
                context[$"step_{step.StepId}_result"] = result.Result;
                executedSteps.Add(step.StepId);

                _logger.LogInformation(
                    "Completed workflow step: {StepId} by agent {AgentId}",
                    step.StepId,
                    agent.AgentId);
            }

            stopwatch.Stop();

            var finalResponse = SynthesizeWorkflowResponse(results, workflow);

            return new OrchestratedResponse
            {
                FinalResponse = finalResponse,
                AgentResults = results,
                TotalExecutionTime = stopwatch.Elapsed,
                IsSuccess = results.All(r => r.IsSuccess),
                Metadata = new Dictionary<string, object>
                {
                    ["WorkflowId"] = workflow.WorkflowId,
                    ["StepsCompleted"] = executedSteps.Count
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Error executing workflow");

            return new OrchestratedResponse
            {
                FinalResponse = "Workflow execution failed.",
                AgentResults = results,
                TotalExecutionTime = stopwatch.Elapsed,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<List<AgentTaskResult>> ExecuteSingleAgentAsync(
        string userRequest,
        TaskRoutingDecision decision,
        CancellationToken cancellationToken)
    {
        var agentId = decision.SelectedAgentIds.FirstOrDefault();
        
        if (agentId == null || !_agents.TryGetValue(agentId, out var agent))
        {
            throw new InvalidOperationException("No agent selected for single execution");
        }

        var task = new AgentTask
        {
            TaskId = Guid.NewGuid().ToString(),
            Description = userRequest,
            UserRequest = userRequest
        };

        var result = await agent.ExecuteAsync(task, cancellationToken);
        
        return new List<AgentTaskResult> { result };
    }

    private async Task<List<AgentTaskResult>> ExecuteParallelAsync(
        string userRequest,
        TaskRoutingDecision decision,
        CancellationToken cancellationToken)
    {
        var tasks = decision.SelectedAgentIds
            .Where(id => _agents.ContainsKey(id))
            .Select(async agentId =>
            {
                var agent = _agents[agentId];
                var task = new AgentTask
                {
                    TaskId = Guid.NewGuid().ToString(),
                    Description = userRequest,
                    UserRequest = userRequest
                };

                return await agent.ExecuteAsync(task, cancellationToken);
            });

        var results = await Task.WhenAll(tasks);
        
        return results.ToList();
    }

    private async Task<List<AgentTaskResult>> ExecuteSequentialAsync(
        string userRequest,
        TaskRoutingDecision decision,
        CancellationToken cancellationToken)
    {
        var results = new List<AgentTaskResult>();
        var context = new Dictionary<string, object>();

        foreach (var agentId in decision.SelectedAgentIds)
        {
            if (!_agents.TryGetValue(agentId, out var agent))
                continue;

            var task = new AgentTask
            {
                TaskId = Guid.NewGuid().ToString(),
                Description = userRequest,
                UserRequest = userRequest,
                Context = new Dictionary<string, object>(context)
            };

            var result = await agent.ExecuteAsync(task, cancellationToken);
            results.Add(result);

            // Pass results to next agent
            context[$"{agentId}_result"] = result.Result;
        }

        return results;
    }

    private string SynthesizeResponse(
        List<AgentTaskResult> results,
        TaskRoutingDecision decision)
    {
        if (results.Count == 0)
            return "No agents were able to process this request.";

        if (results.Count == 1)
            return results[0].Result;

        // Combine results from multiple agents
        var response = new System.Text.StringBuilder();
        response.AppendLine("Here's what I found by coordinating multiple specialists:\n");

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var agent = _agents.GetValueOrDefault(result.AgentId);
            
            if (agent != null)
            {
                response.AppendLine($"**{agent.Name}:**");
                response.AppendLine(result.Result);
                response.AppendLine();
            }
        }

        return response.ToString().TrimEnd();
    }

    private string SynthesizeWorkflowResponse(
        List<AgentTaskResult> results,
        AgentWorkflow workflow)
    {
        var response = new System.Text.StringBuilder();
        response.AppendLine($"Workflow '{workflow.Description}' completed:\n");

        foreach (var result in results)
        {
            var step = workflow.Steps.FirstOrDefault(s => s.StepId == result.TaskId);
            if (step != null)
            {
                response.AppendLine($"✓ {step.Description}");
            }
        }

        response.AppendLine();
        response.AppendLine("Final result:");
        response.AppendLine(results.LastOrDefault()?.Result ?? "Completed");

        return response.ToString();
    }
}