using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InitialAspireProject.ApiIdentity;
using InitialAspireProject.Tests.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace InitialAspireProject.Tests.ApiIdentity;

public class TokenServiceTests
{
    private const string JwtKey = "super-secret-test-key-32-chars-ok!!";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    private static TokenService CreateService() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience
            })
            .Build());

    private static ClaimsPrincipal ParseToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
                ClockSkew = TimeSpan.Zero
            }, out _);
    }

    [Fact]
    public void CreateToken_ReturnsNonEmptyString()
    {
        var service = CreateService();
        var user = new ApplicationUserBuilder().Build();

        var token = service.CreateToken(user, []);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void CreateToken_ReturnsValidJwt()
    {
        var service = CreateService();
        var user = new ApplicationUserBuilder().Build();

        var token = service.CreateToken(user, []);

        Assert.NotNull(ParseToken(token));
    }

    [Fact]
    public void CreateToken_ContainsEmailClaim()
    {
        var service = CreateService();
        var user = new ApplicationUserBuilder().WithEmail("test@example.com").Build();

        var token = service.CreateToken(user, []);

        var principal = ParseToken(token);
        Assert.Equal("test@example.com", principal.FindFirst(ClaimTypes.Name)?.Value);
    }

    [Fact]
    public void CreateToken_ContainsUserIdClaim()
    {
        var service = CreateService();
        var user = new ApplicationUserBuilder().WithId("user-42").Build();

        var token = service.CreateToken(user, []);

        var principal = ParseToken(token);
        Assert.Equal("user-42", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void CreateToken_WithSingleRole_ContainsRoleClaim()
    {
        var service = CreateService();
        var user = new ApplicationUserBuilder().Build();

        var token = service.CreateToken(user, ["Admin"]);

        var principal = ParseToken(token);
        Assert.True(principal.IsInRole("Admin"));
    }

    [Fact]
    public void CreateToken_WithMultipleRoles_ContainsAllRoleClaims()
    {
        var service = CreateService();
        var user = new ApplicationUserBuilder().Build();

        var token = service.CreateToken(user, ["Admin", "User"]);

        var principal = ParseToken(token);
        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.IsInRole("User"));
    }

    [Fact]
    public void CreateToken_WithNoRoles_HasNoRoleClaims()
    {
        var service = CreateService();
        var user = new ApplicationUserBuilder().Build();

        var token = service.CreateToken(user, []);

        var principal = ParseToken(token);
        Assert.False(principal.IsInRole("Admin"));
        Assert.False(principal.IsInRole("User"));
    }

    [Fact]
    public void CreateToken_HasCorrectIssuerAndAudience()
    {
        var service = CreateService();
        var user = new ApplicationUserBuilder().Build();

        var token = service.CreateToken(user, []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
    }

    [Fact]
    public void CreateToken_ExpiresInApproximatelyOneHour()
    {
        var service = CreateService();
        var user = new ApplicationUserBuilder().Build();

        var token = service.CreateToken(user, []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expectedExpiry = DateTime.UtcNow.AddHours(1);
        Assert.InRange(jwt.ValidTo, expectedExpiry.AddMinutes(-1), expectedExpiry.AddMinutes(1));
    }
}
