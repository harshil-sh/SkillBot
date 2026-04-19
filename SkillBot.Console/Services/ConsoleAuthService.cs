namespace SkillBot.Console.Services;

public class ConsoleAuthService : IConsoleAuthService
{
    private readonly ApiClient _apiClient;
    private string? _token;

    public ConsoleAuthService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<string> RegisterAsync(string email, string password, string username)
    {
        var response = await _apiClient.PostAsync<AuthResponse>(
            "/api/auth/register",
            new { email, password, username });

        _token = response?.Token ?? throw new InvalidOperationException("Registration failed: no token returned.");
        _apiClient.SetAuthToken(_token);
        return _token;
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        var response = await _apiClient.PostAsync<AuthResponse>(
            "/api/auth/login",
            new { email, password });

        _token = response?.Token ?? throw new InvalidOperationException("Login failed: no token returned.");
        _apiClient.SetAuthToken(_token);
        return _token;
    }

    public Task LogoutAsync()
    {
        _token = null;
        _apiClient.SetAuthToken(string.Empty);
        return Task.CompletedTask;
    }

    public string? GetCurrentToken() => _token;

    private sealed class AuthResponse
    {
        public required string Token { get; set; }
        public required DateTime ExpiresAt { get; set; }
        public required string UserId { get; set; }
    }
}
