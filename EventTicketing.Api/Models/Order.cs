namespace EventTicketing.Api.Models;

public class Order
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Email { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime BookedAt { get; set; }

    public Event Event { get; set; } = null!;
}
