namespace SkillBot.Core.Exceptions;

/// <summary>
/// Base exception for all SkillBot-related errors.
/// </summary>
public class AgentException : Exception
{
    public AgentException(string message) : base(message) { }
    
    public AgentException(string message, Exception innerException) 
        : base(message, innerException) { }
}