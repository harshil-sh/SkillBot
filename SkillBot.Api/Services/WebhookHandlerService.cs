using SkillBot.Core.Interfaces;
using SkillBot.Core.Models;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Channels;
using SkillBot.Infrastructure.Repositories;

namespace SkillBot.Api.Services;

public class WebhookHandlerService : IWebhookHandlerService
{
    private readonly IChannelManager _channelManager;
    private readonly IAgentEngine _agentEngine;
    private readonly IChannelUserRepository _channelUserRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<WebhookHandlerService> _logger;

    public WebhookHandlerService(
        IChannelManager channelManager,
        IAgentEngine agentEngine,
        IChannelUserRepository channelUserRepository,
        IUserRepository userRepository,
        ILogger<WebhookHandlerService> logger)
    {
        _channelManager = channelManager ?? throw new ArgumentNullException(nameof(channelManager));
        _agentEngine = agentEngine ?? throw new ArgumentNullException(nameof(agentEngine));
        _channelUserRepository = channelUserRepository ?? throw new ArgumentNullException(nameof(channelUserRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleMessageAsync(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation(
            "Handling webhook message {MessageId} from {UserId} on {Channel}",
            message.Id, message.UserId, message.ChannelName);

        var channel = _channelManager.GetChannel(message.ChannelName);
        if (channel is null)
        {
            _logger.LogWarning(
                "No channel registered for {ChannelName}; dropping message {MessageId}",
                message.ChannelName, message.Id);
            return;
        }

        var systemUser = await ResolveOrRegisterUserAsync(message.UserId, message.ChannelName);

        _logger.LogDebug(
            "Resolved system user {SystemUserId} for channel user {ChannelUserId}",
            systemUser.Id, message.UserId);

        var response = await _agentEngine.ExecuteAsync(message.Text);

        var sent = await channel.SendMessageAsync(message.UserId, response.Content);
        if (!sent)
        {
            _logger.LogWarning(
                "Failed to deliver reply to channel user {ChannelUserId} on {Channel}",
                message.UserId, message.ChannelName);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<User> ResolveOrRegisterUserAsync(string channelUserId, string channelName)
    {
        var mapping = await _channelUserRepository.GetByChannelIdAsync(channelName, channelUserId);

        if (mapping is not null)
        {
            var existing = await _userRepository.GetByIdAsync(mapping.SystemUserId);
            if (existing is not null)
                return existing;

            _logger.LogWarning(
                "ChannelUser mapping found for {ChannelUserId} but system user {SystemUserId} no longer exists; re-registering",
                channelUserId, mapping.SystemUserId);
        }

        return await RegisterChannelUserAsync(channelUserId, channelName);
    }

    private async Task<User> RegisterChannelUserAsync(string channelUserId, string channelName)
    {
        var userId = Guid.NewGuid().ToString();

        var newUser = new User
        {
            Id = userId,
            Email = $"{channelName}.{channelUserId}@channel.skillbot.local",
            PasswordHash = string.Empty,
            Username = $"{channelName}_{channelUserId}",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var createdUser = await _userRepository.CreateAsync(newUser);

        var channelUser = new ChannelUser
        {
            Id = Guid.NewGuid().ToString(),
            SystemUserId = createdUser.Id,
            ChannelName = channelName,
            ChannelUserId = channelUserId,
            RegisteredAt = DateTime.UtcNow
        };

        await _channelUserRepository.CreateAsync(channelUser);

        _logger.LogInformation(
            "Auto-registered channel user {ChannelUserId} on {Channel} as system user {SystemUserId}",
            channelUserId, channelName, createdUser.Id);

        return createdUser;
    }
}
