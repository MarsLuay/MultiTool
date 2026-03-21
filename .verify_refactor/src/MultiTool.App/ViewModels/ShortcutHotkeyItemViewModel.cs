using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using MultiTool.Core.Models;

namespace MultiTool.App.ViewModels;

public sealed partial class ShortcutHotkeyItemViewModel : ObservableObject
{
    public ShortcutHotkeyItemViewModel(ShortcutHotkeyInfo shortcut)
    {
        Shortcut = shortcut ?? throw new ArgumentNullException(nameof(shortcut));
        isShortcutEnabled = true;
    }

    public ShortcutHotkeyInfo Shortcut { get; }

    public string Hotkey => Shortcut.Hotkey;

    public string ShortcutName => Shortcut.ShortcutName;

    public string SourceLabel => Shortcut.SourceLabel;

    public string AppliesTo => Shortcut.AppliesTo;

    public string ConflictSummary => Shortcut.ConflictSummary;

    public string Details => Shortcut.Details;

    public string ShortcutPath => Shortcut.ShortcutPath;

    public string FolderPath => Shortcut.FolderPath;

    public string TargetPath => Shortcut.TargetPath;

    public bool HasConflict => Shortcut.HasConflict;

    public bool CanEditShortcutEnabledState => Shortcut.CanDisable && !Shortcut.IsReferenceShortcut;

    public string ShortcutSecondaryText => string.IsNullOrWhiteSpace(Details) ? string.Empty : Details;

    public string ShortcutToolTip => string.IsNullOrWhiteSpace(ShortcutSecondaryText)
        ? ShortcutName
        : $"{ShortcutName}{Environment.NewLine}{ShortcutSecondaryText}";

    public string SourceSecondaryText => string.IsNullOrWhiteSpace(AppliesTo) ? string.Empty : AppliesTo;

    public string LocationPrimaryText
    {
        get
        {
            var fileName = Path.GetFileName(ShortcutPath);
            return string.IsNullOrWhiteSpace(fileName) ? ShortcutPath : fileName;
        }
    }

    public string LocationSecondaryText => string.IsNullOrWhiteSpace(FolderPath) ? ShortcutPath : FolderPath;

    public string LocationToolTip => string.IsNullOrWhiteSpace(TargetPath)
        ? ShortcutPath
        : $"{ShortcutPath}{Environment.NewLine}{TargetPath}";

    [ObservableProperty]
    private bool isShortcutEnabled;
}
