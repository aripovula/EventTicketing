using EventTicketing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Messaging;

/// <summary>
/// Background service that polls the <c>OutboxMessages</c> table and dispatches
/// pending messages to RabbitMQ. Runs every 5 seconds.
///
/// This guarantees at-least-once delivery: if the app crashes after a booking is
/// saved but before the message is sent, the worker picks it up on the next poll
/// after restart. Messages that fail to publish are marked with an <c>Error</c>
/// field and are not retried automatically.
/// </summary>
public class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher publisher,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingAsync(stoppingToken);
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Fetches up to 20 unprocessed messages and forwards each one to RabbitMQ.
    /// Made internal so unit tests can invoke a single dispatch cycle directly
    /// without waiting for the poll interval.
    /// </summary>
    internal async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Error == null)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishRawAsync(message.Payload, message.QueueName, ct);
                message.ProcessedAt = DateTime.UtcNow;
                logger.LogInformation(
                    "Dispatched outbox message {Id} ({Type}) to '{Queue}'",
                    message.Id, message.Type, message.QueueName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to dispatch outbox message {Id} ({Type}) to '{Queue}'",
                    message.Id, message.Type, message.QueueName);
                message.Error = ex.Message;
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
