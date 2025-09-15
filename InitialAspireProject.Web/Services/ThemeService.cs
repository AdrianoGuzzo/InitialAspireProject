namespace InitialAspireProject.Web.Services;

public class ThemeService
{
    private string _currentTheme = "dark";

    public event Action? OnThemeChanged;

    public ThemeService()
    {

    }

    public string CurrentTheme => _currentTheme;

    public Dictionary<string, ThemeInfo> AvailableThemes => new()
    {
        { "default", new ThemeInfo("Default", "Tema padrão do Bootstrap", "primary", "https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css") },
        { "dark", new ThemeInfo("Dark", "Tema escuro moderno", "dark", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/darkly/bootstrap.min.css") },
        { "cerulean", new ThemeInfo("Cerulean", "Tema azul claro e limpo", "info", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/cerulean/bootstrap.min.css") },
        { "cosmo", new ThemeInfo("Cosmo", "Tema moderno e elegante", "primary", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/cosmo/bootstrap.min.css") },
        { "flatly", new ThemeInfo("Flatly", "Design flat e minimalista", "success", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/flatly/bootstrap.min.css") },
        { "journal", new ThemeInfo("Journal", "Estilo jornal clássico", "warning", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/journal/bootstrap.min.css") },
        { "lumen", new ThemeInfo("Lumen", "Tema claro e arejado", "light", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/lumen/bootstrap.min.css") },
        { "pulse", new ThemeInfo("Pulse", "Tema vibrante e colorido", "danger", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/pulse/bootstrap.min.css") },
        { "sandstone", new ThemeInfo("Sandstone", "Tons terrosos e naturais", "secondary", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/sandstone/bootstrap.min.css") },
        { "united", new ThemeInfo("United", "Tema corporativo", "primary", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.0/dist/united/bootstrap.min.css") }
    };

    public async Task InitializeAsync()
    {
        var result = "dark";
        if (!string.IsNullOrEmpty(result) && AvailableThemes.ContainsKey(result))
        {
            _currentTheme = result;
        }
    }

    public async Task SetThemeAsync(string themeKey)
    {
        if (AvailableThemes.ContainsKey(themeKey))
        {
            _currentTheme = themeKey;
            //await _localStorage.SetAsync("selected-theme", themeKey);
            OnThemeChanged?.Invoke();
        }
    }

    public ThemeInfo GetCurrentThemeInfo()
    {
        return AvailableThemes[_currentTheme];
    }
}

public record ThemeInfo(string Name, string Description, string BadgeColor, string CssUrl);