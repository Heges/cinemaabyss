using Confluent.Kafka;

namespace events.Services.Kafka
{
    public class EventKafkaConsumer : IEventKafkaConsumer<byte[]>
    {
        private readonly IConsumer<Ignore, byte[]> _consumer;

        public List<TopicPartition> Assignment => _consumer.Assignment;

        public EventKafkaConsumer(IConsumer<Ignore, byte[]> consumer)
        {
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        }

        public void Subscribe(IEnumerable<string> topics)
        {
            _consumer.Subscribe(topics);
        }

        public ConsumeResult<Ignore, byte[]> Consume(CancellationToken ct)
        {
            return _consumer.Consume(ct);
        }

        public ConsumeResult<Ignore, byte[]>? Consume(TimeSpan timeout)
        {
            return _consumer.Consume(timeout);
        }

        public void Commit(IEnumerable<TopicPartitionOffset> offsets)
        {
            _consumer.Commit(offsets);
        }

        public Offset Position(TopicPartition partition)
        {
            return _consumer.Position(partition);
        }

        public void Dispose()
        {
            try
            {
                _consumer.Close();
            }
            catch
            {
            }

            _consumer.Dispose();
        }
    }
}
