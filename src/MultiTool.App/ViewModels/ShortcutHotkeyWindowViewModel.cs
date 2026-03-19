using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using MultiTool.App.Models;
using MultiTool.App.Localization;
using MultiTool.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiTool.App.ViewModels;

public partial class ShortcutHotkeyWindowViewModel : ObservableObject
{
    private readonly Func<Task<ShortcutHotkeyScanResult>> rescanAsync;
    private readonly Func<IReadOnlyList<ShortcutHotkeyInfo>, Task<ShortcutHotkeyDisableOperationResult>> disableAsync;
    private readonly ObservableCollection<ShortcutHotkeyItemViewModel> allShortcuts = [];
    private int conflictGroupCount;
    private int conflictingShortcutCount;
    private int detectedShortcutCount;
    private int referenceShortcutCount;
    private int scannedShortcutCount;

    public ShortcutHotkeyWindowViewModel(
        ShortcutHotkeyScanResult result,
        bool isCachedResult,
        Func<Task<ShortcutHotkeyScanResult>> rescanAsync,
        Func<IReadOnlyList<ShortcutHotkeyInfo>, Task<ShortcutHotkeyDisableOperationResult>> disableAsync)
    {
        this.rescanAsync = rescanAsync ?? throw new ArgumentNullException(nameof(rescanAsync));
        this.disableAsync = disableAsync ?? throw new ArgumentNullException(nameof(disableAsync));
        ShortcutsView = CollectionViewSource.GetDefaultView(allShortcuts);
        ShortcutsView.Filter = FilterShortcut;
        ApplyResult(result);
        StatusText = isCachedResult
            ? L(AppLanguageKeys.ShortcutExplorerStatusCachedResults)
            : L(AppLanguageKeys.ShortcutExplorerStatusReady);
    }

    public ICollectionView ShortcutsView { get; }

    public string WindowTitleText => L(AppLanguageKeys.ShortcutExplorerTitle);

    public string HeadingText => L(AppLanguageKeys.ShortcutExplorerHeading);

    public string SearchLabelText => L(AppLanguageKeys.ShortcutExplorerSearchLabel);

    public string ConflictsOnlyText => L(AppLanguageKeys.ShortcutExplorerConflictsOnly);

    public string ColumnHotkeyText => L(AppLanguageKeys.ShortcutExplorerColumnHotkey);

    public string ColumnEnabledText => L(AppLanguageKeys.ShortcutExplorerColumnEnabled);

    public string ColumnShortcutText => L(AppLanguageKeys.ShortcutExplorerColumnShortcut);

    public string ColumnSourceText => L(AppLanguageKeys.ShortcutExplorerColumnSource);

    public string ColumnAppliesToText => L(AppLanguageKeys.ShortcutExplorerColumnAppliesTo);

    public string ColumnConflictText => L(AppLanguageKeys.ShortcutExplorerColumnConflict);

    public string ColumnDetailsText => L(AppLanguageKeys.ShortcutExplorerColumnDetails);

    public string ColumnFileText => L(AppLanguageKeys.ShortcutExplorerColumnFile);

    public string RescanButtonText => L(AppLanguageKeys.ShortcutExplorerRescan);

    public string ApplyChangesButtonText => L(AppLanguageKeys.ShortcutExplorerDisableSelected);

    public string CloseButtonText => L(AppLanguageKeys.ShortcutExplorerClose);

    public bool HasWarnings => !string.IsNullOrWhiteSpace(WarningText);

    public bool HasConflicts => !string.IsNullOrWhiteSpace(ConflictWarningText);

    public string FilterSummary => BuildFilterSummary();

    [ObservableProperty]
    private string summaryText = string.Empty;

    [ObservableProperty]
    private string warningText = string.Empty;

    [ObservableProperty]
    private string referenceNoteText = string.Empty;

    [ObservableProperty]
    private string conflictWarningText = string.Empty;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool showOnlyConflicts;

    [ObservableProperty]
    private bool isBusy;

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

    partial void OnWarningTextChanged(string value) => OnPropertyChanged(nameof(HasWarnings));

    partial void OnConflictWarningTextChanged(string value) => OnPropertyChanged(nameof(HasConflicts));

    partial void OnIsBusyChanged(bool value)
    {
        RescanCommand.NotifyCanExecuteChanged();
        ApplyShortcutChangesCommand.NotifyCanExecuteChanged();
    }

