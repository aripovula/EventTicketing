using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EventTicketing.Tests.Services;

public class TokenServiceTests
{
    private static TokenService BuildService() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-jwt-key-that-is-32-characters!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
            })
            .Build());

    private static User SampleUser(string role = "user") => new()
    {
        Id = 42,
        Name = "Jane Doe",
        Email = "jane@example.com",
        Role = role,
    };

    private static JwtSecurityToken ParseToken(string raw)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(raw);
    }

    [Fact]
    public void Generate_ReturnsNonEmptyString()
    {
        var token = BuildService().Generate(SampleUser());
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Generate_TokenContainsCorrectEmail()
    {
        var token = ParseToken(BuildService().Generate(SampleUser()));
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Email && c.Value == "jane@example.com");
    }

    [Fact]
    public void Generate_TokenContainsCorrectName()
    {
        var token = ParseToken(BuildService().Generate(SampleUser()));
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Name && c.Value == "Jane Doe");
    }

    [Fact]
    public void Generate_TokenContainsCorrectUserId()
    {
        var token = ParseToken(BuildService().Generate(SampleUser()));
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "42");
    }

    [Fact]
    public void Generate_TokenContainsUserRole()
    {
        var token = ParseToken(BuildService().Generate(SampleUser("user")));
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "user");
    }

    [Fact]
    public void Generate_TokenContainsAdminRole()
    {
        var token = ParseToken(BuildService().Generate(SampleUser("admin")));
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "admin");
    }

    [Fact]
    public void Generate_TokenExpiresInAboutEightHours()
    {
        var token = ParseToken(BuildService().Generate(SampleUser()));
        var diff = token.ValidTo - DateTime.UtcNow;
        Assert.InRange(diff.TotalHours, 7.9, 8.1);
    }

    [Fact]
    public void Generate_TokenHasCorrectIssuerAndAudience()
    {
        var token = ParseToken(BuildService().Generate(SampleUser()));
        Assert.Equal("TestIssuer", token.Issuer);
        Assert.Contains("TestAudience", token.Audiences);
    }

    [Fact]
    public void Generate_TokenPassesSignatureValidation()
    {
        var service = BuildService();
        var raw = service.Generate(SampleUser());

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-jwt-key-that-is-32-characters!"));

        handler.ValidateToken(raw, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TestIssuer",
            ValidAudience = "TestAudience",
            IssuerSigningKey = key,
        }, out var validated);

        Assert.NotNull(validated);
    }
}
