using System.Security.Claims;
using InitialAspireProject.ApiIdentity.Repository;
using InitialAspireProject.ApiIdentity.Repository.Constants;
using InitialAspireProject.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace InitialAspireProject.Tests.ApiIdentity;

public class SeederTests
{
    private static (
        Mock<IServiceProvider> RootProvider,
        Mock<RoleManager<IdentityRole>> RoleManager,
        Mock<UserManager<ApplicationUser>> UserManager)
        SetupMocks(
            bool adminRoleExists = false,
            bool userRoleExists = false,
            ApplicationUser? existingAdmin = null)
    {
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        roleManagerMock.Setup(x => x.RoleExistsAsync(RoleConstants.Admin)).ReturnsAsync(adminRoleExists);
        roleManagerMock.Setup(x => x.RoleExistsAsync(RoleConstants.User)).ReturnsAsync(userRoleExists);
        roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Setup(x => x.FindByEmailAsync("admin@localhost")).ReturnsAsync(existingAdmin);
        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Admin123$")).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), RoleConstants.Admin)).ReturnsAsync(IdentityResult.Success);

        var scopeServiceProviderMock = new Mock<IServiceProvider>();
        scopeServiceProviderMock.Setup(x => x.GetService(typeof(RoleManager<IdentityRole>))).Returns(roleManagerMock.Object);
        scopeServiceProviderMock.Setup(x => x.GetService(typeof(UserManager<ApplicationUser>))).Returns(userManagerMock.Object);

        var serviceScopeMock = new Mock<IServiceScope>();
        serviceScopeMock.Setup(x => x.ServiceProvider).Returns(scopeServiceProviderMock.Object);

        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);

        var rootProviderMock = new Mock<IServiceProvider>();
        rootProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);

        return (rootProviderMock, roleManagerMock, userManagerMock);
    }

    [Fact]
    public async Task SeedAsync_WhenNothingExists_CreatesRolesAndAdminUser()
    {
        var (provider, roleManager, userManager) = SetupMocks(
            adminRoleExists: false,
            userRoleExists: false,
            existingAdmin: null);

        await Seeder.SeedAsync(provider.Object);

        roleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == RoleConstants.Admin)), Times.Once);
        roleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == RoleConstants.User)), Times.Once);
        userManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Admin123$"), Times.Once);
        userManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), RoleConstants.Admin), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenEverythingExists_SkipsCreation()
    {
        var existingAdmin = new ApplicationUser { FullName = "Admin User", Email = "admin@localhost", UserName = "admin@localhost" };
        var (provider, roleManager, userManager) = SetupMocks(
            adminRoleExists: true,
            userRoleExists: true,
            existingAdmin: existingAdmin);

        await Seeder.SeedAsync(provider.Object);

        roleManager.Verify(x => x.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
        userManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        userManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenOnlyAdminRoleMissing_CreatesOnlyAdminRole()
    {
        var existingAdmin = new ApplicationUser { FullName = "Admin User", Email = "admin@localhost", UserName = "admin@localhost" };
        var (provider, roleManager, _) = SetupMocks(
            adminRoleExists: false,
            userRoleExists: true,
            existingAdmin: existingAdmin);

        await Seeder.SeedAsync(provider.Object);

        roleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == RoleConstants.Admin)), Times.Once);
        roleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == RoleConstants.User)), Times.Never);
    }

    // --- SeedPermissionsAsync ---

    private static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock(
        Dictionary<string, List<Claim>>? existingClaims = null)
    {
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        var adminRole = new IdentityRole(RoleConstants.Admin) { Id = "admin-id" };
        var userRole = new IdentityRole(RoleConstants.User) { Id = "user-id" };

        roleManagerMock.Setup(x => x.FindByNameAsync(RoleConstants.Admin)).ReturnsAsync(adminRole);
        roleManagerMock.Setup(x => x.FindByNameAsync(RoleConstants.User)).ReturnsAsync(userRole);

        var adminClaims = existingClaims?.GetValueOrDefault(RoleConstants.Admin) ?? [];
        var userClaims = existingClaims?.GetValueOrDefault(RoleConstants.User) ?? [];

        roleManagerMock.Setup(x => x.GetClaimsAsync(adminRole)).ReturnsAsync(adminClaims);
        roleManagerMock.Setup(x => x.GetClaimsAsync(userRole)).ReturnsAsync(userClaims);
        roleManagerMock.Setup(x => x.AddClaimAsync(It.IsAny<IdentityRole>(), It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        return roleManagerMock;
    }

    [Fact]
    public async Task SeedPermissionsAsync_AdminRole_GetsAllPermissions()
    {
        var roleManagerMock = CreateRoleManagerMock();

        await Seeder.SeedPermissionsAsync(roleManagerMock.Object);

        foreach (var permission in PermissionConstants.All)
        {
            roleManagerMock.Verify(x => x.AddClaimAsync(
                It.Is<IdentityRole>(r => r.Name == RoleConstants.Admin),
                It.Is<Claim>(c => c.Type == PermissionConstants.ClaimType && c.Value == permission)),
                Times.Once);
        }
    }

    [Fact]
    public async Task SeedPermissionsAsync_UserRole_GetsOnlyCanViewReports()
    {
        var roleManagerMock = CreateRoleManagerMock();

        await Seeder.SeedPermissionsAsync(roleManagerMock.Object);

        roleManagerMock.Verify(x => x.AddClaimAsync(
            It.Is<IdentityRole>(r => r.Name == RoleConstants.User),
            It.Is<Claim>(c => c.Type == PermissionConstants.ClaimType && c.Value == PermissionConstants.CanViewReports)),
            Times.Once);

        roleManagerMock.Verify(x => x.AddClaimAsync(
            It.Is<IdentityRole>(r => r.Name == RoleConstants.User),
            It.Is<Claim>(c => c.Type == PermissionConstants.ClaimType && c.Value == PermissionConstants.CanManagePermissions)),
            Times.Never);
    }

    [Fact]
    public async Task SeedPermissionsAsync_Idempotent_SkipsExistingPermissions()
    {
        var existingClaims = new Dictionary<string, List<Claim>>
        {
            [RoleConstants.Admin] = [new Claim(PermissionConstants.ClaimType, PermissionConstants.CanViewSettings)],
            [RoleConstants.User] = [new Claim(PermissionConstants.ClaimType, PermissionConstants.CanViewReports)]
        };
        var roleManagerMock = CreateRoleManagerMock(existingClaims);

        await Seeder.SeedPermissionsAsync(roleManagerMock.Object);

        roleManagerMock.Verify(x => x.AddClaimAsync(
            It.Is<IdentityRole>(r => r.Name == RoleConstants.Admin),
            It.Is<Claim>(c => c.Value == PermissionConstants.CanViewSettings)),
            Times.Never);

        roleManagerMock.Verify(x => x.AddClaimAsync(
            It.Is<IdentityRole>(r => r.Name == RoleConstants.User),
            It.Is<Claim>(c => c.Value == PermissionConstants.CanViewReports)),
            Times.Never);
    }
}
