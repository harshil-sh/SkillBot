namespace SkillBot.Core.Exceptions;

/// <summary>
/// Thrown when the execution engine encounters an error.
/// </summary>
public class ExecutionException : AgentException
{
    public ExecutionException(string message) : base(message) { }
    
    public ExecutionException(string message, Exception innerException)
        : base(message, innerException) { }
}