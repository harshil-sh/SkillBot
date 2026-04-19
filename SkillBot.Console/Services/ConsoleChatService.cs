using SkillBot.Api.Services;

namespace SkillBot.Console.Services;

public class ConsoleChatService : IConsoleChatService
{
    private readonly ApiClient _apiClient;

    public ConsoleChatService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<string> SendMessageAsync(string message, string? conversationId = null)
    {
        var validator = new InputValidator();
        var validation = await validator.ValidateInputAsync(message);
        if (!validation.IsValid)
            throw new ArgumentException(validation.ErrorMessage);

        var response = await _apiClient.PostAsync<ChatResponse>(
            "/api/chat",
            new { message, conversationId });

        return response?.Message ?? throw new InvalidOperationException("No response received from chat endpoint.");
    }

    public async Task<string> SendMultiAgentMessageAsync(string message, string[] agents)
    {
        var response = await _apiClient.PostAsync<MultiAgentResponse>(
            "/api/multi-agent/chat",
            new { task = message, preferredAgents = agents });

        return response?.FinalResponse ?? throw new InvalidOperationException("No response received from multi-agent endpoint.");
    }

    private sealed class ChatResponse
    {
        public required string Message { get; init; }
        public required string ConversationId { get; init; }
    }

    private sealed class MultiAgentResponse
    {
        public required string FinalResponse { get; init; }
        public required string ConversationId { get; init; }
    }
}
