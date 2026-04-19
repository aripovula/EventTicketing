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
}
