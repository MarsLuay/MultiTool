using AutoClicker.App.ViewModels;
using AutoClicker.App.Views;
using AutoClicker.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AutoClicker.App.Services;

public sealed class HotkeySettingsDialogService : IHotkeySettingsDialogService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IThemeService themeService;

    public HotkeySettingsDialogService(IServiceProvider serviceProvider, IThemeService themeService)
    {
        this.serviceProvider = serviceProvider;
        this.themeService = themeService;
    }

    public HotkeySettings? Edit(HotkeySettings currentSettings)
    {
        var viewModel = ActivatorUtilities.CreateInstance<HotkeySettingsViewModel>(serviceProvider, currentSettings.Clone());
        var window = new HotkeySettingsWindow(viewModel);
        if (System.Windows.Application.Current?.MainWindow is System.Windows.Window owner)
        {
            window.Owner = owner;
        }

        window.SourceInitialized += (_, _) => themeService.ApplyThemeToWindow(window);
        window.ContentRendered += (_, _) => themeService.ApplyThemeToWindow(window);

        return window.ShowDialog() == true ? viewModel.BuildSettings() : null;
    }
}
