using System.ComponentModel.DataAnnotations;
using EventTicketing.Api.Data;
using EventTicketing.Api.Hubs;
using EventTicketing.Api.Models;
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

    [HttpPost]
    public async Task<ActionResult<Event>> Create(Event ev)
    {
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("EventCreated", ev.Id);
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ev);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Event incoming)
    {
        if (id != incoming.Id) return BadRequest();
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        ev.Title = incoming.Title;
        ev.Description = incoming.Description;
        ev.Date = incoming.Date;
        ev.Venue = incoming.Venue;
        ev.TotalSeats = incoming.TotalSeats;
        ev.AvailableSeats = incoming.AvailableSeats;
        ev.Price = incoming.Price;
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("EventUpdated", id);
        return NoContent();
    }

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
        var order = new Order
        {
            EventId = id,
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

    [HttpGet("orders")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByEmail([FromQuery] string email)
    {
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
