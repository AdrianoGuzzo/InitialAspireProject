using System.Net;
using System.Text.Json;
using InitialAspireProject.Bff.Controllers;
using InitialAspireProject.Bff.Services;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InitialAspireProject.Tests.Bff;

public class AuthControllerTests
{
    private static AuthController CreateController(Mock<IIdentityProxyService> identityProxy, string? bearerToken = null)
    {
        var controller = new AuthController(identityProxy.Object);
        var httpContext = new DefaultHttpContext();
        if (bearerToken is not null)
            httpContext.Request.Headers.Authorization = $"Bearer {bearerToken}";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static HttpResponseMessage OkJson(object body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage ErrorJson(HttpStatusCode status, object body) => new(status)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task Login_Success_Returns200WithTokens()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.LoginAsync(It.IsAny<LoginModel>()))
            .ReturnsAsync(OkJson(new { token = "jwt", refreshToken = "rt" }));

        var controller = CreateController(proxy);
        var result = await controller.Login(new LoginModel { Email = "a@b.c", Password = "pass" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Contains("jwt", content.Content);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.LoginAsync(It.IsAny<LoginModel>()))
            .ReturnsAsync(ErrorJson(HttpStatusCode.Unauthorized, new { code = "InvalidCredentials", message = "Invalid" }));

        var controller = CreateController(proxy);
        var result = await controller.Login(new LoginModel { Email = "a@b.c", Password = "wrong" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(401, content.StatusCode);
    }

    [Fact]
    public async Task Register_Success_Returns200()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.RegisterAsync(It.IsAny<RegisterModel>()))
            .ReturnsAsync(OkJson(new { message = "ok" }));

        var controller = CreateController(proxy);
        var result = await controller.Register(new RegisterModel { Email = "a@b.c", Password = "pass", FullName = "Test" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
    }

    [Fact]
    public async Task Register_ValidationErrors_Returns400()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.RegisterAsync(It.IsAny<RegisterModel>()))
            .ReturnsAsync(ErrorJson(HttpStatusCode.BadRequest, new { errors = new[] { "Password too short" } }));

        var controller = CreateController(proxy);
        var result = await controller.Register(new RegisterModel { Email = "a@b.c", Password = "p", FullName = "Test" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(400, content.StatusCode);
    }

    [Fact]
    public async Task Refresh_Success_ReturnsNewTokens()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.RefreshAsync(It.IsAny<RefreshTokenRequest>()))
            .ReturnsAsync(OkJson(new { token = "new-jwt", refreshToken = "new-rt" }));

        var controller = CreateController(proxy);
        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "old-rt" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Contains("new-jwt", content.Content);
    }

    [Fact]
    public async Task Revoke_WithBearerToken_ForwardsToProxy()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.RevokeAsync(It.IsAny<RevokeTokenRequest>(), "my-token"))
            .ReturnsAsync(OkJson(new { message = "ok" }));

        var controller = CreateController(proxy, bearerToken: "my-token");
        var result = await controller.Revoke(new RevokeTokenRequest { RefreshToken = "rt" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        proxy.Verify(x => x.RevokeAsync(It.IsAny<RevokeTokenRequest>(), "my-token"), Times.Once);
    }

    [Fact]
    public async Task Revoke_WithoutBearerToken_Returns401()
    {
        var proxy = new Mock<IIdentityProxyService>();
        var controller = CreateController(proxy);

        var result = await controller.Revoke(new RevokeTokenRequest { RefreshToken = "rt" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ForgotPassword_Always_Returns200()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordModel>()))
            .ReturnsAsync(OkJson(new { message = "ok" }));

        var controller = CreateController(proxy);
        var result = await controller.ForgotPassword(new ForgotPasswordModel { Email = "a@b.c" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_Success_Returns200()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.ConfirmEmailAsync(It.IsAny<ConfirmEmailModel>()))
            .ReturnsAsync(OkJson(new { message = "confirmed" }));

        var controller = CreateController(proxy);
        var result = await controller.ConfirmEmail(new ConfirmEmailModel { Email = "a@b.c", Token = "tok" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
    }

    [Fact]
    public async Task ResendActivation_Success_Returns200()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.ResendActivationAsync(It.IsAny<ForgotPasswordModel>()))
            .ReturnsAsync(OkJson(new { message = "sent" }));

        var controller = CreateController(proxy);
        var result = await controller.ResendActivation(new ForgotPasswordModel { Email = "a@b.c" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_Success_Returns200()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordModel>()))
            .ReturnsAsync(OkJson(new { message = "reset" }));

        var controller = CreateController(proxy);
        var result = await controller.ResetPassword(new ResetPasswordModel { Email = "a@b.c", Token = "tok", NewPassword = "new", ConfirmPassword = "new" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
    }
}
