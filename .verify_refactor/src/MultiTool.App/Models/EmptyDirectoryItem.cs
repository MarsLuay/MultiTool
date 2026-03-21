using System.IO;
using MultiTool.App.Localization;
using MultiTool.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiTool.App.Models;

public partial class EmptyDirectoryItem : ObservableObject
{
    public EmptyDirectoryItem(string rootPath, EmptyDirectoryCandidate candidate)
    {
        FullPath = candidate.FullPath;
        DisplayPath = BuildDisplayPath(rootPath, candidate.FullPath);
        HintText = candidate.ContainsNestedEmptyDirectories
            ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EmptyDirectoryHintNested)
            : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.EmptyDirectoryHintAlreadyEmpty);
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
