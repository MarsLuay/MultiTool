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
    private readonly Dictionary<string, DriveSmartHealthReport> driveSmartHealthReportCache = new(StringComparer.OrdinalIgnoreCase);
    private DriveSmartHealthReport? currentDriveSmartHealthReport;

    public ObservableCollection<DriveSmartTargetInfo> DriveSmartTargets { get; } = [];

    public ObservableCollection<DriveSmartAttributeInfo> DriveSmartAttributes { get; } = [];

    public bool HasDriveSmartTargets => DriveSmartTargets.Count > 0;

    public bool HasDriveSmartAttributes => DriveSmartAttributes.Count > 0;

    [ObservableProperty]
    private DriveSmartTargetInfo? selectedDriveSmartTarget;

    [ObservableProperty]
    private string driveSmartStatusMessage = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsStatusDriveSmartInitial);

    [ObservableProperty]
    private string driveSmartOverallHealth = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsDriveSmartOverallHealthInitial);

    [ObservableProperty]
    private string driveSmartOverallHealthDisplay = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsDriveSmartOverallHealthDisplayInitial);

    [ObservableProperty]
    private string driveSmartSelectedDriveSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsDriveSmartSelectedDriveInitial);

    [ObservableProperty]
    private string driveSmartLastScannedSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsDriveSmartLastScannedInitial);

    [ObservableProperty]
    private string driveSmartReportSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsDriveSmartReportSummaryInitial);

    [ObservableProperty]
    private string driveSmartQuickSummary = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsDriveSmartQuickSummaryInitial);

    [ObservableProperty]
    private string driveSmartRecommendedAction = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.ToolsDriveSmartRecommendedActionInitial);

    partial void OnSelectedDriveSmartTargetChanged(DriveSmartTargetInfo? value)
    {
        if (value is null)
        {
            currentDriveSmartHealthReport = null;
            ReplaceDriveSmartAttributes([]);
            DriveSmartSelectedDriveSummary = L(AppLanguageKeys.ToolsDriveSmartSelectedDriveInitial);
            DriveSmartOverallHealth = L(AppLanguageKeys.ToolsDriveSmartOverallHealthInitial);
            DriveSmartOverallHealthDisplay = L(AppLanguageKeys.ToolsDriveSmartOverallHealthDisplayInitial);
            DriveSmartLastScannedSummary = L(AppLanguageKeys.ToolsDriveSmartLastScannedInitial);
            DriveSmartReportSummary = L(AppLanguageKeys.ToolsDriveSmartReportSummaryInitial);
            ResetDriveSmartFriendlyGuidance();
            RefreshToolCommandStates();
            return;
        }

        DriveSmartSelectedDriveSummary = BuildDriveSmartSelectedDriveSummary(value);

        if (driveSmartHealthReportCache.TryGetValue(value.DeviceId, out var cachedReport))
        {
            ApplyDriveSmartReport(cachedReport);
            RefreshDriveSmartExpiration();
            DriveSmartStatusMessage = F(
                AppLanguageKeys.ToolsStatusDriveSmartCachedFormat,
                cachedReport.Drive.Model,
                cachedReport.CapturedAt.ToLocalTime().ToString("HH:mm:ss"),
                cachedReport.OverallHealth);
        }
        else
        {
            currentDriveSmartHealthReport = null;
            ReplaceDriveSmartAttributes([]);
            DriveSmartOverallHealth = L(AppLanguageKeys.ToolsDriveSmartOverallHealthInitial);
            DriveSmartOverallHealthDisplay = L(AppLanguageKeys.ToolsDriveSmartOverallHealthDisplayInitial);
            DriveSmartLastScannedSummary = L(AppLanguageKeys.ToolsDriveSmartLastScannedInitial);
            DriveSmartReportSummary = L(AppLanguageKeys.ToolsDriveSmartReportSummaryInitial);
            ResetDriveSmartFriendlyGuidance();
        }

        RefreshToolCommandStates();
    }

    private bool CanLoadDriveSmartTargets => !IsToolBusy;

    [RelayCommand(CanExecute = nameof(CanLoadDriveSmartTargets))]
    private async Task LoadDriveSmartTargetsAsync()
    {
        IsToolBusy = true;
        DriveSmartStatusMessage = L(AppLanguageKeys.ToolsStatusDriveSmartLoadingTargets);
        AddToolLog(DriveSmartStatusMessage);

        try
        {
            var targets = await driveSmartHealthService.GetAvailableDrivesAsync().ConfigureAwait(true);
            ReplaceDriveSmartTargets(targets);

            DriveSmartStatusMessage = targets.Count == 0
                ? L(AppLanguageKeys.ToolsStatusDriveSmartNoDrivesFound)
                : F(AppLanguageKeys.ToolsStatusDriveSmartTargetsLoadedFormat, targets.Count, PluralSuffix(targets.Count));
            AddToolLog(DriveSmartStatusMessage);
        }
        catch (Exception ex)
        {
            DriveSmartStatusMessage = F(AppLanguageKeys.ToolsStatusDriveSmartLoadTargetsFailedFormat, ex.Message);
            AddToolLog(DriveSmartStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanScanSelectedDriveSmart => !IsToolBusy && SelectedDriveSmartTarget is not null;

    [RelayCommand(CanExecute = nameof(CanScanSelectedDriveSmart))]
    private async Task ScanSelectedDriveSmartAsync()
    {
        if (SelectedDriveSmartTarget is null)
        {
            DriveSmartStatusMessage = L(AppLanguageKeys.ToolsStatusDriveSmartSelectDriveFirst);
            AddToolLog(DriveSmartStatusMessage);
            return;
        }

        IsToolBusy = true;
        DriveSmartStatusMessage = F(AppLanguageKeys.ToolsStatusDriveSmartScanningFormat, SelectedDriveSmartTarget.Model);
        AddToolLog(DriveSmartStatusMessage);

        try
        {
            var report = await driveSmartHealthService.ScanAsync(SelectedDriveSmartTarget.DeviceId).ConfigureAwait(true);
            driveSmartHealthReportCache[report.Drive.DeviceId] = report;
            ApplyDriveSmartReport(report);
            RefreshDriveSmartExpiration();

            var warningSuffix = report.Warnings.Count == 0
                ? string.Empty
                : F(AppLanguageKeys.ToolsStatusDriveSmartWarningsSuffixFormat, report.Warnings.Count);
            DriveSmartStatusMessage = F(
                AppLanguageKeys.ToolsStatusDriveSmartScannedFormat,
                report.Drive.Model,
                report.OverallHealth,
                report.Attributes.Count,
                PluralSuffix(report.Attributes.Count),
                warningSuffix);
            AddToolLog(DriveSmartStatusMessage);
            foreach (var warning in report.Warnings.Take(5))
            {
                AddToolLog(warning);
            }
        }
        catch (Exception ex)
        {
            DriveSmartStatusMessage = F(AppLanguageKeys.ToolsStatusDriveSmartScanFailedFormat, ex.Message);
            AddToolLog(DriveSmartStatusMessage);
        }
        finally
        {
            IsToolBusy = false;
        }
    }

    private bool CanExportDriveSmartReport => !IsToolBusy && currentDriveSmartHealthReport is not null;

    [RelayCommand(CanExecute = nameof(CanExportDriveSmartReport))]
    private async Task ExportDriveSmartReportAsync()
    {
        if (currentDriveSmartHealthReport is null)
        {
            DriveSmartStatusMessage = L(AppLanguageKeys.ToolsStatusDriveSmartNoReportToExport);
            AddToolLog(DriveSmartStatusMessage);
            return;
        }

        var defaultFileName = BuildDriveSmartExportFileName(currentDriveSmartHealthReport);
        var targetPath = textFileSaveDialogService.PickSavePath(
            L(AppLanguageKeys.ToolsDriveSmartExportDialogTitle),
            defaultFileName,
            "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            ".csv");

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            DriveSmartStatusMessage = L(AppLanguageKeys.ToolsStatusDriveSmartExportCanceled);
            AddToolLog(DriveSmartStatusMessage);
            return;
        }

        try
        {
            var exportContent = Path.GetExtension(targetPath).Equals(".txt", StringComparison.OrdinalIgnoreCase)
                ? BuildDriveSmartTextExport(currentDriveSmartHealthReport)
                : BuildDriveSmartCsvExport(currentDriveSmartHealthReport);
            await File.WriteAllTextAsync(targetPath, exportContent, Encoding.UTF8).ConfigureAwait(true);

            DriveSmartStatusMessage = F(AppLanguageKeys.ToolsStatusDriveSmartExportedFormat, targetPath);
            AddToolLog(DriveSmartStatusMessage);
        }
        catch (Exception ex)
        {
            DriveSmartStatusMessage = F(AppLanguageKeys.ToolsStatusDriveSmartExportFailedFormat, ex.Message);
            AddToolLog(DriveSmartStatusMessage);
        }
    }

    private void ReplaceDriveSmartTargets(IReadOnlyList<DriveSmartTargetInfo> targets)
    {
        var previouslySelectedDeviceId = SelectedDriveSmartTarget?.DeviceId;
        var orderedTargets = targets
            .OrderBy(GetDriveSmartSortRank)
            .ThenBy(static target => NormalizeDriveSmartVolumeRoot(target.PrimaryVolumeRootPath), StringComparer.OrdinalIgnoreCase)
            .ThenBy(static target => target.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        DriveSmartTargets.Clear();
        foreach (var target in orderedTargets)
        {
            DriveSmartTargets.Add(target);
        }

        OnPropertyChanged(nameof(HasDriveSmartTargets));

        var nextSelection = orderedTargets.FirstOrDefault(target =>
                !string.IsNullOrWhiteSpace(previouslySelectedDeviceId) &&
                target.DeviceId.Equals(previouslySelectedDeviceId, StringComparison.OrdinalIgnoreCase))
            ?? orderedTargets.FirstOrDefault(static target => NormalizeDriveSmartVolumeRoot(target.PrimaryVolumeRootPath).Equals("C:", StringComparison.OrdinalIgnoreCase))
            ?? orderedTargets.FirstOrDefault();

        SelectedDriveSmartTarget = null;
        SelectedDriveSmartTarget = nextSelection;
    }

    private void ReplaceDriveSmartAttributes(IReadOnlyList<DriveSmartAttributeInfo> attributes)
    {
        DriveSmartAttributes.Clear();
        foreach (var attribute in attributes)
        {
            DriveSmartAttributes.Add(attribute);
        }

        OnPropertyChanged(nameof(HasDriveSmartAttributes));
    }

    private void ApplyDriveSmartReport(DriveSmartHealthReport report)
    {
        currentDriveSmartHealthReport = report;
        DriveSmartSelectedDriveSummary = BuildDriveSmartSelectedDriveSummary(report.Drive);
        DriveSmartOverallHealth = report.OverallHealth;
        DriveSmartOverallHealthDisplay = BuildDriveSmartOverallHealthDisplay(report.OverallHealth);
        DriveSmartLastScannedSummary = $"Last scanned: {report.CapturedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
        DriveSmartReportSummary = BuildDriveSmartFriendlyReportSummary(report);
        ApplyDriveSmartFriendlyGuidance(report.OverallHealth);
        ReplaceDriveSmartAttributes(report.Attributes);
        RefreshToolCommandStates();
    }

    private void RefreshDriveSmartExpiration()
    {
        if (driveSmartHealthReportCache.Count == 0)
        {
            CancelToolScanResultExpiration(ToolScanResultCacheSlot.DriveSmartHealth);
            return;
        }

        ScheduleToolScanResultExpiration(ToolScanResultCacheSlot.DriveSmartHealth, ExpireDriveSmartResults);
    }

    private void ExpireDriveSmartResults()
    {
        if (driveSmartHealthReportCache.Count == 0 && currentDriveSmartHealthReport is null)
        {
            return;
        }

        driveSmartHealthReportCache.Clear();
        currentDriveSmartHealthReport = null;
        ReplaceDriveSmartAttributes([]);
        DriveSmartOverallHealth = L(AppLanguageKeys.ToolsDriveSmartOverallHealthInitial);
        DriveSmartOverallHealthDisplay = L(AppLanguageKeys.ToolsDriveSmartOverallHealthDisplayInitial);
        DriveSmartLastScannedSummary = L(AppLanguageKeys.ToolsDriveSmartLastScannedInitial);
        DriveSmartReportSummary = L(AppLanguageKeys.ToolsDriveSmartReportSummaryInitial);
        ResetDriveSmartFriendlyGuidance();
        if (SelectedDriveSmartTarget is null)
        {
            DriveSmartSelectedDriveSummary = L(AppLanguageKeys.ToolsDriveSmartSelectedDriveInitial);
        }
        else
        {
            DriveSmartSelectedDriveSummary = BuildDriveSmartSelectedDriveSummary(SelectedDriveSmartTarget);
        }

        DriveSmartStatusMessage = L(AppLanguageKeys.ToolsStatusDriveSmartExpired);
        AddToolLog(DriveSmartStatusMessage);
        RefreshDriveSmartExpiration();
        RefreshToolCommandStates();
    }

    private static string BuildDriveSmartSelectedDriveSummary(DriveSmartTargetInfo target) =>
        string.Join(
            "  |  ",
            new[]
            {
                target.Model,
                target.VolumePathsSummary,
                string.IsNullOrWhiteSpace(target.Size) ? string.Empty : target.Size,
                $"{target.InterfaceType} / {target.MediaType}",
                $"Firmware {target.FirmwareVersion}",
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

    private void ResetDriveSmartFriendlyGuidance()
    {
        DriveSmartQuickSummary = L(AppLanguageKeys.ToolsDriveSmartQuickSummaryInitial);
        DriveSmartRecommendedAction = L(AppLanguageKeys.ToolsDriveSmartRecommendedActionInitial);
    }

    private void ApplyDriveSmartFriendlyGuidance(string overallHealth)
    {
        switch (overallHealth.Trim())
        {
            case "Healthy":
                DriveSmartQuickSummary = L(AppLanguageKeys.ToolsDriveSmartQuickSummaryHealthy);
                DriveSmartRecommendedAction = L(AppLanguageKeys.ToolsDriveSmartRecommendedActionHealthy);
                break;
            case "Warning":
                DriveSmartQuickSummary = L(AppLanguageKeys.ToolsDriveSmartQuickSummaryWarning);
                DriveSmartRecommendedAction = L(AppLanguageKeys.ToolsDriveSmartRecommendedActionWarning);
                break;
            case "Critical":
                DriveSmartQuickSummary = L(AppLanguageKeys.ToolsDriveSmartQuickSummaryCritical);
                DriveSmartRecommendedAction = L(AppLanguageKeys.ToolsDriveSmartRecommendedActionCritical);
                break;
            default:
                DriveSmartQuickSummary = L(AppLanguageKeys.ToolsDriveSmartQuickSummaryUnknown);
                DriveSmartRecommendedAction = L(AppLanguageKeys.ToolsDriveSmartRecommendedActionUnknown);
                break;
        }
    }

    private string BuildDriveSmartOverallHealthDisplay(string overallHealth) =>
        overallHealth.Trim() switch
        {
            "Healthy" => L(AppLanguageKeys.ToolsDriveSmartOverallHealthDisplayHealthy),
            "Warning" => L(AppLanguageKeys.ToolsDriveSmartOverallHealthDisplayWarning),
            "Critical" => L(AppLanguageKeys.ToolsDriveSmartOverallHealthDisplayCritical),
            _ => L(AppLanguageKeys.ToolsDriveSmartOverallHealthDisplayUnknown),
        };

    private string BuildDriveSmartFriendlyReportSummary(DriveSmartHealthReport report)
    {
        var summary = report.Summary.Trim();
        var prefix = $"Overall health: {report.OverallHealth}.";
        if (summary.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            summary = summary[prefix.Length..].Trim();
        }

        return string.IsNullOrWhiteSpace(summary)
            ? L(AppLanguageKeys.ToolsDriveSmartReportSummaryUnavailable)
            : summary;
    }

    private static int GetDriveSmartSortRank(DriveSmartTargetInfo target)
    {
        var normalized = NormalizeDriveSmartVolumeRoot(target.PrimaryVolumeRootPath);
        if (normalized.Length >= 2 && normalized[1] == ':')
        {
            var driveLetter = char.ToUpperInvariant(normalized[0]);
            if (driveLetter == 'C')
            {
                return 0;
            }

            if (driveLetter is >= 'D' and <= 'Z')
            {
                return 1 + (driveLetter - 'D');
            }

            if (driveLetter is >= 'A' and <= 'B')
            {
                return 100 + (driveLetter - 'A');
            }
        }

        return 1000;
    }

    private static string NormalizeDriveSmartVolumeRoot(string? volumeRootPath) =>
        string.IsNullOrWhiteSpace(volumeRootPath)
            ? string.Empty
            : volumeRootPath.Trim().TrimEnd('\\').ToUpperInvariant();

    private static string BuildDriveSmartExportFileName(DriveSmartHealthReport report)
    {
        var baseName = string.Join(
            "-",
            report.Drive.Model
                .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "drive-smart";
        }

        return $"{baseName}-smart-{report.CapturedAt:yyyyMMdd-HHmmss}.csv";
    }

    private static string BuildDriveSmartCsvExport(DriveSmartHealthReport report)
    {
        StringBuilder builder = new();
        builder.AppendLine("Drive,Overall Health,Captured At,Byte,Status,Description,Raw Data");

        if (report.Attributes.Count == 0)
        {
            builder.AppendLine(
                string.Join(
                    ",",
                    EscapeCsv(report.Drive.Model),
                    EscapeCsv(report.OverallHealth),
                    EscapeCsv(report.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsv(string.Empty),
                    EscapeCsv("Unavailable"),
                    EscapeCsv("Raw SMART attributes were not available for this drive on this PC."),
                    EscapeCsv(string.Empty)));
            return builder.ToString();
        }

        foreach (var attribute in report.Attributes)
        {
            builder.AppendLine(
                string.Join(
                    ",",
                    EscapeCsv(report.Drive.Model),
                    EscapeCsv(report.OverallHealth),
                    EscapeCsv(report.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsv(attribute.Byte),
                    EscapeCsv(attribute.Status),
                    EscapeCsv(attribute.Description),
                    EscapeCsv(attribute.RawData)));
        }

        return builder.ToString();
    }

    private static string BuildDriveSmartTextExport(DriveSmartHealthReport report)
    {
        StringBuilder builder = new();
        builder.AppendLine("MultiTool SMART Drive Health");
        builder.AppendLine($"Drive: {report.Drive.Model}");
        builder.AppendLine($"Overall Health: {report.OverallHealth}");
        builder.AppendLine($"Captured: {report.CapturedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Details: {report.Summary}");
        builder.AppendLine($"Interface / Media: {report.Drive.InterfaceType} / {report.Drive.MediaType}");
        builder.AppendLine($"Firmware: {report.Drive.FirmwareVersion}");
        builder.AppendLine($"Serial: {report.Drive.SerialNumber}");
        builder.AppendLine();
        builder.AppendLine("Byte | Status | Description | Raw Data");

        if (report.Attributes.Count == 0)
        {
            builder.AppendLine("--   | Unavailable | Raw SMART attributes were not available for this drive on this PC. | --");
        }
        else
        {
            foreach (var attribute in report.Attributes)
            {
                builder.AppendLine($"{attribute.Byte} | {attribute.Status} | {attribute.Description} | {attribute.RawData}");
            }
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

    private static string EscapeCsv(string value)
    {
        var normalized = value.Replace("\"", "\"\"");
        return $"\"{normalized}\"";
    }
}
