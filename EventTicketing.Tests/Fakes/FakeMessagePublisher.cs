using EventTicketing.Api.Messaging;

namespace EventTicketing.Tests.Fakes;

public class FakeMessagePublisher : IMessagePublisher
{
    public record PublishedItem(object Message, string QueueName);

    public List<PublishedItem> Published { get; } = [];

    public Task PublishAsync<T>(T message, string queueName, CancellationToken ct = default)
    {
        Published.Add(new(message!, queueName));
        return Task.CompletedTask;
    }
}
