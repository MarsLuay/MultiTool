using AutoClicker.App.ViewModels;
using AutoClicker.App.Views;
using AutoClicker.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AutoClicker.App.Services;

public sealed class HotkeySettingsDialogService : IHotkeySettingsDialogService
{
    private readonly IServiceProvider serviceProvider;

    public HotkeySettingsDialogService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public HotkeySettings? Edit(HotkeySettings currentSettings)
    {
        var viewModel = ActivatorUtilities.CreateInstance<HotkeySettingsViewModel>(serviceProvider, currentSettings.Clone());
        var window = new HotkeySettingsWindow(viewModel);
        if (System.Windows.Application.Current?.MainWindow is System.Windows.Window owner)
        {
            window.Owner = owner;
        }

        return window.ShowDialog() == true ? viewModel.BuildSettings() : null;
    }
}
