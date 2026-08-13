using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

public class OutboxMessage
{
    public int Id { get; set; }

    /// <summary>CLR type name of the message (e.g. "BookingConfirmedMessage"), used for logging.</summary>
    [Required]
    [MaxLength(200)]
    public string Type { get; set; } = string.Empty;

    /// <summary>JSON-serialized message payload ready to forward to the broker.</summary>
    [Required]
    public string Payload { get; set; } = string.Empty;

    /// <summary>Target RabbitMQ queue name.</summary>
    [Required]
    [MaxLength(200)]
    public string QueueName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>Set by the outbox worker once the message has been successfully dispatched.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Records the last dispatch error. Messages with a non-null Error are not retried
    /// automatically — they require manual inspection or an alerting pipeline.
    /// </summary>
    public string? Error { get; set; }
}
