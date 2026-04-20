namespace SkillBot.Console.Services;

public interface IConsolePluginService
{
    Task<string> GetPluginsAsync();
    Task<string> GetPluginAsync(string pluginName);
}
