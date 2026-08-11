using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Tests.Services;

public class IdempotencyServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly IdempotencyService _service;

    private const string TestKey = "test-idempotency-key";
    private const string TestPath = "/api/events/1/book";

    public IdempotencyServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new IdempotencyService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ── GetAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNoEntryExists()
    {
        var result = await _service.GetAsync(TestKey, TestPath);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsCachedEntry_WhenValidEntryExists()
    {
        await _service.StoreAsync(TestKey, TestPath, 201, "{\"id\":1}");

        var result = await _service.GetAsync(TestKey, TestPath);

        Assert.NotNull(result);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("{\"id\":1}", result.ResponseBody);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenEntryIsExpired()
    {
        var now = DateTime.UtcNow;
        _db.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = TestKey,
            RequestPath = TestPath,
            StatusCode = 201,
            ResponseBody = "{}",
            CreatedAt = now.AddHours(-25),
            ExpiresAt = now.AddHours(-1),  // already expired
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetAsync(TestKey, TestPath);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenPathDiffers()
    {
        await _service.StoreAsync(TestKey, TestPath, 201, "{}");

        var result = await _service.GetAsync(TestKey, "/api/events/2/book");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenKeyDiffers()
    {
        await _service.StoreAsync(TestKey, TestPath, 201, "{}");

        var result = await _service.GetAsync("different-key", TestPath);

        Assert.Null(result);
    }

    // ── StoreAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task StoreAsync_PersistsEntryToDatabase()
    {
        await _service.StoreAsync(TestKey, TestPath, 201, "{\"id\":1}");

        Assert.Equal(1, await _db.IdempotencyKeys.CountAsync());
    }

    [Fact]
    public async Task StoreAsync_SetsExpiresAtTo24HoursAfterCreatedAt()
    {
        var before = DateTime.UtcNow;
        await _service.StoreAsync(TestKey, TestPath, 201, "{}");
        var after = DateTime.UtcNow;

        var entry = await _db.IdempotencyKeys.FirstAsync();
        var expectedMin = before.AddHours(24);
        var expectedMax = after.AddHours(24);
        Assert.InRange(entry.ExpiresAt, expectedMin, expectedMax);
    }

    [Fact]
    public async Task StoreAsync_SetsCreatedAtToUtc()
    {
        var before = DateTime.UtcNow;
        await _service.StoreAsync(TestKey, TestPath, 201, "{}");
        var after = DateTime.UtcNow;

        var entry = await _db.IdempotencyKeys.FirstAsync();
        Assert.InRange(entry.CreatedAt, before, after);
    }

    [Fact]
    public async Task StoreAsync_StoresAllFields()
    {
        await _service.StoreAsync(TestKey, TestPath, 409, "{\"error\":\"sold out\"}");

        var entry = await _db.IdempotencyKeys.FirstAsync();
        Assert.Equal(TestKey, entry.Key);
        Assert.Equal(TestPath, entry.RequestPath);
        Assert.Equal(409, entry.StatusCode);
        Assert.Equal("{\"error\":\"sold out\"}", entry.ResponseBody);
    }
}
