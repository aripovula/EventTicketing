using EventTicketing.Api.Data;
using EventTicketing.Api.Messaging;
using EventTicketing.Api.Models;
using EventTicketing.Tests.Fakes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventTicketing.Tests.Messaging;

/// <summary>
/// Unit tests for <see cref="OutboxWorker.ProcessPendingAsync"/>.
/// Each test gets a fresh in-memory SQLite database and a FakeMessagePublisher
/// so there is no dependency on a running RabbitMQ broker.
/// </summary>
public class OutboxWorkerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly FakeMessagePublisher _publisher;

    public OutboxWorkerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        _publisher = new FakeMessagePublisher();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    private OutboxWorker CreateWorker() => new(
        _provider.GetRequiredService<IServiceScopeFactory>(),
        _publisher,
        NullLogger<OutboxWorker>.Instance);

    private async Task SeedAsync(string payload = "{}", string queueName = "test-queue",
        DateTime? processedAt = null, string? error = null)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = "TestMessage",
            Payload = payload,
            QueueName = queueName,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = processedAt,
            Error = error,
        });
        await db.SaveChangesAsync();
    }

    private async Task<List<OutboxMessage>> GetAllAsync()
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.OutboxMessages.ToListAsync();
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PendingMessage_IsPublishedToCorrectQueue()
    {
        await SeedAsync(payload: "{\"id\":1}", queueName: "booking-confirmed");

        await CreateWorker().ProcessPendingAsync();

        Assert.Single(_publisher.RawPublished);
        Assert.Equal("booking-confirmed", _publisher.RawPublished[0].QueueName);
        Assert.Equal("{\"id\":1}", _publisher.RawPublished[0].Json);
    }

    [Fact]
    public async Task PendingMessage_IsMarkedProcessed()
    {
        await SeedAsync();

        await CreateWorker().ProcessPendingAsync();

        var msg = (await GetAllAsync()).Single();
        Assert.NotNull(msg.ProcessedAt);
        Assert.Null(msg.Error);
    }

    [Fact]
    public async Task MultiplePendingMessages_AllDispatched()
    {
        await SeedAsync(queueName: "q1");
        await SeedAsync(queueName: "q2");
        await SeedAsync(queueName: "q3");

        await CreateWorker().ProcessPendingAsync();

        Assert.Equal(3, _publisher.RawPublished.Count);
        Assert.All(await GetAllAsync(), m => Assert.NotNull(m.ProcessedAt));
    }

    // ── Skipping rules ────────────────────────────────────────────────────────

    [Fact]
    public async Task AlreadyProcessedMessage_IsNotPublishedAgain()
    {
        await SeedAsync(processedAt: DateTime.UtcNow.AddHours(-1));

        await CreateWorker().ProcessPendingAsync();

        Assert.Empty(_publisher.RawPublished);
    }

    [Fact]
    public async Task MessageWithError_IsNotRetried()
    {
        await SeedAsync(error: "connection refused");

        await CreateWorker().ProcessPendingAsync();

        Assert.Empty(_publisher.RawPublished);
    }

    // ── Failure handling ──────────────────────────────────────────────────────

    [Fact]
    public async Task PublishFailure_SetsErrorAndDoesNotMarkProcessed()
    {
        var failingPublisher = new FailingFakePublisher();
        var worker = new OutboxWorker(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            failingPublisher,
            NullLogger<OutboxWorker>.Instance);

        await SeedAsync();

        await worker.ProcessPendingAsync();

        var msg = (await GetAllAsync()).Single();
        Assert.Null(msg.ProcessedAt);
        Assert.NotNull(msg.Error);
    }

    [Fact]
    public async Task PublishFailure_DoesNotAffectOtherPendingMessages()
    {
        // First message will fail, second should still be attempted.
        var failOnce = new FailOncePublisher();
        var worker = new OutboxWorker(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            failOnce,
            NullLogger<OutboxWorker>.Instance);

        await SeedAsync(queueName: "q1");
        await SeedAsync(queueName: "q2");

        await worker.ProcessPendingAsync();

        var messages = await GetAllAsync();
        Assert.Equal(1, messages.Count(m => m.Error != null));
        Assert.Equal(1, messages.Count(m => m.ProcessedAt != null));
    }
}

// ── Test doubles ──────────────────────────────────────────────────────────────

internal sealed class FailingFakePublisher : IMessagePublisher
{
    public Task PublishAsync<T>(T message, string queueName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task PublishRawAsync(string json, string queueName, CancellationToken ct = default) =>
        throw new InvalidOperationException("broker unavailable");
}

internal sealed class FailOncePublisher : IMessagePublisher
{
    private int _calls;

    public Task PublishAsync<T>(T message, string queueName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task PublishRawAsync(string json, string queueName, CancellationToken ct = default)
    {
        if (++_calls == 1)
            throw new InvalidOperationException("first call fails");
        return Task.CompletedTask;
    }
}
