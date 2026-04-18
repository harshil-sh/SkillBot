namespace SkillBot.Api.Services;

public interface IInputValidator
{
    Task<ValidationResult> ValidateInputAsync(string input);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
}
