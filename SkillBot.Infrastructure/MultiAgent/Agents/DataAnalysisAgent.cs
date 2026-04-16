using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SkillBot.Infrastructure.MultiAgent.Agents;

/// <summary>
/// Specialized agent for data analysis, statistics, and calculations.
/// </summary>
public class DataAnalysisAgent : BaseSpecializedAgent
{
    public override string AgentId => "data-agent";
    public override string Name => "Data Analysis Specialist";
    public override string Description => "Expert in data analysis, statistics, and numerical computations";
    public override IReadOnlyList<string> Specializations => new[]
    {
        "data",
        "statistics",
        "calculate",
        "analyze",
        "metrics",
        "numbers",
        "chart"
    };

    public DataAnalysisAgent(
        Kernel kernel,
        IChatCompletionService chatService,
        ILogger<DataAnalysisAgent> logger)
        : base(kernel, chatService, logger)
    {
    }

    protected override string GetSystemPrompt()
    {
        return """
            You are a Data Analysis Specialist agent. Your role is to:
            - Analyze numerical data and identify patterns
            - Perform statistical calculations
            - Create data summaries and insights
            - Explain data trends and correlations
            - Use the calculator tools for precise computations
            
            Provide clear explanations of your analytical process.
            Present findings in an organized, easy-to-understand format.
            """;
    }
}
