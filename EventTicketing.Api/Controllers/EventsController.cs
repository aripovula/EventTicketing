using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EventTicketing.Api.Data;
using EventTicketing.Api.Hubs;
using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(AppDbContext db, IHubContext<TicketingHub> hub) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetAll()
    {
        return Ok(await db.Events.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetById(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        return Ok(ev);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<Event>> Create(Event ev)
    {
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("EventCreated", ev.Id);
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ev);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Event incoming)
    {
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
        await hub.Clients.All.SendAsync("EventUpdated", id);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("EventDeleted", id);
        return NoContent();
    }

    public class BookRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    [HttpPost("{id}/book")]
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
        return CreatedAtAction(nameof(GetOrderById), new { orderId = order.Id }, order);
    }

    [Authorize]
    [HttpGet("orders")]
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

    [HttpGet("orders/{orderId}")]
    public async Task<ActionResult<Order>> GetOrderById(int orderId)
    {
        var order = await db.Orders.FindAsync(orderId);
        if (order is null) return NotFound();
        return Ok(order);
    }
}
