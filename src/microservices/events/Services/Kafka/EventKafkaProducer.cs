using Confluent.Kafka;

namespace events.Services.Kafka
{
    public class EventKafkaProducer : IEventKafkaProducer, IDisposable
    {
        private readonly IProducer<string, byte[]> _producer;

        public EventKafkaProducer(IConfiguration configuration)
        {
            var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BROKERS")
                ?? configuration["Kafka:ServerUrl"]
                ?? "localhost:9092";

            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                LingerMs = GetInt(configuration["Kafka:EventMessageProducer:LingerMs"], 5),
                RetryBackoffMs = GetInt(configuration["Kafka:EventMessageProducer:RetryBackoffMs"], 200),
                CompressionType = CompressionType.Lz4
            };

            _producer = new ProducerBuilder<string, byte[]>(config)
                .SetKeySerializer(Serializers.Utf8)
                .SetValueSerializer(Serializers.ByteArray)
                .Build();
        }

        public Task<DeliveryResult<string, byte[]>> ProduceAsync(
            string topic,
            string key,
            byte[] payload,
            CancellationToken ct)
        {
            return _producer.ProduceAsync(topic, new Message<string, byte[]>
            {
                Key = key,
                Value = payload
            }, ct);
        }

        public void Flush(TimeSpan timeout)
        {
            _producer.Flush(timeout);
        }

        public void Dispose()
        {
            _producer.Dispose();
        }

        private static int GetInt(string? value, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : fallback;
        }
    }
}
