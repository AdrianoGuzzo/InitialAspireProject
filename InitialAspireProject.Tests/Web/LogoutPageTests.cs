using Bunit;
using InitialAspireProject.Web;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class LogoutPageTests : Bunit.TestContext
{
    public LogoutPageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);
    }

    [Fact]
    public void Logout_OnRender_NavigatesToLogin()
    {
        // HttpContext null avoids SignOutAsync call
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var authProvider = new JwtAuthStateProvider(mockAccessor.Object, Mock.Of<ILogger<JwtAuthStateProvider>>());
        Services.AddSingleton(authProvider);

        var cut = RenderComponent<Logout>();

        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        Assert.EndsWith("/login", nav.Uri);
    }
}
