using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EventTicketing.Api.Controllers;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EventTicketing.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AuthController _controller;

    private const string TestPassword = "Password123";
    private const string TestEmail = "alice@example.com";

    public AuthControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-jwt-key-that-is-32-characters!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
            })
            .Build();

        var env = new FakeWebHostEnvironment(isProduction: false);
        _controller = new AuthController(_db, new TokenService(config), env);

        // Wire up an HttpContext so Response.Cookies is available
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private User SeedUser(string role = "user")
    {
        var user = new User { Name = "Alice", Email = TestEmail, Role = role };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, TestPassword);
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    private void SeedCard(int userId, string last4, bool isDefault)
    {
        _db.Cards.Add(new Card
        {
            UserId = userId,
            Last4 = last4,
            EncryptedNumber = "dummy",
            ExpiryDate = "12/32",
            CardType = "Visa",
            IsDefault = isDefault,
        });
        _db.SaveChanges();
    }

    // ── Login success ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200()
    {
        SeedUser();
        var result = await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = TestPassword });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsUserInfoWithoutToken()
    {
        var seeded = SeedUser();
        var ok = (OkObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = TestPassword })).Result!;

        var info = Assert.IsType<AuthController.UserInfo>(ok.Value);
        Assert.Equal(seeded.Id, info.UserId);
        Assert.Equal(seeded.Name, info.Name);
        Assert.Equal(seeded.Email, info.Email);
        Assert.Equal("user", info.Role);
    }

    [Fact]
    public async Task Login_SetsHttpOnlyCookie()
    {
        SeedUser();
        await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = TestPassword });

        var cookies = _controller.HttpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains(AuthController.CookieName, cookies);
        Assert.Contains("httponly", cookies, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_AdminUser_ReturnsAdminRole()
    {
        SeedUser(role: "admin");
        var ok = (OkObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = TestPassword })).Result!;

        var info = Assert.IsType<AuthController.UserInfo>(ok.Value);
        Assert.Equal("admin", info.Role);
    }

    [Fact]
    public async Task Login_CookieContainsValidJwt()
    {
        SeedUser();
        await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = TestPassword });

        var setCookieHeader = _controller.HttpContext.Response.Headers["Set-Cookie"].ToString();
        // Extract token value: "auth_token=<value>;"
        var tokenValue = setCookieHeader
            .Split(';')[0]
            .Replace(AuthController.CookieName + "=", "");

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenValue);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Email && c.Value == TestEmail);
    }

    // ── Login failure ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var result = await _controller.Login(new AuthController.LoginRequest
        { Email = "nobody@example.com", Password = TestPassword });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        SeedUser();
        var result = await _controller.Login(new AuthController.LoginRequest
        { Email = TestEmail, Password = "wrong-password" });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithWrongPassword_DoesNotLeakReason()
    {
        SeedUser();
        var badPassword = (UnauthorizedObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = "wrong" })).Result!;

        var badEmail = (UnauthorizedObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = "x@x.com", Password = TestPassword })).Result!;

        Assert.Equal(badPassword.Value?.ToString(), badEmail.Value?.ToString());
    }

    // ── /me ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Me_WithAuthenticatedUser_ReturnsUserInfo()
    {
        var seeded = SeedUser();
        _controller.ControllerContext.HttpContext.User = MakePrincipal(seeded);

        var result = _controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var info = Assert.IsType<AuthController.UserInfo>(ok.Value);
        Assert.Equal(seeded.Id, info.UserId);
        Assert.Equal(seeded.Name, info.Name);
        Assert.Equal(seeded.Email, info.Email);
        Assert.Equal("user", info.Role);
    }

    [Fact]
    public void Me_WithMissingClaims_Returns401()
    {
        _controller.ControllerContext.HttpContext.User =
            new System.Security.Claims.ClaimsPrincipal();

        var result = _controller.Me();

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    // ── /me/cards/default ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyDefaultCard_WithDefaultCard_ReturnsCard()
    {
        var seeded = SeedUser();
        SeedCard(seeded.Id, last4: "4242", isDefault: true);
        _controller.ControllerContext.HttpContext.User = MakePrincipal(seeded);

        var result = await _controller.GetMyDefaultCard();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var card = Assert.IsType<AuthController.DefaultCard>(ok.Value);
        Assert.Equal("4242", card.Last4);
        Assert.Equal("Visa", card.CardType);
        Assert.Equal("12/32", card.ExpiryDate);
    }

    [Fact]
    public async Task GetMyDefaultCard_WithNoCards_Returns404()
    {
        var seeded = SeedUser();
        _controller.ControllerContext.HttpContext.User = MakePrincipal(seeded);

        var result = await _controller.GetMyDefaultCard();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetMyDefaultCard_ReturnsOnlyDefaultCard()
    {
        var seeded = SeedUser();
        SeedCard(seeded.Id, last4: "1111", isDefault: false);
        SeedCard(seeded.Id, last4: "4242", isDefault: true);
        _controller.ControllerContext.HttpContext.User = MakePrincipal(seeded);

        var result = await _controller.GetMyDefaultCard();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var card = Assert.IsType<AuthController.DefaultCard>(ok.Value);
        Assert.Equal("4242", card.Last4);
    }

    // ── PUT /me/cards/default ─────────────────────────────────────────────────

    private static AuthController.SaveCardRequest ValidCardRequest(string number = "4111111111111111") =>
        new() { CardNumber = number, CardType = "Visa", ExpiryDate = "12/32" };

    [Fact]
    public async Task SaveMyDefaultCard_NoExistingCard_CreatesCard()
    {
        var user = SeedUser();
        _controller.ControllerContext.HttpContext.User = MakePrincipal(user);

        await _controller.SaveMyDefaultCard(ValidCardRequest());

        Assert.Equal(1, await _db.Cards.CountAsync());
    }

    [Fact]
    public async Task SaveMyDefaultCard_NoExistingCard_ReturnsNoContent()
    {
        var user = SeedUser();
        _controller.ControllerContext.HttpContext.User = MakePrincipal(user);

        var result = await _controller.SaveMyDefaultCard(ValidCardRequest());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SaveMyDefaultCard_StoresCorrectLast4()
    {
        var user = SeedUser();
        _controller.ControllerContext.HttpContext.User = MakePrincipal(user);

        await _controller.SaveMyDefaultCard(ValidCardRequest("4111111111114242"));

        var card = await _db.Cards.FirstAsync();
        Assert.Equal("4242", card.Last4);
    }

    [Fact]
    public async Task SaveMyDefaultCard_NewCardIsDefault()
    {
        var user = SeedUser();
        _controller.ControllerContext.HttpContext.User = MakePrincipal(user);

        await _controller.SaveMyDefaultCard(ValidCardRequest());

        var card = await _db.Cards.FirstAsync();
        Assert.True(card.IsDefault);
    }

    [Fact]
    public async Task SaveMyDefaultCard_UpdatesExistingDefaultCard()
    {
        var user = SeedUser();
        SeedCard(user.Id, last4: "1111", isDefault: true);
        _controller.ControllerContext.HttpContext.User = MakePrincipal(user);

        await _controller.SaveMyDefaultCard(ValidCardRequest("4111111111114242"));

        Assert.Equal(1, await _db.Cards.CountAsync());
        var card = await _db.Cards.FirstAsync();
        Assert.Equal("4242", card.Last4);
    }

    [Fact]
    public async Task SaveMyDefaultCard_UpdatedCardRetainsIsDefault()
    {
        var user = SeedUser();
        SeedCard(user.Id, last4: "1111", isDefault: true);
        _controller.ControllerContext.HttpContext.User = MakePrincipal(user);

        await _controller.SaveMyDefaultCard(ValidCardRequest("4111111111114242"));

        var card = await _db.Cards.FirstAsync();
        Assert.True(card.IsDefault);
    }

    // ── /logout ───────────────────────────────────────────────────────────────

    [Fact]
    public void Logout_Returns204()
    {
        var result = _controller.Logout();
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void Logout_DeletesCookie()
    {
        var result = _controller.Logout();
        var cookies = _controller.HttpContext.Response.Headers["Set-Cookie"].ToString();
        // Deleted cookies have an expiry in the past
        Assert.Contains(AuthController.CookieName, cookies);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static System.Security.Claims.ClaimsPrincipal MakePrincipal(User user) =>
        new(new System.Security.Claims.ClaimsIdentity(
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role),
        ], "Test"));
}

// Minimal IWebHostEnvironment stub for tests
file sealed class FakeWebHostEnvironment(bool isProduction) : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = isProduction ? "Production" : "Development";
    public string ApplicationName { get; set; } = "Test";
    public string WebRootPath { get; set; } = "";
    public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = "";
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
}
