namespace SkillBot.Console.Services;

public interface IConsoleAuthService
{
    Task<string> RegisterAsync(string email, string password, string username);
    Task<string> LoginAsync(string email, string password);
    Task LogoutAsync();
    string? GetCurrentToken();
}
