using MultiTool.App.Models;
using MultiTool.App.Localization;
using MultiTool.Core.Models;

namespace MultiTool.App.ViewModels;

public partial class MainWindowViewModel
{
    private sealed class ShortcutHotkeyToolsCoordinator(MainWindowViewModel owner)
    {
        public async Task ShowAssignedHotkeysAsync()
        {
            if (owner.lastShortcutHotkeyScanResult is not null)
            {
                owner.RefreshShortcutHotkeyResultExpiration();
                owner.ShortcutHotkeyStatusMessage = BuildViewerStatusMessage(
                    owner.lastShortcutHotkeyScanResult,
                    AppLanguageKeys.ToolsStatusShortcutOpenedCachedViewerFormat);
                owner.AddToolLog(owner.ShortcutHotkeyStatusMessage);
                owner.shortcutHotkeyDialogService.Show(
                    owner.lastShortcutHotkeyScanResult,
                    isCachedResult: true,
                    RescanFromExplorerAsync,
                    DisableFromExplorerAsync);
                return;
            }

            owner.IsToolBusy = true;
            owner.ShortcutHotkeyStatusMessage = owner.L(AppLanguageKeys.ToolsStatusShortcutScanRunning);
            owner.StartShortcutHotkeyScanProgress();
            owner.AddToolLog(owner.ShortcutHotkeyStatusMessage);

            try
            {
                var progress = new Progress<ShortcutHotkeyScanProgress>(owner.UpdateShortcutHotkeyScanProgress);
                var result = await owner.shortcutHotkeyInventoryService.ScanAsync(progress).ConfigureAwait(true);
                owner.PersistShortcutHotkeyScanMaxFolderCount(owner.ShortcutHotkeyScanProgressMaximum);
                owner.lastShortcutHotkeyScanResult = result;
                owner.RefreshShortcutHotkeyResultExpiration();

                owner.ShortcutHotkeyStatusMessage = CountDetectedShortcutEntries(result) == 0 && CountReferenceShortcutEntries(result) == 0
                    ? owner.F(AppLanguageKeys.ToolsStatusShortcutScanNoShortcutsFormat, result.ScannedShortcutCount, MainWindowViewModel.PluralSuffix(result.ScannedShortcutCount), BuildWarningSuffix(result))
                    : BuildViewerStatusMessage(result, AppLanguageKeys.ToolsStatusShortcutScanOpenedViewerFormat);
                owner.AddToolLog(owner.ShortcutHotkeyStatusMessage);
                LogWarnings(result);
                owner.shortcutHotkeyDialogService.Show(
                    result,
                    isCachedResult: false,
                    RescanFromExplorerAsync,
                    DisableFromExplorerAsync);
            }
            catch (Exception ex)
            {
                owner.ShortcutHotkeyStatusMessage = owner.F(AppLanguageKeys.ToolsStatusShortcutScanFailedFormat, ex.Message);
                owner.AddToolLog(owner.ShortcutHotkeyStatusMessage);
            }
            finally
            {
                owner.ResetShortcutHotkeyScanProgress();
                owner.IsToolBusy = false;
            }
        }

        public async Task<ShortcutHotkeyScanResult> RescanFromExplorerAsync()
        {
            try
            {
                var result = await owner.shortcutHotkeyInventoryService.ScanAsync().ConfigureAwait(true);
                owner.lastShortcutHotkeyScanResult = result;
                owner.RefreshShortcutHotkeyResultExpiration();
                owner.ShortcutHotkeyStatusMessage = BuildViewerStatusMessage(result, AppLanguageKeys.ToolsStatusShortcutScanOpenedViewerFormat);
                owner.AddToolLog(owner.ShortcutHotkeyStatusMessage);
                LogWarnings(result);
                return result;
            }
            catch (Exception ex)
            {
                owner.ShortcutHotkeyStatusMessage = owner.F(AppLanguageKeys.ToolsStatusShortcutScanFailedFormat, ex.Message);
                owner.AddToolLog(owner.ShortcutHotkeyStatusMessage);
                throw;
            }
        }

