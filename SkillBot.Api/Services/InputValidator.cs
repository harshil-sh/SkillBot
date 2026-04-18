namespace SkillBot.Api.Services;

public class InputValidator : IInputValidator
{
    private const int MaxLength = 10000;
    private static readonly string[] SqlPatterns = { "SELECT", "DROP", "DELETE", "UNION" };
    private static readonly string[] ScriptPatterns = { "<script", "javascript:" };

    public Task<ValidationResult> ValidateInputAsync(string input)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(input))
        {
            result.IsValid = false;
            result.ErrorMessage = "Input cannot be null or empty";
            return Task.FromResult(result);
        }

        if (input.Length > MaxLength)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Input exceeds maximum length of {MaxLength} characters";
            return Task.FromResult(result);
        }

        var upperInput = input.ToUpperInvariant();
        foreach (var pattern in SqlPatterns)
        {
            if (upperInput.Contains(pattern))
            {
                result.IsValid = false;
                result.ErrorMessage = "Input contains potentially malicious SQL patterns";
                return Task.FromResult(result);
            }
        }

        var lowerInput = input.ToLowerInvariant();
        foreach (var pattern in ScriptPatterns)
        {
            if (lowerInput.Contains(pattern))
            {
                result.IsValid = false;
                result.ErrorMessage = "Input contains potentially malicious script content";
                return Task.FromResult(result);
            }
        }

        return Task.FromResult(result);
    }
}
