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
    /// <summary>Returns all orders across all events. Admin only.</summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
    {
        var orders = await db.Orders
            .Include(o => o.Event)
            .OrderByDescending(o => o.BookedAt)
            .ToListAsync();
        return Ok(orders);
    }

    public record EventSummary(int EventId, string Title, int OpeningBalance, int SoldSeats, int RemainingSeats, decimal Revenue);

    /// <summary>Returns a revenue and seat availability summary per event. Admin only.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(IEnumerable<EventSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
