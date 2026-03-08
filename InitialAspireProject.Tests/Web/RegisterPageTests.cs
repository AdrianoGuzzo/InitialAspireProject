using Bunit;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using InitialAspireProject.Web.Services;
using Microsoft.Extensions.Localization;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class RegisterPageTests : Bunit.TestContext
{
    private readonly Mock<IRegisterService> _registerServiceMock;

    public RegisterPageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);

        _registerServiceMock = new Mock<IRegisterService>();
        Services.AddSingleton(_registerServiceMock.Object);
    }

    [Fact]
    public void Register_InitialRender_ShowsForm()
    {
        var cut = RenderComponent<Register>();

        Assert.NotNull(cut.Find("input#fullName"));
        Assert.NotNull(cut.Find("input#email"));
        Assert.NotNull(cut.Find("input#password"));
        Assert.NotNull(cut.Find("input#confirmPassword"));
        Assert.NotNull(cut.Find("input#acceptTerms"));
    }

    [Fact]
    public void Register_InitialRender_NoAlertMessages()
    {
        var cut = RenderComponent<Register>();

        Assert.DoesNotContain("alert-success", cut.Markup);
        Assert.DoesNotContain("alert-danger", cut.Markup);
    }

    [Fact]
    public void Register_SuccessfulRegistration_ShowsSuccessAlert()
    {
        _registerServiceMock.Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                            .ReturnsAsync(new RegisterResult { Success = true, Message = "Account created!" });

        var cut = RenderComponent<Register>();

        cut.Find("input#fullName").Change("Test User");
        cut.Find("input#email").Change("test@example.com");
        cut.Find("input#password").Change("Password123!");
        cut.Find("input#confirmPassword").Change("Password123!");
        cut.Find("input#acceptTerms").Change(true);
        cut.Find("form").Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-success"));
        Assert.Contains("alert-success", cut.Markup);
    }

    [Fact]
    public void Register_FailedRegistration_ShowsDangerAlert()
    {
        _registerServiceMock.Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                            .ReturnsAsync(new RegisterResult { Success = false, Message = "Email already exists" });

        var cut = RenderComponent<Register>();

        cut.Find("input#fullName").Change("Test User");
        cut.Find("input#email").Change("test@example.com");
        cut.Find("input#password").Change("Password123!");
        cut.Find("input#confirmPassword").Change("Password123!");
        cut.Find("input#acceptTerms").Change(true);
        cut.Find("form").Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-danger"));
        Assert.Contains("alert-danger", cut.Markup);
    }

    [Fact]
    public void Register_ServiceThrows_ShowsErrorMessage()
    {
        _registerServiceMock.Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                            .ThrowsAsync(new Exception("Network error"));

        var cut = RenderComponent<Register>();

        cut.Find("input#fullName").Change("Test User");
        cut.Find("input#email").Change("test@example.com");
        cut.Find("input#password").Change("Password123!");
        cut.Find("input#confirmPassword").Change("Password123!");
        cut.Find("input#acceptTerms").Change(true);
        cut.Find("form").Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-danger"));
        Assert.Contains("alert-danger", cut.Markup);
    }

    [Fact]
    public void Register_ShowsLoginLink()
    {
        var cut = RenderComponent<Register>();

        Assert.NotNull(cut.Find("a[href='/login']"));
    }
}
