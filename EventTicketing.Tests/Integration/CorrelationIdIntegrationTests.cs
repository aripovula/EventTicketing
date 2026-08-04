namespace EventTicketing.Tests.Integration;

public class CorrelationIdIntegrationTests : IClassFixture<IntegrationTestFactory>
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly HttpClient _client;

    public CorrelationIdIntegrationTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Response_ContainsCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/api/events");

        Assert.True(response.Headers.Contains(HeaderName));
    }

    [Fact]
    public async Task Response_EchosCallerSuppliedCorrelationId()
    {
        var requestId = "test-correlation-abc-123";
        _client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderName, requestId);

        var response = await _client.GetAsync("/api/events");

        Assert.Equal(requestId, response.Headers.GetValues(HeaderName).First());
    }

    [Fact]
    public async Task Response_GeneratesCorrelationIdWhenNoneSupplied()
    {
        _client.DefaultRequestHeaders.Remove(HeaderName);

        var response = await _client.GetAsync("/api/events");

        var id = response.Headers.GetValues(HeaderName).First();
        Assert.True(Guid.TryParse(id, out _));
    }

    [Fact]
    public async Task Response_CorrelationIdIsPresentOnAnyEndpoint()
    {
        var endpoints = new[] { "/api/events", "/health", "/health/ready" };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.True(
                response.Headers.Contains(HeaderName),
                $"Expected X-Correlation-ID header on {endpoint}");
        }
    }
}
