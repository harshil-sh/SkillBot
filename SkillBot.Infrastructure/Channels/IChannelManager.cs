using SkillBot.Core.Services;

namespace SkillBot.Infrastructure.Channels;

public interface IChannelManager
{
    void RegisterChannel(IMessagingChannel channel);
    IMessagingChannel? GetChannel(string name);
    IEnumerable<IMessagingChannel> GetAllChannels();
    IEnumerable<IMessagingChannel> GetEnabledChannels();
}
