namespace SkillBot.Console.Services;

public interface IConsoleSearchService
{
    Task<string> SearchWebAsync(string query, int count = 5);
    Task<string> SearchNewsAsync(string query, int count = 5);
}
