using InitialAspireProject.Shared.Constants;

namespace InitialAspireProject.Tests.Shared;

public class PermissionConstantsTests
{
    [Fact]
    public void All_ContainsCanViewSettings()
    {
        Assert.Contains(PermissionConstants.CanViewSettings, PermissionConstants.All);
    }

    [Fact]
    public void All_ContainsCanManageUsers()
    {
        Assert.Contains(PermissionConstants.CanManageUsers, PermissionConstants.All);
    }

    [Fact]
    public void All_ContainsCanViewReports()
    {
        Assert.Contains(PermissionConstants.CanViewReports, PermissionConstants.All);
    }

    [Fact]
    public void All_ContainsCanManagePermissions()
    {
        Assert.Contains(PermissionConstants.CanManagePermissions, PermissionConstants.All);
    }

    [Fact]
    public void All_HasExpectedCount()
    {
        Assert.Equal(4, PermissionConstants.All.Length);
    }

    [Fact]
    public void ClaimType_IsPermission()
    {
        Assert.Equal("Permission", PermissionConstants.ClaimType);
    }
}
