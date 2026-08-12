namespace EventTicketing.Api.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string queueName, CancellationToken ct = default);

    /// <summary>
    /// Publishes a pre-serialized JSON payload directly to the specified queue.
    /// Used by <see cref="OutboxWorker"/> to forward stored outbox messages without
    /// deserializing and re-serializing the payload. Unlike <see cref="PublishAsync{T}"/>,
    /// exceptions are not swallowed — the worker decides how to handle failures.
    /// </summary>
    Task PublishRawAsync(string json, string queueName, CancellationToken ct = default);
}
