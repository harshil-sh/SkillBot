using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string userId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailOrUsernameAsync(string identifier);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> ExistsAsync(string email);
}
