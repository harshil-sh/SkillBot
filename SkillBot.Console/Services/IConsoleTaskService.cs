namespace SkillBot.Console.Services;

public interface IConsoleTaskService
{
    Task<string> ScheduleTaskAsync(string task, DateTime executeAt, bool isMultiAgent = false);
    Task<string> ScheduleRecurringTaskAsync(string task, string cronExpression, bool isMultiAgent = false);
    Task<string> GetTaskAsync(string taskId);
    Task<string> GetAllTasksAsync();
    Task CancelTaskAsync(string taskId);
}
