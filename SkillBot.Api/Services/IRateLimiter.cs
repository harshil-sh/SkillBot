namespace SkillBot.Api.Services;

public interface IRateLimiter
{
    Task<RateLimitResult> CheckRateLimitAsync(string userId, string endpoint);
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RemainingRequests { get; set; }
    public TimeSpan RetryAfter { get; set; }
}
