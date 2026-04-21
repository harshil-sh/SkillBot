using Blazored.LocalStorage;

namespace SkillBot.Web.Services;

public class ThemeService
{
    private readonly ILocalStorageService _localStorage;
    private const string ThemeKey = "isDarkMode";

    public bool IsDarkMode { get; private set; }
    public event Action? OnThemeChanged;

    public ThemeService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task LoadThemePreferenceAsync()
    {
        IsDarkMode = await _localStorage.GetItemAsync<bool>(ThemeKey);
        OnThemeChanged?.Invoke();
    }

    public async Task ToggleDarkModeAsync()
    {
        IsDarkMode = !IsDarkMode;
        await _localStorage.SetItemAsync(ThemeKey, IsDarkMode);
        OnThemeChanged?.Invoke();
    }
}
