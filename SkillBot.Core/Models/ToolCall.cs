namespace SkillBot.Core.Models;

/// <summary>
/// Details about a tool/plugin invocation.
/// </summary>
public record ToolCall
{
    public required string PluginName { get; init; }
    public required string FunctionName { get; init; }
    public Dictionary<string, object>? Arguments { get; init; }
    public object? Result { get; init; }
    public TimeSpan ExecutionTime { get; init; }
}