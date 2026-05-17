using Confluent.Kafka;
using events.Models;
using System.Text.Json;
using System.Threading.Channels;

namespace events.Services.Kafka.Background
{
    public class EventMessageProducer : BackgroundService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly Channel<Event> _producerChannel;
        private readonly IEventKafkaProducer _producer;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventMessageProducer> _logger;
        private readonly TimeSpan _flushTimeout;

        public EventMessageProducer(
            Channel<Event> producerChannel,
            IEventKafkaProducer producer,
            IConfiguration configuration,
            ILogger<EventMessageProducer> logger)
        {
            _producerChannel = producerChannel;
            _producer = producer;
            _configuration = configuration;
            _logger = logger;

            var flushSeconds = int.TryParse(
                configuration["Kafka:EventMessageProducer:FlushTimeFromSeconds"],
                out var parsedSeconds) && parsedSeconds > 0
                    ? parsedSeconds
                    : 5;

            _flushTimeout = TimeSpan.FromSeconds(flushSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Kafka event producer started");

            await foreach (var evt in _producerChannel.Reader.ReadAllAsync(ct))
            {
                var topic = ResolveTopic(evt.Type);
                var key = evt.Id.ToString("N");
                var payload = JsonSerializer.SerializeToUtf8Bytes(evt, JsonOptions);

                try
                {
                    var deliveryResult = await _producer.ProduceAsync(topic, key, payload, ct);

                    _logger.LogInformation(
                        "Kafka event produced. Type={EventType} EventId={EventId} Topic={Topic} Partition={Partition} Offset={Offset}",
                        evt.Type,
                        evt.Id,
                        topic,
                        deliveryResult.Partition.Value,
                        deliveryResult.Offset.Value);
                }
                catch (ProduceException<string, byte[]> ex)
                {
                    _logger.LogError(
                        ex,
                        "Kafka event was not produced. Type={EventType} EventId={EventId} Topic={Topic} Reason={Reason}",
                        evt.Type,
                        evt.Id,
                        topic,
                        ex.Error.Reason);
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Kafka event producer stopping");
            _producer.Flush(_flushTimeout);
            return base.StopAsync(cancellationToken);
        }

        private string ResolveTopic(string eventType)
        {
            return eventType.ToLowerInvariant() switch
            {
                "movie" => _configuration["Kafka:Topics:Movie"] ?? "movie-events",
                "user" => _configuration["Kafka:Topics:User"] ?? "user-events",
                "payment" => _configuration["Kafka:Topics:Payment"] ?? "payment-events",
                _ => _configuration["Kafka:Topics:Default"] ?? "movie-events"
            };
        }
    }
}
