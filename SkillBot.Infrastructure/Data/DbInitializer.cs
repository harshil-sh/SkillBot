using Microsoft.EntityFrameworkCore;
using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(SkillBotDbContext context)
    {
        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();

        var hasUsers = await context.Users.AnyAsync();
        if (hasUsers)
        {
            return;
        }

        var adminUser = new User
        {
            Id = "admin",
            Email = "admin@skillbot.local",
            PasswordHash = "CHANGE_ME",
            Username = "admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            PreferredProvider = "openai"
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }
}
