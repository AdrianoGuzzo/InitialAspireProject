using System.Text;
using System.Text.Json;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class JwtAuthStateProviderTests
{
    private static JwtAuthStateProvider CreateProvider(IHttpContextAccessor accessor)
        => new(accessor, NullLogger<JwtAuthStateProvider>.Instance, new ServiceCollection().BuildServiceProvider());

    private static (Mock<IHttpContextAccessor> Accessor, Mock<ISession> Session) SetupSession(string? storedToken)
    {
        var sessionMock = new Mock<ISession>();
        if (storedToken is not null)
        {
            byte[]? tokenBytes = Encoding.UTF8.GetBytes(storedToken);
            sessionMock.Setup(x => x.TryGetValue(SessionConstants.TokenKey, out tokenBytes)).Returns(true);
        }
        else
        {
            byte[]? nullBytes = null;
            sessionMock.Setup(x => x.TryGetValue(It.IsAny<string>(), out nullBytes)).Returns(false);
        }

        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(x => x.Session).Returns(sessionMock.Object);

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(contextMock.Object);

        return (accessorMock, sessionMock);
    }

    // Creates a JWT with standard base64 encoding (not base64url)
    // because ParseBase64WithoutPadding uses Convert.FromBase64String
    private static string CreateJwt(Dictionary<string, object> payload)
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg":"HS256"}""")).TrimEnd('=');
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))).TrimEnd('=');
        return $"{header}.{payloadBase64}.fakesig";
    }

    private static string ValidJwt(string email = "test@example.com") => CreateJwt(new()
    {
        ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
        ["unique_name"] = email,
        ["nameid"] = "user-1"
    });

    private static string ExpiredJwt() => CreateJwt(new()
    {
        ["exp"] = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
        ["unique_name"] = "test@example.com"
    });

    [Fact]
    public async Task GetAuthenticationStateAsync_NoToken_ReturnsAnonymousUser()
    {
        var (accessor, _) = SetupSession(null);
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ValidToken_ReturnsAuthenticatedUser()
    {
        var (accessor, _) = SetupSession(ValidJwt());
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ValidToken_ExtractsEmailClaim()
    {
        var (accessor, _) = SetupSession(ValidJwt("user@example.com"));
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.Equal("user@example.com", state.User.FindFirst("unique_name")?.Value);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ValidToken_ExtractsUserIdClaim()
    {
        var (accessor, _) = SetupSession(ValidJwt());
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.Equal("user-1", state.User.FindFirst("nameid")?.Value);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ExpiredToken_ReturnsAnonymousUser()
    {
        var (accessor, _) = SetupSession(ExpiredJwt());
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ExpiredToken_RemovesTokenFromSession()
    {
        var (accessor, session) = SetupSession(ExpiredJwt());
        var provider = CreateProvider(accessor.Object);

        await provider.GetAuthenticationStateAsync();

        session.Verify(x => x.Remove(SessionConstants.TokenKey), Times.Once);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_TooManyJwtParts_ReturnsAnonymousUser()
    {
        var (accessor, _) = SetupSession("header.payload.sig.extra");
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_TooFewJwtParts_ReturnsAnonymousUser()
    {
        var (accessor, _) = SetupSession("header.payload");
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_MalformedToken_RemovesFromSession()
    {
        var (accessor, session) = SetupSession("header.payload");
        var provider = CreateProvider(accessor.Object);

        await provider.GetAuthenticationStateAsync();

        session.Verify(x => x.Remove(SessionConstants.TokenKey), Times.Once);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_InvalidBase64Payload_ReturnsAnonymousUser()
    {
        var (accessor, _) = SetupSession("header.not!!!valid_base64.sig");
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public void GetStoredToken_WhenTokenExists_ReturnsToken()
    {
        var token = ValidJwt();
        var (accessor, _) = SetupSession(token);
        var provider = CreateProvider(accessor.Object);

        var result = provider.GetStoredToken();

        Assert.Equal(token, result);
    }

    [Fact]
    public void GetStoredToken_WhenNoContext_ReturnsNull()
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var provider = CreateProvider(accessorMock.Object);

        var result = provider.GetStoredToken();

        Assert.Null(result);
    }

    [Fact]
    public void NotifyUserAuthentication_SetsAuthStateWithClaims()
    {
        var (accessor, _) = SetupSession(null);
        var provider = CreateProvider(accessor.Object);
        var token = ValidJwt("notify@example.com");
        AuthenticationState? notifiedState = null;
        provider.AuthenticationStateChanged += t => notifiedState = t.Result;

        provider.NotifyUserAuthentication(token);

        Assert.NotNull(notifiedState);
        Assert.True(notifiedState!.User.Identity?.IsAuthenticated);
        Assert.Equal("notify@example.com", notifiedState.User.FindFirst("unique_name")?.Value);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ArrayClaim_ParsesMultipleValues()
    {
        var jwt = CreateJwt(new()
        {
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            ["unique_name"] = "test@example.com",
            ["Permission"] = new[] { "CanViewSettings", "CanManageUsers" }
        });
        var (accessor, _) = SetupSession(jwt);
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        var permissions = state.User.FindAll("Permission").Select(c => c.Value).ToList();
        Assert.Contains("CanViewSettings", permissions);
        Assert.Contains("CanManageUsers", permissions);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_SinglePermissionClaim_Works()
    {
        var jwt = CreateJwt(new()
        {
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            ["unique_name"] = "test@example.com",
            ["Permission"] = "CanViewSettings"
        });
        var (accessor, _) = SetupSession(jwt);
        var provider = CreateProvider(accessor.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.Equal("CanViewSettings", state.User.FindFirst("Permission")?.Value);
    }
}
