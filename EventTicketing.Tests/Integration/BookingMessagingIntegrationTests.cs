using System.Net;
using System.Net.Http.Json;
using EventTicketing.Api.Messaging;

namespace EventTicketing.Tests.Integration;

/// <summary>
/// Verifies that a successful booking publishes a BookingConfirmedMessage
/// via IMessagePublisher (stubbed with FakeMessagePublisher in test DI).
/// Each test creates its own factory so Published list starts empty.
/// </summary>
public class BookingMessagingIntegrationTests : IDisposable
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public BookingMessagingIntegrationTests()
    {
        _factory = new IntegrationTestFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Book_PublishesExactlyOneMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/events/1/book", new { Email = "test@example.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(_factory.Publisher.Published);
    }

    [Fact]
    public async Task Book_PublishedMessageIsOnCorrectQueue()
    {
        await _client.PostAsJsonAsync("/api/events/1/book", new { Email = "test@example.com" });

        Assert.Equal(BookingConfirmedMessage.QueueName, _factory.Publisher.Published[0].QueueName);
    }

    [Fact]
    public async Task Book_PublishedMessageHasCorrectEventAndEmail()
    {
        await _client.PostAsJsonAsync("/api/events/1/book", new { Email = "test@example.com" });

        var msg = Assert.IsType<BookingConfirmedMessage>(_factory.Publisher.Published[0].Message);
        Assert.Equal(1, msg.EventId);
        Assert.Equal("test@example.com", msg.Email);
    }

    [Fact]
    public async Task Book_NotFound_PublishesNoMessage()
    {
        await _client.PostAsJsonAsync("/api/events/999/book", new { Email = "test@example.com" });

        Assert.Empty(_factory.Publisher.Published);
    }
}
