using System.Net;
using System.Net.Http.Json;
using EventTicketing.Api.Controllers;

namespace EventTicketing.Tests.Integration;

// Each test class instance gets its own WebApplicationFactory (no IClassFixture),
// so the in-memory rate limiter starts fresh for every test method.
public class RateLimitIntegrationTests : IDisposable
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public RateLimitIntegrationTests()
    {
        _factory = new IntegrationTestFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static HttpContent BookingPayload() =>
        JsonContent.Create(new EventsController.BookRequest { Email = "test@example.com", PaymentMethodId = "pm_card_visa" });

    // Non-existent event ID keeps the test side-effect-free (no actual booking
    // is created) while still running through the rate-limiting middleware.
    private const int NonExistentEventId = 99999;

    [Fact]
    public async Task BookingEndpoint_AllowsFirstFiveRequestsWithinWindow()
    {
        for (var i = 0; i < 5; i++)
        {
            var response = await _client.PostAsync(
                $"/api/events/{NonExistentEventId}/book", BookingPayload());

            // 404 means the request reached the controller — not rate-limited
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task BookingEndpoint_Returns429AfterFiveRequests()
    {
        for (var i = 0; i < 5; i++)
            await _client.PostAsync($"/api/events/{NonExistentEventId}/book", BookingPayload());

        var response = await _client.PostAsync(
            $"/api/events/{NonExistentEventId}/book", BookingPayload());

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task GetEventsEndpoint_IsNotBlockedByBookingRateLimiter()
    {
        // Exhaust the booking-specific sliding window
        for (var i = 0; i < 5; i++)
            await _client.PostAsync($"/api/events/{NonExistentEventId}/book", BookingPayload());

        // GET /api/events uses only the global limiter (100/min), so it must succeed
        var response = await _client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
