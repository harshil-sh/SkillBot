using Microsoft.EntityFrameworkCore;
using SkillBot.Core.Models;
using SkillBot.Infrastructure.Data;

namespace SkillBot.Infrastructure.Repositories;

public class ChannelUserRepository : IChannelUserRepository
{
    private readonly SkillBotDbContext _context;

    public ChannelUserRepository(SkillBotDbContext context)
    {
        _context = context;
    }

    public async Task<ChannelUser?> GetByChannelIdAsync(string channelName, string channelUserId) =>
        await _context.ChannelUsers
            .FirstOrDefaultAsync(cu =>
                cu.ChannelName == channelName &&
                cu.ChannelUserId == channelUserId);

    public async Task<List<ChannelUser>> GetBySystemUserIdAsync(string systemUserId) =>
        await _context.ChannelUsers
            .Where(cu => cu.SystemUserId == systemUserId)
            .ToListAsync();

    public async Task<ChannelUser> CreateAsync(ChannelUser channelUser)
    {
        _context.ChannelUsers.Add(channelUser);
        await _context.SaveChangesAsync();
        return channelUser;
    }

    public async Task<bool> ExistsAsync(string channelName, string channelUserId) =>
        await _context.ChannelUsers
            .AnyAsync(cu =>
                cu.ChannelName == channelName &&
                cu.ChannelUserId == channelUserId);
}
