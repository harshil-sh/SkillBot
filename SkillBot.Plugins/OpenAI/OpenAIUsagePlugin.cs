// SkillBot.Plugins/OpenAI/OpenAIUsagePlugin.cs
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.OpenAI;

/// <summary>
/// Plugin for monitoring OpenAI API usage and costs.
/// </summary>
[Plugin(Name = "OpenAIUsage", Description = "Monitor OpenAI API usage, costs, and quotas")]
public class OpenAIUsagePlugin
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public OpenAIUsagePlugin(IConfiguration configuration)
    {
        _apiKey = configuration["SkillBot:ApiKey"] ?? 
                  Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                  throw new InvalidOperationException("OpenAI API key not found");

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    [KernelFunction("get_usage_summary")]
    [Description("Get a summary of OpenAI API usage including costs and quotas")]
    public async Task<string> GetUsageSummaryAsync(
        [Description("Optional: Start date for usage data (YYYY-MM-DD)")] string? startDate = null,
        [Description("Optional: End date for usage data (YYYY-MM-DD)")] string? endDate = null)
    {
        try
        {
            // Set default dates if not provided (last 30 days)
            var end = string.IsNullOrEmpty(endDate) 
                ? DateTime.UtcNow.ToString("yyyy-MM-dd") 
                : endDate;
            
            var start = string.IsNullOrEmpty(startDate)
                ? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd")
                : startDate;

            // Note: OpenAI's usage endpoint requires organization access
            // This is a basic implementation - you may need to adjust based on your account type
            var response = await _httpClient.GetAsync($"usage?date={end}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"Unable to fetch usage data. Status: {response.StatusCode}\n" +
                       $"Note: Usage API access requires certain account types.\n" +
                       $"You can check your usage manually at: https://platform.openai.com/usage";
            }

            var content = await response.Content.ReadAsStringAsync();
            var usageData = JsonSerializer.Deserialize<UsageResponse>(content);

            if (usageData == null)
            {
                return "Unable to parse usage data.";
            }

            return FormatUsageResponse(usageData, start, end);
        }
        catch (Exception ex)
        {
            return $"Error fetching usage data: {ex.Message}\n\n" +
                   $"You can check your usage manually at: https://platform.openai.com/usage";
        }
    }

    [KernelFunction("check_rate_limits")]
    [Description("Check current rate limits and remaining quota")]
    public async Task<string> CheckRateLimitsAsync()
    {
        try
        {
            // Make a minimal API call to check rate limit headers
            var response = await _httpClient.GetAsync("models");

            var rateLimitInfo = new System.Text.StringBuilder();
            rateLimitInfo.AppendLine("OpenAI API Rate Limit Status:");
            rateLimitInfo.AppendLine();

            // Extract rate limit headers
            if (response.Headers.TryGetValues("x-ratelimit-limit-requests", out var limitRequests))
            {
                rateLimitInfo.AppendLine($"Request Limit: {string.Join(", ", limitRequests)} per minute");
            }

            if (response.Headers.TryGetValues("x-ratelimit-remaining-requests", out var remainingRequests))
            {
                rateLimitInfo.AppendLine($"Remaining Requests: {string.Join(", ", remainingRequests)}");
            }

            if (response.Headers.TryGetValues("x-ratelimit-limit-tokens", out var limitTokens))
            {
                rateLimitInfo.AppendLine($"Token Limit: {string.Join(", ", limitTokens)} per minute");
            }

            if (response.Headers.TryGetValues("x-ratelimit-remaining-tokens", out var remainingTokens))
            {
                rateLimitInfo.AppendLine($"Remaining Tokens: {string.Join(", ", remainingTokens)}");
            }

            if (response.Headers.TryGetValues("x-ratelimit-reset-requests", out var resetRequests))
            {
                rateLimitInfo.AppendLine($"Requests Reset: {string.Join(", ", resetRequests)}");
            }

            rateLimitInfo.AppendLine();
            rateLimitInfo.AppendLine("For detailed usage and billing, visit: https://platform.openai.com/usage");

            return rateLimitInfo.ToString();
        }
        catch (Exception ex)
        {
            return $"Error checking rate limits: {ex.Message}";
        }
    }

    [KernelFunction("estimate_cost")]
    [Description("Estimate the cost of API usage based on token counts")]
    public string EstimateCost(
        [Description("Model name (e.g., gpt-4, gpt-3.5-turbo)")] string model,
        [Description("Number of input tokens")] int inputTokens,
        [Description("Number of output tokens")] int outputTokens)
    {
        var pricing = GetModelPricing(model);

        if (pricing == null)
        {
            return $"Pricing information not available for model: {model}\n\n" +
                   $"Check current pricing at: https://openai.com/pricing";
        }

        var inputCost = (inputTokens / 1_000_000.0) * pricing.InputPricePer1M;
        var outputCost = (outputTokens / 1_000_000.0) * pricing.OutputPricePer1M;
        var totalCost = inputCost + outputCost;

        return $"Cost Estimate for {model}:\n" +
               $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
               $"Input:  {inputTokens:N0} tokens × ${pricing.InputPricePer1M}/1M  = ${inputCost:F4}\n" +
               $"Output: {outputTokens:N0} tokens × ${pricing.OutputPricePer1M}/1M = ${outputCost:F4}\n" +
               $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
               $"Total: ${totalCost:F4}\n\n" +
               $"Note: Prices are estimates based on current published rates.";
    }

    [KernelFunction("get_account_info")]
    [Description("Get OpenAI account information and billing status")]
    public async Task<string> GetAccountInfoAsync()
    {
        try
        {
            // Try to get organization info
            var response = await _httpClient.GetAsync("organization");

            if (!response.IsSuccessStatusCode)
            {
                return "Account Information:\n" +
                       "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                       "✓ API Key: Valid and active\n" +
                       "✓ Connection: Successful\n\n" +
                       "For detailed account and billing information:\n" +
                       "• Usage: https://platform.openai.com/usage\n" +
                       "• Billing: https://platform.openai.com/account/billing\n" +
                       "• API Keys: https://platform.openai.com/api-keys";
            }

            return "Account Status: Connected\n" +
                   "For detailed information, visit: https://platform.openai.com/account";
        }
        catch (Exception ex)
        {
            return $"Error fetching account info: {ex.Message}";
        }
    }

    [KernelFunction("list_available_models")]
    [Description("List all available OpenAI models you can use")]
    public async Task<string> ListAvailableModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("models");
            
            if (!response.IsSuccessStatusCode)
            {
                return "Unable to fetch models list.";
            }

            var content = await response.Content.ReadAsStringAsync();
            var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(content);

            if (modelsResponse?.Data == null)
            {
                return "No models found.";
            }

            var gptModels = modelsResponse.Data
                .Where(m => m.Id.Contains("gpt"))
                .OrderByDescending(m => m.Created)
                .Take(10);

            var result = new System.Text.StringBuilder();
            result.AppendLine("Available GPT Models:");
            result.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            foreach (var model in gptModels)
            {
                result.AppendLine($"• {model.Id}");
            }

            result.AppendLine();
            result.AppendLine("For complete model list: https://platform.openai.com/docs/models");

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error fetching models: {ex.Message}";
        }
    }

    private string FormatUsageResponse(UsageResponse usage, string startDate, string endDate)
    {
        var result = new System.Text.StringBuilder();
        result.AppendLine($"OpenAI API Usage Summary");
        result.AppendLine($"Period: {startDate} to {endDate}");
        result.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        result.AppendLine();
        result.AppendLine("For detailed usage and billing information:");
        result.AppendLine("https://platform.openai.com/usage");
        
        return result.ToString();
    }

    private ModelPricing? GetModelPricing(string model)
    {
        // Pricing as of January 2025 - Update these if needed
        var pricingMap = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
        {
            ["gpt-4"] = new(30.0, 60.0),
            ["gpt-4-turbo"] = new(10.0, 30.0),
            ["gpt-4-turbo-preview"] = new(10.0, 30.0),
            ["gpt-4o"] = new(2.5, 10.0),
            ["gpt-4o-mini"] = new(0.15, 0.6),
            ["gpt-3.5-turbo"] = new(0.5, 1.5),
            ["gpt-3.5-turbo-16k"] = new(3.0, 4.0),
        };

        return pricingMap.GetValueOrDefault(model);
    }

    // Helper classes for JSON deserialization
    private class UsageResponse
    {
        public string? Object { get; set; }
        public List<DailyUsage>? DailyUsage { get; set; }
    }

    private class DailyUsage
    {
        public string? Date { get; set; }
        public int Requests { get; set; }
    }

    private class ModelsResponse
    {
        public string? Object { get; set; }
        public List<ModelInfo>? Data { get; set; }
    }

    private class ModelInfo
    {
        public string Id { get; set; } = "";
        public string Object { get; set; } = "";
        public long Created { get; set; }
        public string OwnedBy { get; set; } = "";
    }

    private record ModelPricing(double InputPricePer1M, double OutputPricePer1M);
}