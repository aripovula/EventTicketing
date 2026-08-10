using EventTicketing.Api.Messaging;
using EventTicketing.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;

namespace EventTicketing.Tests.Messaging;

public class RabbitMqPublisherTests
{
    // ── exception safety ──────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_WhenConnectionThrows_DoesNotPropagateException()
    {
        var factory = Substitute.For<IConnectionFactory>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IConnection>(new Exception("RabbitMQ unavailable")));

        var publisher = new RabbitMqPublisher(factory, NullLogger<RabbitMqPublisher>.Instance);

        // A RabbitMQ outage must never fail the caller (e.g. the booking endpoint).
        await publisher.PublishAsync(new { OrderId = 1 }, "test-queue");
    }

    [Fact]
    public async Task PublishAsync_WhenConnectionThrows_LogsErrorWithMessageTypeName()
    {
        var factory = Substitute.For<IConnectionFactory>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IConnection>(new Exception("RabbitMQ unavailable")));

        var logger = new CapturingLogger<RabbitMqPublisher>();
        var publisher = new RabbitMqPublisher(factory, logger);

        await publisher.PublishAsync(new { OrderId = 1 }, "my-queue");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("AnonymousType", entry.Message); // typeof(T).Name appears in the log template
        Assert.Contains("my-queue", entry.Message);
    }

    [Fact]
    public async Task PublishAsync_WhenChannelThrows_DoesNotPropagateException()
    {
        var channel = Substitute.For<IChannel>();
        channel.QueueDeclareAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(),
                Arg.Any<bool>(), Arg.Any<IDictionary<string, object?>>(),
                Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RabbitMQ.Client.QueueDeclareOk>(new Exception("Channel error")));

        var connection = Substitute.For<IConnection>();
        connection.CreateChannelAsync(Arg.Any<RabbitMQ.Client.CreateChannelOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(channel));

        var factory = Substitute.For<IConnectionFactory>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connection));

        var publisher = new RabbitMqPublisher(factory, NullLogger<RabbitMqPublisher>.Instance);

        await publisher.PublishAsync(new { OrderId = 1 }, "test-queue");
    }
}
