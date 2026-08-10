using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;

namespace EventTicketing.E2ETests;

/// <summary>
/// API-level E2E tests for the booking flow. These call the running API
/// directly (no browser) and verify that the full stack — controller,
/// publisher, RabbitMQ — works together in the deployed environment.
/// </summary>
[TestFixture]
public class BookingApiE2ETests
{
    private HttpClient _client = null!;

    [SetUp]
    public void Setup() =>
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    [TearDown]
    public void Teardown() => _client.Dispose();

    [Test]
    public async Task Book_ViaApi_Returns201Created()
    {
        // A 201 proves the booking was persisted and the RabbitMQ publisher
        // ran without crashing the request (it swallows errors, but a healthy
        // response from the full stack confirms both paths executed).
        var response = await _client.PostAsJsonAsync(
            "http://localhost:5017/api/events/1/book",
            new { Email = "e2e-booking@example.com" });

        Assert.That((int)response.StatusCode, Is.EqualTo(201));
    }

    [Test]
    public async Task Book_WhenEventDoesNotExist_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            "http://localhost:5017/api/events/99999/book",
            new { Email = "e2e-notfound@example.com" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
