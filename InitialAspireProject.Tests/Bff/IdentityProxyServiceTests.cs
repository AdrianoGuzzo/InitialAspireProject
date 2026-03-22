using System.Net;
using System.Text;
using InitialAspireProject.Bff.Services;
using InitialAspireProject.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace InitialAspireProject.Tests.Bff;

public class IdentityProxyServiceTests
{
    private static (IdentityProxyService Service, CapturingHandler Handler) CreateService()
    {
        var handler = new CapturingHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var service = new IdentityProxyService(client, NullLogger<IdentityProxyService>.Instance);
        return (service, handler);
    }

    [Fact]
    public async Task LoginAsync_PostsToCorrectUrl()
    {
        var (service, handler) = CreateService();
        await service.LoginAsync(new LoginModel { Email = "a@b.c", Password = "pass" });

        Assert.Equal(HttpMethod.Post, handler.CapturedMethod);
        Assert.Equal("/auth/login", handler.CapturedUrl);
    }

    [Fact]
    public async Task RegisterAsync_PostsToCorrectUrl()
    {
        var (service, handler) = CreateService();
        await service.RegisterAsync(new RegisterModel { Email = "a@b.c", Password = "pass", FullName = "Test" });

        Assert.Equal("/auth/register", handler.CapturedUrl);
    }

    [Fact]
    public async Task RefreshAsync_PostsToCorrectUrl()
    {
        var (service, handler) = CreateService();
        await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = "rt" });

        Assert.Equal("/auth/refresh", handler.CapturedUrl);
    }

    [Fact]
    public async Task RevokeAsync_IncludesBearerToken()
    {
        var (service, handler) = CreateService();
        await service.RevokeAsync(new RevokeTokenRequest { RefreshToken = "rt" }, "my-jwt");

        Assert.Equal("/auth/revoke", handler.CapturedUrl);
        Assert.Equal("Bearer my-jwt", handler.CapturedAuthorization);
    }

    [Fact]
    public async Task ForgotPasswordAsync_PostsToCorrectUrl()
    {
        var (service, handler) = CreateService();
        await service.ForgotPasswordAsync(new ForgotPasswordModel { Email = "a@b.c" });

        Assert.Equal("/auth/forgot-password", handler.CapturedUrl);
    }

    [Fact]
    public async Task GetProfileAsync_UsesGetWithBearer()
    {
        var (service, handler) = CreateService();
        await service.GetProfileAsync("my-jwt");

        Assert.Equal(HttpMethod.Get, handler.CapturedMethod);
        Assert.Equal("/auth/profile", handler.CapturedUrl);
        Assert.Equal("Bearer my-jwt", handler.CapturedAuthorization);
    }

    [Fact]
    public async Task UpdateProfileAsync_UsesPutWithBearer()
    {
        var (service, handler) = CreateService();
        await service.UpdateProfileAsync(new UpdateProfileModel { FullName = "New" }, "my-jwt");

        Assert.Equal(HttpMethod.Put, handler.CapturedMethod);
        Assert.Equal("/auth/profile", handler.CapturedUrl);
        Assert.Equal("Bearer my-jwt", handler.CapturedAuthorization);
    }

    [Fact]
    public async Task ChangePasswordAsync_PostsWithBearer()
    {
        var (service, handler) = CreateService();
        await service.ChangePasswordAsync(new ChangePasswordModel { CurrentPassword = "old", NewPassword = "new" }, "my-jwt");

        Assert.Equal("/auth/change-password", handler.CapturedUrl);
        Assert.Equal("Bearer my-jwt", handler.CapturedAuthorization);
    }

    [Fact]
    public async Task LoginAsync_SerializesBodyAsJson()
    {
        var (service, handler) = CreateService();
        await service.LoginAsync(new LoginModel { Email = "a@b.c", Password = "pass" });

        Assert.NotNull(handler.CapturedBody);
        Assert.Contains("a@b.c", handler.CapturedBody);
    }

    [Fact]
    public async Task ConfirmEmailAsync_PostsToCorrectUrl()
    {
        var (service, handler) = CreateService();
        await service.ConfirmEmailAsync(new ConfirmEmailModel { Email = "a@b.c", Token = "tok" });

        Assert.Equal("/auth/confirm-email", handler.CapturedUrl);
    }

    [Fact]
    public async Task ResendActivationAsync_PostsToCorrectUrl()
    {
        var (service, handler) = CreateService();
        await service.ResendActivationAsync(new ForgotPasswordModel { Email = "a@b.c" });

        Assert.Equal("/auth/resend-activation", handler.CapturedUrl);
    }

    [Fact]
    public async Task ResetPasswordAsync_PostsToCorrectUrl()
    {
        var (service, handler) = CreateService();
        await service.ResetPasswordAsync(new ResetPasswordModel { Email = "a@b.c", Token = "tok", NewPassword = "new", ConfirmPassword = "new" });

        Assert.Equal("/auth/reset-password", handler.CapturedUrl);
    }

    [Fact]
    public async Task LoginAsync_ForwardsAcceptLanguageHeader()
    {
        var (service, handler) = CreateService();
        await service.LoginAsync(new LoginModel { Email = "a@b.c", Password = "pass" }, "en");

        Assert.Equal("en", handler.CapturedAcceptLanguage);
    }

    [Fact]
    public async Task LoginAsync_WithoutAcceptLanguage_DoesNotSetHeader()
    {
        var (service, handler) = CreateService();
        await service.LoginAsync(new LoginModel { Email = "a@b.c", Password = "pass" });

        Assert.Null(handler.CapturedAcceptLanguage);
    }

    internal sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpMethod? CapturedMethod { get; private set; }
        public string? CapturedUrl { get; private set; }
        public string? CapturedAuthorization { get; private set; }
        public string? CapturedBody { get; private set; }
        public string? CapturedAcceptLanguage { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CapturedMethod = request.Method;
            CapturedUrl = request.RequestUri?.AbsolutePath;
            CapturedAuthorization = request.Headers.Authorization?.ToString();
            CapturedAcceptLanguage = request.Headers.AcceptLanguage.Count > 0
                ? request.Headers.AcceptLanguage.ToString()
                : null;
            if (request.Content is not null)
                CapturedBody = await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }
    }
}
