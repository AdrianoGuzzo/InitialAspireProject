using Blazored.LocalStorage;
using InitialAspireProject.Web.Services;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class ThemeServiceTests
{
    private readonly Mock<ILocalStorageService> _mockLocalStorage = new();

    private ThemeService CreateService() => new(_mockLocalStorage.Object);

    [Fact]
    public void CurrentTheme_DefaultIsDark()
    {
        var service = CreateService();

        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public async Task InitializeAsync_KeepsDarkTheme()
    {
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public async Task InitializeAsync_ReadsFromLocalStorage()
    {
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync("selected-theme", default))
            .ReturnsAsync("flatly");
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal("flatly", service.CurrentTheme);
    }

    [Fact]
    public async Task InitializeAsync_InvalidStoredTheme_KeepsDark()
    {
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync("selected-theme", default))
            .ReturnsAsync("nonexistent");
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_ValidTheme_ChangesCurrentTheme()
    {
        var service = CreateService();

        await service.SetThemeAsync("flatly");

        Assert.Equal("flatly", service.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_ValidTheme_PersistsToLocalStorage()
    {
        var service = CreateService();

        await service.SetThemeAsync("flatly");

        _mockLocalStorage.Verify(x => x.SetItemAsStringAsync("selected-theme", "flatly", default), Times.Once);
    }

    [Fact]
    public async Task SetThemeAsync_InvalidTheme_DoesNotChange()
    {
        var service = CreateService();

        await service.SetThemeAsync("nonexistent-theme");

        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_ValidTheme_FiresOnThemeChanged()
    {
        var service = CreateService();
        var fired = false;
        service.OnThemeChanged += () => fired = true;

        await service.SetThemeAsync("cerulean");

        Assert.True(fired);
    }

    [Fact]
    public async Task SetThemeAsync_InvalidTheme_DoesNotFireOnThemeChanged()
    {
        var service = CreateService();
        var fired = false;
        service.OnThemeChanged += () => fired = true;

        await service.SetThemeAsync("invalid");

        Assert.False(fired);
    }

    [Fact]
    public void AvailableThemes_ContainsTenThemes()
    {
        var service = CreateService();

        Assert.Equal(10, service.AvailableThemes.Count);
    }

    [Fact]
    public void AvailableThemes_ContainsDark()
    {
        var service = CreateService();

        Assert.True(service.AvailableThemes.ContainsKey("dark"));
    }

    [Fact]
    public void GetCurrentThemeInfo_Default_ReturnsDarkThemeInfo()
    {
        var service = CreateService();

        var info = service.GetCurrentThemeInfo();

        Assert.Equal("Dark", info.Name);
        Assert.Equal("dark", info.BadgeColor);
    }

    [Fact]
    public async Task GetCurrentThemeInfo_AfterSetTheme_ReturnsNewThemeInfo()
    {
        var service = CreateService();
        await service.SetThemeAsync("flatly");

        var info = service.GetCurrentThemeInfo();

        Assert.Equal("Flatly", info.Name);
    }
}
