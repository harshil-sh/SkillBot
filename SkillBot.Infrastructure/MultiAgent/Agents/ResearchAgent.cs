using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SkillBot.Infrastructure.MultiAgent.Agents;

/// <summary>
/// Specialized agent for research, information gathering, and analysis.
/// </summary>
public class ResearchAgent : BaseSpecializedAgent
{
    public override string AgentId => "research-agent";
    public override string Name => "Research Specialist";
    public override string Description => "Expert in information gathering, research, and analytical tasks";
    public override IReadOnlyList<string> Specializations => new[] 
    { 
        "research", 
        "analysis", 
        "information", 
        "investigate",
        "compare",
        "summarize"
    };

    public ResearchAgent(
        Kernel kernel,
        IChatCompletionService chatService,
        ILogger<ResearchAgent> logger)
        : base(kernel, chatService, logger)
    {
    }

    protected override string GetSystemPrompt()
    {
        return """
            You are a Research Specialist agent. Your role is to:
            - Gather and synthesize information from available sources
            - Perform analytical tasks and comparisons
            - Provide well-researched, fact-based responses
            - Summarize complex information clearly
            
            Use the available tools to search for information and provide comprehensive answers.
            Be thorough but concise in your responses.
            """;
    }
}
