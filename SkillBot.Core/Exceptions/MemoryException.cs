namespace SkillBot.Core.Exceptions;

/// <summary>
/// Thrown when memory operations fail.
/// </summary>
public class MemoryException : AgentException
{
    public MemoryException(string message) : base(message) { }
    
    public MemoryException(string message, Exception innerException)
        : base(message, innerException) { }
}