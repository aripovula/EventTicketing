using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokenService, IWebHostEnvironment env, IAuditLogger audit) : ControllerBase
{
    public const string CookieName = "auth_token";

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public record UserInfo(int UserId, string Name, string Email, string Role);

    /// <summary>Authenticates a user and sets an HttpOnly JWT cookie.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfo>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
        {
            await audit.LogAsync(AuditAction.UserLoginFailed, "User", userEmail: request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var hasher = new PasswordHasher<Models.User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            await audit.LogAsync(AuditAction.UserLoginFailed, "User", userId: user.Id, userEmail: user.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var token = tokenService.Generate(user);

        Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = env.IsProduction(),   // require HTTPS in production only
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(8),
        });

        await audit.LogAsync(AuditAction.UserLoggedIn, "User", userId: user.Id, userEmail: user.Email);
        return Ok(new UserInfo(user.Id, user.Name, user.Email, user.Role));
    }

    /// <summary>Returns the currently authenticated user's profile.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserInfo> Me()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var emailClaim = User.FindFirstValue(ClaimTypes.Email);
        var nameClaim = User.FindFirstValue(ClaimTypes.Name);
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);

        if (idClaim is null || emailClaim is null || nameClaim is null || roleClaim is null)
            return Unauthorized();

        return Ok(new UserInfo(int.Parse(idClaim), nameClaim, emailClaim, roleClaim));
    }

    public record DefaultCard(string Last4, string CardType, string ExpiryDate);

    /// <summary>Returns all orders belonging to the authenticated user.</summary>
    [Authorize]
    [HttpGet("me/orders")]
    [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<Order>>> GetMyOrders()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await db.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Event)
            .OrderByDescending(o => o.BookedAt)
            .ToListAsync();
        return Ok(orders);
    }

    public class SaveCardRequest
    {
        [Required]
        [RegularExpression(@"^\d{13,19}$", ErrorMessage = "Card number must be 13–19 digits.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [AllowedValues("Visa", "Mastercard", "Amex", "Discover")]
        public string CardType { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Expiry must be MM/YY.")]
        public string ExpiryDate { get; set; } = string.Empty;
    }

    /// <summary>Saves or updates the authenticated user's default payment card.</summary>
    [Authorize]
    [HttpPut("me/cards/default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SaveMyDefaultCard(SaveCardRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var last4 = request.CardNumber[^4..];

        var existing = await db.Cards
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsDefault);

        if (existing is not null)
        {
            existing.Last4 = last4;
            existing.CardType = request.CardType;
            existing.ExpiryDate = request.ExpiryDate;
            existing.EncryptedNumber = $"redacted-{last4}";
        }
        else
        {
            db.Cards.Add(new Card
            {
                UserId = userId,
                Last4 = last4,
                CardType = request.CardType,
                ExpiryDate = request.ExpiryDate,
                EncryptedNumber = $"redacted-{last4}",
                IsDefault = true,
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Returns the authenticated user's default payment card (last 4 digits, type, expiry).</summary>
    [Authorize]
    [HttpGet("me/cards/default")]
    [ProducesResponseType(typeof(DefaultCard), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DefaultCard>> GetMyDefaultCard()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var card = await db.Cards
            .Where(c => c.UserId == userId && c.IsDefault)
            .Select(c => new DefaultCard(c.Last4, c.CardType, c.ExpiryDate))
            .FirstOrDefaultAsync();

        if (card is null) return NotFound();
        return Ok(card);
    }

    /// <summary>Clears the auth cookie, logging the user out.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName);
        return NoContent();
    }
}
