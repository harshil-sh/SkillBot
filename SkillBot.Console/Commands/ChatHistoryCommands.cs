using SkillBot.Console.Helpers;
using SkillBot.Console.Services;

namespace SkillBot.Console.Commands;

public static class ChatHistoryCommands
{
    public static async Task HandleHistoryAsync(Dictionary<string, string> args, IConsoleChatService chatService)
    {
        var limit = 50;
        if (args.TryGetValue("limit", out var limitStr) && int.TryParse(limitStr, out var l))
            limit = l;

        try
        {
            var result = await chatService.GetHistoryAsync(limit);
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleGetConversationAsync(Dictionary<string, string> args, IConsoleChatService chatService)
    {
        if (!args.TryGetValue("0", out var id) || string.IsNullOrWhiteSpace(id))
        {
            ConsoleHelper.WriteError("Conversation ID is required. Usage: conversation <id>");
            return;
        }

        try
        {
            var result = await chatService.GetConversationAsync(id);
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleDeleteConversationAsync(Dictionary<string, string> args, IConsoleChatService chatService)
    {
        if (!args.TryGetValue("0", out var id) || string.IsNullOrWhiteSpace(id))
        {
            ConsoleHelper.WriteError("Conversation ID is required. Usage: delete-conversation <id>");
            return;
        }

        try
        {
            await chatService.DeleteConversationAsync(id);
            ConsoleHelper.WriteSuccess($"✅ Conversation '{id}' deleted.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }
}
