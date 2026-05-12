using events.Models;

namespace events.Services.Kafka
{
    public interface IEventRegistry
    {
        ValueTask<Event> RegisterAsync(string eventType, object payload, CancellationToken ct);
    }
}
