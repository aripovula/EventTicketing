using EventTicketing.Api.Messaging;
using EventTicketing.Api.Services;
using EventTicketing.Tests.Fakes;
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

    /// <summary>In-memory publisher exposed so tests can assert on published messages.</summary>
    public FakeMessagePublisher Publisher { get; } = new();

    /// <summary>In-memory payment service exposed so tests can assert on charge/refund calls.</summary>
    public FakePaymentService Payment { get; } = new();

    /// <summary>In-memory blob service exposed so tests can assert on SAS URL generation calls.</summary>
    public FakeBlobService Blob { get; } = new();

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

            // Replace the real RabbitMQ publisher with an in-memory fake so tests
            // run without a broker and can assert on published messages.
            services.RemoveAll<IMessagePublisher>();
            services.AddSingleton<IMessagePublisher>(Publisher);

            // Replace the real Stripe payment service with an in-memory fake so
            // tests run without a Stripe account or network access.
            services.RemoveAll<IPaymentService>();
            services.AddSingleton<IPaymentService>(Payment);

            // Replace the real Azure Blob service with an in-memory fake so
            // tests run without an Azure storage account or network access.
            services.RemoveAll<IBlobService>();
            services.AddSingleton<IBlobService>(Blob);

            // Remove background services that try to open a RabbitMQ connection on
            // startup — they would fail in CI where no broker is running.
            var consumerDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(BookingConfirmationConsumer));
            if (consumerDescriptor is not null)
                services.Remove(consumerDescriptor);

            var workerDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(OutboxWorker));
            if (workerDescriptor is not null)
                services.Remove(workerDescriptor);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
