using System.Text;
using System.Text.Json.Serialization;

namespace SkillBot.Console.Services;

public class ConsoleAdminService : IConsoleAdminService
{
    private readonly ApiClient _apiClient;

    public ConsoleAdminService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<string> GetUsageStatsAsync()
    {
        var stats = await _apiClient.GetAsync<UsageStatsResponse>("/api/usage/stats");
        if (stats is null)
            return "No usage data available.";

        var sb = new StringBuilder();
        sb.AppendLine("Usage Statistics");
        sb.AppendLine(new string('─', 40));
        sb.AppendLine($"  Total Tokens:    {stats.TotalTokens:N0}");
        sb.AppendLine($"  Total Cost:      ${stats.TotalCost:F4}");
        sb.AppendLine($"  Total Requests:  {stats.TotalRequests:N0}");
        sb.AppendLine($"  Avg Tokens/Req:  {stats.AverageTokensPerRequest:N0}");
        sb.AppendLine($"  Conversations:   {stats.ConversationCount:N0}");

        if (stats.Since.HasValue)
            sb.AppendLine($"  Since:           {stats.Since:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"  Until:           {stats.Until:yyyy-MM-dd HH:mm}");

        if (stats.ModelBreakdown?.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("  Model Breakdown:");
            foreach (var (model, m) in stats.ModelBreakdown)
                sb.AppendLine($"    {model}: {m.Tokens:N0} tokens, {m.Requests} requests, ${m.Cost:F4}");
        }

        sb.Append(new string('─', 40));
        return sb.ToString();
    }

    public async Task<string> GetCacheStatsAsync()
    {
        var stats = await _apiClient.GetAsync<CacheStatsResponse>("/api/cache/stats");
        if (stats is null)
            return "No cache data available.";

        var sb = new StringBuilder();
        sb.AppendLine("Cache Statistics");
        sb.AppendLine(new string('─', 40));
        sb.AppendLine($"  Hit Rate:        {stats.HitRatePercentage:F1}%");
        sb.AppendLine($"  Total Requests:  {stats.TotalRequests:N0}");
        sb.AppendLine($"  Total Hits:      {stats.TotalHits:N0} (L1: {stats.L1Hits:N0}, L2: {stats.L2Hits:N0})");
        sb.AppendLine($"  Total Misses:    {stats.TotalMisses:N0} (L1: {stats.L1Misses:N0}, L2: {stats.L2Misses:N0})");
        sb.AppendLine($"  Entries:         {stats.TotalEntries:N0}");
        sb.AppendLine($"  Size:            {stats.TotalSizeBytes / 1024.0:F1} KB");

        if (stats.OldestEntry.HasValue)
            sb.AppendLine($"  Oldest Entry:    {stats.OldestEntry:yyyy-MM-dd HH:mm}");
        if (stats.NewestEntry.HasValue)
            sb.AppendLine($"  Newest Entry:    {stats.NewestEntry:yyyy-MM-dd HH:mm}");

        sb.Append(new string('─', 40));
        return sb.ToString();
    }

    public Task<string> ListUsersAsync()
        => Task.FromResult("User listing is not available (no /api/users endpoint).");

    public async Task<string> GetHealthAsync()
    {
        var body = await _apiClient.GetStringAsync("/health");
        return $"Health: {body.Trim()}";
    }

    public async Task<string> GetConversationStatsAsync(string conversationId)
    {
        var stats = await _apiClient.GetAsync<UsageStatsResponse>($"/api/usage/stats/{conversationId}");
        if (stats is null)
            return "No usage data for that conversation.";

        var sb = new StringBuilder();
        sb.AppendLine($"Usage for conversation: {conversationId}");
        sb.AppendLine(new string('─', 40));
        sb.AppendLine($"  Total Tokens:    {stats.TotalTokens:N0}");
        sb.AppendLine($"  Total Cost:      ${stats.TotalCost:F4}");
        sb.AppendLine($"  Total Requests:  {stats.TotalRequests:N0}");
        sb.Append(new string('─', 40));
        return sb.ToString();
    }

    public async Task<string> GetTopConversationsAsync(int limit = 10)
    {
        var items = await _apiClient.GetAsync<List<ConversationUsageResponse>>($"/api/usage/top-conversations?limit={limit}");
        if (items is null || items.Count == 0)
            return "No conversation usage data available.";

        var sb = new StringBuilder();
        sb.AppendLine($"Top {items.Count} conversations by token usage:");
        sb.AppendLine(new string('─', 50));
        var rank = 1;
        foreach (var item in items)
        {
            sb.AppendLine($"  #{rank++}  {item.ConversationId}");
            sb.AppendLine($"      Tokens: {item.TotalTokens:N0}  Requests: {item.RequestCount}  Cost: ${item.TotalCost:F4}");
        }
        sb.Append(new string('─', 50));
        return sb.ToString();
    }

    public async Task ResetStatsAsync()
    {
        await _apiClient.DeleteAsync("/api/usage/stats");
    }

    public async Task<string> GetCacheHealthAsync()
    {
        var health = await _apiClient.GetAsync<CacheHealthResponse>("/api/cache/health");
        if (health is null)
            return "Cache health unavailable.";

        var status = health.IsHealthy ? "✅ Healthy" : "⚠️  Unhealthy";
        var sb = new StringBuilder();
        sb.AppendLine("Cache Health");
        sb.AppendLine(new string('─', 40));
        sb.AppendLine($"  Status:    {status}");
        sb.AppendLine($"  Hit Rate:  {health.HitRate:F1}%");
        sb.AppendLine($"  Entries:   {health.TotalEntries:N0}");
        if (!string.IsNullOrEmpty(health.Message))
            sb.AppendLine($"  Message:   {health.Message}");
        sb.Append(new string('─', 40));
        return sb.ToString();
    }

    public async Task ClearCacheAsync()
    {
        await _apiClient.DeleteAsync("/api/cache");
    }

    public async Task InvalidateCacheAsync(string pattern)
    {
        await _apiClient.DeleteAsync($"/api/cache/invalidate/{Uri.EscapeDataString(pattern)}");
    }

    public async Task<string> GetAgentsAsync()
    {
        var agents = await _apiClient.GetAsync<List<AgentStatusResponse>>("/api/multi-agent/agents");
        if (agents is null || agents.Count == 0)
            return "No agents registered.";

        var sb = new StringBuilder();
        sb.AppendLine($"Available agents ({agents.Count}):");
        sb.AppendLine(new string('─', 50));
        foreach (var a in agents)
        {
            sb.AppendLine($"  {a.Name}  [{a.Status}]");
            sb.AppendLine($"    {a.Description}");
            if (a.Specializations?.Count > 0)
                sb.AppendLine($"    Specializations: {string.Join(", ", a.Specializations)}");
        }
        sb.Append(new string('─', 50));
        return sb.ToString();
    }

    // Local DTOs — mirrors only the fields consumed here
    private sealed class UsageStatsResponse
    {
        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; init; }

        [JsonPropertyName("totalCost")]
        public double TotalCost { get; init; }

        [JsonPropertyName("totalRequests")]
        public int TotalRequests { get; init; }

        [JsonPropertyName("averageTokensPerRequest")]
        public int AverageTokensPerRequest { get; init; }

        [JsonPropertyName("conversationCount")]
        public int ConversationCount { get; init; }

        [JsonPropertyName("modelBreakdown")]
        public Dictionary<string, ModelStatsResponse>? ModelBreakdown { get; init; }

        [JsonPropertyName("since")]
        public DateTime? Since { get; init; }

        [JsonPropertyName("until")]
        public DateTime Until { get; init; }
    }

