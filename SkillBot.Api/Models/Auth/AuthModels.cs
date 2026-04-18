namespace SkillBot.Api.Models.Auth;

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class RegisterRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Username { get; set; }
}

public class AuthResponse
{
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public required string UserId { get; set; }
}
