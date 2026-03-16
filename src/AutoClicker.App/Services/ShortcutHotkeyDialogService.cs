using AutoClicker.App.ViewModels;
using AutoClicker.App.Views;
using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public sealed class ShortcutHotkeyDialogService : IShortcutHotkeyDialogService
{
    private readonly IThemeService themeService;

    public ShortcutHotkeyDialogService(IThemeService themeService)
    {
        this.themeService = themeService;
    }

    public void Show(ShortcutHotkeyScanResult result)
    {
        var viewModel = new ShortcutHotkeyWindowViewModel(result);
        var window = new ShortcutHotkeyWindow(viewModel);
        if (System.Windows.Application.Current?.MainWindow is System.Windows.Window owner)
        {
            window.Owner = owner;
        }

        window.SourceInitialized += (_, _) => themeService.ApplyThemeToWindow(window);
        window.ShowDialog();
    }
}
