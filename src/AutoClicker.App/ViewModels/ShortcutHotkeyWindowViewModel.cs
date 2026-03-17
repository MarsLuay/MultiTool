using System.ComponentModel;
using System.Windows.Data;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoClicker.App.ViewModels;

public partial class ShortcutHotkeyWindowViewModel : ObservableObject
{
    private readonly IReadOnlyList<ShortcutHotkeyInfo> allShortcuts;
    private readonly int conflictGroupCount;
    private readonly int conflictingShortcutCount;
    private readonly int detectedShortcutCount;
    private readonly int referenceShortcutCount;

    public ShortcutHotkeyWindowViewModel(ShortcutHotkeyScanResult result)
    {
        allShortcuts = result.Shortcuts;
        conflictGroupCount = result.ConflictGroupCount;
        conflictingShortcutCount = result.ConflictingShortcutCount;
        detectedShortcutCount = allShortcuts.Count(static shortcut => !shortcut.IsReferenceShortcut);
        referenceShortcutCount = allShortcuts.Count(static shortcut => shortcut.IsReferenceShortcut);
        ShortcutsView = CollectionViewSource.GetDefaultView(allShortcuts);
        ShortcutsView.Filter = FilterShortcut;
        SummaryText = BuildSummaryText(result);
        WarningText = result.Warnings.Count == 0
            ? string.Empty
            : $"Skipped {result.Warnings.Count} folder or shortcut read{(result.Warnings.Count == 1 ? string.Empty : "s")} during the scan.";
        ReferenceNoteText = referenceShortcutCount == 0
            ? string.Empty
            : "Built-in Windows and common app shortcuts are included as a reference catalog. The scanner now also pulls real keymaps from supported apps when available, but Windows still has no universal API that exposes every private keybind from every program.";
        ConflictWarningText = BuildConflictWarningText(result);
    }

    public ICollectionView ShortcutsView { get; }

    public string SummaryText { get; }

    public string WarningText { get; }

    public bool HasWarnings => !string.IsNullOrWhiteSpace(WarningText);

    public string ReferenceNoteText { get; }

    public string ConflictWarningText { get; }

    public bool HasConflicts => !string.IsNullOrWhiteSpace(ConflictWarningText);

    public string FilterSummary => BuildFilterSummary();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool showOnlyConflicts;

    partial void OnSearchTextChanged(string value)
    {
        ShortcutsView.Refresh();
        OnPropertyChanged(nameof(FilterSummary));
    }

    partial void OnShowOnlyConflictsChanged(bool value)
    {
        ShortcutsView.Refresh();
        OnPropertyChanged(nameof(FilterSummary));
    }

    private bool FilterShortcut(object item)
    {
        if (item is not ShortcutHotkeyInfo shortcut)
        {
            return false;
        }

        if (ShowOnlyConflicts && !shortcut.HasConflict)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return Contains(shortcut.Hotkey, SearchText)
            || Contains(shortcut.ShortcutName, SearchText)
            || Contains(shortcut.SourceLabel, SearchText)
            || Contains(shortcut.AppliesTo, SearchText)
            || Contains(shortcut.Details, SearchText)
            || Contains(shortcut.TargetPath, SearchText)
            || Contains(shortcut.FolderPath, SearchText)
            || Contains(shortcut.ShortcutPath, SearchText);
    }

    private string BuildFilterSummary()
    {
        var visibleCount = ShortcutsView.Cast<object>().Count();
        var baseSummary = string.IsNullOrWhiteSpace(SearchText) && !ShowOnlyConflicts
            ? $"{visibleCount} shortcut{(visibleCount == 1 ? string.Empty : "s")} listed"
            : $"{visibleCount} matching shortcut{(visibleCount == 1 ? string.Empty : "s")} shown";
        if (conflictingShortcutCount == 0)
        {
            return AppendSourceSummary(baseSummary);
        }

        return $"{AppendSourceSummary(baseSummary)}. {conflictingShortcutCount} shortcut{(conflictingShortcutCount == 1 ? string.Empty : "s")} are in conflict across {conflictGroupCount} shared hotkey{(conflictGroupCount == 1 ? string.Empty : "s")}.";
    }

    private string BuildSummaryText(ShortcutHotkeyScanResult result)
    {
        if (detectedShortcutCount == 0 && referenceShortcutCount == 0)
        {
            return $"Scanned {result.ScannedShortcutCount} .lnk shortcut file{(result.ScannedShortcutCount == 1 ? string.Empty : "s")} on fixed drives. No shortcut keys were found.";
        }

        var detectedSummary = detectedShortcutCount == 0
            ? "No assigned .lnk shortcut hotkeys were found on this PC."
            : $"Found {detectedShortcutCount} assigned .lnk shortcut hotkey{(detectedShortcutCount == 1 ? string.Empty : "s")} after scanning {result.ScannedShortcutCount} .lnk shortcut file{(result.ScannedShortcutCount == 1 ? string.Empty : "s")} on fixed drives.";
        var referenceSummary = referenceShortcutCount == 0
            ? string.Empty
            : $" Included {referenceShortcutCount} built-in Windows and common app shortcut reference entr{(referenceShortcutCount == 1 ? "y" : "ies")}.";

        return $"{detectedSummary}{referenceSummary}";
    }

    private static string BuildConflictWarningText(ShortcutHotkeyScanResult result)
    {
        if (result.ConflictingShortcutCount == 0)
        {
            return string.Empty;
        }

        return $"Warning: {result.ConflictingShortcutCount} shortcut{(result.ConflictingShortcutCount == 1 ? string.Empty : "s")} share {result.ConflictGroupCount} hotkey{(result.ConflictGroupCount == 1 ? string.Empty : "s")}. Some overlaps are harmless reference combos, but detected Windows shortcut-file hotkeys can still conflict in real use.";
    }

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private string AppendSourceSummary(string baseSummary) =>
        referenceShortcutCount == 0
            ? baseSummary
            : $"{baseSummary}. {detectedShortcutCount} detected from this PC, {referenceShortcutCount} built-in or common references";
}
