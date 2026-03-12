using Blazored.LocalStorage;

namespace InitialAspireProject.Web.Services;

public class ThemeService
{
    private readonly ILocalStorageService _localStorage;
    private string _currentTheme = "pulse";

    public event Action? OnThemeChanged;

    public ThemeService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public string CurrentTheme => _currentTheme;

    public Dictionary<string, ThemeInfo> AvailableThemes => new()
    {
        { "default", new ThemeInfo("Default", "ThemeDescDefault", "primary", "https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css") },
        { "dark", new ThemeInfo("Dark", "ThemeDescDark", "dark", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/darkly/bootstrap.min.css") },
        { "cerulean", new ThemeInfo("Cerulean", "ThemeDescCerulean", "info", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/cerulean/bootstrap.min.css") },
        { "cosmo", new ThemeInfo("Cosmo", "ThemeDescCosmo", "primary", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/cosmo/bootstrap.min.css") },
        { "flatly", new ThemeInfo("Flatly", "ThemeDescFlatly", "success", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/flatly/bootstrap.min.css") },
        { "journal", new ThemeInfo("Journal", "ThemeDescJournal", "warning", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/journal/bootstrap.min.css") },
        { "lumen", new ThemeInfo("Lumen", "ThemeDescLumen", "light", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/lumen/bootstrap.min.css") },
        { "pulse", new ThemeInfo("Pulse", "ThemeDescPulse", "danger", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/pulse/bootstrap.min.css") },
        { "sandstone", new ThemeInfo("Sandstone", "ThemeDescSandstone", "secondary", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/sandstone/bootstrap.min.css") },
        { "united", new ThemeInfo("United", "ThemeDescUnited", "primary", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/united/bootstrap.min.css") }
    };

    public async Task InitializeAsync()
    {
        try
        {
            var result = await _localStorage.GetItemAsStringAsync("selected-theme");
            if (!string.IsNullOrEmpty(result) && AvailableThemes.ContainsKey(result))
            {
                _currentTheme = result;
            }
        }
        catch
        {
            // localStorage not available during SSR
        }
    }

    public async Task SetThemeAsync(string themeKey)
    {
        if (AvailableThemes.ContainsKey(themeKey))
        {
            _currentTheme = themeKey;
            try
            {
                await _localStorage.SetItemAsStringAsync("selected-theme", themeKey);
            }
            catch
            {
                // localStorage not available during SSR
            }
            OnThemeChanged?.Invoke();
        }
    }

    public ThemeInfo GetCurrentThemeInfo()
    {
        return AvailableThemes[_currentTheme];
    }
}

public record ThemeInfo(string Name, string Description, string BadgeColor, string CssUrl);
