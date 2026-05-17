using Confluent.Kafka;

namespace events.Services.Kafka
{
    public interface IEventKafkaConsumer<TValue> : IDisposable
    {
        List<TopicPartition> Assignment { get; }
        void Subscribe(IEnumerable<string> topics);
        ConsumeResult<Ignore, TValue> Consume(CancellationToken ct);
        ConsumeResult<Ignore, TValue>? Consume(TimeSpan timeout);
        void Commit(IEnumerable<TopicPartitionOffset> offsets);
        Offset Position(TopicPartition partition);
    }
}
