using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EventTicketing.Api.Controllers;
using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
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
    private const string TestEmail    = "alice@example.com";

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
                ["Jwt:Key"]      = "test-jwt-key-that-is-32-characters!",
                ["Jwt:Issuer"]   = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
            })
            .Build();

        _controller = new AuthController(_db, new TokenService(config));
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

    // ── Login success ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200()
    {
        SeedUser();
        var result = await _controller.Login(new AuthController.LoginRequest
            { Email = TestEmail, Password = TestPassword });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        SeedUser();
        var ok = (OkObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = TestPassword })).Result!;

        var response = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsCorrectUserInfo()
    {
        var seeded = SeedUser();
        var ok = (OkObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = TestPassword })).Result!;

        var response = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.Equal(seeded.Id,    response.UserId);
        Assert.Equal(seeded.Name,  response.Name);
        Assert.Equal(seeded.Email, response.Email);
        Assert.Equal("user",       response.Role);
    }

    [Fact]
    public async Task Login_AdminUser_ReturnsAdminRole()
    {
        SeedUser(role: "admin");
        var ok = (OkObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = TestPassword })).Result!;

        var response = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.Equal("admin", response.Role);
    }

    [Fact]
    public async Task Login_TokenContainsCorrectClaims()
    {
        SeedUser();
        var ok = (OkObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = TestPassword })).Result!;

        var response = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);

        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Email && c.Value == TestEmail);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role  && c.Value == "user");
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
        // Both bad-email and bad-password return the same generic message
        SeedUser();
        var badPassword = (UnauthorizedObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = TestEmail, Password = "wrong" })).Result!;

        var badEmail = (UnauthorizedObjectResult)(await _controller.Login(
            new AuthController.LoginRequest { Email = "x@x.com", Password = TestPassword })).Result!;

        Assert.Equal(badPassword.Value?.ToString(), badEmail.Value?.ToString());
    }
}
