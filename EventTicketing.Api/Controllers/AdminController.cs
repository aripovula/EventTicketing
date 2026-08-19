using System.ComponentModel.DataAnnotations;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController(AppDbContext db, IBlobService blob) : ControllerBase
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

    public class BlobSasRequest
    {
        /// <summary>Original file name (e.g. "banner.jpg"). Used to derive the blob name.</summary>
        [Required]
        public string FileName { get; set; } = string.Empty;

        /// <summary>MIME type of the file (e.g. "image/jpeg").</summary>
        [Required]
        public string ContentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generates a write-only Azure Blob Storage SAS URL so the browser can upload
    /// an event image directly to Blob Storage without routing the file through this API.
    /// Admin only.
    /// </summary>
    [HttpPost("upload/blob-sas-url")]
    [ProducesResponseType(typeof(BlobSasResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BlobSasResult>> GetBlobSasUrl(
        [FromBody] BlobSasRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await blob.GenerateUploadSasUrlAsync(request.FileName, request.ContentType, ct);
        return Ok(result);
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
