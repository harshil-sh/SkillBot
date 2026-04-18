using Microsoft.AspNetCore.Mvc;
using SkillBot.Api.Services;

namespace SkillBot.Api.Controllers;

/// <summary>
/// Background task scheduling and management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly IBackgroundTaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        IBackgroundTaskService taskService,
        ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    /// <summary>
    /// Schedule a one-time task to run at a specific time
    /// </summary>
    /// <param name="request">Task scheduling request</param>
    /// <returns>Task ID</returns>
    /// <response code="200">Task scheduled successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("schedule")]
    [ProducesResponseType(typeof(ScheduleTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<ScheduleTaskResponse> ScheduleTask([FromBody] ScheduleTaskRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Task))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "InvalidRequest",
                    Message = "Task cannot be empty"
                });
            }

            if (request.ExecuteAt <= DateTime.UtcNow)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "InvalidRequest",
                    Message = "Execute time must be in the future"
                });
            }

            var taskId = _taskService.ScheduleAgentTask(
                request.Task,
                request.ExecuteAt,
                request.IsMultiAgent);

            return Ok(new ScheduleTaskResponse
            {
                TaskId = taskId,
                Message = $"Task scheduled to execute at {request.ExecuteAt:yyyy-MM-dd HH:mm:ss} UTC"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling task");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to schedule task",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Schedule a recurring task using cron expression
    /// </summary>
    /// <param name="request">Recurring task request</param>
    /// <returns>Task ID</returns>
    /// <response code="200">Recurring task scheduled successfully</response>
    /// <response code="400">Invalid cron expression</response>
    [HttpPost("recurring")]
    [ProducesResponseType(typeof(ScheduleTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<ScheduleTaskResponse> ScheduleRecurringTask([FromBody] RecurringTaskRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Task))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "InvalidRequest",
                    Message = "Task cannot be empty"
                });
            }

            if (string.IsNullOrWhiteSpace(request.CronExpression))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "InvalidRequest",
                    Message = "Cron expression cannot be empty"
                });
            }

            var taskId = _taskService.ScheduleRecurringTask(
                request.Task,
                request.CronExpression,
                request.IsMultiAgent);

            return Ok(new ScheduleTaskResponse
            {
                TaskId = taskId,
                Message = $"Recurring task scheduled with cron expression: {request.CronExpression}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling recurring task");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalError",
                Message = "Failed to schedule recurring task",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get information about a scheduled task
    /// </summary>
    /// <param name="taskId">Task ID</param>
    /// <returns>Task information</returns>
    [HttpGet("{taskId}")]
    [ProducesResponseType(typeof(BackgroundTaskInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<BackgroundTaskInfo> GetTask(string taskId)
    {
        var taskInfo = _taskService.GetTaskInfo(taskId);
        
        if (taskInfo == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "NotFound",
                Message = $"Task {taskId} not found"
            });
        }

        return Ok(taskInfo);
    }

    /// <summary>
    /// Get all scheduled tasks
    /// </summary>
    /// <returns>List of all tasks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<BackgroundTaskInfo>), StatusCodes.Status200OK)]
    public ActionResult<List<BackgroundTaskInfo>> GetAllTasks()
    {
        var tasks = _taskService.GetAllTasks();
        return Ok(tasks);
    }

    /// <summary>
    /// Cancel a scheduled task
    /// </summary>
    /// <param name="taskId">Task ID to cancel</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{taskId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult CancelTask(string taskId)
    {
        var cancelled = _taskService.CancelTask(taskId);
        
        if (!cancelled)
        {
            return NotFound(new ErrorResponse
            {
                Error = "NotFound",
                Message = $"Task {taskId} not found"
            });
        }

        _logger.LogInformation("Cancelled task {TaskId}", taskId);
        return NoContent();
    }
}

// Request/Response models
public class ScheduleTaskRequest
{
    /// <summary>
    /// Task to execute
    /// </summary>
    /// <example>Research AI trends and summarize</example>
    public required string Task { get; init; }
    
    /// <summary>
    /// When to execute the task (UTC)
    /// </summary>
    /// <example>2026-04-17T10:00:00Z</example>
    public required DateTime ExecuteAt { get; init; }
    
    /// <summary>
    /// Use multi-agent orchestration
    /// </summary>
    public bool IsMultiAgent { get; init; }
}

public class RecurringTaskRequest
{
    /// <summary>
    /// Task to execute
    /// </summary>
    /// <example>Generate daily report</example>
    public required string Task { get; init; }
    
    /// <summary>
    /// Cron expression for scheduling
    /// Examples:
    /// - "0 9 * * *" - Daily at 9 AM
    /// - "0 */6 * * *" - Every 6 hours
    /// - "0 0 * * 0" - Weekly on Sunday at midnight
    /// </summary>
    /// <example>0 9 * * *</example>
    public required string CronExpression { get; init; }
    
    /// <summary>
    /// Use multi-agent orchestration
    /// </summary>
    public bool IsMultiAgent { get; init; }
}

public class ScheduleTaskResponse
{
    public required string TaskId { get; init; }
    public required string Message { get; init; }
}

public class ErrorResponse
{
    public required string Error { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
}
