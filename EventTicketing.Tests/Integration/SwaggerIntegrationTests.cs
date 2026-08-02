using System.Net;

namespace EventTicketing.Tests.Integration;

public class SwaggerIntegrationTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;

    public SwaggerIntegrationTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SwaggerJson_ReturnsOk()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerJson_ReturnsJsonContentType()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SwaggerJson_ContainsOpenApiVersion()
    {
        var content = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("\"openapi\"", content);
    }

    [Fact]
    public async Task SwaggerJson_ContainsApiTitle()
    {
        var content = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("Event Ticketing API", content);
    }

    // Assert against XML doc summaries rather than raw paths — Swashbuckle's
    // JSON serializer escapes forward slashes (\/api\/events) so path strings
    // are not reliable substrings. Summaries are a more meaningful check anyway.

    [Fact]
    public async Task SwaggerJson_DocumentsEventsController()
    {
        var content = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("Returns all events", content);
        Assert.Contains("Books a ticket for an event", content);
    }

    [Fact]
    public async Task SwaggerJson_DocumentsAuthController()
    {
        var content = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("Authenticates a user", content);
        Assert.Contains("Returns the currently authenticated user", content);
    }

    [Fact]
    public async Task SwaggerJson_DocumentsAdminController()
    {
        var content = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("Returns all orders across all events", content);
        Assert.Contains("revenue and seat availability summary", content);
    }

    [Fact]
    public async Task SwaggerJson_ContainsBearerSecurityScheme()
    {
        var content = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("securitySchemes", content);
        Assert.Contains("Bearer", content);
    }
}
