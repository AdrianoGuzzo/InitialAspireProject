using Bunit;
using Bunit.TestDoubles;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using Microsoft.Extensions.Localization;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class SettingsPageTests : Bunit.TestContext
{
    public SettingsPageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);
    }

    [Fact]
    public void Settings_Authorized_RendersTitle()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");
        authContext.SetPolicies("CanViewSettings");

        var cut = RenderComponent<Settings>();

        Assert.Contains("SettingsTitle", cut.Markup);
    }
}
