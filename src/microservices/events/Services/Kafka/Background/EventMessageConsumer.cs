using Confluent.Kafka;
using events.Models;
using System.Threading.Channels;

namespace events.Services.Kafka.Background
{
    public class EventMessageConsumer : BackgroundService
    {
        private readonly Channel<KafkaEnvelope> _mainEventChannel;
        private readonly Channel<KafkaWriteResult> _kafkaResultChannel;
        private readonly CommitManager _commitManager;
        private readonly IEventKafkaConsumerFactory<byte[]> _consumerFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventMessageConsumer> _logger;

        public EventMessageConsumer(
            Channel<KafkaEnvelope> mainEventChannel,
            Channel<KafkaWriteResult> kafkaResultChannel,
            CommitManager commitManager,
            IEventKafkaConsumerFactory<byte[]> consumerFactory,
            IConfiguration configuration,
            ILogger<EventMessageConsumer> logger)
        {
            _mainEventChannel = mainEventChannel;
            _kafkaResultChannel = kafkaResultChannel;
            _commitManager = commitManager;
            _consumerFactory = consumerFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await Task.Yield();

            var topics = new[]
            {
                _configuration["Kafka:Topics:Movie"] ?? "movie-events",
                _configuration["Kafka:Topics:User"] ?? "user-events",
                _configuration["Kafka:Topics:Payment"] ?? "payment-events"
            };

            var delayMs = GetPositiveInt(_configuration["Kafka:EventMessageConsumer:DelayMs"], 200);
            var retrySec = GetPositiveInt(_configuration["Kafka:EventMessageConsumer:RetryAttemptSec"], 2);

            using var consumer = _consumerFactory.Create();
            consumer.Subscribe(topics);

            _logger.LogInformation("Kafka consumer started. Topics={Topics}", string.Join(",", topics));

            while (!ct.IsCancellationRequested)
            {
                CommitCompletedOffsets(consumer);

                ConsumeResult<Ignore, byte[]>? result;
                try
                {
                    result = consumer.Consume(TimeSpan.FromMilliseconds(delayMs));
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning(
                        ex,
                        "Kafka topic is not ready. Reason={Reason}. Retry in {RetrySec}s",
                        ex.Error.Reason,
                        retrySec);

                    await Task.Delay(TimeSpan.FromSeconds(retrySec), ct);
                    continue;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Kafka consume error. Reason={Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(retrySec), ct);
                    continue;
                }

                if (result?.Message?.Value is null || result.IsPartitionEOF)
                {
                    continue;
                }

                await _mainEventChannel.Writer.WriteAsync(
                    new KafkaEnvelope(
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value,
                        result.Message.Value,
                        result.Message.Timestamp.UtcDateTime),
                    ct);
            }
        }

        private void CommitCompletedOffsets(IEventKafkaConsumer<byte[]> consumer)
        {
            while (_kafkaResultChannel.Reader.TryRead(out var result))
            {
                var offsets = _commitManager.Ack(result);
                if (offsets.Count > 0)
                {
                    consumer.Commit(offsets);
                }
            }
        }

        private static int GetPositiveInt(string? value, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
        }
    }
}