        public async Task<ShortcutHotkeyDisableOperationResult> DisableFromExplorerAsync(IReadOnlyList<ShortcutHotkeyInfo> shortcuts)
        {
            try
            {
                var disableResult = await owner.shortcutHotkeyDisableService.DisableAsync(shortcuts).ConfigureAwait(true);
                ShortcutHotkeyScanResult scanResult;

                if (disableResult.DisabledCount > 0 || owner.lastShortcutHotkeyScanResult is null)
                {
                    scanResult = await owner.shortcutHotkeyInventoryService.ScanAsync().ConfigureAwait(true);
                    owner.lastShortcutHotkeyScanResult = scanResult;
                    owner.RefreshShortcutHotkeyResultExpiration();
                }
                else
                {
                    scanResult = owner.lastShortcutHotkeyScanResult;
                }

                owner.ShortcutHotkeyStatusMessage = BuildDisableStatusMessage(disableResult);
                owner.AddToolLog(owner.ShortcutHotkeyStatusMessage);
                LogDisableWarnings(disableResult);
                return new ShortcutHotkeyDisableOperationResult(scanResult, disableResult);
            }
            catch (Exception ex)
            {
                owner.ShortcutHotkeyStatusMessage = owner.F(AppLanguageKeys.ToolsStatusShortcutDisableFailedFormat, ex.Message);
                owner.AddToolLog(owner.ShortcutHotkeyStatusMessage);
                throw;
            }
        }

        private string BuildViewerStatusMessage(ShortcutHotkeyScanResult result, string formatKey)
        {
            var detectedCount = CountDetectedShortcutEntries(result);
            var referenceCount = CountReferenceShortcutEntries(result);
            return owner.F(
                formatKey,
                detectedCount,
                MainWindowViewModel.PluralSuffix(detectedCount),
                referenceCount,
                referenceCount == 1 ? "y" : "ies",
                BuildWarningSuffix(result));
        }

        private string BuildWarningSuffix(ShortcutHotkeyScanResult result) =>
            result.Warnings.Count == 0
                ? string.Empty
                : owner.F(AppLanguageKeys.ToolsStatusShortcutScanWarningsSuffixFormat, result.Warnings.Count, MainWindowViewModel.PluralSuffix(result.Warnings.Count));

        private static int CountDetectedShortcutEntries(ShortcutHotkeyScanResult result) =>
            result.Shortcuts.Count(static shortcut => !shortcut.IsReferenceShortcut);

        private static int CountReferenceShortcutEntries(ShortcutHotkeyScanResult result) =>
            result.Shortcuts.Count(static shortcut => shortcut.IsReferenceShortcut);

        private void LogWarnings(ShortcutHotkeyScanResult result)
        {
            foreach (var warning in result.Warnings.Take(10))
            {
                owner.AddToolLog(warning);
            }
        }

        private string BuildDisableStatusMessage(ShortcutHotkeyDisableResult result)
        {
            var warningSuffix = result.Warnings.Count == 0
                ? string.Empty
                : owner.F(AppLanguageKeys.ToolsStatusShortcutDisableWarningsSuffixFormat, result.Warnings.Count, MainWindowViewModel.PluralSuffix(result.Warnings.Count));

            if (result.DisabledCount > 0)
            {
                var unsupportedSuffix = result.UnsupportedCount == 0
                    ? string.Empty
                    : owner.F(AppLanguageKeys.ToolsStatusShortcutDisableSkippedUnsupportedSuffixFormat, result.UnsupportedCount, result.UnsupportedCount == 1 ? "y" : "ies");
                return owner.F(
                    AppLanguageKeys.ToolsStatusShortcutDisableCompletedFormat,
                    result.DisabledCount,
                    MainWindowViewModel.PluralSuffix(result.DisabledCount),
                    unsupportedSuffix,
                    warningSuffix);
            }

            if (result.SupportedCount > 0)
            {
                return owner.F(AppLanguageKeys.ToolsStatusShortcutDisableNoChangesFormat, warningSuffix);
            }

            return owner.F(AppLanguageKeys.ToolsStatusShortcutDisableUnsupportedFormat, warningSuffix);
        }

        private void LogDisableWarnings(ShortcutHotkeyDisableResult result)
        {
            foreach (var warning in result.Warnings.Take(10))
            {
                owner.AddToolLog(warning);
            }
        }
    }
}
