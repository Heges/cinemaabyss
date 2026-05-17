using events.Models;
using System.Text;
using System.Threading.Channels;

namespace events.Services.Kafka.Background
{
    public class EventMessageProcessor : BackgroundService
    {
        private readonly Channel<KafkaEnvelope> _mainEventChannel;
        private readonly Channel<KafkaWriteResult> _kafkaResultChannel;
        private readonly ILogger<EventMessageProcessor> _logger;

        public EventMessageProcessor(
            Channel<KafkaEnvelope> mainEventChannel,
            Channel<KafkaWriteResult> kafkaResultChannel,
            ILogger<EventMessageProcessor> logger)
        {
            _mainEventChannel = mainEventChannel;
            _kafkaResultChannel = kafkaResultChannel;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Kafka event processor started");

            await foreach (var envelope in _mainEventChannel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    var payload = Encoding.UTF8.GetString(envelope.PayloadBytes);

                    _logger.LogInformation(
                        "Kafka event processed. Topic={Topic} Partition={Partition} Offset={Offset} Payload={Payload}",
                        envelope.Topic,
                        envelope.Partition,
                        envelope.Offset,
                        payload);

                    await _kafkaResultChannel.Writer.WriteAsync(
                        new KafkaWriteResult(envelope.TopicPartition, envelope.Offset),
                        ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(
                        ex,
                        "Kafka event processing failed. Topic={Topic} Partition={Partition} Offset={Offset}",
                        envelope.Topic,
                        envelope.Partition,
                        envelope.Offset);
                }
            }
        }
    }
}
