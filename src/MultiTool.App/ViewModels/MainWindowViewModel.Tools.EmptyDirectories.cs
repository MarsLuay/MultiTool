using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.Core.Models;

namespace MultiTool.App.ViewModels;

public partial class MainWindowViewModel
{
    private sealed class EmptyDirectoryToolsCoordinator(MainWindowViewModel owner)
    {
        public void HandleItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EmptyDirectoryItem.IsSelected))
            {
                owner.OnPropertyChanged(nameof(HasSelectedEmptyDirectories));
                owner.OnPropertyChanged(nameof(EmptyDirectorySelectionSummary));
                owner.RefreshToolCommandStates();
            }
        }

        public void BrowseRoot()
        {
            var selectedPath = owner.folderPickerService.PickFolder(owner.EmptyDirectoryRootPath, owner.L(AppLanguageKeys.ToolsFolderPickerSelectEmptyDirectoryRoot));
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                owner.EmptyDirectoryStatusMessage = owner.L(AppLanguageKeys.ToolsStatusFolderSelectionCanceled);
                owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
                return;
            }

            owner.EmptyDirectoryRootPath = selectedPath;
            owner.EmptyDirectoryStatusMessage = owner.F(AppLanguageKeys.ToolsStatusEmptyDirectoryRootSetFormat, selectedPath);
            owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
        }

        public void OpenRoot()
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = owner.EmptyDirectoryRootPath,
                        UseShellExecute = true,
                    });

                owner.EmptyDirectoryStatusMessage = owner.L(AppLanguageKeys.ToolsStatusOpenedScanRoot);
                owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
            }
            catch (Exception ex)
            {
                owner.EmptyDirectoryStatusMessage = owner.F(AppLanguageKeys.ToolsStatusOpenScanRootFailedFormat, ex.Message);
                owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
            }
        }

        public void SelectAll()
        {
            foreach (var item in owner.EmptyDirectoryCandidates)
            {
                item.IsSelected = true;
            }

            owner.EmptyDirectoryStatusMessage = owner.L(AppLanguageKeys.ToolsStatusEmptyDirectorySelectAll);
            owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
        }

        public void ClearSelection()
        {
            foreach (var item in owner.EmptyDirectoryCandidates.Where(item => item.IsSelected))
            {
                item.IsSelected = false;
            }

            owner.EmptyDirectoryStatusMessage = owner.L(AppLanguageKeys.ToolsStatusEmptyDirectorySelectionCleared);
            owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
        }

        public async Task DeleteSelectedAsync()
        {
            var selectedPaths = owner.EmptyDirectoryCandidates
                .Where(item => item.IsSelected)
                .Select(item => item.FullPath)
                .ToArray();
            if (selectedPaths.Length == 0)
            {
                owner.EmptyDirectoryStatusMessage = owner.L(AppLanguageKeys.ToolsStatusEmptyDirectorySelectAtLeastOne);
                owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
                return;
            }

            owner.IsToolBusy = true;
            owner.EmptyDirectoryStatusMessage = owner.F(AppLanguageKeys.ToolsStatusEmptyDirectoryDeletingFormat, selectedPaths.Length, selectedPaths.Length == 1 ? "y" : "ies");
            owner.AddToolLog(owner.EmptyDirectoryStatusMessage);

            try
            {
                var results = await owner.emptyDirectoryService.DeleteDirectoriesAsync(selectedPaths).ConfigureAwait(true);
                foreach (var result in results)
                {
                    owner.AddToolLog($"{result.DirectoryPath}: {result.Message}");
                }

                var deletedCount = results.Count(result => result.Succeeded && result.Deleted);
                var missingCount = results.Count(result => result.Succeeded && !result.Deleted);
                var failedCount = results.Count(result => !result.Succeeded);

                await ScanAsync(addLogEntry: false, manageBusyState: false).ConfigureAwait(true);
                owner.EmptyDirectoryStatusMessage = owner.F(
                    AppLanguageKeys.ToolsStatusEmptyDirectoryDeleteSummaryFormat,
                    deletedCount,
                    missingCount,
                    failedCount,
                    owner.EmptyDirectoryCandidates.Count,
                    owner.EmptyDirectoryCandidates.Count == 1 ? "y" : "ies");
                owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
            }
            catch (Exception ex)
            {
                owner.EmptyDirectoryStatusMessage = owner.F(AppLanguageKeys.ToolsStatusEmptyDirectoryDeleteFailedFormat, ex.Message);
                owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
            }
            finally
            {
                owner.IsToolBusy = false;
            }
        }

        public async Task ScanAsync(bool addLogEntry, bool manageBusyState)
        {
            var rootPath = owner.EmptyDirectoryRootPath?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                owner.EmptyDirectoryStatusMessage = owner.L(AppLanguageKeys.ToolsStatusEmptyDirectoryChooseRootFirst);
                if (addLogEntry)
                {
                    owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
                }

                return;
            }

            if (!Directory.Exists(rootPath))
            {
                owner.EmptyDirectoryStatusMessage = owner.F(AppLanguageKeys.ToolsStatusEmptyDirectoryRootMissingFormat, rootPath);
                if (addLogEntry)
                {
                    owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
                }

                return;
            }

            var fullRootPath = Path.GetFullPath(rootPath);
            owner.EmptyDirectoryRootPath = fullRootPath;
            if (manageBusyState)
            {
                owner.IsToolBusy = true;
            }

            owner.EmptyDirectoryStatusMessage = owner.F(AppLanguageKeys.ToolsStatusEmptyDirectoryScanningFormat, fullRootPath);
            owner.StartEmptyDirectoryScanProgress(fullRootPath);
            if (addLogEntry)
            {
                owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
            }

            try
            {
                var progress = new Progress<EmptyDirectoryScanProgress>(owner.UpdateEmptyDirectoryScanProgress);
                var scanResult = await owner.emptyDirectoryService.FindEmptyDirectoriesAsync(fullRootPath, progress).ConfigureAwait(true);
                owner.PersistEmptyDirectoryScanMaxFolderCount(fullRootPath, owner.EmptyDirectoryScanProgressMaximum);
                ReplaceCandidates(fullRootPath, scanResult.Candidates);

                var warningSuffix = scanResult.Warnings.Count == 0
                    ? string.Empty
                    : owner.F(AppLanguageKeys.ToolsStatusEmptyDirectoryWarningsSuffixFormat, scanResult.Warnings.Count, MainWindowViewModel.PluralSuffix(scanResult.Warnings.Count));
                owner.EmptyDirectoryStatusMessage = scanResult.Candidates.Count == 0
                    ? owner.F(AppLanguageKeys.ToolsStatusEmptyDirectoryNoneFoundFormat, warningSuffix)
                    : owner.F(AppLanguageKeys.ToolsStatusEmptyDirectoryFoundFormat, scanResult.Candidates.Count, scanResult.Candidates.Count == 1 ? "y" : "ies", warningSuffix);

                if (addLogEntry)
                {
                    owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
                    foreach (var warning in scanResult.Warnings.Take(10))
                    {
                        owner.AddToolLog(warning);
                    }
                }
            }
            catch (Exception ex)
            {
                owner.EmptyDirectoryStatusMessage = owner.F(AppLanguageKeys.ToolsStatusEmptyDirectoryScanFailedFormat, ex.Message);
                if (addLogEntry)
                {
                    owner.AddToolLog(owner.EmptyDirectoryStatusMessage);
                }
            }
            finally
            {
                owner.ResetEmptyDirectoryScanProgress();
                if (manageBusyState)
                {
                    owner.IsToolBusy = false;
                }
            }
        }

        public void ReplaceCandidates(string rootPath, IReadOnlyList<MultiTool.Core.Models.EmptyDirectoryCandidate> candidates)
        {
            foreach (var item in owner.EmptyDirectoryCandidates)
            {
                item.PropertyChanged -= HandleItemPropertyChanged;
            }

            owner.EmptyDirectoryCandidates.Clear();
            foreach (var candidate in candidates)
            {
                var item = new EmptyDirectoryItem(rootPath, candidate);
                item.PropertyChanged += HandleItemPropertyChanged;
                owner.EmptyDirectoryCandidates.Add(item);
            }

            owner.OnPropertyChanged(nameof(HasSelectedEmptyDirectories));
            owner.OnPropertyChanged(nameof(EmptyDirectorySelectionSummary));
            owner.RefreshEmptyDirectoryResultExpiration();
            owner.RefreshToolCommandStates();
        }
    }
}
