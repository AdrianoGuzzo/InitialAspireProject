using Bunit;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using InitialAspireProject.Web.Services;
using Microsoft.Extensions.Localization;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class ForgotPasswordPageTests : Bunit.TestContext
{
    private readonly Mock<IForgotPasswordService> _serviceMock;

    public ForgotPasswordPageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);

        _serviceMock = new Mock<IForgotPasswordService>();
        Services.AddSingleton(_serviceMock.Object);
    }

    [Fact]
    public void ForgotPassword_InitialRender_ShowsEmailForm()
    {
        var cut = RenderComponent<ForgotPassword>();

        Assert.NotNull(cut.Find("input#email"));
        Assert.NotNull(cut.Find("button[type=submit]"));
    }

    [Fact]
    public void ForgotPassword_InitialRender_DoesNotShowSuccessMessage()
    {
        var cut = RenderComponent<ForgotPassword>();

        Assert.DoesNotContain("alert-success", cut.Markup);
    }

    [Fact]
    public void ForgotPassword_SubmitValidEmail_ShowsSuccessMessage()
    {
        _serviceMock.Setup(s => s.ForgotPasswordAsync(It.IsAny<string>(), default))
                    .ReturnsAsync(new ServiceResult { Success = true });

        var cut = RenderComponent<ForgotPassword>();

        cut.Find("input#email").Change("test@example.com");
        cut.Find("form").Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-success"));
        Assert.Contains("alert-success", cut.Markup);
    }

    [Fact]
    public void ForgotPassword_SubmitValidEmail_HidesFormAfterSuccess()
    {
        _serviceMock.Setup(s => s.ForgotPasswordAsync(It.IsAny<string>(), default))
                    .ReturnsAsync(new ServiceResult { Success = true });

        var cut = RenderComponent<ForgotPassword>();
        cut.Find("input#email").Change("user@example.com");
        cut.Find("form").Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-success"));
        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public void ForgotPassword_SubmitValidEmail_CallsServiceWithEmail()
    {
        _serviceMock.Setup(s => s.ForgotPasswordAsync("test@example.com", default))
                    .ReturnsAsync(new ServiceResult { Success = true });

        var cut = RenderComponent<ForgotPassword>();
        cut.Find("input#email").Change("test@example.com");
        cut.Find("form").Submit();

        _serviceMock.Verify(s => s.ForgotPasswordAsync("test@example.com", default), Times.Once);
    }
}
