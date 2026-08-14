using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

public class IdempotencyIntegrationTests : IDisposable
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public IdempotencyIntegrationTests()
    {
        _factory = new IntegrationTestFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static HttpRequestMessage BookRequest(int eventId, string email, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/book")
        {
            Content = JsonContent.Create(new { Email = email, PaymentMethodId = "pm_card_visa" })
        };
        if (idempotencyKey is not null)
            request.Headers.Add(IdempotentAttribute.HeaderName, idempotencyKey);
        return request;
    }

    private async Task<int> GetOrderCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Orders.CountAsync();
    }

    private async Task<int> GetAvailableSeatsAsync(int eventId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.Events.FindAsync(eventId))!.AvailableSeats;
    }

    // ── First request ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FirstRequest_WithIdempotencyKey_Returns201()
    {
        var response = await _client.SendAsync(BookRequest(1, "test@example.com", Guid.NewGuid().ToString()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ── Retry with same key ───────────────────────────────────────────────────

    [Fact]
    public async Task RetryWithSameKey_ReturnsSameStatusCode()
    {
        var key = Guid.NewGuid().ToString();

        var first = await _client.SendAsync(BookRequest(1, "test@example.com", key));
        var second = await _client.SendAsync(BookRequest(1, "test@example.com", key));

        Assert.Equal(first.StatusCode, second.StatusCode);
    }

    [Fact]
    public async Task RetryWithSameKey_ReturnsSameOrderId()
    {
        var key = Guid.NewGuid().ToString();

        var first = await _client.SendAsync(BookRequest(1, "test@example.com", key));
        var second = await _client.SendAsync(BookRequest(1, "test@example.com", key));

        var firstBody = await first.Content.ReadAsStringAsync();
        var secondBody = await second.Content.ReadAsStringAsync();

        var firstId = JsonDocument.Parse(firstBody).RootElement.GetProperty("id").GetInt32();
        var secondId = JsonDocument.Parse(secondBody).RootElement.GetProperty("id").GetInt32();

        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task RetryWithSameKey_CreatesOnlyOneOrder()
    {
        var countBefore = await GetOrderCountAsync();
        var key = Guid.NewGuid().ToString();

        await _client.SendAsync(BookRequest(1, "test@example.com", key));
        await _client.SendAsync(BookRequest(1, "test@example.com", key));

        Assert.Equal(countBefore + 1, await GetOrderCountAsync());
    }

    [Fact]
    public async Task RetryWithSameKey_DecrementsAvailableSeatsOnlyOnce()
    {
        var seatsBefore = await GetAvailableSeatsAsync(1);
        var key = Guid.NewGuid().ToString();

        await _client.SendAsync(BookRequest(1, "test@example.com", key));
        await _client.SendAsync(BookRequest(1, "test@example.com", key));

        Assert.Equal(seatsBefore - 1, await GetAvailableSeatsAsync(1));
    }

    // ── Different keys ────────────────────────────────────────────────────────

    [Fact]
    public async Task DifferentKeys_CreateTwoIndependentBookings()
    {
        var countBefore = await GetOrderCountAsync();

        await _client.SendAsync(BookRequest(1, "a@example.com", Guid.NewGuid().ToString()));
        await _client.SendAsync(BookRequest(1, "b@example.com", Guid.NewGuid().ToString()));

        Assert.Equal(countBefore + 2, await GetOrderCountAsync());
    }

    // ── No key ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NoIdempotencyKey_ProcessesEachRequestIndependently()
    {
        var countBefore = await GetOrderCountAsync();

        await _client.SendAsync(BookRequest(1, "a@example.com"));
        await _client.SendAsync(BookRequest(1, "b@example.com"));

        Assert.Equal(countBefore + 2, await GetOrderCountAsync());
    }

    // ── Cached failure ────────────────────────────────────────────────────────

    [Fact]
    public async Task RetryWithSameKey_CachesSoldOutResponse()
    {
        // Exhaust the event's seats first
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ev = await db.Events.FindAsync(1);
        ev!.AvailableSeats = 0;
        await db.SaveChangesAsync();

        var key = Guid.NewGuid().ToString();
        var first = await _client.SendAsync(BookRequest(1, "test@example.com", key));
        var second = await _client.SendAsync(BookRequest(1, "test@example.com", key));

        Assert.Equal(HttpStatusCode.Conflict, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
