using System.Text.RegularExpressions;

namespace SkillBot.Api.Services;

public class ContentSafetyService : IContentSafetyService
{
    private static readonly Regex EmailPattern = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"\b(\+?1[-.]?)?\(?([0-9]{3})\)?[-.]?([0-9]{3})[-.]?([0-9]{4})\b", RegexOptions.Compiled);
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b|\b\d{9}\b", RegexOptions.Compiled);

    private static readonly string[] ToxicKeywords =
    {
        "hate", "kill", "violent", "attack", "abuse",
        "threat", "harm", "racist", "offensive", "explicit"
    };

    private static readonly string[] PromptInjectionPatterns =
    {
        "ignore previous instructions",
        "system:",
        "ignore all previous",
        "disregard previous",
        "forget previous"
    };

    public Task<SafetyResult> CheckContentAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(new SafetyResult
            {
                IsSafe = true,
                Category = SafetyCategory.Safe
            });
        }

        if (EmailPattern.IsMatch(content))
        {
            return Task.FromResult(new SafetyResult
            {
                IsSafe = false,
                Reason = "Content contains email address (PII)",
                Category = SafetyCategory.PII
            });
        }

        if (PhonePattern.IsMatch(content))
        {
            return Task.FromResult(new SafetyResult
            {
                IsSafe = false,
                Reason = "Content contains phone number (PII)",
                Category = SafetyCategory.PII
            });
        }

        if (SsnPattern.IsMatch(content))
        {
            return Task.FromResult(new SafetyResult
            {
                IsSafe = false,
                Reason = "Content contains SSN pattern (PII)",
                Category = SafetyCategory.PII
            });
        }

        var lowerContent = content.ToLowerInvariant();

        foreach (var keyword in ToxicKeywords)
        {
            if (lowerContent.Contains(keyword))
            {
                return Task.FromResult(new SafetyResult
                {
                    IsSafe = false,
                    Reason = "Content contains toxic or inappropriate language",
                    Category = SafetyCategory.Toxic
                });
            }
        }

        foreach (var pattern in PromptInjectionPatterns)
        {
            if (lowerContent.Contains(pattern))
            {
                return Task.FromResult(new SafetyResult
                {
                    IsSafe = false,
                    Reason = "Content contains potential prompt injection attempt",
                    Category = SafetyCategory.Inappropriate
                });
            }
        }

        return Task.FromResult(new SafetyResult
        {
            IsSafe = true,
            Category = SafetyCategory.Safe
        });
    }
}
