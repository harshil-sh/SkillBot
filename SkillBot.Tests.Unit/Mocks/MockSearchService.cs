namespace SkillBot.Tests.Unit.Mocks;

/// <summary>
/// Mock search service that wraps SerpApiPlugin behavior without making real API calls.
/// </summary>
public class MockSearchService
{
    public List<string> SearchCalls { get; } = new();

    public Task<string> SearchAsync(string query)
    {
        SearchCalls.Add(query);
        return Task.FromResult($"Mock search result for: {query}");
    }
}
