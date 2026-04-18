/// <summary>
/// Function information
/// </summary>
public class FunctionInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<ParameterInfo> Parameters { get; init; } = new();
}