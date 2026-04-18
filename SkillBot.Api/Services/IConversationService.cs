using Microsoft.Extensions.Caching.Memory;
using SkillBot.Api.Models.Responses;
using SkillBot.Core.Models;

namespace SkillBot.Api.Services;

/// <summary>
/// Interface for conversation management
/// </summary>
public interface IConversationService
{
    Task<string> CreateConversationAsync();
    Task<ConversationResponse?> GetConversationAsync(string conversationId);
    Task SaveMessageAsync(string conversationId, string role, string content);
    Task<bool> DeleteConversationAsync(string conversationId);
}