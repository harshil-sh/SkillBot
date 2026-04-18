using Hangfire;
using SkillBot.Core.Interfaces;

namespace SkillBot.Api.Services;

/// <summary>
/// Interface for background task scheduling
/// </summary>
public interface IBackgroundTaskService
{
    string ScheduleAgentTask(string task, DateTime executeAt, bool isMultiAgent = false);
    string ScheduleRecurringTask(string task, string cronExpression, bool isMultiAgent = false);
    bool CancelTask(string taskId);
    BackgroundTaskInfo? GetTaskInfo(string taskId);
    List<BackgroundTaskInfo> GetAllTasks();
}

/// <summary>
/// Manages background and scheduled agent tasks using Hangfire
/// </summary>
public class BackgroundTaskService : IBackgroundTaskService
{
    private readonly ILogger<BackgroundTaskService> _logger;
    internal static readonly Dictionary<string, BackgroundTaskInfo> _taskRegistry = new();
    private static readonly object _lock = new();

    public BackgroundTaskService(ILogger<BackgroundTaskService> logger)
    {
        _logger = logger;
    }

    public string ScheduleAgentTask(string task, DateTime executeAt, bool isMultiAgent = false)
    {
        try
        {
            var taskId = $"task_{Guid.NewGuid():N}";
            var delay = executeAt - DateTime.UtcNow;

            if (delay.TotalSeconds < 0)
            {
                throw new ArgumentException("Execute time must be in the future");
            }

            // Schedule the job with Hangfire
            var jobId = BackgroundJob.Schedule<AgentTaskExecutor>(
                executor => executor.ExecuteAsync(task, isMultiAgent, taskId),
                delay);

            // Register task info
            var taskInfo = new BackgroundTaskInfo
            {
                TaskId = taskId,
                HangfireJobId = jobId,
                Task = task,
                IsMultiAgent = isMultiAgent,
                ScheduledFor = executeAt,
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            };

            lock (_lock)
            {
                _taskRegistry[taskId] = taskInfo;
            }

            _logger.LogInformation(
                "Scheduled task {TaskId} to execute at {ExecuteAt}",
                taskId,
                executeAt);

            return taskId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling task");
            throw;
        }
    }

    public string ScheduleRecurringTask(string task, string cronExpression, bool isMultiAgent = false)
    {
        try
        {
            var taskId = $"recurring_{Guid.NewGuid():N}";

            // Schedule recurring job with Hangfire
            RecurringJob.AddOrUpdate(
                taskId,
                () => ExecuteRecurringTaskAsync(task, isMultiAgent, taskId),
                cronExpression);

            // Register task info
            var taskInfo = new BackgroundTaskInfo
            {
                TaskId = taskId,
                HangfireJobId = taskId, // For recurring jobs, ID is the same
                Task = task,
                IsMultiAgent = isMultiAgent,
                IsRecurring = true,
                CronExpression = cronExpression,
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            };

            lock (_lock)
            {
                _taskRegistry[taskId] = taskInfo;
            }

            _logger.LogInformation(
                "Scheduled recurring task {TaskId} with cron '{Cron}'",
                taskId,
                cronExpression);

            return taskId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling recurring task");
            throw;
        }
    }

    public bool CancelTask(string taskId)
    {
        try
        {
            lock (_lock)
            {
                if (!_taskRegistry.TryGetValue(taskId, out var taskInfo))
                {
                    return false;
                }

                if (taskInfo.IsRecurring)
                {
                    RecurringJob.RemoveIfExists(taskId);
                }
                else
                {
                    BackgroundJob.Delete(taskInfo.HangfireJobId);
                }

                taskInfo.Status = "Cancelled";
                _logger.LogInformation("Cancelled task {TaskId}", taskId);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling task {TaskId}", taskId);
            return false;
        }
    }

    public BackgroundTaskInfo? GetTaskInfo(string taskId)
    {
        lock (_lock)
        {
            return _taskRegistry.TryGetValue(taskId, out var info) ? info : null;
        }
    }

    public List<BackgroundTaskInfo> GetAllTasks()
    {
        lock (_lock)
        {
            return _taskRegistry.Values.ToList();
        }
    }

    // Called by Hangfire for recurring tasks
    public static async Task ExecuteRecurringTaskAsync(string task, bool isMultiAgent, string taskId)
    {
        var executor = new AgentTaskExecutor();
        await executor.ExecuteAsync(task, isMultiAgent, taskId);
    }
}

/// <summary>
/// Executes agent tasks in the background
/// </summary>
public class AgentTaskExecutor
{
    public async Task ExecuteAsync(string task, bool isMultiAgent, string taskId)
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<AgentTaskExecutor>();

        try
        {
            logger.LogInformation(
                "Executing background task {TaskId}: {Task}",
                taskId,
                task);

            // Update task status
            UpdateTaskStatus(taskId, "Running");

            // Get services from DI (Hangfire provides this)
            var serviceProvider = GetServiceProvider();
            var result = "";

            if (isMultiAgent)
            {
                var orchestrator = serviceProvider.GetRequiredService<IAgentOrchestrator>();
                var response = await orchestrator.ExecuteTaskAsync(task);
                result = response.FinalResponse;
            }
            else
            {
                var engine = serviceProvider.GetRequiredService<IAgentEngine>();
                var response = await engine.ExecuteAsync(task);
                result = response.Content;
            }

            // Update task with result
            UpdateTaskStatus(taskId, "Completed", result);

            logger.LogInformation(
                "Completed background task {TaskId}",
                taskId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing background task {TaskId}", taskId);
            UpdateTaskStatus(taskId, "Failed", ex.Message);
        }
    }

    private static void UpdateTaskStatus(string taskId, string status, string? result = null)
    {
        if (BackgroundTaskService._taskRegistry.TryGetValue(taskId, out var taskInfo))
        {
            taskInfo.Status = status;
            taskInfo.Result = result;
            
            if (status == "Running" && !taskInfo.StartedAt.HasValue)
            {
                taskInfo.StartedAt = DateTime.UtcNow;
            }
            else if (status is "Completed" or "Failed")
            {
                taskInfo.CompletedAt = DateTime.UtcNow;
            }
        }
    }

    private static IServiceProvider GetServiceProvider()
    {
        // This will be injected by Hangfire's IoC container
        return (IServiceProvider)Hangfire.GlobalConfiguration.Configuration
            .GetType()
            .GetProperty("ServiceProvider")?
            .GetValue(null) ?? throw new InvalidOperationException("Service provider not available");
    }
}

/// <summary>
/// Background task information
/// </summary>
public class BackgroundTaskInfo
{
    public required string TaskId { get; init; }
    public required string HangfireJobId { get; init; }
    public required string Task { get; init; }
    public bool IsMultiAgent { get; init; }
    public bool IsRecurring { get; init; }
    public string? CronExpression { get; init; }
    public DateTime? ScheduledFor { get; init; }
    public required string Status { get; set; } // Scheduled, Running, Completed, Failed, Cancelled
    public string? Result { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
