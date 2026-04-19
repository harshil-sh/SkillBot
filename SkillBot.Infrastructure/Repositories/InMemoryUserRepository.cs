using System.Collections.Concurrent;
using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new();

    public Task<User?> GetByIdAsync(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var user = _users.Values.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailOrUsernameAsync(string identifier)
    {
        var user = _users.Values.FirstOrDefault(u =>
            u.Email.Equals(identifier, StringComparison.OrdinalIgnoreCase)
            || u.Username.Equals(identifier, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }

    public Task<User> CreateAsync(User user)
    {
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user)
    {
        if (!_users.ContainsKey(user.Id))
            throw new KeyNotFoundException($"User {user.Id} not found.");
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<bool> ExistsAsync(string email)
    {
        var exists = _users.Values.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }
}
