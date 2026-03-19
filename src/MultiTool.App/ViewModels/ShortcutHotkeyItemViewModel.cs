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

    [ObservableProperty]
    private bool isShortcutEnabled;
}
