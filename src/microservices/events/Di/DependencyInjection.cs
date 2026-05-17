using Confluent.Kafka;
using events.Models;
using events.Services.Kafka;
using events.Services.Kafka.Background;
using System.Threading.Channels;

namespace events.Di
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddEventContracts(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IEventKafkaProducer, EventKafkaProducer>();
            services.AddSingleton<IEventRegistry, EventRegistry>();

            services.AddSingleton(Channel.CreateBounded<Event>(
                new BoundedChannelOptions(150_000)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                }));

            services.AddSingleton(Channel.CreateBounded<KafkaEnvelope>(
                new BoundedChannelOptions(150_000)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = false,
                    SingleWriter = true
                }));

            services.AddSingleton(Channel.CreateUnbounded<KafkaWriteResult>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                }));

            services.AddSingleton<CommitManager>();
            services.AddSingleton<IEventKafkaConsumerFactory<byte[]>, EventKafkaConsumerFactory>(provider =>
            {
                var kafkaConfig = provider.GetRequiredService<IConfiguration>();
                var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BROKERS")
                    ?? kafkaConfig["Kafka:ServerUrl"]
                    ?? "localhost:9092";

                var groupId = kafkaConfig["Kafka:GroupId"] ?? "cinemaabyss-events-service";

                var config = new ConsumerConfig
                {
                    BootstrapServers = bootstrapServers,
                    GroupId = groupId,
                    EnableAutoCommit = false,
                    EnableAutoOffsetStore = false,
                    AutoOffsetReset = AutoOffsetReset.Earliest
                };

                return new EventKafkaConsumerFactory(
                    config,
                    provider.GetRequiredService<CommitManager>(),
                    kafkaConfig);
            });

            services.AddHostedService<EventMessageConsumer>();
            services.AddHostedService<EventMessageProcessor>();
            services.AddHostedService<EventMessageProducer>();

            return services;
        }
    }
}
