using Microsoft.EntityFrameworkCore;
using SkillBot.Core.Models;
using SkillBot.Infrastructure.Data;

namespace SkillBot.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly SkillBotDbContext _context;

    public ConversationRepository(SkillBotDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<List<Conversation>> GetByUserIdAsync(string userId, int limit = 50)
    {
        return await _context.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Conversation>> GetByConversationIdAsync(string conversationId)
    {
        return await _context.Conversations
            .Where(c => c.ConversationId == conversationId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(string userId)
    {
        return await _context.Conversations.CountAsync(c => c.UserId == userId);
    }
}
