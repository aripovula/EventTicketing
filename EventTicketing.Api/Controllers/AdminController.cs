using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController(AppDbContext db) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
    {
        var orders = await db.Orders
            .Include(o => o.Event)
            .OrderByDescending(o => o.BookedAt)
            .ToListAsync();
        return Ok(orders);
    }

    public record EventSummary(int EventId, string Title, int OpeningBalance, int SoldSeats, int RemainingSeats, decimal Revenue);

    [HttpGet("summary")]
    public async Task<ActionResult<IEnumerable<EventSummary>>> GetSummary()
    {
        var summary = await db.Events
            .OrderBy(e => e.StartTime)
            .Select(e => new EventSummary(
                e.Id,
                e.Title,
                e.TotalSeats,
                e.TotalSeats - e.AvailableSeats,
                e.AvailableSeats,
                (e.TotalSeats - e.AvailableSeats) * e.Price))
            .ToListAsync();
        return Ok(summary);
    }
}
