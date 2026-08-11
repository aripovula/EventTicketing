using System.Net.Http.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Integration;

public class AuditLoggingIntegrationTests : IDisposable
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public AuditLoggingIntegrationTests()
    {
        _factory = new IntegrationTestFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<List<AuditLog>> GetAuditLogsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AuditLogs.ToListAsync();
    }

    private async Task<User> SeedUserAsync(string email, string password, string role = "user")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User { Name = "Test User", Email = email, Role = role };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // ── Booking ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Book_WritesTicketBookedAuditEntry()
    {
        await _client.PostAsJsonAsync("/api/events/1/book", new { Email = "test@example.com" });

        var logs = await GetAuditLogsAsync();
        Assert.Contains(logs, l => l.Action == AuditAction.TicketBooked);
    }

    [Fact]
    public async Task Book_AuditEntryHasCorrectEmailAndEntityType()
    {
        await _client.PostAsJsonAsync("/api/events/1/book", new { Email = "audit@example.com" });

        var logs = await GetAuditLogsAsync();
        var entry = Assert.Single(logs, l => l.Action == AuditAction.TicketBooked);
        Assert.Equal("Order", entry.EntityType);
        Assert.Equal("audit@example.com", entry.UserEmail);
    }

    // ── Login success ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_Success_WritesUserLoggedInAuditEntry()
    {
        var user = await SeedUserAsync("login@example.com", "Password123");

        await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "login@example.com", Password = "Password123" });

        var logs = await GetAuditLogsAsync();
        var entry = Assert.Single(logs, l => l.Action == AuditAction.UserLoggedIn);
        Assert.Equal(user.Id, entry.UserId);
        Assert.Equal("login@example.com", entry.UserEmail);
    }

    // ── Login failure ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_UnknownEmail_WritesUserLoginFailedEntryWithoutUserId()
    {
        await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nobody@example.com", Password = "Password123" });

        var logs = await GetAuditLogsAsync();
        var entry = Assert.Single(logs, l => l.Action == AuditAction.UserLoginFailed);
        Assert.Null(entry.UserId);
        Assert.Equal("nobody@example.com", entry.UserEmail);
    }

    [Fact]
    public async Task Login_WrongPassword_WritesUserLoginFailedEntryWithUserId()
    {
        var user = await SeedUserAsync("wrongpw@example.com", "Password123");

        await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "wrongpw@example.com", Password = "wrong" });

        var logs = await GetAuditLogsAsync();
        var entry = Assert.Single(logs, l => l.Action == AuditAction.UserLoginFailed);
        Assert.Equal(user.Id, entry.UserId);
        Assert.Equal("wrongpw@example.com", entry.UserEmail);
    }
}
