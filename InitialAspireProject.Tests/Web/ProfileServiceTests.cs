using System.Text;
using System.Text.Json;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;
using InitialAspireProject.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class ProfileServiceTests
{
    private static (ProfileService Service, Mock<IHttpContextAccessor> Accessor) CreateService(HttpMessageHandler handler, string? token = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var (accessor, _) = SetupSession(token);
        var service = new ProfileService(httpClient, accessor.Object, NullLogger<ProfileService>.Instance);
        return (service, accessor);
    }

    private static (Mock<IHttpContextAccessor> Accessor, Mock<ISession> Session) SetupSession(string? token)
    {
        var sessionMock = new Mock<ISession>();
        if (token is not null)
        {
            byte[]? bytes = Encoding.UTF8.GetBytes(token);
            sessionMock.Setup(x => x.TryGetValue(SessionConstants.TokenKey, out bytes)).Returns(true);
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

    // --- GetProfileAsync ---

    [Fact]
    public async Task GetProfileAsync_Success_ReturnsProfile()
    {
        var profile = new ProfileResponse { Email = "test@test.com", FullName = "Test User", Roles = ["User"] };
        var json = JsonSerializer.Serialize(profile);
        var handler = new StubHttpHandler(HttpStatusCode.OK, json);
        var (service, _) = CreateService(handler, "my-token");

        var result = await service.GetProfileAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("test@test.com", result.Email);
        Assert.Equal("Test User", result.FullName);
        Assert.Contains("User", result.Roles);
    }

    [Fact]
    public async Task GetProfileAsync_WithToken_SetsAuthorizationHeader()
    {
        var profile = new ProfileResponse { Email = "test@test.com", FullName = "Test", Roles = [] };
        var json = JsonSerializer.Serialize(profile);
        var capturer = new CapturingHandler(HttpStatusCode.OK, json);
        var (service, _) = CreateService(capturer, "my-jwt-token");

        await service.GetProfileAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Bearer my-jwt-token", capturer.CapturedAuthorization);
    }

    [Fact]
    public async Task GetProfileAsync_NetworkError_ReturnsNull()
    {
        var handler = new ThrowingHttpHandler();
        var (service, _) = CreateService(handler);

        var result = await service.GetProfileAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // --- UpdateProfileAsync ---

    [Fact]
    public async Task UpdateProfileAsync_Success_ReturnsSuccess()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "\"Profile updated\"");
        var (service, _) = CreateService(handler, "token");

        var result = await service.UpdateProfileAsync("New Name", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidationError_ReturnsErrorMessage()
    {
        var errors = JsonSerializer.Serialize(new[]
        {
            new { Code = "InvalidName", Description = "Nome inválido." }
        });
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest, errors);
        var (service, _) = CreateService(handler, "token");

        var result = await service.UpdateProfileAsync("", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Nome inválido.", result.Message);
    }

    [Fact]
    public async Task UpdateProfileAsync_NetworkError_ReturnsInternalError()
    {
        var handler = new ThrowingHttpHandler();
        var (service, _) = CreateService(handler);

        var result = await service.UpdateProfileAsync("Name", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Erro de conexão. Tente novamente mais tarde.", result.Message);
    }

    // --- ChangePasswordAsync ---

    [Fact]
    public async Task ChangePasswordAsync_Success_ReturnsSuccess()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "\"Password changed\"");
        var (service, _) = CreateService(handler, "token");

        var result = await service.ChangePasswordAsync("OldPass123$", "NewPass123$", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongPassword_ReturnsError()
    {
        var errors = JsonSerializer.Serialize(new[]
        {
            new { Code = "PasswordMismatch", Description = "Senha atual incorreta." }
        });
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest, errors);
        var (service, _) = CreateService(handler, "token");

        var result = await service.ChangePasswordAsync("WrongPass", "NewPass123$", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Senha atual incorreta.", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_NetworkError_ReturnsInternalError()
    {
        var handler = new ThrowingHttpHandler();
        var (service, _) = CreateService(handler);

        var result = await service.ChangePasswordAsync("Old", "New", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Erro de conexão. Tente novamente mais tarde.", result.Message);
    }

    private sealed class StubHttpHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        public string? CapturedAuthorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CapturedAuthorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class ThrowingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Network error");
    }
}
