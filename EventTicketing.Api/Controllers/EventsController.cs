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
    public async Task<IActionResult> Update(int id, Event ev)
    {
        if (id != ev.Id) return BadRequest();
        db.Entry(ev).State = EntityState.Modified;
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

    // If SaveChangesAsync() fails the decrement is rolled back and 
    // the DB stays consistent. The exception propagates as a 500. No data corruption.
    [HttpPost("{id}/book")]
    public async Task<ActionResult<Event>> Book(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.AvailableSeats == 0) return Conflict();
        ev.AvailableSeats--;
        await db.SaveChangesAsync();
        return Ok(ev);
    }
}
