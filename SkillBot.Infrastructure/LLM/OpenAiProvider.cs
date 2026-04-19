using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SkillBot.Core.Services;

namespace SkillBot.Infrastructure.LLM;

public class OpenAiProvider : ILLMProvider
{
    private const string DefaultModel = "gpt-4o-mini";

    private readonly IConfiguration _configuration;

    public string Name => "openai";
    public bool RequiresApiKey => true;

    public OpenAiProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> GenerateResponseAsync(string prompt, string? apiKey = null)
    {
        var key = apiKey
            ?? _configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("No OpenAI API key available.");

        var model = _configuration["OpenAI:Model"] ?? DefaultModel;

        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(model, key)
            .Build();

        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.7,
            MaxTokens = 2000
        };

        var result = await chatService.GetChatMessageContentAsync(history, settings, kernel);
        return result.Content ?? string.Empty;
    }
}
