using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiTool.App.Localization;
using MultiTool.Core.Models;

namespace MultiTool.App.ViewModels;

public partial class MainWindowViewModel
{
    private readonly Dictionary<string, StorageBenchmarkReport> storageBenchmarkReportCache = new(StringComparer.OrdinalIgnoreCase);
    private StorageBenchmarkReport? currentStorageBenchmarkReport;

    public ObservableCollection<StorageBenchmarkTargetInfo> StorageBenchmarkTargets { get; } = [];

    public ObservableCollection<StorageBenchmarkModeResult> StorageBenchmarkResults { get; } = [];

    public bool HasStorageBenchmarkTargets => StorageBenchmarkTargets.Count > 0;

    public bool HasStorageBenchmarkResults => StorageBenchmarkResults.Count > 0;

    [ObservableProperty]
    private StorageBenchmarkTargetInfo? selectedStorageBenchmarkTarget;

    [ObservableProperty]
    private string storageBenchmarkStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusStorageBenchmarkInitial);

    [ObservableProperty]
    private string storageBenchmarkSelectedDriveSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStorageBenchmarkSelectedDriveInitial);

    [ObservableProperty]
    private string storageBenchmarkLastBenchmarkedSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStorageBenchmarkLastBenchmarkedInitial);

    [ObservableProperty]
    private string storageBenchmarkSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStorageBenchmarkSummaryInitial);

    [ObservableProperty]
    private string storageBenchmarkBalanceAssessment = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStorageBenchmarkAssessmentInitial);

    [ObservableProperty]
    private string storageBenchmarkDetectedSystemSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStorageBenchmarkDetectedSystemInitial);

    [ObservableProperty]
    private bool isStorageBenchmarkProgressVisible;

    [ObservableProperty]
    private int storageBenchmarkProgressValue;

    [ObservableProperty]
    private int storageBenchmarkProgressMaximum = 1;

    [ObservableProperty]
    private string storageBenchmarkProgressSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStorageBenchmarkProgressInitial);

    partial void OnSelectedStorageBenchmarkTargetChanged(StorageBenchmarkTargetInfo? value)
    {
        if (value is null)
        {
            currentStorageBenchmarkReport = null;
            ReplaceStorageBenchmarkResults([]);
            StorageBenchmarkSelectedDriveSummary = L(AppLanguageKeys.ToolsStorageBenchmarkSelectedDriveInitial);
            StorageBenchmarkLastBenchmarkedSummary = L(AppLanguageKeys.ToolsStorageBenchmarkLastBenchmarkedInitial);
            StorageBenchmarkSummary = L(AppLanguageKeys.ToolsStorageBenchmarkSummaryInitial);
            StorageBenchmarkBalanceAssessment = L(AppLanguageKeys.ToolsStorageBenchmarkAssessmentInitial);
            StorageBenchmarkDetectedSystemSummary = L(AppLanguageKeys.ToolsStorageBenchmarkDetectedSystemInitial);
            RefreshToolCommandStates();
            return;
        }

        StorageBenchmarkSelectedDriveSummary = BuildStorageBenchmarkSelectedDriveSummary(value);

        if (storageBenchmarkReportCache.TryGetValue(value.TargetId, out var cachedReport))
        {
            ApplyStorageBenchmarkReport(cachedReport);
            RefreshStorageBenchmarkExpiration();
            StorageBenchmarkStatusMessage = F(
                AppLanguageKeys.ToolsStatusStorageBenchmarkCachedFormat,
                value.DisplayName,
                cachedReport.CapturedAt.ToLocalTime().ToString("HH:mm:ss"));
        }
        else
        {
            currentStorageBenchmarkReport = null;
            ReplaceStorageBenchmarkResults([]);
            StorageBenchmarkLastBenchmarkedSummary = L(AppLanguageKeys.ToolsStorageBenchmarkLastBenchmarkedInitial);
            StorageBenchmarkSummary = L(AppLanguageKeys.ToolsStorageBenchmarkSummaryInitial);
            StorageBenchmarkBalanceAssessment = L(AppLanguageKeys.ToolsStorageBenchmarkAssessmentInitial);
            StorageBenchmarkDetectedSystemSummary = L(AppLanguageKeys.ToolsStorageBenchmarkDetectedSystemInitial);
        }

        RefreshToolCommandStates();
    }

    private bool CanLoadStorageBenchmarkTargets => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanLoadStorageBenchmarkTargets))]
    private async Task LoadStorageBenchmarkTargetsAsync()
    {
        IsToolBusy = true;
        StorageBenchmarkStatusMessage = L(AppLanguageKeys.ToolsStatusStorageBenchmarkLoadingTargets);
        AddToolLog(StorageBenchmarkStatusMessage);

        try
        {
            var targets = await storageBenchmarkService.GetAvailableTargetsAsync().ConfigureAwait(true);
            ReplaceStorageBenchmarkTargets(targets);

            StorageBenchmarkStatusMessage = targets.Count == 0
                ? L(AppLanguageKeys.ToolsStatusStorageBenchmarkNoDrivesFound)
                : F(AppLanguageKeys.ToolsStatusStorageBenchmarkTargetsLoadedFormat, targets.Count, PluralSuffix(targets.Count));
            AddToolLog(StorageBenchmarkStatusMessage);
        }
        catch (Exception ex)
        {
            StorageBenchmarkStatusMessage = F(AppLanguageKeys.ToolsStatusStorageBenchmarkLoadTargetsFailedFormat, ex.Message);
            AddToolLog(StorageBenchmarkStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanRunSelectedStorageBenchmark => !IsToolBusy && SelectedStorageBenchmarkTarget is not null;

    [RelayCommand(CanExecute = nameof(CanRunSelectedStorageBenchmark))]
    private async Task RunSelectedStorageBenchmarkAsync()
    {
        var benchmarkTarget = SelectedStorageBenchmarkTarget;
        if (benchmarkTarget is null)
        {
            StorageBenchmarkStatusMessage = L(AppLanguageKeys.ToolsStatusStorageBenchmarkSelectDriveFirst);
            AddToolLog(StorageBenchmarkStatusMessage);
            return;
        }

        IsToolBusy = true;
        StartStorageBenchmarkProgress();
        StorageBenchmarkStatusMessage = F(AppLanguageKeys.ToolsStatusStorageBenchmarkRunningFormat, benchmarkTarget.DisplayName);
        AddToolLog(StorageBenchmarkStatusMessage);

        try
        {
            var progress = new Progress<StorageBenchmarkProgressUpdate>(update => UpdateStorageBenchmarkProgress(benchmarkTarget.DisplayName, update));
            var report = await storageBenchmarkService.RunAsync(benchmarkTarget.TargetId, progress).ConfigureAwait(true);
            storageBenchmarkReportCache[report.Target.TargetId] = report;
            ApplyStorageBenchmarkReport(report);
            RefreshStorageBenchmarkExpiration();

            var warningSuffix = report.Warnings.Count == 0
                ? string.Empty
                : $" Warnings: {report.Warnings.Count}.";
            StorageBenchmarkStatusMessage = F(
                AppLanguageKeys.ToolsStatusStorageBenchmarkCompletedFormat,
                report.Target.DisplayName,
                report.Summary,
                report.BalanceAssessment,
                warningSuffix);
            AddToolLog(StorageBenchmarkStatusMessage);
            foreach (var warning in report.Warnings.Take(5))
            {
                AddToolLog(warning);
            }
        }
        catch (Exception ex)
        {
            StorageBenchmarkStatusMessage = F(AppLanguageKeys.ToolsStatusStorageBenchmarkFailedFormat, ex.Message);
            AddToolLog(StorageBenchmarkStatusMessage);
        }
        finally
        {
            ResetStorageBenchmarkProgress();
            IsToolBusy = false;
        }
    }

    private bool CanExportStorageBenchmarkReport => !IsToolBusy && currentStorageBenchmarkReport is not null;

    [RelayCommand(CanExecute = nameof(CanExportStorageBenchmarkReport))]
    private async Task ExportStorageBenchmarkReportAsync()
    {
        if (currentStorageBenchmarkReport is null)
        {
            StorageBenchmarkStatusMessage = L(AppLanguageKeys.ToolsStatusStorageBenchmarkNoReportToExport);
            AddToolLog(StorageBenchmarkStatusMessage);
            return;
        }

        var defaultFileName = BuildStorageBenchmarkExportFileName(currentStorageBenchmarkReport);
        var targetPath = textFileSaveDialogService.PickSavePath(
            L(AppLanguageKeys.ToolsStorageBenchmarkExportDialogTitle),
            defaultFileName,
            "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            ".csv");

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            StorageBenchmarkStatusMessage = L(AppLanguageKeys.ToolsStatusStorageBenchmarkExportCanceled);
            AddToolLog(StorageBenchmarkStatusMessage);
            return;
        }

        try
        {
            var exportContent = Path.GetExtension(targetPath).Equals(".txt", StringComparison.OrdinalIgnoreCase)
                ? BuildStorageBenchmarkTextExport(currentStorageBenchmarkReport)
                : BuildStorageBenchmarkCsvExport(currentStorageBenchmarkReport);
            await File.WriteAllTextAsync(targetPath, exportContent, Encoding.UTF8).ConfigureAwait(true);

            StorageBenchmarkStatusMessage = F(AppLanguageKeys.ToolsStatusStorageBenchmarkExportedFormat, targetPath);
            AddToolLog(StorageBenchmarkStatusMessage);
        }
        catch (Exception ex)
        {
            StorageBenchmarkStatusMessage = F(AppLanguageKeys.ToolsStatusStorageBenchmarkExportFailedFormat, ex.Message);
            AddToolLog(StorageBenchmarkStatusMessage);
        }
    }

    private void ReplaceStorageBenchmarkTargets(IReadOnlyList<StorageBenchmarkTargetInfo> targets)
    {
        var previouslySelectedTargetId = SelectedStorageBenchmarkTarget?.TargetId;
        StorageBenchmarkTargets.Clear();
        foreach (var target in targets)
        {
            StorageBenchmarkTargets.Add(target);
        }

        OnPropertyChanged(nameof(HasStorageBenchmarkTargets));

        var nextSelection = targets.FirstOrDefault(target =>
                !string.IsNullOrWhiteSpace(previouslySelectedTargetId)
                && target.TargetId.Equals(previouslySelectedTargetId, StringComparison.OrdinalIgnoreCase))
            ?? targets.FirstOrDefault();

        SelectedStorageBenchmarkTarget = null;
        SelectedStorageBenchmarkTarget = nextSelection;
    }

    private void ReplaceStorageBenchmarkResults(IReadOnlyList<StorageBenchmarkModeResult> results)
    {
        StorageBenchmarkResults.Clear();
        foreach (var result in results)
        {
            StorageBenchmarkResults.Add(result);
        }

        OnPropertyChanged(nameof(HasStorageBenchmarkResults));
    }

    private void ApplyStorageBenchmarkReport(StorageBenchmarkReport report)
    {
        currentStorageBenchmarkReport = report;
        StorageBenchmarkSelectedDriveSummary = BuildStorageBenchmarkSelectedDriveSummary(report.Target);
        StorageBenchmarkLastBenchmarkedSummary = $"Last benchmark: {report.CapturedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
        StorageBenchmarkSummary = report.Summary;
        StorageBenchmarkBalanceAssessment = report.BalanceAssessment;
        StorageBenchmarkDetectedSystemSummary = report.DetectedSystemSummary;
        ReplaceStorageBenchmarkResults(report.Results);
        RefreshToolCommandStates();
    }

    private void RefreshStorageBenchmarkExpiration()
    {
        if (storageBenchmarkReportCache.Count == 0)
        {
            CancelToolScanResultExpiration(ToolScanResultCacheSlot.StorageBenchmark);
            return;
        }

        ScheduleToolScanResultExpiration(ToolScanResultCacheSlot.StorageBenchmark, ExpireStorageBenchmarkResults);
    }

    private void ExpireStorageBenchmarkResults()
    {
        if (storageBenchmarkReportCache.Count == 0 && currentStorageBenchmarkReport is null)
        {
            return;
        }

        storageBenchmarkReportCache.Clear();
        currentStorageBenchmarkReport = null;
        ReplaceStorageBenchmarkResults([]);
        StorageBenchmarkLastBenchmarkedSummary = L(AppLanguageKeys.ToolsStorageBenchmarkLastBenchmarkedInitial);
        StorageBenchmarkSummary = L(AppLanguageKeys.ToolsStorageBenchmarkSummaryInitial);
        StorageBenchmarkBalanceAssessment = L(AppLanguageKeys.ToolsStorageBenchmarkAssessmentInitial);
        StorageBenchmarkDetectedSystemSummary = L(AppLanguageKeys.ToolsStorageBenchmarkDetectedSystemInitial);
        StorageBenchmarkSelectedDriveSummary = SelectedStorageBenchmarkTarget is null
            ? L(AppLanguageKeys.ToolsStorageBenchmarkSelectedDriveInitial)
            : BuildStorageBenchmarkSelectedDriveSummary(SelectedStorageBenchmarkTarget);

        StorageBenchmarkStatusMessage = L(AppLanguageKeys.ToolsStatusStorageBenchmarkExpired);
        AddToolLog(StorageBenchmarkStatusMessage);
        RefreshStorageBenchmarkExpiration();
        RefreshToolCommandStates();
    }

    private static string BuildStorageBenchmarkSelectedDriveSummary(StorageBenchmarkTargetInfo target)
    {
        var volumeText = string.IsNullOrWhiteSpace(target.VolumeLabel)
            ? target.VolumeRootPath.TrimEnd('\\')
            : $"{target.VolumeRootPath.TrimEnd('\\')} ({target.VolumeLabel})";

        return string.Join(
            "  |  ",
            new[]
            {
                target.Model,
                volumeText,
                $"{target.InterfaceType} / {target.MediaType}",
                string.IsNullOrWhiteSpace(target.FileSystem) ? string.Empty : target.FileSystem,
                string.IsNullOrWhiteSpace(target.FreeSpace) ? string.Empty : $"{target.FreeSpace} free",
                $"Firmware {target.FirmwareVersion}",
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildStorageBenchmarkExportFileName(StorageBenchmarkReport report)
    {
        var baseName = string.Join(
            "-",
            report.Target.Model
                .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "ssd-benchmark";
        }

        return $"{baseName}-ssd-benchmark-{report.CapturedAt:yyyyMMdd-HHmmss}.csv";
    }

    private static string BuildStorageBenchmarkCsvExport(StorageBenchmarkReport report)
    {
        StringBuilder builder = new();
        builder.AppendLine("Drive,Volume,Detected System,Balance Assessment,Captured At,Mode,Throughput MB/s,IOPS,Block Size,Notes");

        foreach (var result in report.Results)
        {
            builder.AppendLine(
                string.Join(
                    ",",
                    EscapeStorageBenchmarkCsv(report.Target.Model),
                    EscapeStorageBenchmarkCsv(report.Target.VolumeRootPath.TrimEnd('\\')),
                    EscapeStorageBenchmarkCsv(report.DetectedSystemSummary),
                    EscapeStorageBenchmarkCsv(report.BalanceAssessment),
                    EscapeStorageBenchmarkCsv(report.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeStorageBenchmarkCsv(result.Mode),
                    EscapeStorageBenchmarkCsv(result.ThroughputMegabytesPerSecond.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                    EscapeStorageBenchmarkCsv(result.Iops.ToString("0", System.Globalization.CultureInfo.InvariantCulture)),
                    EscapeStorageBenchmarkCsv(result.BlockSizeBytes.ToString()),
                    EscapeStorageBenchmarkCsv(result.Notes)));
        }

        return builder.ToString();
    }

    private static string BuildStorageBenchmarkTextExport(StorageBenchmarkReport report)
    {
        StringBuilder builder = new();
        builder.AppendLine("MultiTool SSD Benchmark");
        builder.AppendLine($"Drive: {report.Target.Model}");
        builder.AppendLine($"Volume: {report.Target.VolumeRootPath.TrimEnd('\\')}");
        builder.AppendLine($"Captured: {report.CapturedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Detected System: {report.DetectedSystemSummary}");
        builder.AppendLine($"Summary: {report.Summary}");
        builder.AppendLine($"Assessment: {report.BalanceAssessment}");
        builder.AppendLine();
        builder.AppendLine("Mode | Throughput MB/s | IOPS | Block Size | Notes");

        foreach (var result in report.Results)
        {
            builder.AppendLine(
                $"{result.Mode} | {result.ThroughputMegabytesPerSecond:N2} | {result.Iops:N0} | {result.BlockSizeBytes} | {result.Notes}");
        }

        if (report.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Warnings");
            foreach (var warning in report.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        return builder.ToString();
    }

    private static string EscapeStorageBenchmarkCsv(string value)
    {
        var normalized = value.Replace("\"", "\"\"");
        return $"\"{normalized}\"";
    }

    private void StartStorageBenchmarkProgress()
    {
        IsStorageBenchmarkProgressVisible = true;
        StorageBenchmarkProgressValue = 0;
        StorageBenchmarkProgressMaximum = 1;
        StorageBenchmarkProgressSummary = L(AppLanguageKeys.ToolsStorageBenchmarkProgressInitial);
    }

    private void UpdateStorageBenchmarkProgress(string targetDisplayName, StorageBenchmarkProgressUpdate progress)
    {
        StorageBenchmarkProgressMaximum = Math.Max(progress.TotalStages, 1);
        StorageBenchmarkProgressValue = Math.Clamp(progress.CurrentStage, 0, StorageBenchmarkProgressMaximum);
        StorageBenchmarkProgressSummary = F(
            AppLanguageKeys.ToolsStorageBenchmarkProgressSummaryFormat,
            progress.CurrentStage,
            progress.TotalStages,
            progress.StageName,
            progress.Detail);
        StorageBenchmarkStatusMessage = F(
            AppLanguageKeys.ToolsStatusStorageBenchmarkRunningStageFormat,
            targetDisplayName,
            progress.CurrentStage,
            progress.TotalStages,
            progress.StageName,
            progress.Detail);
    }

    private void ResetStorageBenchmarkProgress()
    {
        IsStorageBenchmarkProgressVisible = false;
        StorageBenchmarkProgressValue = 0;
        StorageBenchmarkProgressMaximum = 1;
        StorageBenchmarkProgressSummary = L(AppLanguageKeys.ToolsStorageBenchmarkProgressInitial);
    }
}
