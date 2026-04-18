namespace SkillBot.Api.Services;

public interface IContentSafetyService
{
    Task<SafetyResult> CheckContentAsync(string content);
}

public class SafetyResult
{
    public bool IsSafe { get; set; }
    public string? Reason { get; set; }
    public SafetyCategory Category { get; set; }
}

public enum SafetyCategory
{
    Safe,
    PII,
    Toxic,
    Inappropriate
}
