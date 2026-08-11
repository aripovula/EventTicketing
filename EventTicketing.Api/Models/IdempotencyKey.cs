using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

public class IdempotencyKey
{
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string RequestPath { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    [Required]
    public string ResponseBody { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