    private bool CanRescan() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRescan))]
    private async Task RescanAsync()
    {
        IsBusy = true;
        StatusText = L(AppLanguageKeys.ShortcutExplorerStatusRescanning);

        try
        {
            var result = await rescanAsync().ConfigureAwait(true);
            ApplyResult(result);
            StatusText = F(
                AppLanguageKeys.ShortcutExplorerStatusRescannedFormat,
                detectedShortcutCount,
                PluralSuffix(detectedShortcutCount),
                referenceShortcutCount,
                EntrySuffix(referenceShortcutCount));
        }
        catch (Exception ex)
        {
            StatusText = F(AppLanguageKeys.ShortcutExplorerStatusRescanFailedFormat, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanApplyShortcutChanges() => !IsBusy && GetShortcutsMarkedForDisable().Count > 0;

    [RelayCommand(CanExecute = nameof(CanApplyShortcutChanges))]
    private async Task ApplyShortcutChangesAsync()
    {
        var shortcutsToDisable = GetShortcutsMarkedForDisable();
        if (shortcutsToDisable.Count == 0)
        {
            StatusText = L(AppLanguageKeys.ShortcutExplorerStatusDisableNoSelection);
            return;
        }

        IsBusy = true;
        StatusText = L(AppLanguageKeys.ShortcutExplorerStatusDisabling);

        try
        {
            var operationResult = await disableAsync(shortcutsToDisable).ConfigureAwait(true);
            ApplyResult(operationResult.ScanResult);
            StatusText = BuildDisableStatusText(operationResult.DisableResult);
        }
        catch (Exception ex)
        {
            StatusText = F(AppLanguageKeys.ShortcutExplorerStatusDisableFailedFormat, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyResult(ShortcutHotkeyScanResult result)
    {
        scannedShortcutCount = result.ScannedShortcutCount;
        conflictGroupCount = result.ConflictGroupCount;
        conflictingShortcutCount = result.ConflictingShortcutCount;
        detectedShortcutCount = result.Shortcuts.Count(static shortcut => !shortcut.IsReferenceShortcut);
        referenceShortcutCount = result.Shortcuts.Count(static shortcut => shortcut.IsReferenceShortcut);

        foreach (var shortcut in allShortcuts)
        {
            shortcut.PropertyChanged -= Shortcut_OnPropertyChanged;
        }

        allShortcuts.Clear();
        foreach (var shortcut in result.Shortcuts)
        {
            var item = new ShortcutHotkeyItemViewModel(shortcut);
            item.PropertyChanged += Shortcut_OnPropertyChanged;
            allShortcuts.Add(item);
        }

        SummaryText = BuildSummaryText();
        WarningText = result.Warnings.Count == 0
            ? string.Empty
            : F(AppLanguageKeys.ShortcutExplorerWarningSkippedFormat, result.Warnings.Count, PluralSuffix(result.Warnings.Count));
        ReferenceNoteText = referenceShortcutCount == 0
            ? string.Empty
            : L(AppLanguageKeys.ShortcutExplorerReferenceNote);
        ConflictWarningText = BuildConflictWarningText();

        ShortcutsView.Refresh();
        OnPropertyChanged(nameof(FilterSummary));
        ApplyShortcutChangesCommand.NotifyCanExecuteChanged();
    }

    private bool FilterShortcut(object item)
    {
        if (item is not ShortcutHotkeyItemViewModel shortcut)
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

    private string BuildSummaryText()
    {
        if (detectedShortcutCount == 0 && referenceShortcutCount == 0)
        {
            return F(AppLanguageKeys.ShortcutExplorerSummaryNoneFormat, scannedShortcutCount, PluralSuffix(scannedShortcutCount));
        }

        var detectedSummary = detectedShortcutCount == 0
            ? L(AppLanguageKeys.ShortcutExplorerSummaryNoAssigned)
            : F(
                AppLanguageKeys.ShortcutExplorerSummaryFoundFormat,
                detectedShortcutCount,
                PluralSuffix(detectedShortcutCount),
                scannedShortcutCount,
                PluralSuffix(scannedShortcutCount));
        var referenceSummary = referenceShortcutCount == 0
            ? string.Empty
            : F(AppLanguageKeys.ShortcutExplorerReferenceIncludedFormat, referenceShortcutCount, EntrySuffix(referenceShortcutCount));

        return $"{detectedSummary}{referenceSummary}";
    }

    private string BuildConflictWarningText()
    {
        if (conflictingShortcutCount == 0)
        {
            return string.Empty;
        }

        return F(
            AppLanguageKeys.ShortcutExplorerConflictWarningFormat,
            conflictingShortcutCount,
            PluralSuffix(conflictingShortcutCount),
            conflictGroupCount,
            PluralSuffix(conflictGroupCount));
    }

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private string AppendSourceSummary(string baseSummary) =>
        referenceShortcutCount == 0
            ? baseSummary
            : $"{baseSummary}{F(AppLanguageKeys.ShortcutExplorerFilterSourceSuffixFormat, detectedShortcutCount, referenceShortcutCount)}";

    private string BuildDisableStatusText(ShortcutHotkeyDisableResult result)
    {
        var warningSuffix = result.Warnings.Count == 0
            ? string.Empty
            : F(AppLanguageKeys.ShortcutExplorerStatusDisableWarningsSuffixFormat, result.Warnings.Count, PluralSuffix(result.Warnings.Count));

        if (result.DisabledCount > 0)
        {
            var unsupportedSuffix = result.UnsupportedCount == 0
                ? string.Empty
                : F(
                    AppLanguageKeys.ShortcutExplorerStatusDisableSkippedUnsupportedSuffixFormat,
                    result.UnsupportedCount,
                    result.UnsupportedCount == 1 ? "y" : "ies");
            return F(
                AppLanguageKeys.ShortcutExplorerStatusDisabledFormat,
                result.DisabledCount,
                PluralSuffix(result.DisabledCount),
                unsupportedSuffix,
                warningSuffix);
        }

        if (result.SupportedCount > 0)
        {
            return F(AppLanguageKeys.ShortcutExplorerStatusDisableNoChangesFormat, warningSuffix);
        }

        return F(AppLanguageKeys.ShortcutExplorerStatusDisableUnsupportedFormat, warningSuffix);
    }

    private static string L(string key) => AppLanguageStrings.GetForCurrentLanguage(key);

    private static string F(string key, params object[] args) => AppLanguageStrings.FormatForCurrentLanguage(key, args);

    private static string PluralSuffix(int count) => count == 1 ? string.Empty : "s";

    private static string EntrySuffix(int count) => count == 1 ? "y" : "ies";

    private void Shortcut_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShortcutHotkeyItemViewModel.IsShortcutEnabled))
        {
            ApplyShortcutChangesCommand.NotifyCanExecuteChanged();
        }
    }

    private IReadOnlyList<ShortcutHotkeyInfo> GetShortcutsMarkedForDisable() =>
        allShortcuts
            .Where(static shortcut => shortcut.CanEditShortcutEnabledState && !shortcut.IsShortcutEnabled)
            .Select(static shortcut => shortcut.Shortcut)
            .ToArray();
}
