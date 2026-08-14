using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

/// <summary>
/// Verifies that a successful booking writes an OutboxMessage to the database
/// in the same transaction as the order. Each test creates its own factory so
/// the OutboxMessages table starts empty.
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

    private async Task<List<Api.Models.OutboxMessage>> GetOutboxMessagesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.OutboxMessages.ToListAsync();
    }

    [Fact]
    public async Task Book_WritesOneOutboxMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/events/1/book", new { Email = "test@example.com", PaymentMethodId = "pm_card_visa" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(await GetOutboxMessagesAsync());
    }

    [Fact]
    public async Task Book_OutboxMessageIsOnCorrectQueue()
    {
        await _client.PostAsJsonAsync("/api/events/1/book", new { Email = "test@example.com", PaymentMethodId = "pm_card_visa" });

        var messages = await GetOutboxMessagesAsync();
        Assert.Equal(BookingConfirmedMessage.QueueName, messages[0].QueueName);
    }

    [Fact]
    public async Task Book_OutboxMessagePayloadHasCorrectEventAndEmail()
    {
        await _client.PostAsJsonAsync("/api/events/1/book", new { Email = "test@example.com", PaymentMethodId = "pm_card_visa" });

        var messages = await GetOutboxMessagesAsync();
        var payload = JsonSerializer.Deserialize<BookingConfirmedMessage>(messages[0].Payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(1, payload!.EventId);
        Assert.Equal("test@example.com", payload.Email);
    }

    [Fact]
    public async Task Book_NotFound_WritesNoOutboxMessage()
    {
        await _client.PostAsJsonAsync("/api/events/999/book", new { Email = "test@example.com", PaymentMethodId = "pm_card_visa" });

        Assert.Empty(await GetOutboxMessagesAsync());
    }

    [Fact]
    public async Task Book_OutboxMessageAndOrderWrittenAtomically()
    {
        // After a successful booking both the order and the outbox message must
        // exist. This does not prove they were written in the same transaction
        // (that would require injecting a fault), but it does verify neither is
        // missing from the happy path.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orderCountBefore = await db.Orders.CountAsync();

        await _client.PostAsJsonAsync("/api/events/1/book", new { Email = "test@example.com", PaymentMethodId = "pm_card_visa" });

        db.ChangeTracker.Clear();
        Assert.Equal(orderCountBefore + 1, await db.Orders.CountAsync());
        Assert.Single(await GetOutboxMessagesAsync());
    }
}
