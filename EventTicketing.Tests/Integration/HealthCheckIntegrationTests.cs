using System.Net;

namespace EventTicketing.Tests.Integration;

public class HealthCheckIntegrationTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckIntegrationTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LivenessEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LivenessEndpoint_ReturnsHealthyBody()
    {
        var body = await _client.GetStringAsync("/health");
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task ReadinessEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadinessEndpoint_ReturnsHealthyBody()
    {
        var body = await _client.GetStringAsync("/health/ready");
        Assert.Equal("Healthy", body);
    }
}
