using Confluent.Kafka;

namespace events.Models
{
    public class KafkaEnvelope
    {
        public string Topic { get; }
        public int Partition { get; }
        public long Offset { get; }
        public byte[] PayloadBytes { get; }
        public DateTimeOffset Timestamp { get; }

        public TopicPartition TopicPartition => new(Topic, new Partition(Partition));

        public KafkaEnvelope(string topic, int partition, long offset, byte[] payloadBytes, DateTimeOffset timestamp)
        {
            Topic = topic;
            Partition = partition;
            Offset = offset;
            PayloadBytes = payloadBytes;
            Timestamp = timestamp;
        }
    }
}
