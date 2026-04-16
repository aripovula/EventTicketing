using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(AppDbContext db) : ControllerBase
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
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/book")]
    public async Task<ActionResult<Event>> Book(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.AvailableSeats == 0) return Conflict();
        ev.AvailableSeats--;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
        return Ok(ev);
    }
}
