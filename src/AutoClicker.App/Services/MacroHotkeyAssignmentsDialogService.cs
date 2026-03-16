using AutoClicker.App.Models;
using AutoClicker.App.ViewModels;
using AutoClicker.App.Views;
using AutoClicker.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AutoClicker.App.Services;

public sealed class MacroHotkeyAssignmentsDialogService : IMacroHotkeyAssignmentsDialogService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IThemeService themeService;

    public MacroHotkeyAssignmentsDialogService(IServiceProvider serviceProvider, IThemeService themeService)
    {
        this.serviceProvider = serviceProvider;
        this.themeService = themeService;
    }

    public IReadOnlyList<MacroHotkeyAssignment>? Edit(
        IReadOnlyList<SavedMacroEntry> savedMacros,
        IReadOnlyList<MacroHotkeyAssignment> currentAssignments)
    {
        var viewModel = ActivatorUtilities.CreateInstance<MacroHotkeyAssignmentsWindowViewModel>(
            serviceProvider,
            savedMacros,
            currentAssignments);
        var window = new MacroHotkeyAssignmentsWindow(viewModel);
        if (System.Windows.Application.Current?.MainWindow is System.Windows.Window owner)
        {
            window.Owner = owner;
        }

        window.SourceInitialized += (_, _) => themeService.ApplyThemeToWindow(window);
        return window.ShowDialog() == true ? viewModel.BuildAssignments() : null;
    }
}
