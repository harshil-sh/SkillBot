using Microsoft.EntityFrameworkCore;
using SkillBot.Core.Models;
using SkillBot.Infrastructure.Data;

namespace SkillBot.Tests.Unit.Helpers;

public static class TestDbContextHelper
{
    public static SkillBotDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SkillBotDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SkillBotDbContext(options);
    }

    public static async Task SeedTestDataAsync(SkillBotDbContext context)
    {
        var user = new User
        {
            Id = "test-user-1",
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            OpenAiApiKey = "sk-test-key",
            PreferredProvider = "openai"
        };
        context.Users.Add(user);

        var conversation = new Conversation
        {
            Id = "conv-1",
            UserId = "test-user-1",
            ConversationId = "session-1",
            Message = "Hello",
            Response = "Hi there!",
            TokensUsed = 20,
            CreatedAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);

        await context.SaveChangesAsync();
    }
}
