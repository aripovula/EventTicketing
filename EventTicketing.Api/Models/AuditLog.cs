using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public int? UserId { get; set; }

    [MaxLength(200)]
    public string? UserEmail { get; set; }

    /// <summary>Optional JSON blob with extra context (e.g. event title, order price).</summary>
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; }
}
