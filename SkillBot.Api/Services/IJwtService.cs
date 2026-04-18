using System.Security.Claims;
using SkillBot.Core.Models;

namespace SkillBot.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
}
