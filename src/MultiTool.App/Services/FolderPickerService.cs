using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace MultiTool.App.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    public string? PickFolder(string currentPath, string description)
    {
        var trimmedCurrentPath = string.IsNullOrWhiteSpace(currentPath) ? null : currentPath.Trim();
        var fallbackPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var initialDirectory = ResolveInitialDirectory(trimmedCurrentPath, fallbackPath);

        var dialog = new OpenFolderDialog
        {
            Title = description,
            InitialDirectory = initialDirectory,
            FolderName = trimmedCurrentPath is not null && Directory.Exists(trimmedCurrentPath)
                ? trimmedCurrentPath
                : initialDirectory,
        };

        var owner = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(static window => window.IsActive)
            ?? System.Windows.Application.Current?.MainWindow;

        var result = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return result == true ? dialog.FolderName : null;
    }

    internal static string ResolveInitialDirectory(string? currentPath, string fallbackPath)
    {
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return fallbackPath;
        }

        var candidatePath = currentPath.Trim();
        if (Directory.Exists(candidatePath))
        {
            return candidatePath;
        }

        try
        {
            while (!string.IsNullOrWhiteSpace(candidatePath))
            {
                candidatePath = Path.GetDirectoryName(candidatePath);
                if (!string.IsNullOrWhiteSpace(candidatePath) && Directory.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }
        }
        catch (Exception)
        {
            // Invalid or malformed paths should fall back to a safe starting directory.
        }

        return fallbackPath;
    }
}
