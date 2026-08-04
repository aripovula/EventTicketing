using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace EventTicketing.E2ETests;

/// <summary>
/// Verifies rate limiting behaviour against the live API.
/// Uses APIRequestContext so no browser page is involved.
/// Note: these tests share the server's rate limiter state with other tests
/// running in the same process. They are designed to be self-contained —
/// each test exhausts the limit within its own call sequence — but they
/// must not be run in parallel with other booking-heavy tests.
/// </summary>
[NonParallelizable]
public class RateLimitE2ETests : PlaywrightTest
{
    private IAPIRequestContext _api = null!;

    [SetUp]
    public async Task SetUp()
    {
        _api = await Playwright.APIRequest.NewContextAsync(new()
        {
            // Use the Vite dev server (same as all other E2E tests); it proxies
            // /api/* to the backend, so this works identically in local dev and CI.
            BaseURL = "http://localhost:5173",
            MaxRedirects = 0,
        });
    }

    [TearDown]
    public async Task TearDown()
    {
        await _api.DisposeAsync();
    }

    private Task<IAPIResponse> PostBooking() =>
        _api.PostAsync("/api/events/99999/book", new()
        {
            DataObject = new { email = "ratelimit-e2e@example.com" },
        });

    [Test]
    public async Task BookingEndpoint_Returns429AfterFiveRequests()
    {
        // First 5 requests must reach the controller (404 = event not found, not throttled)
        for (var i = 0; i < 5; i++)
        {
            var r = await PostBooking();
            Assert.That(r.Status, Is.EqualTo(404),
                $"Request {i + 1} should reach the controller but got {r.Status}");
        }

        // 6th request must be rejected by the rate limiter
        var throttled = await PostBooking();
        Assert.That(throttled.Status, Is.EqualTo(429));
    }

    [Test]
    public async Task GetEvents_IsNotThrottledByBookingLimit()
    {
        // Exhaust the booking policy
        for (var i = 0; i < 5; i++)
            await PostBooking();

        // GET /api/events uses only the global 100/min limit — must succeed
        var response = await _api.GetAsync("/api/events");
        Assert.That(response.Status, Is.EqualTo(200));
    }
}
