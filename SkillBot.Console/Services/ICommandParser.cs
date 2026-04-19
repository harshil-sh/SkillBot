namespace SkillBot.Console.Services;

public interface ICommandParser
{
    Task<CommandResult> ParseAsync(string input);
}

public class CommandResult
{
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, string> Arguments { get; set; } = new();
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}
