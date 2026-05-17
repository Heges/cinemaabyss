using Confluent.Kafka;
using events.Models;
using System.Collections.Concurrent;

namespace events.Services.Kafka
{
    public class CommitManager
    {
        private readonly ConcurrentDictionary<TopicPartition, PartitionState> _states = new();

        public void OnAssigned(IEnumerable<TopicPartition> partitions, Func<TopicPartition, long> position)
        {
            foreach (var topicPartition in partitions)
            {
                _states[topicPartition] = new PartitionState
                {
                    NextToCommit = position(topicPartition)
                };
            }
        }

        public void OnRevoked(IEnumerable<TopicPartition> partitions)
        {
            foreach (var topicPartition in partitions)
            {
                _states.TryRemove(topicPartition, out _);
            }
        }

        public List<TopicPartitionOffset> Ack(KafkaWriteResult signal)
        {
            if (!_states.TryGetValue(signal.TopicPartition, out var state))
            {
                return [];
            }

            lock (state.Gate)
            {
                state.Completed.Add(signal.Offset);

                long last = -1;
                while (state.Completed.Contains(state.NextToCommit))
                {
                    state.Completed.Remove(state.NextToCommit);
                    last = state.NextToCommit;
                    state.NextToCommit++;
                }

                if (last == -1)
                {
                    return [];
                }

                return
                [
                    new TopicPartitionOffset(
                        signal.TopicPartition,
                        new Offset(last + 1))
                ];
            }
        }
    }
}
