using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SkillBot.Infrastructure.MultiAgent.Agents;

/// <summary>
/// Specialized agent for coding, debugging, and technical tasks.
/// </summary>
public class CodingAgent : BaseSpecializedAgent
{
    public override string AgentId => "coding-agent";
    public override string Name => "Coding Specialist";
    public override string Description => "Expert in programming, code review, debugging, and technical solutions";
    public override IReadOnlyList<string> Specializations => new[]
    {
        "code",
        "programming",
        "debug",
        "develop",
        "implement",
        "algorithm",
        "function"
    };

    public CodingAgent(
        Kernel kernel,
        IChatCompletionService chatService,
        ILogger<CodingAgent> logger)
        : base(kernel, chatService, logger)
    {
    }

    protected override string GetSystemPrompt()
    {
        return """
            You are a Coding Specialist agent. Your role is to:
            - Write clean, efficient, and well-documented code
            - Debug and fix code issues
            - Explain technical concepts clearly
            - Provide code reviews and suggestions
            - Implement algorithms and data structures
            
            Always include code comments and explanations.
            Follow best practices and modern coding standards.
            """;
    }
}
