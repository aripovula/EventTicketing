namespace EventTicketing.Api.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string queueName, CancellationToken ct = default);
}
