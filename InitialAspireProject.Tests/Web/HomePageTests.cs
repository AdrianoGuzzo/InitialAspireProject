using Bunit;
using Bunit.TestDoubles;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using Microsoft.Extensions.Localization;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class HomePageTests : Bunit.TestContext
{
    public HomePageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);
    }

    [Fact]
    public void Home_Authenticated_ShowsWelcomeMessage()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");

        var cut = RenderComponent<Home>();

        Assert.Contains("WelcomeUser", cut.Markup);
    }

    [Fact]
    public void Home_Authenticated_ShowsCounterAndWeatherLinks()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");

        var cut = RenderComponent<Home>();

        Assert.NotNull(cut.Find("a[href='/counter']"));
        Assert.NotNull(cut.Find("a[href='/weather']"));
    }

    [Fact]
    public void Home_NotAuthenticated_ShowsLoginButton()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetNotAuthorized();

        var cut = RenderComponent<Home>();

        Assert.Contains("LimitedAccess", cut.Markup);
    }

    [Fact]
    public void Home_ShowsTechStack()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");

        var cut = RenderComponent<Home>();

        Assert.Contains("TechStack", cut.Markup);
        Assert.Contains("ASP.NET Core", cut.Markup);
        Assert.Contains("Blazor", cut.Markup);
        Assert.Contains("PostgreSQL", cut.Markup);
    }

    [Fact]
    public void Home_ShowsAppFeatures()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");

        var cut = RenderComponent<Home>();

        Assert.Contains("AppFeatures", cut.Markup);
        Assert.Contains("ThemeSystemTitle", cut.Markup);
        Assert.Contains("InteractiveCounterTitle", cut.Markup);
        Assert.Contains("WeatherForecastTitle", cut.Markup);
    }
}
