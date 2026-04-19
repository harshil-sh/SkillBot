using SkillBot.Api.Models.Settings;

namespace SkillBot.Api.Services;

public interface IUserSettingsService
{
    Task<UserSettingsResponse> GetSettingsAsync(string userId);
    Task UpdateApiKeyAsync(string userId, string provider, string apiKey);
    Task UpdateProviderAsync(string userId, string provider);
}
