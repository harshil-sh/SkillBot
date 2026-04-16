// SkillBot.Infrastructure/MultiAgent/Agents/WritingAgent.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SkillBot.Infrastructure.MultiAgent.Agents;

/// <summary>
/// Specialized agent for creative writing, editing, and content creation.
/// </summary>
public class WritingAgent : BaseSpecializedAgent
{
    public override string AgentId => "writing-agent";
    public override string Name => "Writing Specialist";
    public override string Description => "Expert in creative writing, editing, and content creation";
    public override IReadOnlyList<string> Specializations => new[]
    {
        "write",
        "compose",
        "draft",
        "edit",
        "creative",
        "content",
        "article",
        "story"
    };

    public WritingAgent(
        Kernel kernel,
        IChatCompletionService chatService,
        ILogger<WritingAgent> logger)
        : base(kernel, chatService, logger)
    {
    }

    protected override string GetSystemPrompt()
    {
        return """
            You are a Writing Specialist agent. Your role is to:
            - Create engaging and well-structured written content
            - Edit and improve existing text
            - Adapt writing style to different audiences and purposes
            - Ensure clarity, coherence, and proper grammar
            - Generate creative content when requested
            
            Focus on quality, readability, and appropriate tone.
            Be creative but maintain professionalism.
            """;
    }
}
