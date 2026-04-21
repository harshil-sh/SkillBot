using SkillBot.Core.Interfaces;
using SkillBot.Core.Models;

namespace SkillBot.Tests.Unit.Mocks;

public class MockAgentEngine : IAgentEngine
{
    public List<string> ExecuteCalls { get; } = new();

    public IExecutionContext Context { get; } = new MockExecutionContext();

    public Task<AgentResponse> ExecuteAsync(string message, CancellationToken cancellationToken = default)
    {
        ExecuteCalls.Add(message);
        return Task.FromResult(new AgentResponse
        {
            Content = $"Mock response for: {message}",
            IsSuccess = true
        });
    }

    public async IAsyncEnumerable<string> ExecuteStreamingAsync(
        string message,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ExecuteCalls.Add(message);
        yield return $"Mock response for: {message}";
        await Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private class MockExecutionContext : IExecutionContext
    {
        public string SessionId { get; } = "mock-session";
        public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
        public int TurnCount { get; } = 0;
        public int ToolCallCount { get; } = 0;
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();
    }
}
