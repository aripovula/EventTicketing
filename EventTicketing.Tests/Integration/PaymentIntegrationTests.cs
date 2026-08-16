using System.Net;
using System.Net.Http.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Filters;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

/// <summary>
/// Verifies the Stripe payment integration at the HTTP layer. Uses FakePaymentService
/// so tests run without a Stripe account and stay deterministic.
/// </summary>
public class PaymentIntegrationTests : IDisposable
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public PaymentIntegrationTests()
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
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/book")
        {
            Content = JsonContent.Create(new { Email = email, PaymentMethodId = "pm_card_visa" })
        };
        if (idempotencyKey is not null)
            req.Headers.Add(IdempotentAttribute.HeaderName, idempotencyKey);
        return req;
    }

    private async Task<int> GetOrderCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Orders.CountAsync();
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Book_Returns201WhenPaymentSucceeds()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/events/1/book",
            new { Email = "pay-success@example.com", PaymentMethodId = "pm_card_visa" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Book_ChargesStripeOnce()
    {
        await _client.PostAsJsonAsync(
            "/api/events/1/book",
            new { Email = "pay-once@example.com", PaymentMethodId = "pm_card_visa" });

        Assert.Single(_factory.Payment.ChargeCalls);
    }

    [Fact]
    public async Task Book_StoresPaymentIntentIdOnOrder()
    {
        await _client.PostAsJsonAsync(
            "/api/events/1/book",
            new { Email = "pay-store@example.com", PaymentMethodId = "pm_card_visa" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.Orders.OrderByDescending(o => o.Id).FirstAsync();

        Assert.Equal(_factory.Payment.FakePaymentIntentId, order.StripePaymentIntentId);
    }

    // ── Payment decline ───────────────────────────────────────────────────────

    [Fact]
    public async Task Book_Returns402WhenPaymentDeclined()
    {
        _factory.Payment.ThrowOnCharge = true;

        var response = await _client.PostAsJsonAsync(
            "/api/events/1/book",
            new { Email = "pay-decline@example.com", PaymentMethodId = "pm_card_declined" });

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
    }

    [Fact]
    public async Task Book_DoesNotCreateOrderWhenPaymentDeclined()
    {
        _factory.Payment.ThrowOnCharge = true;
        var countBefore = await GetOrderCountAsync();

        await _client.PostAsJsonAsync(
            "/api/events/1/book",
            new { Email = "pay-decline@example.com", PaymentMethodId = "pm_card_declined" });

        Assert.Equal(countBefore, await GetOrderCountAsync());
    }

    [Fact]
    public async Task Book_DoesNotDecrementSeatsWhenPaymentDeclined()
    {
        _factory.Payment.ThrowOnCharge = true;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seatsBefore = (await db.Events.FindAsync(1))!.AvailableSeats;

        await _client.PostAsJsonAsync(
            "/api/events/1/book",
            new { Email = "pay-decline@example.com", PaymentMethodId = "pm_card_declined" });

        db.ChangeTracker.Clear();
        var seatsAfter = (await db.Events.FindAsync(1))!.AvailableSeats;
        Assert.Equal(seatsBefore, seatsAfter);
    }

    // ── Idempotency key forwarded to Stripe ───────────────────────────────────

    [Fact]
    public async Task Book_ForwardsIdempotencyKeyToStripe()
    {
        var key = Guid.NewGuid().ToString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/events/1/book")
        {
            Content = JsonContent.Create(new { Email = "idem@example.com", PaymentMethodId = "pm_card_visa" })
        };
        req.Headers.Add(IdempotentAttribute.HeaderName, key);
        await _client.SendAsync(req);

        Assert.Equal(key, _factory.Payment.ChargeCalls.Single().IdempotencyKey);
    }

    [Fact]
    public async Task Book_RetryWithSameKey_ChargesStripeOnlyOnce()
    {
        var key = Guid.NewGuid().ToString();

        // First request — real booking
        using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/events/1/book")
        {
            Content = JsonContent.Create(new { Email = "idem-retry@example.com", PaymentMethodId = "pm_card_visa" })
        };
        req1.Headers.Add(IdempotentAttribute.HeaderName, key);
        await _client.SendAsync(req1);

        // Retry — idempotent layer should short-circuit before calling payment
        using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/events/1/book")
        {
            Content = JsonContent.Create(new { Email = "idem-retry@example.com", PaymentMethodId = "pm_card_visa" })
        };
        req2.Headers.Add(IdempotentAttribute.HeaderName, key);
        await _client.SendAsync(req2);

        // The idempotency filter returns the cached response before the controller
        // body runs, so ChargeAsync must have been called exactly once.
        Assert.Single(_factory.Payment.ChargeCalls);
    }
}
