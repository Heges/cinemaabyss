namespace events.Services.Kafka
{
    public interface IEventKafkaConsumerFactory<TValue>
    {
        IEventKafkaConsumer<TValue> Create();
    }
}
