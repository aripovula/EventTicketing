using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace EventTicketing.Api.Messaging;

/// <summary>
/// Publishes messages to RabbitMQ queues. Creates a short-lived connection per
/// publish so callers are isolated from connection-level failures and there is no
/// shared mutable state to manage.
/// </summary>
public class RabbitMqPublisher(IConnectionFactory connectionFactory, ILogger<RabbitMqPublisher> logger)
    : IMessagePublisher
{
    public async Task PublishAsync<T>(T message, string queueName, CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // Publishing is best-effort — a RabbitMQ outage must not fail the booking.
            logger.LogError(ex, "Failed to publish {MessageType} to queue '{Queue}'",
                typeof(T).Name, queueName);
        }
    }

    public async Task PublishRawAsync(string json, string queueName, CancellationToken ct = default)
    {
        // No try/catch — the OutboxWorker catches exceptions and records them on the message.
        await using var connection = await connectionFactory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            body: body,
            cancellationToken: ct);
    }
}
