using EventTicketing.Api.Controllers;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Tests.Controllers;

public class AdminControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _controller = new AdminController(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Order> SeedOrder(int eventId = 1, string email = "user@example.com")
    {
        var order = new Order
        {
            EventId = eventId,
            Email = email,
            Price = 25m,
            BookedAt = DateTime.UtcNow,
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    // GET /api/admin/orders

    [Fact]
    public async Task GetAllOrders_ReturnsOk()
    {
        var result = await _controller.GetAllOrders();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyListWhenNoOrders()
    {
        var result = await _controller.GetAllOrders();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value);
        Assert.Empty(orders);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsAllOrders()
    {
        await SeedOrder(1, "alice@example.com");
        await SeedOrder(2, "bob@example.com");

        var result = await _controller.GetAllOrders();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value);
        Assert.Equal(2, orders.Count());
    }

    [Fact]
    public async Task GetAllOrders_IncludesEventOnEachOrder()
    {
        await SeedOrder(1, "user@example.com");

        var result = await _controller.GetAllOrders();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value).ToList();
        Assert.NotNull(orders[0].Event);
        Assert.Equal("Jazz Night", orders[0].Event.Title);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsOrdersNewestFirst()
    {
        await SeedOrder(1, "first@example.com");
        await SeedOrder(1, "second@example.com");

        var result = await _controller.GetAllOrders();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value).ToList();
        Assert.True(orders[0].Id > orders[1].Id);
    }

    // GET /api/admin/summary

    [Fact]
    public async Task GetSummary_ReturnsOk()
    {
        var result = await _controller.GetSummary();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSummary_ReturnsOneRowPerEvent()
    {
        var result = await _controller.GetSummary();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IEnumerable<AdminController.EventSummary>>(ok.Value);
        Assert.Equal(await _db.Events.CountAsync(), rows.Count());
    }

    [Fact]
    public async Task GetSummary_OpeningBalanceEqualsTotalSeats()
    {
        var result = await _controller.GetSummary();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IEnumerable<AdminController.EventSummary>>(ok.Value).ToList();
        var jazzRow = rows.Single(r => r.Title == "Jazz Night");
        Assert.Equal(120, jazzRow.OpeningBalance); // seeded with TotalSeats = 120
    }

    [Fact]
    public async Task GetSummary_SoldSeatsReflectsBookings()
    {
        // Jazz Night: TotalSeats=120, set AvailableSeats=119 → 1 sold
        var ev = await _db.Events.FindAsync(1);
        ev!.AvailableSeats = 119;
        await _db.SaveChangesAsync();

        var result = await _controller.GetSummary();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IEnumerable<AdminController.EventSummary>>(ok.Value).ToList();
        var jazzRow = rows.Single(r => r.Title == "Jazz Night");
        Assert.Equal(1, jazzRow.SoldSeats);
        Assert.Equal(119, jazzRow.RemainingSeats);
    }

    [Fact]
    public async Task GetSummary_RevenueIsZeroWhenNoSeatsSold()
    {
        // Add a fresh event with all seats available, then verify its revenue is zero
        var fresh = new Event
        {
            Title = "Fresh Event", Description = "D.", StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2), EventType = "Other",
            Venue = "V", TotalSeats = 50, AvailableSeats = 50, Price = 10m
        };
        _db.Events.Add(fresh);
        await _db.SaveChangesAsync();

        var result = await _controller.GetSummary();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IEnumerable<AdminController.EventSummary>>(ok.Value).ToList();
        var freshRow = rows.Single(r => r.Title == "Fresh Event");
        Assert.Equal(0m, freshRow.Revenue);
    }

    [Fact]
    public async Task GetSummary_RevenueIsSoldSeatsByPrice()
    {
        // Jazz Night: TotalSeats=120, Price=$25. Set AvailableSeats=118 → 2 sold → $50
        var ev = await _db.Events.FindAsync(1);
        ev!.AvailableSeats = 118;
        await _db.SaveChangesAsync();

        var result = await _controller.GetSummary();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IEnumerable<AdminController.EventSummary>>(ok.Value).ToList();
        var jazzRow = rows.Single(r => r.Title == "Jazz Night");
        Assert.Equal(50m, jazzRow.Revenue); // 2 × $25
    }

    [Fact]
    public async Task GetSummary_ReturnsEventsOrderedByDate()
    {
        var result = await _controller.GetSummary();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IEnumerable<AdminController.EventSummary>>(ok.Value).ToList();
        // Jazz Night is May 2, Tech Conference is May 5
        Assert.Equal("Jazz Night", rows[0].Title);
        Assert.Equal("Tech Conference 2026", rows[1].Title);
    }
}
