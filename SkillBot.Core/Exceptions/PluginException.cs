namespace SkillBot.Core.Exceptions;

/// <summary>
/// Thrown when plugin registration or execution fails.
/// </summary>
public class PluginException : AgentException
{
    public string? PluginName { get; init; }
    
    public PluginException(string message, string? pluginName = null) 
        : base(message)
    {
        PluginName = pluginName;
    }
    
    public PluginException(string message, Exception innerException, string? pluginName = null)
        : base(message, innerException)
    {
        PluginName = pluginName;
    }
}