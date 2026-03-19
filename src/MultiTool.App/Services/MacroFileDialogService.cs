using System.IO;

namespace MultiTool.App.Services;

public sealed class MacroFileDialogService : IMacroFileDialogService
{
    private readonly IMacroLibraryService macroLibraryService;

    public MacroFileDialogService(IMacroLibraryService macroLibraryService)
    {
        this.macroLibraryService = macroLibraryService;
    }

    public string? PickOpenPath()
    {
        Directory.CreateDirectory(macroLibraryService.DefaultDirectory);

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Load Macro",
            InitialDirectory = macroLibraryService.DefaultDirectory,
            Filter = "MultiTool Macro (*.acmacro.json)|*.acmacro.json|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Multiselect = false,
            CheckFileExists = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
