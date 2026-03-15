using Bunit;
using Bunit.TestDoubles;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using InitialAspireProject.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class AdminPermissionsPageTests : Bunit.TestContext
{
    private readonly Mock<IPermissionService> _permissionServiceMock;

    public AdminPermissionsPageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);

        _permissionServiceMock = new Mock<IPermissionService>();
        _permissionServiceMock.Setup(x => x.GetAllPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PermissionConstants.All);
        _permissionServiceMock.Setup(x => x.GetAllRolePermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RolePermissionsDto { RoleName = "Admin", Permissions = [PermissionConstants.CanViewSettings] },
                new RolePermissionsDto { RoleName = "User", Permissions = [] }
            ]);
        Services.AddSingleton(_permissionServiceMock.Object);
    }

    [Fact]
    public void AdminPermissions_Authorized_RendersGrid()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");
        authContext.SetPolicies("CanManagePermissions");

        var cut = RenderComponent<AdminPermissions>();

        Assert.Contains("PermissionsTitle", cut.Markup);
        Assert.Contains("Admin", cut.Markup);
        Assert.Contains("User", cut.Markup);
    }

    [Fact]
    public void AdminPermissions_RendersAllPermissionNames()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");
        authContext.SetPolicies("CanManagePermissions");

        var cut = RenderComponent<AdminPermissions>();

        foreach (var perm in PermissionConstants.AllPermissions)
        {
            Assert.Contains($"Permission_{perm.Key}", cut.Markup);
        }
    }

    [Fact]
    public void AdminPermissions_RendersPermissionDescriptions()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");
        authContext.SetPolicies("CanManagePermissions");

        var cut = RenderComponent<AdminPermissions>();

        foreach (var perm in PermissionConstants.AllPermissions)
        {
            Assert.Contains($"Permission_{perm.Key}_Desc", cut.Markup);
        }
    }

    [Fact]
    public void AdminPermissions_RendersCategoryNodes()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");
        authContext.SetPolicies("CanManagePermissions");

        var cut = RenderComponent<AdminPermissions>();

        Assert.Contains("PermissionCategory_System", cut.Markup);
        Assert.Contains("PermissionCategory_Settings", cut.Markup);
        Assert.Contains("PermissionCategory_Users", cut.Markup);
        Assert.Contains("PermissionCategory_Reports", cut.Markup);
        Assert.Contains("PermissionCategory_Security", cut.Markup);
    }

    [Fact]
    public async Task AdminPermissions_ToggleOn_CallsAssignPermission()
    {
        _permissionServiceMock.Setup(x => x.AssignPermissionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");
        authContext.SetPolicies("CanManagePermissions");

        var cut = RenderComponent<AdminPermissions>();

        // Find an unchecked toggle (User role doesn't have CanViewSettings)
        var checkboxes = cut.FindAll("input[type='checkbox']");
        // The tree has 4 permissions x 2 roles = 8 checkboxes
        // Second column (User) of first permission = index 1
        var uncheckedBox = checkboxes[1]; // User + CanViewSettings
        await cut.InvokeAsync(() => uncheckedBox.Change(true));

        _permissionServiceMock.Verify(x => x.AssignPermissionAsync(
            "User", PermissionConstants.CanViewSettings, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AdminPermissions_TreeHasExpandCollapseIcons()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@localhost");
        authContext.SetPolicies("CanManagePermissions");

        var cut = RenderComponent<AdminPermissions>();

        // Category rows should have chevron-down icons (expanded by default)
        Assert.Contains("bi-chevron-down", cut.Markup);
    }
}
