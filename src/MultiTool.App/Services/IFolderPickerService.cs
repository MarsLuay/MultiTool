namespace MultiTool.App.Services;

public interface IFolderPickerService
{
    string? PickFolder(string currentPath, string description);
}
