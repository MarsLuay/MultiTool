using System.ComponentModel;
using System.Windows.Data;
using AutoClicker.App.Localization;
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
            : F(AppLanguageKeys.ShortcutExplorerWarningSkippedFormat, result.Warnings.Count, PluralSuffix(result.Warnings.Count));
        ReferenceNoteText = referenceShortcutCount == 0
            ? string.Empty
            : L(AppLanguageKeys.ShortcutExplorerReferenceNote);
        ConflictWarningText = BuildConflictWarningText(result);
    }

    public ICollectionView ShortcutsView { get; }

    public string WindowTitleText => L(AppLanguageKeys.ShortcutExplorerTitle);

    public string HeadingText => L(AppLanguageKeys.ShortcutExplorerHeading);

    public string SearchLabelText => L(AppLanguageKeys.ShortcutExplorerSearchLabel);

    public string ConflictsOnlyText => L(AppLanguageKeys.ShortcutExplorerConflictsOnly);

    public string ColumnHotkeyText => L(AppLanguageKeys.ShortcutExplorerColumnHotkey);

    public string ColumnShortcutText => L(AppLanguageKeys.ShortcutExplorerColumnShortcut);

    public string ColumnSourceText => L(AppLanguageKeys.ShortcutExplorerColumnSource);

    public string ColumnAppliesToText => L(AppLanguageKeys.ShortcutExplorerColumnAppliesTo);

    public string ColumnConflictText => L(AppLanguageKeys.ShortcutExplorerColumnConflict);

    public string ColumnDetailsText => L(AppLanguageKeys.ShortcutExplorerColumnDetails);

    public string ColumnFileText => L(AppLanguageKeys.ShortcutExplorerColumnFile);

    public string CloseButtonText => L(AppLanguageKeys.ShortcutExplorerClose);

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
            ? F(AppLanguageKeys.ShortcutExplorerFilterListedFormat, visibleCount, PluralSuffix(visibleCount))
            : F(AppLanguageKeys.ShortcutExplorerFilterMatchingFormat, visibleCount, PluralSuffix(visibleCount));
        if (conflictingShortcutCount == 0)
        {
            return AppendSourceSummary(baseSummary);
        }

        return $"{AppendSourceSummary(baseSummary)}{F(AppLanguageKeys.ShortcutExplorerFilterConflictSuffixFormat, conflictingShortcutCount, PluralSuffix(conflictingShortcutCount), conflictGroupCount, PluralSuffix(conflictGroupCount))}";
    }

    private string BuildSummaryText(ShortcutHotkeyScanResult result)
    {
        if (detectedShortcutCount == 0 && referenceShortcutCount == 0)
        {
            return F(AppLanguageKeys.ShortcutExplorerSummaryNoneFormat, result.ScannedShortcutCount, PluralSuffix(result.ScannedShortcutCount));
        }

        var detectedSummary = detectedShortcutCount == 0
            ? L(AppLanguageKeys.ShortcutExplorerSummaryNoAssigned)
            : F(
                AppLanguageKeys.ShortcutExplorerSummaryFoundFormat,
                detectedShortcutCount,
                PluralSuffix(detectedShortcutCount),
                result.ScannedShortcutCount,
                PluralSuffix(result.ScannedShortcutCount));
        var referenceSummary = referenceShortcutCount == 0
            ? string.Empty
            : F(AppLanguageKeys.ShortcutExplorerReferenceIncludedFormat, referenceShortcutCount, EntrySuffix(referenceShortcutCount));

        return $"{detectedSummary}{referenceSummary}";
    }

    private static string BuildConflictWarningText(ShortcutHotkeyScanResult result)
    {
        if (result.ConflictingShortcutCount == 0)
        {
            return string.Empty;
        }

        return F(
            AppLanguageKeys.ShortcutExplorerConflictWarningFormat,
            result.ConflictingShortcutCount,
            PluralSuffix(result.ConflictingShortcutCount),
            result.ConflictGroupCount,
            PluralSuffix(result.ConflictGroupCount));
    }

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private string AppendSourceSummary(string baseSummary) =>
        referenceShortcutCount == 0
            ? baseSummary
            : $"{baseSummary}{F(AppLanguageKeys.ShortcutExplorerFilterSourceSuffixFormat, detectedShortcutCount, referenceShortcutCount)}";

    private static string L(string key) => AppLanguageStrings.GetForCurrentLanguage(key);

    private static string F(string key, params object[] args) => AppLanguageStrings.FormatForCurrentLanguage(key, args);

    private static string PluralSuffix(int count) => count == 1 ? string.Empty : "s";

    private static string EntrySuffix(int count) => count == 1 ? "y" : "ies";
}
