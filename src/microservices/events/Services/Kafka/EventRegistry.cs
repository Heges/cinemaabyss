using events.Models;
using System.Threading.Channels;

namespace events.Services.Kafka
{
    public class EventRegistry : IEventRegistry
    {
        private readonly Channel<Event> _eventChannel;
        private readonly ILogger<EventRegistry> _logger;

        public EventRegistry(Channel<Event> eventChannel, ILogger<EventRegistry> logger)
        {
            _eventChannel = eventChannel;
            _logger = logger;
        }

        public async ValueTask<Event> RegisterAsync(string eventType, object payload, CancellationToken ct)
        {
            var evt = new Event
            {
                Id = Guid.NewGuid(),
                Type = eventType,
                Timestamp = DateTimeOffset.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Payload = payload,
                Status = "queued"
            };

            await _eventChannel.Writer.WriteAsync(evt, ct);

            _logger.LogInformation(
                "Event queued for Kafka producer. Type={EventType} EventId={EventId}",
                eventType,
                evt.Id);

            return evt;
        }
    }
}
