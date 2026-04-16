using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SkillBot.Core.Interfaces;
using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.MultiAgent.Agents;

/// <summary>
/// Base implementation for specialized agents.
/// </summary>
public abstract class BaseSpecializedAgent : ISpecializedAgent
{
    protected readonly Kernel _kernel;
    protected readonly IChatCompletionService _chatService;
    protected readonly ILogger _logger;
    
    private int _tasksCompleted;
    private int _tasksFailed;
    private string _currentState = "idle";
    private DateTimeOffset _lastActive = DateTimeOffset.UtcNow;

    public abstract string AgentId { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract IReadOnlyList<string> Specializations { get; }

    protected BaseSpecializedAgent(
        Kernel kernel,
        IChatCompletionService chatService,
        ILogger logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual async Task<bool> CanHandleAsync(
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        // Default implementation: check if task description contains specialization keywords
        var taskLower = task.Description.ToLowerInvariant();
        
        return Specializations.Any(spec => 
            taskLower.Contains(spec.ToLowerInvariant()));
    }

    public virtual async Task<AgentTaskResult> ExecuteAsync(
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _currentState = "busy";
        _lastActive = DateTimeOffset.UtcNow;

        try
        {
            _logger.LogInformation(
                "{AgentName} executing task: {TaskId}",
                Name,
                task.TaskId);

            // Build system prompt for this agent
            var systemPrompt = GetSystemPrompt();

            // Create chat history
            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(task.UserRequest);

            // Execute with specialized plugins
            var response = await _chatService.GetChatMessageContentAsync(
                chatHistory,
                kernel: _kernel,
                cancellationToken: cancellationToken);

            stopwatch.Stop();
            _tasksCompleted++;
            _currentState = "idle";
            _lastActive = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "{AgentName} completed task {TaskId} in {ElapsedMs}ms",
                Name,
                task.TaskId,
                stopwatch.ElapsedMilliseconds);

            return new AgentTaskResult
            {
                TaskId = task.TaskId,
                AgentId = AgentId,
                Result = response.Content ?? "",
                IsSuccess = true,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _tasksFailed++;
            _currentState = "error";
            _lastActive = DateTimeOffset.UtcNow;

            _logger.LogError(ex, "{AgentName} failed task {TaskId}", Name, task.TaskId);

            return new AgentTaskResult
            {
                TaskId = task.TaskId,
                AgentId = AgentId,
                Result = "",
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    public AgentStatus GetStatus()
    {
        return new AgentStatus
        {
            AgentId = AgentId,
            State = _currentState,
            TasksCompleted = _tasksCompleted,
            TasksFailed = _tasksFailed,
            LastActive = _lastActive
        };
    }

    protected abstract string GetSystemPrompt();
}
