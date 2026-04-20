using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.Repositories;

public interface IChannelUserRepository
{
    Task<ChannelUser?> GetByChannelIdAsync(string channelName, string channelUserId);
    Task<List<ChannelUser>> GetBySystemUserIdAsync(string systemUserId);
    Task<ChannelUser> CreateAsync(ChannelUser channelUser);
    Task<bool> ExistsAsync(string channelName, string channelUserId);
}
