/// <summary>
/// Parameter information
/// </summary>
public class ParameterInfo
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Description { get; init; }
    public bool IsRequired { get; init; }
}