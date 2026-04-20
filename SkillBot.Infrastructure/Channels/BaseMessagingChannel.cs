using Microsoft.Extensions.Logging;
using SkillBot.Core.Models;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Repositories;

namespace SkillBot.Infrastructure.Channels;

/// <summary>
/// Shared base for all messaging channel implementations.
/// Handles user registration and lookup; concrete channels implement
/// <see cref="SendMessageAsync"/> and <see cref="ReceiveMessageAsync"/>.
/// </summary>
public abstract class BaseMessagingChannel : IMessagingChannel
{
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly IUserRepository _userRepository;
    protected readonly ILogger Logger;

    public abstract string Name { get; }
    public abstract bool IsConfigured { get; }

    protected BaseMessagingChannel(
        IChannelUserRepository channelUserRepository,
        IUserRepository userRepository,
        ILogger logger)
    {
        _channelUserRepository = channelUserRepository ?? throw new ArgumentNullException(nameof(channelUserRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract Task<bool> SendMessageAsync(string userId, string message);

    public abstract Task<Message?> ReceiveMessageAsync();

    /// <inheritdoc/>
    public async Task<bool> RegisterUserAsync(string channelUserId, string systemUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(systemUserId);

        if (await _channelUserRepository.ExistsAsync(Name, channelUserId))
        {
            Logger.LogDebug(
                "Channel user {ChannelUserId} already registered on {Channel}",
                channelUserId, Name);
            return false;
        }

        var channelUser = new ChannelUser
        {
            Id = Guid.NewGuid().ToString(),
            SystemUserId = systemUserId,
            ChannelName = Name,
            ChannelUserId = channelUserId,
            RegisteredAt = DateTime.UtcNow
        };

        await _channelUserRepository.CreateAsync(channelUser);

        Logger.LogInformation(
            "Registered channel user {ChannelUserId} on {Channel} -> system user {SystemUserId}",
            channelUserId, Name, systemUserId);

        return true;
    }

    /// <inheritdoc/>
    public async Task<User?> GetUserByChannelIdAsync(string channelUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelUserId);

        var channelUser = await _channelUserRepository.GetByChannelIdAsync(Name, channelUserId);
        if (channelUser is null)
            return null;

        return await _userRepository.GetByIdAsync(channelUser.SystemUserId);
    }

    // ── Protected helpers ────────────────────────────────────────────────────

    /// <summary>Returns true when the channel user is already mapped to a system account.</summary>
    protected Task<bool> IsUserRegisteredAsync(string channelUserId) =>
        _channelUserRepository.ExistsAsync(Name, channelUserId);

    /// <summary>Looks up the <see cref="ChannelUser"/> mapping without resolving the full system user.</summary>
    protected Task<ChannelUser?> GetChannelUserAsync(string channelUserId) =>
        _channelUserRepository.GetByChannelIdAsync(Name, channelUserId);
}
