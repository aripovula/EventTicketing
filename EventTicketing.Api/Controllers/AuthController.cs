using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EventTicketing.Api.Data;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokenService, IWebHostEnvironment env) : ControllerBase
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

    [HttpPost("login")]
    public async Task<ActionResult<UserInfo>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid email or password." });

        var hasher = new PasswordHasher<Models.User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

        var token = tokenService.Generate(user);

        Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure   = env.IsProduction(),   // require HTTPS in production only
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddHours(8),
        });

        return Ok(new UserInfo(user.Id, user.Name, user.Email, user.Role));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<UserInfo> Me()
    {
        var idClaim    = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var emailClaim = User.FindFirstValue(ClaimTypes.Email);
        var nameClaim  = User.FindFirstValue(ClaimTypes.Name);
        var roleClaim  = User.FindFirstValue(ClaimTypes.Role);

        if (idClaim is null || emailClaim is null || nameClaim is null || roleClaim is null)
            return Unauthorized();

        return Ok(new UserInfo(int.Parse(idClaim), nameClaim, emailClaim, roleClaim));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName);
        return NoContent();
    }
}