    private sealed class ModelStatsResponse
    {
        [JsonPropertyName("tokens")]
        public int Tokens { get; init; }

        [JsonPropertyName("cost")]
        public double Cost { get; init; }

        [JsonPropertyName("requests")]
        public int Requests { get; init; }
    }

    private sealed class CacheStatsResponse
    {
        [JsonPropertyName("l1Hits")]
        public long L1Hits { get; init; }

        [JsonPropertyName("l1Misses")]
        public long L1Misses { get; init; }

        [JsonPropertyName("l2Hits")]
        public long L2Hits { get; init; }

        [JsonPropertyName("l2Misses")]
        public long L2Misses { get; init; }

        [JsonPropertyName("totalEntries")]
        public int TotalEntries { get; init; }

        [JsonPropertyName("totalSizeBytes")]
        public long TotalSizeBytes { get; init; }

        [JsonPropertyName("hitRatePercentage")]
        public double HitRatePercentage { get; init; }

        [JsonPropertyName("totalRequests")]
        public long TotalRequests { get; init; }

        [JsonPropertyName("totalHits")]
        public long TotalHits { get; init; }

        [JsonPropertyName("totalMisses")]
        public long TotalMisses { get; init; }

        [JsonPropertyName("oldestEntry")]
        public DateTime? OldestEntry { get; init; }

        [JsonPropertyName("newestEntry")]
        public DateTime? NewestEntry { get; init; }
    }

    private sealed class CacheHealthResponse
    {
        [JsonPropertyName("isHealthy")]    public bool IsHealthy { get; init; }
        [JsonPropertyName("hitRate")]      public double HitRate { get; init; }
        [JsonPropertyName("totalEntries")] public int TotalEntries { get; init; }
        [JsonPropertyName("message")]      public string? Message { get; init; }
    }

    private sealed class ConversationUsageResponse
    {
        [JsonPropertyName("conversationId")] public string ConversationId { get; init; } = "";
        [JsonPropertyName("totalTokens")]    public int TotalTokens { get; init; }
        [JsonPropertyName("requestCount")]   public int RequestCount { get; init; }
        [JsonPropertyName("totalCost")]      public double TotalCost { get; init; }
    }

    private sealed class AgentStatusResponse
    {
        [JsonPropertyName("agentId")]          public string AgentId { get; init; } = "";
        [JsonPropertyName("name")]             public string Name { get; init; } = "";
        [JsonPropertyName("description")]      public string Description { get; init; } = "";
        [JsonPropertyName("specializations")]  public List<string>? Specializations { get; init; }
        [JsonPropertyName("status")]           public string Status { get; init; } = "";
    }
}
