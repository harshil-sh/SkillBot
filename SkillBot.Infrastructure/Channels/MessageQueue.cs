using System.Collections.Concurrent;
using SkillBot.Core.Services;

namespace SkillBot.Infrastructure.Channels;

public class MessageQueue
{
    private readonly ConcurrentQueue<Message> _queue = new();

    public void Enqueue(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _queue.Enqueue(message);
    }

    public Message? Dequeue() =>
        _queue.TryDequeue(out var message) ? message : null;

    public int Count => _queue.Count;
}
