namespace EventTicketing.Api.Messaging;

public record BookingConfirmedMessage(
    int OrderId,
    int EventId,
    string EventTitle,
    string Email,
    decimal Price,
    DateTime BookedAt)
{
    public const string QueueName = "booking-confirmed";
}
