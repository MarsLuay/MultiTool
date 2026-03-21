using System.Globalization;
using MultiTool.App.ViewModels;

namespace MultiTool.App.Localization;

public enum AppLanguage
{
    English,
    CatSpeak,
}

public static partial class AppLanguageKeys
{
}

public static partial class AppLanguageStrings
{
    private static readonly Dictionary<string, (string English, string CatSpeak)> Values = BuildValues();

    private static Dictionary<string, (string English, string CatSpeak)> BuildValues()
    {
        Dictionary<string, (string English, string CatSpeak)> values = new(StringComparer.Ordinal);

        AddCoreValues(values);
        AddMainValues(values);
        AddDialogValues(values);
        AddMacroEditingValues(values);
        AddShortcutValues(values);
        AddInstallerValues(values);
        AddToolsValues(values);

        return values;
    }

    public static string Get(string key, AppLanguage language)
    {
        if (!Values.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"Missing language key '{key}'.");
        }

        return language == AppLanguage.CatSpeak
            ? value.CatSpeak
            : value.English;
    }

    public static string Format(string key, AppLanguage language, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key, language), args);

    public static AppLanguage ResolveCurrentLanguage()
    {
        if (System.Windows.Application.Current?.MainWindow?.DataContext is MainWindowViewModel vm)
        {
            return vm.IsSillyModeEnabled ? AppLanguage.CatSpeak : AppLanguage.English;
        }

        return AppLanguage.English;
    }

    public static string GetForCurrentLanguage(string key) => Get(key, ResolveCurrentLanguage());

    public static string FormatForCurrentLanguage(string key, params object[] args) =>
        Format(key, ResolveCurrentLanguage(), args);
}
