using System.Net;
using System.Text.Json;
using InitialAspireProject.Bff.Controllers;
using InitialAspireProject.Bff.Services;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InitialAspireProject.Tests.Bff;

public class ProfileControllerTests
{
    private static ProfileController CreateController(Mock<IIdentityProxyService> identityProxy, string? bearerToken = null, string? acceptLanguage = null)
    {
        var controller = new ProfileController(identityProxy.Object);
        var httpContext = new DefaultHttpContext();
        if (bearerToken is not null)
            httpContext.Request.Headers.Authorization = $"Bearer {bearerToken}";
        if (acceptLanguage is not null)
            httpContext.Request.Headers.AcceptLanguage = acceptLanguage;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static HttpResponseMessage OkJson(object body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task GetProfile_WithToken_ReturnsProfile()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.GetProfileAsync("my-token", It.IsAny<string?>()))
            .ReturnsAsync(OkJson(new { email = "a@b.c", fullName = "Test", roles = new[] { "Admin" } }));

        var controller = CreateController(proxy, bearerToken: "my-token");
        var result = await controller.GetProfile();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Contains("a@b.c", content.Content);
    }

    [Fact]
    public async Task UpdateProfile_WithToken_ForwardsToProxy()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.UpdateProfileAsync(It.IsAny<UpdateProfileModel>(), "my-token", It.IsAny<string?>()))
            .ReturnsAsync(OkJson(new { message = "updated" }));

        var controller = CreateController(proxy, bearerToken: "my-token");
        var result = await controller.UpdateProfile(new UpdateProfileModel { FullName = "New Name" });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        proxy.Verify(x => x.UpdateProfileAsync(It.IsAny<UpdateProfileModel>(), "my-token", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithToken_ForwardsToProxy()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.ChangePasswordAsync(It.IsAny<ChangePasswordModel>(), "my-token", It.IsAny<string?>()))
            .ReturnsAsync(OkJson(new { message = "changed" }));

        var controller = CreateController(proxy, bearerToken: "my-token");
        var result = await controller.ChangePassword(new ChangePasswordModel
        {
            CurrentPassword = "old",
            NewPassword = "new"
        });

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ForwardsAcceptLanguageHeader()
    {
        var proxy = new Mock<IIdentityProxyService>();
        proxy.Setup(x => x.GetProfileAsync("my-token", "es"))
            .ReturnsAsync(OkJson(new { email = "a@b.c", fullName = "Test", roles = new[] { "Admin" } }));

        var controller = CreateController(proxy, bearerToken: "my-token", acceptLanguage: "es");
        await controller.GetProfile();

        proxy.Verify(x => x.GetProfileAsync("my-token", "es"), Times.Once);
    }
}
