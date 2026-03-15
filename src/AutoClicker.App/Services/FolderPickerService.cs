namespace AutoClicker.App.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    public string? PickFolder(string currentPath, string description)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            InitialDirectory = string.IsNullOrWhiteSpace(currentPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : currentPath,
            UseDescriptionForTitle = true,
            Description = description,
            ShowNewFolderButton = true,
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }
}
