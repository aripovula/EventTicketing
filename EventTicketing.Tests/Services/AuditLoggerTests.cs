using EventTicketing.Api.Data;
using EventTicketing.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Tests.Services;

public class AuditLoggerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AuditLogger _logger;

    public AuditLoggerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _logger = new AuditLogger(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task LogAsync_PersistsEntryToDatabase()
    {
        await _logger.LogAsync(AuditAction.EventCreated, "Event");

        Assert.Equal(1, await _db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task LogAsync_StoresActionAndEntityType()
    {
        await _logger.LogAsync(AuditAction.TicketBooked, "Order");

        var entry = await _db.AuditLogs.FirstAsync();
        Assert.Equal(AuditAction.TicketBooked, entry.Action);
        Assert.Equal("Order", entry.EntityType);
    }

    [Fact]
    public async Task LogAsync_StoresAllOptionalFields()
    {
        await _logger.LogAsync(AuditAction.EventCreated, "Event",
            entityId: 42, userId: 7, userEmail: "admin@example.com", details: "Jazz Night");

        var entry = await _db.AuditLogs.FirstAsync();
        Assert.Equal(42, entry.EntityId);
        Assert.Equal(7, entry.UserId);
        Assert.Equal("admin@example.com", entry.UserEmail);
        Assert.Equal("Jazz Night", entry.Details);
    }

    [Fact]
    public async Task LogAsync_NullableFieldsDefaultToNull()
    {
        await _logger.LogAsync(AuditAction.UserLoginFailed, "User");

        var entry = await _db.AuditLogs.FirstAsync();
        Assert.Null(entry.EntityId);
        Assert.Null(entry.UserId);
        Assert.Null(entry.UserEmail);
        Assert.Null(entry.Details);
    }

    [Fact]
    public async Task LogAsync_SetsTimestampToUtc()
    {
        var before = DateTime.UtcNow;
        await _logger.LogAsync(AuditAction.EventDeleted, "Event");
        var after = DateTime.UtcNow;

        var entry = await _db.AuditLogs.FirstAsync();
        Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
        Assert.InRange(entry.Timestamp, before, after);
    }
}
