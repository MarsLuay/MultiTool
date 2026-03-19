using MultiTool.App.Models;
using MultiTool.App.ViewModels;
using MultiTool.App.Views;
using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public sealed class ShortcutHotkeyDialogService : IShortcutHotkeyDialogService
{
    private readonly IThemeService themeService;

    public ShortcutHotkeyDialogService(IThemeService themeService)
    {
        this.themeService = themeService;
    }

    public void Show(
        ShortcutHotkeyScanResult result,
        bool isCachedResult,
        Func<Task<ShortcutHotkeyScanResult>> rescanAsync,
        Func<IReadOnlyList<ShortcutHotkeyInfo>, Task<ShortcutHotkeyDisableOperationResult>> disableAsync)
    {
        var viewModel = new ShortcutHotkeyWindowViewModel(result, isCachedResult, rescanAsync, disableAsync);
        var window = new ShortcutHotkeyWindow(viewModel);
        if (System.Windows.Application.Current?.MainWindow is System.Windows.Window owner)
        {
            window.Owner = owner;
        }

        window.SourceInitialized += (_, _) => themeService.ApplyThemeToWindow(window);
        window.ShowDialog();
    }
}
