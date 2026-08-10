using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using EventTicketing.Api.Data;
using EventTicketing.Api.Hubs;
using EventTicketing.Api.Messaging;
using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(AppDbContext db, IHubContext<TicketingHub> hub, IDistributedCache cache, IMessagePublisher publisher) : ControllerBase
{
    private const string EventsCacheKey = "events:all";

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        // Absolute expiry: the cache entry is evicted after 2 minutes regardless
        // of how often it is read. This is the safety-net TTL — if invalidation
        // somehow fails (crash mid-request), stale data disappears on its own.
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
    };

    /// <summary>Returns all events.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Event>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Event>>> GetAll()
    {
        var cached = await cache.GetStringAsync(EventsCacheKey);
        if (cached is not null)
            return Ok(JsonSerializer.Deserialize<List<Event>>(cached));

        var events = await db.Events.ToListAsync();
        await cache.SetStringAsync(EventsCacheKey, JsonSerializer.Serialize(events), CacheOptions);
        return Ok(events);
    }

    /// <summary>Returns a single event by ID.</summary>
    /// <param name="id">Event ID.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Event>> GetById(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        return Ok(ev);
    }

    /// <summary>Creates a new event. Admin only.</summary>
    [Authorize(Roles = "admin")]
    [HttpPost]
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Event>> Create([FromBody] Event ev)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        await cache.RemoveAsync(EventsCacheKey);
        await hub.Clients.All.SendAsync("EventCreated", ev.Id);
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ev);
    }

    /// <summary>Updates an existing event. Admin only.</summary>
    /// <param name="id">Event ID.</param>
    /// <param name="incoming">Updated event data.</param>
    [Authorize(Roles = "admin")]
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] Event incoming)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (id != incoming.Id) return BadRequest();
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        ev.Title = incoming.Title;
        ev.Description = incoming.Description;
        ev.StartTime = incoming.StartTime;
        ev.EndTime = incoming.EndTime;
        ev.Venue = incoming.Venue;
        ev.TotalSeats = incoming.TotalSeats;
        ev.AvailableSeats = incoming.AvailableSeats;
        ev.Price = incoming.Price;
        ev.ImageUrl = incoming.ImageUrl;
        ev.EventType = incoming.EventType;
        await db.SaveChangesAsync();
        await cache.RemoveAsync(EventsCacheKey);
        await hub.Clients.All.SendAsync("EventUpdated", id);
        return NoContent();
    }

    /// <summary>Deletes an event. Admin only.</summary>
    /// <param name="id">Event ID.</param>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        await cache.RemoveAsync(EventsCacheKey);
        await hub.Clients.All.SendAsync("EventDeleted", id);
        return NoContent();
    }

    public class BookRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Books a ticket for an event. Authentication optional — email is required either way.</summary>
    /// <param name="id">Event ID.</param>
    /// <param name="request">Booking request containing the attendee email.</param>
    [EnableRateLimiting("booking")]
    [HttpPost("{id}/book")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Order>> Book(int id, BookRequest request)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.AvailableSeats == 0) return Conflict();
        ev.AvailableSeats--;
        var userIdClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = new Order
        {
            EventId = id,
            UserId = userIdClaim is not null ? int.Parse(userIdClaim) : null,
            Email = request.Email,
            Price = ev.Price,
            BookedAt = DateTime.UtcNow,
        };
        db.Orders.Add(order);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }

        await hub.Clients.All.SendAsync("BookingMade", id);

        await publisher.PublishAsync(new BookingConfirmedMessage(
            OrderId: order.Id,
            EventId: id,
            EventTitle: ev.Title,
            Email: request.Email,
            Price: ev.Price,
            BookedAt: order.BookedAt
        ), BookingConfirmedMessage.QueueName);

        return CreatedAtAction(nameof(GetOrderById), new { orderId = order.Id }, order);
    }

    /// <summary>Returns all orders for a given email address. Requires authentication.</summary>
    /// <param name="email">Email address to look up orders for.</param>
    [Authorize]
    [HttpGet("orders")]
    [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByEmail([FromQuery] string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("email is required");

        var orders = await db.Orders
            .Where(o => o.Email == email)
            .OrderByDescending(o => o.BookedAt)
            .ToListAsync();
        return Ok(orders);
    }

    /// <summary>Returns a single order by ID.</summary>
    /// <param name="orderId">Order ID.</param>
    [HttpGet("orders/{orderId}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Order>> GetOrderById(int orderId)
    {
        var order = await db.Orders.FindAsync(orderId);
        if (order is null) return NotFound();
        return Ok(order);
    }
}
