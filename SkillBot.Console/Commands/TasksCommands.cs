using SkillBot.Console.Helpers;
using SkillBot.Console.Services;

namespace SkillBot.Console.Commands;

public static class TasksCommands
{
    public static async Task HandleTasksAsync(Dictionary<string, string> args, IConsoleTaskService taskService)
    {
        if (!args.TryGetValue("0", out var sub))
        {
            PrintUsage();
            return;
        }

        switch (sub.ToLowerInvariant())
        {
            case "schedule":
                await HandleScheduleAsync(ShiftPositional(args), taskService);
                break;
            case "recurring":
                await HandleRecurringAsync(ShiftPositional(args), taskService);
                break;
            case "get":
                await HandleGetAsync(ShiftPositional(args), taskService);
                break;
            case "list":
                await HandleListAsync(taskService);
                break;
            case "cancel":
                await HandleCancelAsync(ShiftPositional(args), taskService);
                break;
            default:
                PrintUsage();
                break;
        }
    }

    private static async Task HandleScheduleAsync(Dictionary<string, string> args, IConsoleTaskService taskService)
    {
        var task = BuildMessage(args);
        if (string.IsNullOrWhiteSpace(task))
        {
            ConsoleHelper.WriteError("Usage: tasks schedule <task description> --at <YYYY-MM-DDTHH:mm:ssZ> [--multi-agent]");
            return;
        }

        if (!args.TryGetValue("at", out var atRaw) || !DateTime.TryParse(atRaw, out var executeAt))
        {
            ConsoleHelper.WriteError("--at <datetime> is required. Example: --at 2026-05-01T09:00:00Z");
            return;
        }

        var isMultiAgent = args.ContainsKey("multi-agent");

        try
        {
            var result = await taskService.ScheduleTaskAsync(task, executeAt.ToUniversalTime(), isMultiAgent);
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    private static async Task HandleRecurringAsync(Dictionary<string, string> args, IConsoleTaskService taskService)
    {
        var task = BuildMessage(args);
        if (string.IsNullOrWhiteSpace(task))
        {
            ConsoleHelper.WriteError("Usage: tasks recurring <task description> --cron <expression> [--multi-agent]");
            return;
        }

        if (!args.TryGetValue("cron", out var cron) || string.IsNullOrWhiteSpace(cron))
        {
            ConsoleHelper.WriteError("--cron <expression> is required. Example: --cron \"0 9 * * *\"");
            return;
        }

        var isMultiAgent = args.ContainsKey("multi-agent");

        try
        {
            var result = await taskService.ScheduleRecurringTaskAsync(task, cron, isMultiAgent);
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    private static async Task HandleGetAsync(Dictionary<string, string> args, IConsoleTaskService taskService)
    {
        if (!args.TryGetValue("0", out var taskId) || string.IsNullOrWhiteSpace(taskId))
        {
            ConsoleHelper.WriteError("Task ID is required. Usage: tasks get <taskId>");
            return;
        }

        try
        {
            var result = await taskService.GetTaskAsync(taskId);
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    private static async Task HandleListAsync(IConsoleTaskService taskService)
    {
        try
        {
            var result = await taskService.GetAllTasksAsync();
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    private static async Task HandleCancelAsync(Dictionary<string, string> args, IConsoleTaskService taskService)
    {
        if (!args.TryGetValue("0", out var taskId) || string.IsNullOrWhiteSpace(taskId))
        {
            ConsoleHelper.WriteError("Task ID is required. Usage: tasks cancel <taskId>");
            return;
        }

        try
        {
            await taskService.CancelTaskAsync(taskId);
            ConsoleHelper.WriteSuccess($"✅ Task '{taskId}' cancelled.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    private static string BuildMessage(Dictionary<string, string> args) =>
        string.Join(" ", args
            .Where(p => int.TryParse(p.Key, out _))
            .Select(p => new { Index = int.Parse(p.Key), p.Value })
            .OrderBy(p => p.Index)
            .Select(p => p.Value)).Trim();

    private static Dictionary<string, string> ShiftPositional(Dictionary<string, string> args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in args)
        {
            if (int.TryParse(key, out var index))
            {
                if (index > 0)
                    result[(index - 1).ToString()] = value;
            }
            else
            {
                result[key] = value;
            }
        }
        return result;
    }

    private static void PrintUsage()
    {
        System.Console.WriteLine("Usage: tasks <subcommand>");
        System.Console.WriteLine("  tasks schedule <description> --at <datetime> [--multi-agent]");
        System.Console.WriteLine("  tasks recurring <description> --cron <expression> [--multi-agent]");
        System.Console.WriteLine("  tasks get <taskId>");
        System.Console.WriteLine("  tasks list");
        System.Console.WriteLine("  tasks cancel <taskId>");
    }
}
