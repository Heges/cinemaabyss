using Confluent.Kafka;

namespace events.Models
{
    public class KafkaWriteResult
    {
        public TopicPartition TopicPartition { get; }
        public long Offset { get; }

        public KafkaWriteResult(TopicPartition topicPartition, long offset)
        {
            TopicPartition = topicPartition;
            Offset = offset;
        }
    }
}
