using SkillBot.Core.Services;

namespace SkillBot.Api.Services;

public interface IWebhookHandlerService
{
    Task HandleMessageAsync(Message message);
}
