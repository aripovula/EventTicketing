using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

public class Card
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // AES-encrypted full card number, base64-encoded (IV prepended)
    [Required]
    [MaxLength(500)]
    public string EncryptedNumber { get; set; } = string.Empty;

    // Stored in plain for display ("ending in 4242") — no security value on its own
    [Required]
    [StringLength(4, MinimumLength = 4)]
    [RegularExpression(@"^\d{4}$")]
    public string Last4 { get; set; } = string.Empty;

    // MM/YY
    [Required]
    [MaxLength(5)]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$")]
    public string ExpiryDate { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [AllowedValues("Visa", "Mastercard", "Amex", "Discover")]
    public string CardType { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
