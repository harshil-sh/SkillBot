using SkillBot.Api.Models.Auth;
using SkillBot.Core.Models;

namespace SkillBot.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<User?> GetCurrentUserAsync(string userId);
}
