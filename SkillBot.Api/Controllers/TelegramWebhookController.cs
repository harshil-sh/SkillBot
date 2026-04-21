using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using SkillBot.Api.Services;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Channels;
using CoreMessage = SkillBot.Core.Services.Message;

namespace SkillBot.Api.Controllers;

/// <summary>
/// Receives incoming webhook updates from the Telegram Bot API.
/// </summary>
[ApiController]
[Route("api/webhook/telegram")]
[AllowAnonymous]
public class TelegramWebhookController : ControllerBase
{
    private readonly IWebhookHandlerService _webhookHandler;
    private readonly IChannelManager _channelManager;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        IWebhookHandlerService webhookHandler,
        IChannelManager channelManager,
        ILogger<TelegramWebhookController> logger)
    {
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        _channelManager = channelManager ?? throw new ArgumentNullException(nameof(channelManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles an incoming Telegram Update posted by the Bot API.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HandleUpdate([FromBody] Update update)
    {
        if (update?.Message is null)
        {
            _logger.LogDebug("Received Telegram update with no message; ignoring.");
            return Ok();
        }

        var telegramMessage = update.Message;
        if (string.IsNullOrWhiteSpace(telegramMessage.Text))
        {
            _logger.LogDebug("Received Telegram message with no text from chat {ChatId}; ignoring.", telegramMessage.Chat.Id);
            return Ok();
        }

        var chatId = telegramMessage.Chat.Id.ToString();
        var text = telegramMessage.Text;

        _logger.LogInformation("Telegram webhook: chat {ChatId}, text length {Length}", chatId, text.Length);

        // Route commands directly to the channel; chat messages go through the agent engine
        if (text.StartsWith('/'))
        {
            var channel = _channelManager.GetChannel("telegram") as TelegramChannel;
            if (channel is not null)
            {
                var spaceIndex = text.IndexOf(' ');
                var command  = spaceIndex > 0 ? text[..spaceIndex] : text;
                var arguments = spaceIndex > 0 ? text[(spaceIndex + 1)..] : null;

                var handled = await channel.HandleCommandAsync(chatId, command, arguments);
                if (handled)
                    return Ok();
            }
        }

        var message = new CoreMessage
        {
            Id          = Guid.NewGuid().ToString(),
            UserId      = chatId,
            ChannelName = "telegram",
            Text        = text,
            ReceivedAt  = DateTime.UtcNow
        };

        await _webhookHandler.HandleMessageAsync(message);
        return Ok();
    }
}
