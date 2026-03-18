using Bunit;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using InitialAspireProject.Web.Services;
using Microsoft.Extensions.Localization;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class ResetPasswordPageTests : Bunit.TestContext
{
    private readonly Mock<IResetPasswordService> _serviceMock;

    public ResetPasswordPageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);

        _serviceMock = new Mock<IResetPasswordService>();
        Services.AddSingleton(_serviceMock.Object);
    }

    [Fact]
    public void ResetPassword_MissingQueryParams_ShowsInvalidLink()
    {
        var cut = RenderComponent<ResetPassword>();

        Assert.Contains("InvalidResetLink", cut.Markup);
    }

    [Fact]
    public void ResetPassword_MissingQueryParams_DoesNotShowForm()
    {
        var cut = RenderComponent<ResetPassword>();

        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public void ResetPassword_WithValidParams_ShowsForm()
    {
        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        nav.NavigateTo("/reset-password?email=test@example.com&token=abc123");

        var cut = RenderComponent<ResetPassword>();

        Assert.NotNull(cut.Find("input#newPassword"));
        Assert.NotNull(cut.Find("input#confirmPassword"));
    }

    [Fact]
    public void ResetPassword_SuccessfulReset_ShowsSuccessMessage()
    {
        _serviceMock.Setup(s => s.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                    .ReturnsAsync(new ServiceResult { Success = true });

        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        nav.NavigateTo("/reset-password?email=test@example.com&token=abc123");

        var cut = RenderComponent<ResetPassword>();

        cut.Find("input#newPassword").Change("NewPassword1!");
        cut.Find("input#confirmPassword").Change("NewPassword1!");
        cut.Find("form").Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-success"));
        Assert.Contains("PasswordResetSuccessMsg", cut.Markup);
    }

    [Fact]
    public void ResetPassword_FailedReset_ShowsErrorMessage()
    {
        _serviceMock.Setup(s => s.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                    .ReturnsAsync(new ServiceResult { Success = false, Message = "Token expired" });

        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        nav.NavigateTo("/reset-password?email=test@example.com&token=abc123");

        var cut = RenderComponent<ResetPassword>();

        cut.Find("input#newPassword").Change("NewPassword1!");
        cut.Find("input#confirmPassword").Change("NewPassword1!");
        cut.Find("form").Submit();

        cut.WaitForState(() => cut.Markup.Contains("Token expired"));
        Assert.Contains("Token expired", cut.Markup);
    }

    [Fact]
    public void ResetPassword_SuccessfulReset_ShowsLoginLink()
    {
        _serviceMock.Setup(s => s.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                    .ReturnsAsync(new ServiceResult { Success = true });

        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        nav.NavigateTo("/reset-password?email=test@example.com&token=abc123");

        var cut = RenderComponent<ResetPassword>();

        cut.Find("input#newPassword").Change("NewPassword1!");
        cut.Find("input#confirmPassword").Change("NewPassword1!");
        cut.Find("form").Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-success"));
        Assert.NotNull(cut.Find("a[href='/login']"));
    }
}
