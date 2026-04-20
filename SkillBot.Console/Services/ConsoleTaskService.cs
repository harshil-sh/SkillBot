using System.Text;
using System.Text.Json.Serialization;

namespace SkillBot.Console.Services;

public class ConsoleTaskService : IConsoleTaskService
{
    private readonly ApiClient _apiClient;

    public ConsoleTaskService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<string> ScheduleTaskAsync(string task, DateTime executeAt, bool isMultiAgent = false)
    {
        var result = await _apiClient.PostAsync<ScheduleResponse>(
            "/api/tasks/schedule",
            new { task, executeAt, isMultiAgent });

        return result is null
            ? "Task scheduled (no details returned)."
            : $"✅ {result.Message}  [TaskId: {result.TaskId}]";
    }

    public async Task<string> ScheduleRecurringTaskAsync(string task, string cronExpression, bool isMultiAgent = false)
    {
        var result = await _apiClient.PostAsync<ScheduleResponse>(
            "/api/tasks/recurring",
            new { task, cronExpression, isMultiAgent });

        return result is null
            ? "Recurring task scheduled (no details returned)."
            : $"✅ {result.Message}  [TaskId: {result.TaskId}]";
    }

    public async Task<string> GetTaskAsync(string taskId)
    {
        var task = await _apiClient.GetAsync<TaskInfo>($"/api/tasks/{taskId}");
        return task is null ? $"Task '{taskId}' not found." : FormatTask(task);
    }

    public async Task<string> GetAllTasksAsync()
    {
        var tasks = await _apiClient.GetAsync<List<TaskInfo>>("/api/tasks");
        if (tasks is null || tasks.Count == 0)
            return "No scheduled tasks found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Scheduled tasks ({tasks.Count}):");
        sb.AppendLine(new string('─', 60));
        foreach (var t in tasks)
            sb.AppendLine(FormatTask(t));
        sb.Append(new string('─', 60));
        return sb.ToString();
    }

    public async Task CancelTaskAsync(string taskId)
    {
        await _apiClient.DeleteAsync($"/api/tasks/{taskId}");
    }

    private static string FormatTask(TaskInfo t)
    {
        var recurring = string.IsNullOrEmpty(t.CronExpression) ? "" : $"  Cron: {t.CronExpression}";
        var executeAt = t.ExecuteAt.HasValue ? $"  At: {t.ExecuteAt:yyyy-MM-dd HH:mm:ss} UTC" : "";
        return $"  [{t.TaskId}] {t.Status} — {t.Task}{executeAt}{recurring}";
    }

    private sealed class ScheduleResponse
    {
        [JsonPropertyName("taskId")]  public string TaskId { get; init; } = "";
        [JsonPropertyName("message")] public string Message { get; init; } = "";
    }

    private sealed class TaskInfo
    {
        [JsonPropertyName("taskId")]         public string TaskId { get; init; } = "";
        [JsonPropertyName("task")]           public string Task { get; init; } = "";
        [JsonPropertyName("status")]         public string Status { get; init; } = "";
        [JsonPropertyName("executeAt")]      public DateTime? ExecuteAt { get; init; }
        [JsonPropertyName("cronExpression")] public string? CronExpression { get; init; }
        [JsonPropertyName("isMultiAgent")]   public bool IsMultiAgent { get; init; }
    }
}
