using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.Repositories;

public interface IConversationRepository
{
    Task<Conversation> CreateAsync(Conversation conversation);
    Task<List<Conversation>> GetByUserIdAsync(string userId, int limit = 50);
    Task<List<Conversation>> GetByConversationIdAsync(string conversationId);
    Task<int> GetCountByUserIdAsync(string userId);
}
