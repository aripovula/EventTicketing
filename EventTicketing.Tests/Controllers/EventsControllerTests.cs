using EventTicketing.Api.Controllers;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Tests.Controllers;

public class EventsControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated(); // creates schema and applies HasData seeds (Id 1 & 2)

        _controller = new EventsController(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // GET /api/events

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var result = await _controller.GetAll();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_ReturnsSeededEvents()
    {
        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<IEnumerable<Event>>(ok.Value);
        Assert.Equal(2, events.Count());
    }

    // GET /api/events/{id}

    [Fact]
    public async Task GetById_ExistingId_ReturnsEvent()
    {
        var result = await _controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ev = Assert.IsType<Event>(ok.Value);
        Assert.Equal("Jazz Night", ev.Title);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // POST /api/events

    [Fact]
    public async Task Create_ReturnsCreatedWithLocation()
    {
        var newEvent = new Event { Title = "New Show", Description = "Desc.", Date = new DateTime(2026, 9, 1), Venue = "Arena", TotalSeats = 200, AvailableSeats = 200, Price = 50m };

        var result = await _controller.Create(newEvent);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetById), created.ActionName);
        var ev = Assert.IsType<Event>(created.Value);
        Assert.True(ev.Id > 0);
    }

    [Fact]
    public async Task Create_PersistsEventToDatabase()
    {
        var newEvent = new Event { Title = "New Show", Description = "Desc.", Date = new DateTime(2026, 9, 1), Venue = "Arena", TotalSeats = 200, AvailableSeats = 200, Price = 50m };

        await _controller.Create(newEvent);

        Assert.Equal(3, await _db.Events.CountAsync());
    }
}
