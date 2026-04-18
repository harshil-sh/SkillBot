/// <summary>
/// Information about a tool/plugin call
/// </summary>
public class ToolCallInfo
{
    public required string PluginName { get; init; }
    public required string FunctionName { get; init; }
    public Dictionary<string, object>? Arguments { get; init; }
    public double ExecutionTimeMs { get; init; }
}