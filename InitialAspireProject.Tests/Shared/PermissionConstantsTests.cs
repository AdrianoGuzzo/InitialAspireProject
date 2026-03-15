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

    [Fact]
    public void AllPermissions_HasSameCountAsAll()
    {
        Assert.Equal(PermissionConstants.All.Length, PermissionConstants.AllPermissions.Length);
    }

    [Fact]
    public void AllPermissions_ContainsAllPermissionKeys()
    {
        foreach (var perm in PermissionConstants.All)
        {
            Assert.Contains(PermissionConstants.AllPermissions, p => p.Key == perm);
        }
    }

    [Fact]
    public void AllPermissions_AllHaveCategories()
    {
        foreach (var perm in PermissionConstants.AllPermissions)
        {
            Assert.False(string.IsNullOrEmpty(perm.Category));
        }
    }

    [Fact]
    public void AllPermissions_CategoriesStartWithSystem()
    {
        foreach (var perm in PermissionConstants.AllPermissions)
        {
            Assert.StartsWith("System.", perm.Category);
        }
    }

    [Fact]
    public void PermissionInfo_RecordEquality()
    {
        var a = new PermissionConstants.PermissionInfo("CanViewSettings", "System.Settings");
        var b = new PermissionConstants.PermissionInfo("CanViewSettings", "System.Settings");
        Assert.Equal(a, b);
    }
}
