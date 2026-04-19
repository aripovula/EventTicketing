using System.ComponentModel.DataAnnotations;
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

    // PUT /api/events/{id}

    [Fact]
    public async Task Update_ExistingId_ReturnsNoContent()
    {
        var updated = new Event { Id = 1, Title = "Jazz Night Updated", Description = "Live jazz.", Date = new DateTime(2026, 8, 15, 20, 0, 0), Venue = "Blue Note Club", TotalSeats = 100, AvailableSeats = 90, Price = 30m };

        var result = await _controller.Update(1, updated);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_PersistsChangesToDatabase()
    {
        var updated = new Event { Id = 1, Title = "Jazz Night Updated", Description = "Live jazz.", Date = new DateTime(2026, 8, 15, 20, 0, 0), Venue = "Blue Note Club", TotalSeats = 100, AvailableSeats = 90, Price = 30m };

        await _controller.Update(1, updated);

        var ev = await _db.Events.FindAsync(1);
        Assert.Equal("Jazz Night Updated", ev!.Title);
        Assert.Equal(30m, ev.Price);
    }

    [Fact]
    public async Task Update_MismatchedId_ReturnsBadRequest()
    {
        var updated = new Event { Id = 2, Title = "Wrong", Description = ".", Date = DateTime.Now, Venue = "V", TotalSeats = 1, AvailableSeats = 1, Price = 1m };

        var result = await _controller.Update(1, updated);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var updated = new Event { Id = 999, Title = "Ghost", Description = ".", Date = DateTime.Now, Venue = "V", TotalSeats = 1, AvailableSeats = 1, Price = 1m };

        var result = await _controller.Update(999, updated);

        Assert.IsType<NotFoundResult>(result);
    }

    // DELETE /api/events/{id}

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var result = await _controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_RemovesEventFromDatabase()
    {
        await _controller.Delete(1);

        Assert.Null(await _db.Events.FindAsync(1));
        Assert.Equal(1, await _db.Events.CountAsync());
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var result = await _controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    // POST /api/events/{id}/book

    private static readonly EventsController.BookRequest TestBookRequest = new("test@example.com");

    [Fact]
    public async Task Book_ExistingEvent_ReturnsCreated()
    {
        var result = await _controller.Book(1, TestBookRequest);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Book_DecrementsAvailableSeats()
    {
        var before = (await _db.Events.FindAsync(1))!.AvailableSeats;

        await _controller.Book(1, TestBookRequest);

        _db.ChangeTracker.Clear();
        var ev = await _db.Events.FindAsync(1);
        Assert.Equal(before - 1, ev!.AvailableSeats);
    }

    [Fact]
    public async Task Book_ReturnsCreatedOrder()
    {
        var result = await _controller.Book(1, TestBookRequest);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var order = Assert.IsType<Order>(created.Value);
        Assert.True(order.Id > 0);
        Assert.Equal(1, order.EventId);
    }

    [Fact]
    public async Task Book_OrderStoresEmailAndPrice()
    {
        var ev = await _db.Events.FindAsync(1);

        var result = await _controller.Book(1, TestBookRequest);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var order = Assert.IsType<Order>(created.Value);
        Assert.Equal("test@example.com", order.Email);
        Assert.Equal(ev!.Price, order.Price);
    }

    [Fact]
    public async Task Book_PersistsOrderToDatabase()
    {
        await _controller.Book(1, TestBookRequest);

        Assert.Equal(1, await _db.Orders.CountAsync());
    }

    [Fact]
    public async Task Book_UnknownId_ReturnsNotFound()
    {
        var result = await _controller.Book(999, TestBookRequest);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Book_SoldOutEvent_ReturnsConflict()
    {
        var ev = await _db.Events.FindAsync(1);
        ev!.AvailableSeats = 0;
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _controller.Book(1, TestBookRequest);

        Assert.IsType<ConflictResult>(result.Result);
    }

    [Fact]
    public async Task Book_SoldOutEvent_DoesNotChangeAvailableSeats()
    {
        var ev = await _db.Events.FindAsync(1);
        ev!.AvailableSeats = 0;
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        await _controller.Book(1, TestBookRequest);

        _db.ChangeTracker.Clear();
        var after = await _db.Events.FindAsync(1);
        Assert.Equal(0, after!.AvailableSeats);
    }

    [Fact]
    public async Task Book_ConcurrentRequest_ReturnsConflict()
    {
        var ev = await _db.Events.FindAsync(1);
        ev!.AvailableSeats = 1;
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var tracked = await _db.Events.FindAsync(1);

        using var concurrentConnection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        concurrentConnection.Open();
        var concurrentOptions = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<EventTicketing.Api.Data.AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        await using var concurrentDb = new EventTicketing.Api.Data.AppDbContext(concurrentOptions);
        var concurrentEv = await concurrentDb.Events.FindAsync(1);
        concurrentEv!.AvailableSeats = 0;
        await concurrentDb.SaveChangesAsync();

        tracked!.AvailableSeats--;
        var ex = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(
            () => _db.SaveChangesAsync());
        Assert.NotNull(ex);
    }

    // GET /api/events/orders?email=...

    [Fact]
    public async Task GetOrdersByEmail_ReturnsMatchingOrders()
    {
        await _controller.Book(1, TestBookRequest);
        await _controller.Book(1, TestBookRequest);

        var result = await _controller.GetOrdersByEmail("test@example.com");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value);
        Assert.Equal(2, orders.Count());
    }

    [Fact]
    public async Task GetOrdersByEmail_UnknownEmail_ReturnsEmptyList()
    {
        var result = await _controller.GetOrdersByEmail("nobody@example.com");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value);
        Assert.Empty(orders);
    }

    [Fact]
    public async Task GetOrdersByEmail_DoesNotReturnOrdersForOtherEmail()
    {
        await _controller.Book(1, TestBookRequest);
        await _controller.Book(1, new EventsController.BookRequest("other@example.com"));

        var result = await _controller.GetOrdersByEmail("test@example.com");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value);
        Assert.All(orders, o => Assert.Equal("test@example.com", o.Email));
    }

    [Fact]
    public async Task GetOrdersByEmail_ReturnsOrdersNewestFirst()
    {
        await _controller.Book(1, TestBookRequest);
        await _controller.Book(1, TestBookRequest);

        var result = await _controller.GetOrdersByEmail("test@example.com");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(ok.Value).ToList();
        Assert.True(orders[0].Id > orders[1].Id);
    }

    // GET /api/events/orders/{orderId}

    [Fact]
    public async Task GetOrderById_ExistingOrder_ReturnsOrder()
    {
        var bookResult = await _controller.Book(1, TestBookRequest);
        var order = Assert.IsType<Order>(Assert.IsType<CreatedAtActionResult>(bookResult.Result).Value);

        var result = await _controller.GetOrderById(order.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<Order>(ok.Value);
    }

    [Fact]
    public async Task GetOrderById_UnknownId_ReturnsNotFound()
    {
        var result = await _controller.GetOrderById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // Model validation — Event

    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Event_ValidModel_PassesValidation()
    {
        var ev = new Event { Title = "T", Description = "D", Venue = "V", TotalSeats = 1, AvailableSeats = 0, Price = 0m };

        Assert.Empty(ValidateModel(ev));
    }

    [Theory]
    [InlineData("Title", "")]
    [InlineData("Description", "")]
    [InlineData("Venue", "")]
    public void Event_EmptyRequiredString_FailsValidation(string propertyName, string value)
    {
        var ev = new Event { Title = "T", Description = "D", Venue = "V", TotalSeats = 1, AvailableSeats = 0, Price = 0m };
        typeof(Event).GetProperty(propertyName)!.SetValue(ev, value);

        var results = ValidateModel(ev);

        Assert.Contains(results, r => r.MemberNames.Contains(propertyName));
    }

    [Fact]
    public void Event_TitleExceedsMaxLength_FailsValidation()
    {
        var ev = new Event { Title = new string('x', 201), Description = "D", Venue = "V", TotalSeats = 1, AvailableSeats = 0, Price = 0m };

        var results = ValidateModel(ev);

        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Event_TotalSeatsZero_FailsValidation()
    {
        var ev = new Event { Title = "T", Description = "D", Venue = "V", TotalSeats = 0, AvailableSeats = 0, Price = 0m };

        var results = ValidateModel(ev);

        Assert.Contains(results, r => r.MemberNames.Contains("TotalSeats"));
    }

    [Fact]
    public void Event_AvailableSeatsNegative_FailsValidation()
    {
        var ev = new Event { Title = "T", Description = "D", Venue = "V", TotalSeats = 1, AvailableSeats = -1, Price = 0m };

        var results = ValidateModel(ev);

        Assert.Contains(results, r => r.MemberNames.Contains("AvailableSeats"));
    }

    [Fact]
    public void Event_PriceExceedsMax_FailsValidation()
    {
        var ev = new Event { Title = "T", Description = "D", Venue = "V", TotalSeats = 1, AvailableSeats = 0, Price = 100_001m };

        var results = ValidateModel(ev);

        Assert.Contains(results, r => r.MemberNames.Contains("Price"));
    }

    // Model validation — BookRequest

    [Fact]
    public void BookRequest_ValidEmail_PassesValidation()
    {
        var req = new EventsController.BookRequest("user@example.com");

        Assert.Empty(ValidateModel(req));
    }

    [Fact]
    public void BookRequest_EmptyEmail_FailsValidation()
    {
        var req = new EventsController.BookRequest("");

        var results = ValidateModel(req);

        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void BookRequest_InvalidEmailFormat_FailsValidation()
    {
        var req = new EventsController.BookRequest("not-an-email");

        var results = ValidateModel(req);

        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }
}
