using SkillBot.Api.Services;
using System.Text;
using System.Text.Json.Serialization;

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

    public async Task<string> GetHistoryAsync(int limit = 50)
    {
        var items = await _apiClient.GetAsync<List<ConversationRecord>>($"/api/chat/history?limit={limit}");
        if (items is null || items.Count == 0)
            return "No conversation history found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Last {items.Count} conversation(s):");
        sb.AppendLine(new string('─', 60));
        foreach (var item in items)
        {
            sb.AppendLine($"[{item.CreatedAt:yyyy-MM-dd HH:mm}] Conv:{item.ConversationId}");
            sb.AppendLine($"  You: {Truncate(item.Message, 80)}");
            sb.AppendLine($"  Bot: {Truncate(item.Response, 80)}");
        }
        sb.Append(new string('─', 60));
        return sb.ToString();
    }

    public async Task<string> GetConversationAsync(string conversationId)
    {
        var conv = await _apiClient.GetAsync<ConversationDetail>($"/api/chat/{conversationId}");
        if (conv is null)
            return "Conversation not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Conversation: {conversationId}");
        sb.AppendLine(new string('─', 60));
        if (conv.Messages is not null)
        {
            foreach (var msg in conv.Messages)
                sb.AppendLine($"  [{msg.Role}]: {msg.Content}");
        }
        sb.Append(new string('─', 60));
        return sb.ToString();
    }

    public async Task DeleteConversationAsync(string conversationId)
    {
        await _apiClient.DeleteAsync($"/api/chat/{conversationId}");
    }

    private static string Truncate(string? s, int max) =>
        s is null ? "" : s.Length <= max ? s : s[..max] + "…";

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

    private sealed class ConversationRecord
    {
        [JsonPropertyName("conversationId")] public string ConversationId { get; init; } = "";
        [JsonPropertyName("message")]        public string? Message { get; init; }
        [JsonPropertyName("response")]       public string? Response { get; init; }
        [JsonPropertyName("createdAt")]      public DateTime CreatedAt { get; init; }
    }

    private sealed class ConversationDetail
    {
        [JsonPropertyName("messages")] public List<ConversationMessage>? Messages { get; init; }
    }

    private sealed class ConversationMessage
    {
        [JsonPropertyName("role")]    public string Role { get; init; } = "";
        [JsonPropertyName("content")] public string Content { get; init; } = "";
    }
}
