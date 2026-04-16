// SkillBot.Plugins/OpenAI/SimpleUsagePlugin.cs
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.OpenAI;

/// <summary>
/// Simplified plugin for OpenAI API monitoring that works with all account types.
/// </summary>
[Plugin(Name = "OpenAIMonitor", Description = "Monitor OpenAI API status and estimate costs")]
public class SimpleUsagePlugin
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public SimpleUsagePlugin(IConfiguration configuration)
    {
        _apiKey = configuration["SkillBot:ApiKey"] ?? 
                  throw new InvalidOperationException("OpenAI API key not found");

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    [KernelFunction("check_api_status")]
    [Description("Check if your OpenAI API key is working and get rate limit information")]
    public async Task<string> CheckApiStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("models");

            if (!response.IsSuccessStatusCode)
            {
                return $"❌ API Status: Failed\n" +
                       $"Status Code: {response.StatusCode}\n" +
                       $"Your API key may be invalid or expired.\n\n" +
                       $"Check your API keys at: https://platform.openai.com/api-keys";
            }

            var result = new System.Text.StringBuilder();
            result.AppendLine("✓ OpenAI API Status: Active");
            result.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            result.AppendLine();

            // Extract rate limit information from headers
            if (response.Headers.TryGetValues("x-ratelimit-limit-requests", out var limitReq))
            {
                result.AppendLine($"📊 Rate Limits:");
                result.AppendLine($"  Request Limit: {string.Join(", ", limitReq)} per minute");
            }

            if (response.Headers.TryGetValues("x-ratelimit-remaining-requests", out var remainingReq))
            {
                result.AppendLine($"  Remaining: {string.Join(", ", remainingReq)} requests");
            }

            if (response.Headers.TryGetValues("x-ratelimit-limit-tokens", out var limitTok))
            {
                result.AppendLine($"  Token Limit: {string.Join(", ", limitTok)} per minute");
            }

            if (response.Headers.TryGetValues("x-ratelimit-remaining-tokens", out var remainingTok))
            {
                result.AppendLine($"  Remaining: {string.Join(", ", remainingTok)} tokens");
            }

            result.AppendLine();
            result.AppendLine("📈 View Usage & Billing:");
            result.AppendLine("  https://platform.openai.com/usage");

            return result.ToString();
        }
        catch (HttpRequestException ex)
        {
            return $"❌ Connection Error: {ex.Message}\n" +
                   $"Please check your internet connection.";
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }

    [KernelFunction("estimate_cost")]
    [Description("Estimate the cost of using OpenAI models based on token counts")]
    public string EstimateCost(
        [Description("Model name (gpt-4, gpt-4o, gpt-4o-mini, gpt-3.5-turbo)")] string model,
        [Description("Number of input/prompt tokens")] int inputTokens,
        [Description("Number of output/completion tokens")] int outputTokens)
    {
        var pricing = GetModelPricing(model.ToLower());

        if (pricing == null)
        {
            var availableModels = string.Join(", ", GetAvailableModels());
            return $"❌ Unknown model: {model}\n\n" +
                   $"Available models: {availableModels}\n\n" +
                   $"Check pricing at: https://openai.com/pricing";
        }

        var inputCost = (inputTokens / 1_000_000.0) * pricing.Value.InputPrice;
        var outputCost = (outputTokens / 1_000_000.0) * pricing.Value.OutputPrice;
        var totalCost = inputCost + outputCost;

        var result = new System.Text.StringBuilder();
        result.AppendLine($"💰 Cost Estimate for {model}");
        result.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        result.AppendLine();
        result.AppendLine($"📥 Input:  {inputTokens:N0} tokens");
        result.AppendLine($"   ${pricing.Value.InputPrice:F2}/1M tokens = ${inputCost:F6}");
        result.AppendLine();
        result.AppendLine($"📤 Output: {outputTokens:N0} tokens");
        result.AppendLine($"   ${pricing.Value.OutputPrice:F2}/1M tokens = ${outputCost:F6}");
        result.AppendLine();
        result.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        result.AppendLine($"💵 Total Cost: ${totalCost:F6}");
        result.AppendLine();
        result.AppendLine("Note: Estimate based on published rates");
        result.AppendLine("Actual costs may vary slightly");

        return result.ToString();
    }

    [KernelFunction("compare_model_costs")]
    [Description("Compare costs across different OpenAI models for the same usage")]
    public string CompareModelCosts(
        [Description("Number of input tokens")] int inputTokens,
        [Description("Number of output tokens")] int outputTokens)
    {
        var models = new[] { "gpt-4", "gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo" };
        var result = new System.Text.StringBuilder();

        result.AppendLine($"💰 Cost Comparison");
        result.AppendLine($"For {inputTokens:N0} input + {outputTokens:N0} output tokens");
        result.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        result.AppendLine();

        foreach (var model in models)
        {
            var pricing = GetModelPricing(model);
            if (pricing == null) continue;

            var inputCost = (inputTokens / 1_000_000.0) * pricing.Value.InputPrice;
            var outputCost = (outputTokens / 1_000_000.0) * pricing.Value.OutputPrice;
            var total = inputCost + outputCost;

            result.AppendLine($"{model,-20} ${total:F6}");
        }

        result.AppendLine();
        result.AppendLine("💡 Tip: Use gpt-4o-mini for cost-effective tasks");

        return result.ToString();
    }

    [KernelFunction("get_pricing_info")]
    [Description("Get current pricing information for all OpenAI models")]
    public string GetPricingInfo()
    {
        var result = new System.Text.StringBuilder();
        result.AppendLine("💰 OpenAI Model Pricing (per 1M tokens)");
        result.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        result.AppendLine();

        var models = GetAvailableModels();
        foreach (var model in models)
        {
            var pricing = GetModelPricing(model);
            if (pricing == null) continue;

            result.AppendLine($"📦 {model}");
            result.AppendLine($"   Input:  ${pricing.Value.InputPrice:F2}");
            result.AppendLine($"   Output: ${pricing.Value.OutputPrice:F2}");
            result.AppendLine();
        }

        result.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        result.AppendLine("For latest pricing:");
        result.AppendLine("https://openai.com/pricing");

        return result.ToString();
    }

    [KernelFunction("get_usage_links")]
    [Description("Get direct links to check your OpenAI usage and billing")]
    public string GetUsageLinks()
    {
        return """
            📊 OpenAI Account Management Links
            ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

            📈 Usage Dashboard:
            https://platform.openai.com/usage
            → View detailed usage statistics
            → See token consumption by day/model

            💳 Billing Dashboard:
            https://platform.openai.com/account/billing
            → Current billing cycle
            → Payment methods
            → Usage limits

            🔑 API Keys:
            https://platform.openai.com/api-keys
            → Manage your API keys
            → Create new keys
            → Monitor key usage

            ⚙️ Organization Settings:
            https://platform.openai.com/account/organization
            → Manage team members
            → Organization quotas

            Note: You must be logged in to access these pages.
            """;
    }

    private (double InputPrice, double OutputPrice)? GetModelPricing(string model)
    {
        // Pricing as of January 2025 (per 1M tokens in USD)
        // Source: https://openai.com/pricing
        var pricing = new Dictionary<string, (double, double)>
        {
            ["gpt-4"] = (30.0, 60.0),
            ["gpt-4-turbo"] = (10.0, 30.0),
            ["gpt-4o"] = (2.5, 10.0),
            ["gpt-4o-mini"] = (0.15, 0.6),
            ["gpt-3.5-turbo"] = (0.5, 1.5),
        };

        return pricing.TryGetValue(model, out var price) 
            ? (price.Item1, price.Item2) 
            : null;
    }

    private string[] GetAvailableModels()
    {
        return new[] { "gpt-4", "gpt-4-turbo", "gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo" };
    }
}