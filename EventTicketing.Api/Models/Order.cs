namespace EventTicketing.Api.Models;

public class Order
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime BookedAt { get; set; }

    /// <summary>
    /// Stripe PaymentIntent ID recorded after a successful charge.
    /// Null for any order created before Stripe was introduced.
    /// Used to issue refunds by looking up the order via the customer's email.
    /// </summary>
    public string? StripePaymentIntentId { get; set; }

    public Event Event { get; set; } = null!;
    public User? User { get; set; }
}
