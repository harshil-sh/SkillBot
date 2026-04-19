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
}
