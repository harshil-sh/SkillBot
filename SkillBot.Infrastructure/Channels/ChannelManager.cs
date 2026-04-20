using System.Collections.Concurrent;
using SkillBot.Core.Services;

namespace SkillBot.Infrastructure.Channels;

public class ChannelManager : IChannelManager
{
    private readonly ConcurrentDictionary<string, IMessagingChannel> _channels = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterChannel(IMessagingChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channels[channel.Name] = channel;
    }

    public IMessagingChannel? GetChannel(string name)
    {
        _channels.TryGetValue(name, out var channel);
        return channel;
    }

    public IEnumerable<IMessagingChannel> GetAllChannels() => _channels.Values;

    public IEnumerable<IMessagingChannel> GetEnabledChannels() =>
        _channels.Values.Where(c => c.IsConfigured);
}
