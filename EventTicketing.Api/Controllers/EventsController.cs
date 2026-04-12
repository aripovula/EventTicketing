using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private static readonly List<Event> _events =
    [
        new Event
        {
            Id = 1,
            Title = "Jazz Night",
            Description = "An evening of live jazz music.",
            Date = new DateTime(2026, 6, 15, 20, 0, 0),
            Venue = "Blue Note Club",
            TotalSeats = 100,
            AvailableSeats = 100,
            Price = 25.00m
        },
        new Event
        {
            Id = 2,
            Title = "Tech Conference 2026",
            Description = "A full-day conference on modern software development.",
            Date = new DateTime(2026, 7, 10, 9, 0, 0),
            Venue = "City Convention Centre",
            TotalSeats = 500,
            AvailableSeats = 500,
            Price = 149.00m
        }
    ];

    [HttpGet]
    public ActionResult<IEnumerable<Event>> GetAll()
    {
        return Ok(_events);
    }

    [HttpGet("{id}")]
    public ActionResult<Event> GetById(int id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        if (ev is null) return NotFound();
        return Ok(ev);
    }
}
