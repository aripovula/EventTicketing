using EventTicketing.Api.Messaging;

namespace EventTicketing.Tests.Fakes;

public class FakeMessagePublisher : IMessagePublisher
{
    public record PublishedItem(object Message, string QueueName);
    public record RawPublishedItem(string Json, string QueueName);

    public List<PublishedItem> Published { get; } = [];
    public List<RawPublishedItem> RawPublished { get; } = [];

    public Task PublishAsync<T>(T message, string queueName, CancellationToken ct = default)
    {
        Published.Add(new(message!, queueName));
        return Task.CompletedTask;
    }

    public Task PublishRawAsync(string json, string queueName, CancellationToken ct = default)
    {
        RawPublished.Add(new(json, queueName));
        return Task.CompletedTask;
    }
}
