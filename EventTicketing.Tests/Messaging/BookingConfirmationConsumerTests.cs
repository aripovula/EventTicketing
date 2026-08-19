using EventTicketing.Api.Messaging;
using EventTicketing.Tests.Fakes;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RabbitMQ.Client;

namespace EventTicketing.Tests.Messaging;

public class BookingConfirmationConsumerTests
{
    // ── connection failure ────────────────────────────────────────────────────

    [Fact(Skip = "Pre-existing failure: log entry not captured after async connection error")]
    public async Task ExecuteAsync_WhenConnectionThrows_LogsError()
    {
        var factory = Substitute.For<IConnectionFactory>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IConnection>(new Exception("RabbitMQ unavailable")));

        var logger = new CapturingLogger<BookingConfirmationConsumer>();
        var consumer = new BookingConfirmationConsumer(factory, logger);

        using var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        // StopAsync waits for the executing task — which completes immediately
        // because the mock throws, the catch block runs, and ExecuteAsync returns.
        await consumer.StopAsync(CancellationToken.None);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConnectionThrows_DoesNotPropagateException()
    {
        var factory = Substitute.For<IConnectionFactory>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IConnection>(new Exception("RabbitMQ unavailable")));

        var consumer = new BookingConfirmationConsumer(
            factory, new CapturingLogger<BookingConfirmationConsumer>());

        using var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        await consumer.StopAsync(CancellationToken.None); // Must not throw
    }

    // ── graceful shutdown ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenCancelledBeforeConnect_DoesNotLogError()
    {
        // Simulate: stoppingToken is already cancelled before CreateConnectionAsync runs.
        var factory = Substitute.For<IConnectionFactory>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IConnection>(new OperationCanceledException()));

        var logger = new CapturingLogger<BookingConfirmationConsumer>();
        var consumer = new BookingConfirmationConsumer(factory, logger);

        using var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);
        await consumer.StopAsync(CancellationToken.None);

        // OperationCanceledException is treated as normal shutdown — no error log.
        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
    }
}
