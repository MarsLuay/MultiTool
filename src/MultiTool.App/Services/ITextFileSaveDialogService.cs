namespace MultiTool.App.Services;

public interface ITextFileSaveDialogService
{
    string? PickSavePath(string title, string defaultFileName, string filter, string defaultExtension);
}
