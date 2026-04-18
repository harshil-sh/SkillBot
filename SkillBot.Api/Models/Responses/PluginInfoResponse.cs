public class PluginInfoResponse
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<FunctionInfo> Functions { get; init; } = new();
}