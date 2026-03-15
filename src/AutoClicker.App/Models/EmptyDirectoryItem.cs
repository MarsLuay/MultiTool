using System.IO;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoClicker.App.Models;

public partial class EmptyDirectoryItem : ObservableObject
{
    public EmptyDirectoryItem(string rootPath, EmptyDirectoryCandidate candidate)
    {
        FullPath = candidate.FullPath;
        DisplayPath = BuildDisplayPath(rootPath, candidate.FullPath);
        HintText = candidate.ContainsNestedEmptyDirectories
            ? "Becomes empty after nested empty folders are removed."
            : "Already empty.";
    }

    public string FullPath { get; }

    public string DisplayPath { get; }

    public string HintText { get; }

    [ObservableProperty]
    private bool isSelected = true;

    private static string BuildDisplayPath(string rootPath, string fullPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        return string.IsNullOrWhiteSpace(relativePath) || relativePath == "."
            ? Path.GetFileName(fullPath)
            : relativePath;
    }
}
