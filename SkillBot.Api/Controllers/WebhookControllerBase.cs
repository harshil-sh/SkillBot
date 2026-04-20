using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Interfaces;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Channels;

namespace SkillBot.Api.Controllers;

/// <summary>
/// Abstract base for channel webhook controllers.
/// Provides shared channel resolution and inbound message processing logic.
/// Concrete webhook controllers (e.g. Telegram, WhatsApp) inherit from this.
/// </summary>
[ApiController]
public abstract class WebhookControllerBase : ControllerBase
{
    protected IChannelManager ChannelManager { get; }
    protected ILogger Logger { get; }

    private readonly IAgentEngine _agentEngine;

    protected WebhookControllerBase(
        IChannelManager channelManager,
        IAgentEngine agentEngine,
        ILogger logger)
    {
        ChannelManager = channelManager ?? throw new ArgumentNullException(nameof(channelManager));
        _agentEngine = agentEngine ?? throw new ArgumentNullException(nameof(agentEngine));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves a registered channel by name; returns <c>null</c> if not found or not configured.
    /// </summary>
    protected IMessagingChannel? GetChannel(string name) => ChannelManager.GetChannel(name);

    /// <summary>
    /// Processes an inbound message through the agent engine and sends the reply back via the originating channel.
    /// </summary>
    protected async Task ProcessIncomingMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Logger.LogInformation(
            "Processing incoming message {MessageId} from {UserId} on {Channel}",
            message.Id, message.UserId, message.ChannelName);

        var channel = GetChannel(message.ChannelName);
        if (channel is null)
        {
            Logger.LogWarning(
                "No channel registered for {ChannelName}; dropping message {MessageId}",
                message.ChannelName, message.Id);
            return;
        }

        try
        {
            var response = await _agentEngine.ExecuteAsync(message.Text);

            var sent = await channel.SendMessageAsync(message.UserId, response.Content);
            if (!sent)
            {
                Logger.LogWarning(
                    "Failed to deliver reply to {UserId} on {Channel} for message {MessageId}",
                    message.UserId, message.ChannelName, message.Id);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error processing message {MessageId} from {UserId} on {Channel}",
                message.Id, message.UserId, message.ChannelName);
        }
    }
}
