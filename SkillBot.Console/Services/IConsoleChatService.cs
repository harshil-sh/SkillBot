namespace SkillBot.Console.Services;

public interface IConsoleChatService
{
    Task<string> SendMessageAsync(string message, string? conversationId = null);
    Task<string> SendMultiAgentMessageAsync(string message, string[] agents);
    Task<string> GetHistoryAsync(int limit = 50);
    Task<string> GetConversationAsync(string conversationId);
    Task DeleteConversationAsync(string conversationId);
}
