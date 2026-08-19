using System.Net;
using System.Net.Http.Json;
using EventTicketing.Api.Services;

namespace EventTicketing.Tests.Integration;

/// <summary>
/// Verifies the POST /api/admin/upload/blob-sas-url endpoint at the HTTP layer.
/// Uses FakeBlobService so tests run without an Azure storage account.
/// </summary>
public class BlobSasIntegrationTests : IDisposable
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public BlobSasIntegrationTests()
    {
        _factory = new IntegrationTestFactory();
        // UseCookies=true so the auth_token cookie from POST /api/auth/login
        // is stored and re-sent automatically on subsequent requests.
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task LoginAsAdminAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "admin@example.com", Password = "Password" });
    }

    private async Task LoginAsUserAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "john@example.com", Password = "Password" });
    }

    private static HttpRequestMessage SasRequest(string fileName = "banner.jpg", string contentType = "image/jpeg")
        => new(HttpMethod.Post, "/api/admin/upload/blob-sas-url")
        {
            Content = JsonContent.Create(new { fileName, contentType })
        };

    // ── Auth guards ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBlobSasUrl_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(SasRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBlobSasUrl_AuthenticatedAsUser_Returns403()
    {
        await LoginAsUserAsync();

        var response = await _client.SendAsync(SasRequest());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBlobSasUrl_MissingFileName_Returns400()
    {
        await LoginAsAdminAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/admin/upload/blob-sas-url",
            new { contentType = "image/jpeg" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBlobSasUrl_MissingContentType_Returns400()
    {
        await LoginAsAdminAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/admin/upload/blob-sas-url",
            new { fileName = "banner.jpg" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBlobSasUrl_ValidRequest_Returns200()
    {
        await LoginAsAdminAsync();

        var response = await _client.SendAsync(SasRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBlobSasUrl_ValidRequest_ReturnsUploadAndPublicUrls()
    {
        await LoginAsAdminAsync();

        var response = await _client.SendAsync(SasRequest());
        var result = await response.Content.ReadFromJsonAsync<BlobSasResult>();

        Assert.NotNull(result);
        Assert.Equal(_factory.Blob.FakeUploadUrl, result.UploadUrl);
        Assert.Equal(_factory.Blob.FakePublicUrl, result.PublicUrl);
    }

    [Fact]
    public async Task GetBlobSasUrl_ValidRequest_CallsBlobServiceWithCorrectArgs()
    {
        await LoginAsAdminAsync();

        await _client.SendAsync(SasRequest("event-photo.png", "image/png"));

        var call = Assert.Single(_factory.Blob.Calls);
        Assert.Equal("event-photo.png", call.FileName);
        Assert.Equal("image/png", call.ContentType);
    }
}
