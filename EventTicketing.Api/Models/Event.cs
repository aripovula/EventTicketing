using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [Required]
    [MaxLength(200)]
    public string Venue { get; set; } = string.Empty;

    [Range(1, 10_000)]
    public int TotalSeats { get; set; }

    [ConcurrencyCheck]
    [Range(0, 10_000)]
    public int AvailableSeats { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }
}
