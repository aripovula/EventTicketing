using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventTicketing.Api.Messaging;

/// <summary>
/// Long-running background service that consumes <see cref="BookingConfirmedMessage"/>
/// messages from RabbitMQ and processes them (currently: structured logging; extend here
/// to send confirmation emails, trigger downstream services, etc.).
/// </summary>
public class BookingConfirmationConsumer(
    IConnectionFactory connectionFactory,
    ILogger<BookingConfirmationConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: BookingConfirmedMessage.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var msg = JsonSerializer.Deserialize<BookingConfirmedMessage>(json);

                    if (msg is not null)
                    {
                        logger.LogInformation(
                            "BookingConfirmed: OrderId={OrderId} EventId={EventId} " +
                            "Event={EventTitle} Email={Email} Price={Price:C} BookedAt={BookedAt:O}",
                            msg.OrderId, msg.EventId, msg.EventTitle, msg.Email,
                            msg.Price, msg.BookedAt);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing BookingConfirmedMessage");
                }
                finally
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: BookingConfirmedMessage.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            // Keep the consumer alive until the host signals shutdown.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal graceful shutdown — nothing to log.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BookingConfirmationConsumer encountered a fatal error and stopped");
        }
    }
}
