using Confluent.Kafka;

namespace events.Services.Kafka
{
    public interface IEventKafkaProducer
    {
        Task<DeliveryResult<string, byte[]>> ProduceAsync(string topic, string key, byte[] payload, CancellationToken ct);
        void Flush(TimeSpan timeout);
    }
}
