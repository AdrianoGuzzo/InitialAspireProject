using InitialAspireProject.Web.Services;

namespace InitialAspireProject.Tests.Web;

public class ThemeServiceTests
{
    [Fact]
    public void CurrentTheme_DefaultIsDark()
    {
        var service = new ThemeService();

        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public async Task InitializeAsync_KeepsDarkTheme()
    {
        var service = new ThemeService();

        await service.InitializeAsync();

        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_ValidTheme_ChangesCurrentTheme()
    {
        var service = new ThemeService();

        await service.SetThemeAsync("flatly");

        Assert.Equal("flatly", service.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_InvalidTheme_DoesNotChange()
    {
        var service = new ThemeService();

        await service.SetThemeAsync("nonexistent-theme");

        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_ValidTheme_FiresOnThemeChanged()
    {
        var service = new ThemeService();
        var fired = false;
        service.OnThemeChanged += () => fired = true;

        await service.SetThemeAsync("cerulean");

        Assert.True(fired);
    }

    [Fact]
    public async Task SetThemeAsync_InvalidTheme_DoesNotFireOnThemeChanged()
    {
        var service = new ThemeService();
        var fired = false;
        service.OnThemeChanged += () => fired = true;

        await service.SetThemeAsync("invalid");

        Assert.False(fired);
    }

    [Fact]
    public void AvailableThemes_ContainsTenThemes()
    {
        var service = new ThemeService();

        Assert.Equal(10, service.AvailableThemes.Count);
    }

    [Fact]
    public void AvailableThemes_ContainsDark()
    {
        var service = new ThemeService();

        Assert.True(service.AvailableThemes.ContainsKey("dark"));
    }

    [Fact]
    public void GetCurrentThemeInfo_Default_ReturnsDarkThemeInfo()
    {
        var service = new ThemeService();

        var info = service.GetCurrentThemeInfo();

        Assert.Equal("Dark", info.Name);
        Assert.Equal("dark", info.BadgeColor);
    }

    [Fact]
    public async Task GetCurrentThemeInfo_AfterSetTheme_ReturnsNewThemeInfo()
    {
        var service = new ThemeService();
        await service.SetThemeAsync("flatly");

        var info = service.GetCurrentThemeInfo();

        Assert.Equal("Flatly", info.Name);
    }
}
