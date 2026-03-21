namespace MultiTool.App.Services;

public sealed class TextFileSaveDialogService : ITextFileSaveDialogService
{
    public string? PickSavePath(string title, string defaultFileName, string filter, string defaultExtension)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = title,
            FileName = defaultFileName,
            Filter = filter,
            DefaultExt = defaultExtension,
            AddExtension = true,
            OverwritePrompt = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
