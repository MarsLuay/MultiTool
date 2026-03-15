namespace AutoClicker.App.Services;

public interface IThemeService
{
    bool GetSystemPrefersDarkMode();

    void ApplyTheme(bool isDarkMode);

    void ApplyThemeToWindow(System.Windows.Window window);

    bool TryApplySystemDarkModePreference(out string message);
}
