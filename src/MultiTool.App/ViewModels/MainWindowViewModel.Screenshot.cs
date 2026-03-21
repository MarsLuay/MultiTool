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


    public void CaptureScreenshotHotkey(Key key, ModifierKeys modifiers)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var binding = HotkeyDisplayNameFormatter.CreateKeyboardBinding(capturedKey, modifiers);
        if (binding.VirtualKey <= 0)
        {
            return;
        }

        ScreenshotHotkeyVirtualKey = binding.VirtualKey;
        ScreenshotHotkeyModifiers = binding.Modifiers;
        ScreenshotHotkeyDisplay = binding.DisplayName;
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
        latestScreenshotFilePath = filePath;
        LatestScreenshotCaption = fileName;
        latestScreenshotUpdatedAtUtc = File.GetLastWriteTimeUtc(filePath);
        RefreshLatestScreenshotPresentation(forceReload: true);
    }

    private bool RefreshLatestVideoFromCaptureService()
    {
        return RefreshLatestVideo(screenshotCaptureService.LastSavedVideoPath);
    }

    private bool RefreshLatestVideo(string? filePath)
    {
        var previousPath = latestVideoFilePath;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        var fileLastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
        var isSamePath = string.Equals(previousPath, filePath, StringComparison.OrdinalIgnoreCase);
        var wasUpdated = !isSamePath || fileLastWriteTimeUtc != latestVideoUpdatedAtUtc;

        latestVideoFilePath = filePath;
        LatestVideoCaption = Path.GetFileName(filePath);
        latestVideoUpdatedAtUtc = fileLastWriteTimeUtc;
        RefreshLatestVideoPresentation(forceReload: true);
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

    private void HandleScreenshotTabSelectionChanged(int selectedTabIndex)
    {
        if (selectedTabIndex == ScreenshotTabIndex)
        {
            RefreshLatestScreenshotPresentation(forceReload: true);
            RefreshLatestVideoPresentation(forceReload: true);
            return;
        }

        UnloadLatestMediaPresentation();
    }

    private void RefreshLatestScreenshotPresentation(bool forceReload)
    {
        if (SelectedMainTabIndex != ScreenshotTabIndex)
        {
            if (LatestScreenshotPreview is not null)
            {
                LatestScreenshotPreview = null;
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(latestScreenshotFilePath))
        {
            if (LatestScreenshotPreview is not null)
            {
                LatestScreenshotPreview = null;
            }

            return;
        }

        if (!File.Exists(latestScreenshotFilePath))
        {
            ClearLatestScreenshotState();
            return;
        }

        if (!forceReload && LatestScreenshotPreview is not null)
        {
            return;
        }

        try
        {
            LatestScreenshotPreview = LoadPreview(latestScreenshotFilePath);
        }
        catch (Exception ex)
        {
            AppLog.Info($"Unable to reload the latest screenshot preview from '{latestScreenshotFilePath}': {ex.Message}");
            ClearLatestScreenshotState();
        }
    }

    private void RefreshLatestVideoPresentation(bool forceReload)
    {
        if (SelectedMainTabIndex != ScreenshotTabIndex)
        {
            if (LatestVideoPath is not null)
            {
                LatestVideoPath = null;
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(latestVideoFilePath))
        {
            if (LatestVideoPath is not null)
            {
                LatestVideoPath = null;
            }

            return;
        }

        if (!File.Exists(latestVideoFilePath))
        {
            ClearLatestVideoState();
            return;
        }

        if (!forceReload && string.Equals(LatestVideoPath, latestVideoFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Force MediaElement to reopen when the same file path is reused.
        if (forceReload && string.Equals(LatestVideoPath, latestVideoFilePath, StringComparison.OrdinalIgnoreCase))
        {
            LatestVideoPath = null;
        }

        LatestVideoPath = latestVideoFilePath;
    }

    private void UnloadLatestMediaPresentation()
    {
        if (LatestScreenshotPreview is not null)
        {
            LatestScreenshotPreview = null;
        }

        if (LatestVideoPath is not null)
        {
            LatestVideoPath = null;
        }
    }

    private void ClearLatestScreenshotState()
    {
        latestScreenshotFilePath = null;
        latestScreenshotUpdatedAtUtc = default;
        LatestScreenshotPreview = null;
        LatestScreenshotCaption = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainLatestScreenshotNone);
    }

    private void ClearLatestVideoState()
    {
        latestVideoFilePath = null;
        latestVideoUpdatedAtUtc = default;
        LatestVideoPath = null;
        LatestVideoCaption = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainLatestVideoNone);
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

        if (ShouldSuppressScreenshotHotkeyAfterVideoStop())
        {
            ResetScreenshotHotkeySequence();
            return;
        }

        if (TryHandleActiveScreenshotAreaSelectionHotkey())
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

        var savedVideoPath = await screenshotCaptureService.StopVideoCaptureAsync();
        var videoWasSaved = RefreshLatestVideo(savedVideoPath) || RefreshLatestVideoFromCaptureService();
        ScreenshotStatusMessage = videoWasSaved
            ? F(AppLanguageKeys.MainScreenshotStatusSavedVideoFormat, LatestVideoCaption)
            : L(AppLanguageKeys.ScreenshotStatusRecordingStopped);
        AddScreenshotLog(ScreenshotStatusMessage);
        suppressScreenshotHotkeyUntilUtc = DateTime.UtcNow + ScreenshotHotkeyStopSuppressionWindow;
        return true;
    }

    private bool ShouldSuppressScreenshotHotkeyAfterVideoStop()
    {
        if (suppressScreenshotHotkeyUntilUtc <= DateTime.UtcNow)
        {
            return false;
        }

        AppLog.Info("Screenshot hotkey ignored because a video recording just stopped.");
        return true;
    }

    private async Task QueueScreenshotHotkeySequenceAsync()
    {
        CancellationTokenSource sequenceCancellationTokenSource;
        bool shouldExecuteImmediately;

        lock (screenshotHotkeySequenceSync)
        {
            pendingScreenshotHotkeyPressCount = Math.Min(pendingScreenshotHotkeyPressCount + 1, 4);

            pendingScreenshotHotkeySequenceCancellationTokenSource?.Cancel();
            pendingScreenshotHotkeySequenceCancellationTokenSource?.Dispose();
            pendingScreenshotHotkeySequenceCancellationTokenSource = new CancellationTokenSource();
            sequenceCancellationTokenSource = pendingScreenshotHotkeySequenceCancellationTokenSource;
            shouldExecuteImmediately = pendingScreenshotHotkeyPressCount >= 4;
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
            case 3:
                await StartVideoCaptureWithPickerAsync();
                return;
            default:
                await StartVideoCaptureForCurrentScreenAsync();
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

    private bool TryHandleActiveScreenshotAreaSelectionHotkey()
    {
        if (activeScreenshotAreaSelectionCancellationTokenSource is null || activeScreenshotAreaSelectionMode is null)
        {
            return false;
        }

        if (activeScreenshotAreaSelectionMode == ScreenshotMode.Area)
        {
            promoteActiveAreaSelectionToVideo = true;
            AppLog.Info("Screenshot hotkey pressed while area selection is active. Promoting selection to video capture.");
        }
        else
        {
            AppLog.Info("Screenshot hotkey pressed while video area selection is active. Canceling the current selection.");
        }

        activeScreenshotAreaSelectionCancellationTokenSource.Cancel();
        return true;
    }

    private async Task StartVideoCaptureWithPickerAsync()
    {
        var selection = await SelectVideoCaptureAsync();
        if (selection is null)
        {
            return;
        }

        switch (selection.Kind)
        {
            case VideoCaptureSelectionKind.Area when selection.Area is not null:
                await StartVideoCaptureAsync(selection.Area.Value, AppLanguageKeys.MainScreenshotStatusAreaRecordingStarted);
                return;
            case VideoCaptureSelectionKind.CurrentScreen:
                await StartVideoCaptureAsync(
                    selection.Area ?? GetCurrentScreenRectangle(),
                    AppLanguageKeys.MainScreenshotStatusCurrentScreenRecordingStarted);
                return;
            case VideoCaptureSelectionKind.AllScreens:
                await StartVideoCaptureAsync(null, AppLanguageKeys.MainScreenshotStatusAllScreensRecordingStarted);
                return;
            default:
                ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusVideoCanceled);
                AddScreenshotLog(ScreenshotStatusMessage);
                return;
        }
    }

    private async Task StartVideoCaptureForCurrentScreenAsync()
        => await StartVideoCaptureAsync(GetCurrentScreenRectangle(), AppLanguageKeys.MainScreenshotStatusCurrentScreenRecordingStarted);

    private async Task StartVideoCaptureAsync(ScreenRectangle? area, string startedMessageKey)
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
            ScreenshotStatusMessage = L(startedMessageKey);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
        catch (Exception ex)
        {
            ScreenshotStatusMessage = F(AppLanguageKeys.MainScreenshotStatusFailedFormat, ex.Message);
            AddScreenshotLog(ScreenshotStatusMessage);
        }
    }

    private async Task<ScreenRectangle?> SelectScreenshotAreaAsync(ScreenshotMode requestedMode)
    {
        using var selectionCancellationTokenSource = new CancellationTokenSource();
        activeScreenshotAreaSelectionCancellationTokenSource = selectionCancellationTokenSource;
        activeScreenshotAreaSelectionMode = requestedMode;

        ScreenRectangle? area = null;
        var shouldPromoteToVideo = false;

        try
        {
            AppLog.Info($"Area selection requested from MainWindowViewModel. Mode={requestedMode}.");
            area = await screenshotAreaSelectionService.SelectAreaAsync(selectionCancellationTokenSource.Token);
            if (area is null)
            {
                shouldPromoteToVideo = requestedMode == ScreenshotMode.Area && promoteActiveAreaSelectionToVideo;
            }
        }
        finally
        {
            if (ReferenceEquals(activeScreenshotAreaSelectionCancellationTokenSource, selectionCancellationTokenSource))
            {
                activeScreenshotAreaSelectionCancellationTokenSource = null;
                activeScreenshotAreaSelectionMode = null;
                promoteActiveAreaSelectionToVideo = false;
            }
        }

        if (area is null)
        {
            if (shouldPromoteToVideo)
            {
                AppLog.Info("Area selection canceled so the screenshot hotkey flow can continue with the video capture picker.");
                await StartVideoCaptureWithPickerAsync();
                return null;
            }

            AppLog.Info($"Area selection canceled before capture service call. Mode={requestedMode}.");
            ScreenshotStatusMessage = L(
                requestedMode == ScreenshotMode.Video
                    ? AppLanguageKeys.MainScreenshotStatusVideoCanceled
                    : AppLanguageKeys.MainScreenshotStatusAreaCanceled);
            AddScreenshotLog(ScreenshotStatusMessage);
            return null;
        }

        // Give the area-selection overlay a moment to fully disappear before capturing.
        await Task.Delay(120);
        AppLog.Info($"Area selection using area=({area.Value.X},{area.Value.Y},{area.Value.Width}x{area.Value.Height}) Mode={requestedMode}");
        return area;
    }

    private async Task<VideoCaptureSelection?> SelectVideoCaptureAsync()
    {
        using var selectionCancellationTokenSource = new CancellationTokenSource();
        activeScreenshotAreaSelectionCancellationTokenSource = selectionCancellationTokenSource;
        activeScreenshotAreaSelectionMode = ScreenshotMode.Video;

        VideoCaptureSelection? selection = null;

        try
        {
            AppLog.Info("Video capture selection requested from MainWindowViewModel.");
            selection = await screenshotAreaSelectionService.SelectVideoCaptureAsync(selectionCancellationTokenSource.Token);
        }
        finally
        {
            if (ReferenceEquals(activeScreenshotAreaSelectionCancellationTokenSource, selectionCancellationTokenSource))
            {
                activeScreenshotAreaSelectionCancellationTokenSource = null;
                activeScreenshotAreaSelectionMode = null;
                promoteActiveAreaSelectionToVideo = false;
            }
        }

        if (selection is null)
        {
            AppLog.Info("Video capture selection canceled before capture service call.");
            ScreenshotStatusMessage = L(AppLanguageKeys.MainScreenshotStatusVideoCanceled);
            AddScreenshotLog(ScreenshotStatusMessage);
            return null;
        }

        // Give the selection overlay a moment to fully disappear before recording starts.
        await Task.Delay(120);

        if (selection.Area is { } area)
        {
            AppLog.Info($"Video capture selection using area=({area.X},{area.Y},{area.Width}x{area.Height}) Kind={selection.Kind}");
        }
        else
        {
            AppLog.Info($"Video capture selection using mode={selection.Kind} across all screens.");
        }

        return selection;
    }

    private static ScreenRectangle GetCurrentScreenRectangle()
    {
        var currentScreen = global::System.Windows.Forms.Screen.FromPoint(global::System.Windows.Forms.Cursor.Position);
        var bounds = currentScreen.Bounds;
        return new ScreenRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
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
                        var area = await SelectScreenshotAreaAsync(ScreenshotMode.Area);
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
