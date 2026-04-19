namespace SkillBot.Console.Services;

public interface IConsoleChatService
{
    Task<string> SendMessageAsync(string message, string? conversationId = null);
    Task<string> SendMultiAgentMessageAsync(string message, string[] agents);
}
