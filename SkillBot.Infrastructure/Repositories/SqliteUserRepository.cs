using SkillBot.Core.Models;
using SkillBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SkillBot.Infrastructure.Repositories;

public class SqliteUserRepository : IUserRepository
{
    private readonly SkillBotDbContext _context;

    public SqliteUserRepository(SkillBotDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(string userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByEmailOrUsernameAsync(string identifier)
    {
        return await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == identifier || u.Username == identifier);
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        // Detach any already-tracked instance with the same key.
        // This happens when a record `with { ... }` expression creates a new object
        // from an entity that EF Core is still tracking from the same DbContext scope.
        var tracked = _context.ChangeTracker.Entries<User>()
            .FirstOrDefault(e => e.Entity.Id == user.Id);
        if (tracked is not null)
            tracked.State = EntityState.Detached;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}
