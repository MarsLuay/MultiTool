using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MultiTool.App.Helpers;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.App.Services;
using MultiTool.Core.Defaults;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using MultiTool.Core.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiTool.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private async Task CaptureScreenshotAsync()
        => await PerformScreenshotAsync(ScreenshotMode.FullScreen);

    [RelayCommand]
    private void OpenScreenshotFolder()
    {
        try
        {
            Directory.CreateDirectory(ScreenshotFolderPath);
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = ScreenshotFolderPath,
                    UseShellExecute = true,
                });

            ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusOpenedFolder);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
        catch (Exception ex)
        {
            ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusOpenFolderFailedFormat, ex.Message);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
    }


    public void CaptureScreenshotHotkey(Key key)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(capturedKey);
        if (virtualKey <= 0)
        {
            return;
        }

        ScreenshotHotkeyVirtualKey = virtualKey;
        ScreenshotHotkeyDisplay = HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey);
        ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusHotkeySetFormat, ScreenshotHotkeyDisplay);
        AddScreenshotLog(ScreenshotStatusMessage);
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
    }


    private void AddScreenshotLog(string message)
    {
        AddActivityLog(message);
    }

    private void UpdateLatestScreenshotPreview(string filePath, string fileName)
    {
        LatestScreenshotPreview = LoadPreview(filePath);
        LatestScreenshotCaption = fileName;
        latestScreenshotUpdatedAtUtc = File.GetLastWriteTimeUtc(filePath);
        OnPropertyChanged(nameof(IsLatestMediaVideo));
    }

    private bool RefreshLatestVideoFromCaptureService()
    {
        var previousPath = LatestVideoPath;
        var filePath = screenshotCaptureService.LastSavedVideoPath;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        var fileLastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
        var isSamePath = string.Equals(previousPath, filePath, StringComparison.OrdinalIgnoreCase);
        var wasUpdated = !isSamePath || fileLastWriteTimeUtc != latestVideoUpdatedAtUtc;

        // Force a source refresh when the recorder writes to the same file path.
        if (isSamePath)
        {
            LatestVideoPath = null;
        }

        LatestVideoPath = filePath;
        LatestVideoCaption = Path.GetFileName(filePath);
        latestVideoUpdatedAtUtc = fileLastWriteTimeUtc;
        OnPropertyChanged(nameof(IsLatestMediaVideo));
        return wasUpdated;
    }

    private static ImageSource LoadPreview(string filePath)
    {
        using var stream = File.OpenRead(filePath);

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = 720;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }


    private void BrowseScreenshotFolder()
    {
        var selectedPath = folderPickerService.PickFolder(ScreenshotFolderPath, L(AppLanguageKeys.MainScreenshotFolderPickerPrompt));
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusFolderSelectionCanceled);
            AddScreenshotLog(ScreenshotStatusMessage);
            return;
        }

        ScreenshotFolderPath = selectedPath;
        ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusFolderSetFormat, selectedPath);
        AddScreenshotLog(ScreenshotStatusMessage);
    }

    [RelayCommand]

    private async Task HandleScreenshotCaptureHotkeyAsync()
    {
        if (await TryStopVideoCaptureAsync())
        {
            ResetScreenshotHotkeySequence();
            return;
        }

        await QueueScreenshotHotkeySequenceAsync();
    }

    private async Task<bool> TryStopVideoCaptureAsync()
    {
        if (!screenshotCaptureService.IsVideoCaptureRunning)
        {
            return false;
        }

        await screenshotCaptureService.StopVideoCaptureAsync();
        var videoWasSaved = RefreshLatestVideoFromCaptureService();
        ScreenshotStatusMessage = videoWasSaved
            ? F(AppLanguageKeys.MainScreenshotStatusSavedVideoFormat, LatestVideoCaption)
            : L(AppLanguageKeys.ScreenshotStatusRecordingStopped);
        AddScreenshotLog(ScreenshotStatusMessage);
        return true;
    }

    private async Task QueueScreenshotHotkeySequenceAsync()
    {
        CancellationTokenSource sequenceCancellationTokenSource;
        bool shouldExecuteImmediately;

        lock (screenshotHotkeySequenceSync)
        {
            pendingScreenshotHotkeyPressCount = Math.Min(pendingScreenshotHotkeyPressCount + 1, 3);

            pendingScreenshotHotkeySequenceCancellationTokenSource?.Cancel();
            pendingScreenshotHotkeySequenceCancellationTokenSource?.Dispose();
            pendingScreenshotHotkeySequenceCancellationTokenSource = new CancellationTokenSource();
            sequenceCancellationTokenSource = pendingScreenshotHotkeySequenceCancellationTokenSource;
            shouldExecuteImmediately = pendingScreenshotHotkeyPressCount >= 3;
        }

        if (shouldExecuteImmediately)
        {
            await ExecutePendingScreenshotHotkeySequenceAsync(sequenceCancellationTokenSource);
            return;
        }

        try
        {
            await Task.Delay(ScreenshotHotkeySequenceWindow, sequenceCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await ExecutePendingScreenshotHotkeySequenceAsync(sequenceCancellationTokenSource);
    }

    private async Task ExecutePendingScreenshotHotkeySequenceAsync(CancellationTokenSource sequenceCancellationTokenSource)
    {
        int pressCount;

        lock (screenshotHotkeySequenceSync)
        {
            if (!ReferenceEquals(pendingScreenshotHotkeySequenceCancellationTokenSource, sequenceCancellationTokenSource))
            {
                sequenceCancellationTokenSource.Dispose();
                return;
            }

            pressCount = pendingScreenshotHotkeyPressCount;
            pendingScreenshotHotkeyPressCount = 0;
            pendingScreenshotHotkeySequenceCancellationTokenSource = null;
        }

        sequenceCancellationTokenSource.Dispose();

        switch (pressCount)
        {
            case 1:
                await PerformScreenshotAsync(ScreenshotMode.FullScreen);
                return;
            case 2:
                await PerformScreenshotAsync(ScreenshotMode.Area);
                return;
            default:
                await StartVideoCaptureWithAreaSelectionAsync();
                return;
        }
    }

    private void ResetScreenshotHotkeySequence()
    {
        CancellationTokenSource? sequenceCancellationTokenSource;

        lock (screenshotHotkeySequenceSync)
        {
            pendingScreenshotHotkeyPressCount = 0;
            sequenceCancellationTokenSource = pendingScreenshotHotkeySequenceCancellationTokenSource;
            pendingScreenshotHotkeySequenceCancellationTokenSource = null;
        }

        if (sequenceCancellationTokenSource is null)
        {
            return;
        }

        sequenceCancellationTokenSource.Cancel();
        sequenceCancellationTokenSource.Dispose();
    }

    private async Task StartVideoCaptureWithAreaSelectionAsync()
    {
        var area = await SelectScreenshotAreaAsync();
        if (area is null)
        {
            return;
        }

        await StartVideoCaptureForAreaAsync(area.Value);
    }

    private async Task StartVideoCaptureForAreaAsync(ScreenRectangle area)
    {
        var settings = BuildScreenshotSettings();
        var validation = settingsValidator.ValidateScreenshot(settings);
        if (!validation.IsValid)
        {
            ScreenshotStatusMessage = validation.Summary;
            AddScreenshotLog(validation.Summary);
            return;
        }

        try
        {
            await screenshotCaptureService.StartVideoCaptureAsync(settings.SaveFolderPath, settings.FilePrefix, area);
            ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusAreaRecordingStarted);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
        catch (Exception ex)
        {
            ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusFailedFormat, ex.Message);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
    }

    private async Task<ScreenRectangle?> SelectScreenshotAreaAsync()
    {
        AppLog.Info("Area capture requested from MainWindowViewModel.");
        var area = screenshotAreaSelectionService.SelectArea();
        if (area is null)
        {
            AppLog.Info("Area capture canceled before capture service call.");
            ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusAreaCanceled);
            AddScreenshotLog(ScreenshotStatusMessage);
            return null;
        }

        // Give the area-selection overlay a moment to fully disappear before capturing.
        await Task.Delay(120);
        AppLog.Info($"Area capture using area=({area.Value.X},{area.Value.Y},{area.Value.Width}x{area.Value.Height})");
        return area;
    }

    private async Task PerformScreenshotAsync(ScreenshotMode mode)
    {
        try
        {
            var settings = BuildScreenshotSettings();
            var validation = settingsValidator.ValidateScreenshot(settings);
            if (!validation.IsValid)
            {
                ScreenshotStatusMessage = validation.Summary;
                AddScreenshotLog(validation.Summary);
                return;
            }

            switch (mode)
            {
                case ScreenshotMode.FullScreen:
                    {
                        var path = await screenshotCaptureService.CaptureDesktopAsync(settings.SaveFolderPath, settings.FilePrefix);
                        var fileName = Path.GetFileName(path);
                        UpdateLatestScreenshotPreview(path, fileName);
                        ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusSavedAndCopiedFormat, fileName);
                        AddScreenshotLog(F(AppLanguageKeys.MainScreenshotLogSavedFullScreenFormat, path));
                        break;
                    }
                case ScreenshotMode.Area:
                    {
                        var area = await SelectScreenshotAreaAsync();
                        if (area is null)
                        {
                            return;
                        }

                        var path = await screenshotCaptureService.CaptureAreaAsync(area.Value, settings.SaveFolderPath, settings.FilePrefix);
                        var fileName = Path.GetFileName(path);
                        AppLog.Info($"Area capture completed. OutputPath={path}");
                        UpdateLatestScreenshotPreview(path, fileName);
                        ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusSavedAndCopiedFormat, fileName);
                        AddScreenshotLog(F(AppLanguageKeys.MainScreenshotLogSavedAreaFormat, path));
                        break;
                    }
                case ScreenshotMode.Video:
                    throw new NotSupportedException("Video recording is started from the screenshot hotkey workflow.");
                default:
                    throw new NotSupportedException($"Screenshot mode {mode} is not supported.");
            }
        }
        catch (Exception ex)
        {
            ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusFailedFormat, ex.Message);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
    }



}
