using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventTicketing.Tests.Integration;

/// <summary>
/// Shared WebApplicationFactory fixture for integration tests.
/// Boots the full app against a temporary SQLite database.
/// </summary>
public class IntegrationTestFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"et_integration_{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Jwt:Key", "integration-test-jwt-secret-key-32chars!");
        builder.UseSetting("Jwt:Issuer", "EventTicketing");
        builder.UseSetting("Jwt:Audience", "EventTicketingUsers");
        builder.UseSetting("CardEncryption:Key", "integration-test-card-encryption-key!");

        builder.ConfigureServices(services =>
        {
            // Replace the Redis IDistributedCache registration with an in-memory
            // one so integration tests run without a Redis instance in CI.
            // RemoveAll first: AddDistributedMemoryCache uses TryAdd and silently
            // no-ops if IDistributedCache is already registered (e.g. from Redis).
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
