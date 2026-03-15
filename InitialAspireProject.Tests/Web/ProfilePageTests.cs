using Bunit;
using InitialAspireProject.Shared.Models;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using InitialAspireProject.Web.Services;
using Microsoft.Extensions.Localization;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class ProfilePageTests : Bunit.TestContext
{
    private readonly Mock<IProfileService> _profileServiceMock;

    public ProfilePageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);

        _profileServiceMock = new Mock<IProfileService>();
        _profileServiceMock.Setup(s => s.GetProfileAsync(default))
            .ReturnsAsync(new ProfileResponse
            {
                Email = "test@test.com",
                FullName = "Test User",
                Roles = ["User", "Admin"]
            });
        Services.AddSingleton(_profileServiceMock.Object);
    }

    [Fact]
    public void Profile_LoadsAndShowsUserInfo()
    {
        var cut = RenderComponent<Profile>();

        cut.WaitForState(() => !cut.Markup.Contains("spinner-border"));
        Assert.Contains("test@test.com", cut.Markup);
        Assert.Contains("Test User", cut.Markup);
    }

    [Fact]
    public void Profile_ShowsRoleBadges()
    {
        var cut = RenderComponent<Profile>();

        cut.WaitForState(() => !cut.Markup.Contains("spinner-border"));
        Assert.Contains("User", cut.Markup);
        Assert.Contains("Admin", cut.Markup);
    }

    [Fact]
    public void Profile_ShowsEditNameForm()
    {
        var cut = RenderComponent<Profile>();

        cut.WaitForState(() => !cut.Markup.Contains("spinner-border"));
        Assert.NotNull(cut.Find("input#profileFullName"));
    }

    [Fact]
    public void Profile_ShowsChangePasswordForm()
    {
        var cut = RenderComponent<Profile>();

        cut.WaitForState(() => !cut.Markup.Contains("spinner-border"));
        Assert.NotNull(cut.Find("input#currentPassword"));
        Assert.NotNull(cut.Find("input#newPassword"));
        Assert.NotNull(cut.Find("input#confirmNewPassword"));
    }

    [Fact]
    public void Profile_LoadError_ShowsErrorMessage()
    {
        _profileServiceMock.Setup(s => s.GetProfileAsync(default))
            .ReturnsAsync((ProfileResponse?)null);

        var cut = RenderComponent<Profile>();

        cut.WaitForState(() => cut.Markup.Contains("alert-danger"));
        Assert.Contains("ProfileLoadError", cut.Markup);
    }

    [Fact]
    public void Profile_UpdateName_Success_ShowsSuccessAlert()
    {
        _profileServiceMock.Setup(s => s.UpdateProfileAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new ProfileResult { Success = true });

        var cut = RenderComponent<Profile>();
        cut.WaitForState(() => !cut.Markup.Contains("spinner-border"));

        cut.Find("input#profileFullName").Change("New Name");
        cut.FindAll("form")[0].Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-success"));
        Assert.Contains("ProfileUpdateSuccess", cut.Markup);
    }

    [Fact]
    public void Profile_UpdateName_Failure_ShowsDangerAlert()
    {
        _profileServiceMock.Setup(s => s.UpdateProfileAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new ProfileResult { Success = false, Message = "Update failed" });

        var cut = RenderComponent<Profile>();
        cut.WaitForState(() => !cut.Markup.Contains("spinner-border"));

        cut.Find("input#profileFullName").Change("New Name");
        cut.FindAll("form")[0].Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-danger"));
        Assert.Contains("Update failed", cut.Markup);
    }

    [Fact]
    public void Profile_ChangePassword_Success_ShowsSuccessAlert()
    {
        _profileServiceMock.Setup(s => s.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new ProfileResult { Success = true });

        var cut = RenderComponent<Profile>();
        cut.WaitForState(() => !cut.Markup.Contains("spinner-border"));

        cut.Find("input#currentPassword").Change("OldPass123$");
        cut.Find("input#newPassword").Change("NewPass123$");
        cut.Find("input#confirmNewPassword").Change("NewPass123$");
        cut.FindAll("form")[1].Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-success"));
        Assert.Contains("PasswordChangeSuccess", cut.Markup);
    }

    [Fact]
    public void Profile_ChangePassword_Failure_ShowsDangerAlert()
    {
        _profileServiceMock.Setup(s => s.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new ProfileResult { Success = false, Message = "Wrong password" });

        var cut = RenderComponent<Profile>();
        cut.WaitForState(() => !cut.Markup.Contains("spinner-border"));

        cut.Find("input#currentPassword").Change("WrongPass");
        cut.Find("input#newPassword").Change("NewPass123$");
        cut.Find("input#confirmNewPassword").Change("NewPass123$");
        cut.FindAll("form")[1].Submit();

        cut.WaitForState(() => cut.Markup.Contains("alert-danger"));
        Assert.Contains("Wrong password", cut.Markup);
    }
}
