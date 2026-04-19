using SkillBot.Console.Helpers;
using SkillBot.Console.Services;

namespace SkillBot.Console.Commands;

public static class ChatCommands
{
    public static async Task HandleChatAsync(Dictionary<string, string> args, IConsoleChatService chatService)
    {
        var message = BuildMessage(args);
        if (string.IsNullOrWhiteSpace(message))
        {
            ConsoleHelper.WriteError("Message is required. Usage: chat <message>");
            return;
        }

        args.TryGetValue("conversationId", out var conversationId);

        try
        {
            ConsoleHelper.WriteInfo("Thinking...");
            var response = await chatService.SendMessageAsync(message, conversationId);
            System.Console.WriteLine($"\nAssistant: {response}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleMultiAgentChatAsync(Dictionary<string, string> args, IConsoleChatService chatService)
    {
        var message = BuildMessage(args);
        if (string.IsNullOrWhiteSpace(message))
        {
            ConsoleHelper.WriteError("Message is required. Usage: multi-agent <message> --agents agent1,agent2");
            return;
        }

        if (!args.TryGetValue("agents", out var agentsRaw) || string.IsNullOrWhiteSpace(agentsRaw))
        {
            ConsoleHelper.WriteError("Agents are required. Usage: multi-agent <message> --agents agent1,agent2");
            return;
        }

        var agents = agentsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        try
        {
            ConsoleHelper.WriteInfo($"Coordinating {agents.Length} agent(s)...");
            var response = await chatService.SendMultiAgentMessageAsync(message, agents);
            System.Console.WriteLine($"\nOrchestrator: {response}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    private static string BuildMessage(Dictionary<string, string> args)
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
