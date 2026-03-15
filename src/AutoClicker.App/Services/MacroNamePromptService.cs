using AutoClicker.App.Views;

namespace AutoClicker.App.Services;

public sealed class MacroNamePromptService : IMacroNamePromptService
{
    private readonly IThemeService themeService;
    private readonly IMacroLibraryService macroLibraryService;

    public MacroNamePromptService(IThemeService themeService, IMacroLibraryService macroLibraryService)
    {
        this.themeService = themeService;
        this.macroLibraryService = macroLibraryService;
    }

    public string? PromptForName(string suggestedName)
    {
        var window = new MacroNamePromptWindow(suggestedName, macroLibraryService.DefaultDirectory);
        if (System.Windows.Application.Current?.MainWindow is System.Windows.Window owner)
        {
            window.Owner = owner;
        }

        window.SourceInitialized += (_, _) => themeService.ApplyThemeToWindow(window);

        return window.ShowDialog() == true ? window.MacroName : null;
    }
}
