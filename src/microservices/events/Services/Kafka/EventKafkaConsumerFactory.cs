using Confluent.Kafka;

namespace events.Services.Kafka
{
    public class EventKafkaConsumerFactory : IEventKafkaConsumerFactory<byte[]>
    {
        private readonly ConsumerConfig _config;
        private readonly CommitManager _commitManager;
        private readonly TimeSpan _committedOffsetsTimeout;

        public EventKafkaConsumerFactory(ConsumerConfig config, CommitManager commitManager, IConfiguration configuration)
        {
            _config = config;
            _commitManager = commitManager;

            var value = configuration["Kafka:EventKafkaConsumer:CommitOffsetTimeoutMs"];
            if (!int.TryParse(value, out var parsedMs) || parsedMs <= 0 || parsedMs > 100_000)
            {
                parsedMs = 5000;
            }

            _committedOffsetsTimeout = TimeSpan.FromMilliseconds(parsedMs);
        }

        public IEventKafkaConsumer<byte[]> Create()
        {
            var consumer = new ConsumerBuilder<Ignore, byte[]>(_config)
                .SetValueDeserializer(Deserializers.ByteArray)
                .SetPartitionsAssignedHandler((consumer, partitions) =>
                {
                    var committed = consumer
                        .Committed(partitions, _committedOffsetsTimeout)
                        .ToDictionary(x => x.TopicPartition, x => x.Offset.Value);

                    long GetInitialNextToCommit(TopicPartition topicPartition)
                    {
                        if (committed.TryGetValue(topicPartition, out var committedOffset) && committedOffset >= 0)
                        {
                            return committedOffset;
                        }

                        var position = consumer.Position(topicPartition).Value;
                        return position >= 0 ? position : 0;
                    }

                    _commitManager.OnAssigned(partitions, GetInitialNextToCommit);
                })
                .SetPartitionsRevokedHandler((_, partitionOffsets) =>
                {
                    _commitManager.OnRevoked(partitionOffsets.Select(x => x.TopicPartition));
                })
                .Build();

            return new EventKafkaConsumer(consumer);
        }
    }
}
