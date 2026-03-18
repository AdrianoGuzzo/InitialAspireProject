using System.Security.Claims;
using InitialAspireProject.ApiIdentity.Controllers;
using InitialAspireProject.ApiIdentity.Resources;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;

namespace InitialAspireProject.Tests.ApiIdentity;

public class PermissionControllerTests
{
    private static (Mock<RoleManager<IdentityRole>>, PermissionController) CreateController()
    {
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        var localizer = new Mock<IStringLocalizer<AuthMessages>>();
        localizer.Setup(l => l[It.IsAny<string>()])
                 .Returns<string>(key => new LocalizedString(key, key));

        var controller = new PermissionController(roleManagerMock.Object, localizer.Object);
        return (roleManagerMock, controller);
    }

    [Fact]
    public void GetAllPermissions_ReturnsAllConstants()
    {
        var (_, controller) = CreateController();

        var result = controller.GetAllPermissions();

        var ok = Assert.IsType<OkObjectResult>(result);
        var permissions = Assert.IsType<string[]>(ok.Value);
        Assert.Equal(PermissionConstants.All.Length, permissions.Length);
    }

    [Fact]
    public async Task GetAllRolePermissions_ReturnsRolesWithPermissions()
    {
        var (roleManager, controller) = CreateController();
        var adminRole = new IdentityRole("Admin") { Id = "1" };
        var roles = new List<IdentityRole> { adminRole }.AsQueryable();

        roleManager.Setup(x => x.Roles).Returns(roles);
        roleManager.Setup(x => x.GetClaimsAsync(adminRole))
            .ReturnsAsync([new Claim(PermissionConstants.ClaimType, PermissionConstants.CanViewSettings)]);

        var result = await controller.GetAllRolePermissions();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<RolePermissionsDto>>(ok.Value);
        Assert.Single(list);
        Assert.Contains(PermissionConstants.CanViewSettings, list[0].Permissions);
    }

    [Fact]
    public async Task GetRolePermissions_ExistingRole_ReturnsPermissions()
    {
        var (roleManager, controller) = CreateController();
        var role = new IdentityRole("Admin") { Id = "1" };

        roleManager.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(role);
        roleManager.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync([new Claim(PermissionConstants.ClaimType, PermissionConstants.CanViewSettings)]);

        var result = await controller.GetRolePermissions("Admin");

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<RolePermissionsDto>(ok.Value);
        Assert.Equal("Admin", dto.RoleName);
        Assert.Contains(PermissionConstants.CanViewSettings, dto.Permissions);
    }

    [Fact]
    public async Task GetRolePermissions_NonExistentRole_ReturnsNotFound()
    {
        var (roleManager, controller) = CreateController();
        roleManager.Setup(x => x.FindByNameAsync("NonExistent")).ReturnsAsync((IdentityRole?)null);

        var result = await controller.GetRolePermissions("NonExistent");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AssignPermission_ValidRequest_ReturnsOk()
    {
        var (roleManager, controller) = CreateController();
        var role = new IdentityRole("Admin") { Id = "1" };

        roleManager.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(role);
        roleManager.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync([]);
        roleManager.Setup(x => x.AddClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        var result = await controller.AssignPermission("Admin",
            new AssignPermissionModel { Permission = PermissionConstants.CanViewSettings });

        Assert.IsType<OkObjectResult>(result);
        roleManager.Verify(x => x.AddClaimAsync(role, It.Is<Claim>(c =>
            c.Type == PermissionConstants.ClaimType && c.Value == PermissionConstants.CanViewSettings)), Times.Once);
    }

    [Fact]
    public async Task AssignPermission_InvalidPermission_ReturnsBadRequest()
    {
        var (_, controller) = CreateController();

        var result = await controller.AssignPermission("Admin",
            new AssignPermissionModel { Permission = "InvalidPerm" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AssignPermission_RoleNotFound_ReturnsNotFound()
    {
        var (roleManager, controller) = CreateController();
        roleManager.Setup(x => x.FindByNameAsync("NonExistent")).ReturnsAsync((IdentityRole?)null);

        var result = await controller.AssignPermission("NonExistent",
            new AssignPermissionModel { Permission = PermissionConstants.CanViewSettings });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AssignPermission_AlreadyAssigned_ReturnsConflict()
    {
        var (roleManager, controller) = CreateController();
        var role = new IdentityRole("Admin") { Id = "1" };

        roleManager.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(role);
        roleManager.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync([new Claim(PermissionConstants.ClaimType, PermissionConstants.CanViewSettings)]);

        var result = await controller.AssignPermission("Admin",
            new AssignPermissionModel { Permission = PermissionConstants.CanViewSettings });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task RemovePermission_ExistingPermission_ReturnsOk()
    {
        var (roleManager, controller) = CreateController();
        var role = new IdentityRole("Admin") { Id = "1" };
        var claim = new Claim(PermissionConstants.ClaimType, PermissionConstants.CanViewSettings);

        roleManager.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(role);
        roleManager.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync([claim]);
        roleManager.Setup(x => x.RemoveClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        var result = await controller.RemovePermission("Admin", PermissionConstants.CanViewSettings);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RemovePermission_RoleNotFound_ReturnsNotFound()
    {
        var (roleManager, controller) = CreateController();
        roleManager.Setup(x => x.FindByNameAsync("NonExistent")).ReturnsAsync((IdentityRole?)null);

        var result = await controller.RemovePermission("NonExistent", PermissionConstants.CanViewSettings);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RemovePermission_PermissionNotAssigned_ReturnsNotFound()
    {
        var (roleManager, controller) = CreateController();
        var role = new IdentityRole("Admin") { Id = "1" };

        roleManager.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(role);
        roleManager.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync([]);

        var result = await controller.RemovePermission("Admin", PermissionConstants.CanViewSettings);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
