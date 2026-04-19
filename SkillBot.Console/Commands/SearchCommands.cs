using SkillBot.Console.Helpers;
using SkillBot.Console.Services;

namespace SkillBot.Console.Commands;

public static class SearchCommands
{
    public static async Task HandleSearchAsync(Dictionary<string, string> args, IConsoleSearchService searchService)
    {
        var query = BuildQuery(args);
        if (string.IsNullOrWhiteSpace(query))
        {
            ConsoleHelper.WriteError("Query is required. Usage: search <query> [--count <n>]");
            return;
        }

        var count = ParseCount(args);

        try
        {
            ConsoleHelper.WriteInfo($"Searching web for '{query}'...");
            var results = await searchService.SearchWebAsync(query, count);
            PrintResults(results);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleSearchNewsAsync(Dictionary<string, string> args, IConsoleSearchService searchService)
    {
        var query = BuildQuery(args);
        if (string.IsNullOrWhiteSpace(query))
        {
            ConsoleHelper.WriteError("Query is required. Usage: search-news <query> [--count <n>]");
            return;
        }

        var count = ParseCount(args);

        try
        {
            ConsoleHelper.WriteInfo($"Searching news for '{query}'...");
            var results = await searchService.SearchNewsAsync(query, count);
            PrintResults(results);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    private static int ParseCount(Dictionary<string, string> args)
    {
        if (args.TryGetValue("count", out var raw) && int.TryParse(raw, out var n))
            return n;
        return 5;
    }

    private static void PrintResults(string results)
    {
        System.Console.WriteLine();
        System.Console.WriteLine(new string('─', 50));
        System.Console.WriteLine(results);
        System.Console.WriteLine(new string('─', 50));
    }

    private static string BuildQuery(Dictionary<string, string> args)
    {
        var parts = args
            .Where(pair => int.TryParse(pair.Key, out _))
            .Select(pair => new { Index = int.Parse(pair.Key), pair.Value })
            .OrderBy(pair => pair.Index)
            .Select(pair => pair.Value)
            .ToArray();

        return string.Join(" ", parts).Trim();
    }
}
