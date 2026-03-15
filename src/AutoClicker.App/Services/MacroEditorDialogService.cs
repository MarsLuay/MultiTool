using AutoClicker.App.ViewModels;
using AutoClicker.App.Views;
using AutoClicker.Core.Models;
using System.Windows;

namespace AutoClicker.App.Services;

public sealed class MacroEditorDialogService : IMacroEditorDialogService
{
    private readonly IThemeService themeService;

    public MacroEditorDialogService(IThemeService themeService)
    {
        this.themeService = themeService;
    }

    public RecordedMacro? Edit(RecordedMacro macro)
    {
        var viewModel = new MacroEditorViewModel(macro);
        var window = new MacroEditorWindow(viewModel);
        if (System.Windows.Application.Current?.MainWindow is Window owner)
        {
            window.Owner = owner;
        }

        window.SourceInitialized += (_, _) => themeService.ApplyThemeToWindow(window);
        return window.ShowDialog() == true ? window.EditedMacro : null;
    }
}
