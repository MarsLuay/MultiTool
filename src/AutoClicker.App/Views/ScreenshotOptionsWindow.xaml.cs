using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AutoClicker.App.Services;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.App.Views;

public partial class ScreenshotOptionsWindow : Window
{
    private readonly IScreenshotCaptureService screenshotCaptureService;
    private readonly IScreenshotAreaSelectionService screenshotAreaSelectionService;
    private readonly ScreenshotSettings screenshotSettings;
    private bool suppressVideoToggleEvents;
    private ScreenshotMode? videoTargetMode;
    private ScreenRectangle? videoTargetArea;

    public ScreenshotOptionsWindow(
        IScreenshotCaptureService screenshotCaptureService,
        IScreenshotAreaSelectionService screenshotAreaSelectionService,
        ScreenshotSettings screenshotSettings)
    {
        InitializeComponent();
        this.screenshotCaptureService = screenshotCaptureService;
        this.screenshotAreaSelectionService = screenshotAreaSelectionService;
        this.screenshotSettings = screenshotSettings.Clone();
        Loaded += ScreenshotOptionsWindow_Loaded;
        Closing += ScreenshotOptionsWindow_Closing;
    }

    public ScreenshotMode? SelectedMode { get; private set; }

    public bool WasHandledInDialog { get; private set; }

    private bool IsVideoModeArmed => VideoRecordingCheckBox.IsChecked == true;

    private void ScreenshotOptionsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Keyboard.Focus(FullScreenButton);
        RefreshVideoUi();
    }

    public async Task<bool> HandleCaptureHotkeyAsync()
    {
        if (!IsVisible)
        {
            return false;
        }

        Activate();

        if (!IsVideoModeArmed)
        {
            VideoStatusTextBlock.Text = "Screenshot options are open. Choose Full Screen, Area, or arm Record Video first.";
            return true;
        }

        if (screenshotCaptureService.IsVideoCaptureRunning)
        {
            await StopRecordingAndCloseAsync();
            return true;
        }

        if (videoTargetMode is null)
        {
            VideoStatusTextBlock.Text = "Choose Full Screen or Area first, then press the screenshot hotkey to start recording.";
            return true;
        }

        try
        {
            await screenshotCaptureService.StartVideoCaptureAsync(
                screenshotSettings.SaveFolderPath,
                screenshotSettings.FilePrefix,
                videoTargetMode == ScreenshotMode.Area ? videoTargetArea : null);

            WasHandledInDialog = true;
            RefreshVideoUi();
            VideoStatusTextBlock.Text = "Recording started. Press the screenshot hotkey again to stop and save the video.";
        }
        catch (Exception ex)
        {
            VideoStatusTextBlock.Text = $"Unable to start video recording: {ex.Message}";
            RefreshVideoUi();
        }

        return true;
    }

    private void FullScreenButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (IsVideoModeArmed)
        {
            videoTargetMode = ScreenshotMode.FullScreen;
            videoTargetArea = null;
            VideoStatusTextBlock.Text = "Video target set to Full Screen. Press the screenshot hotkey to start recording.";
            RefreshVideoUi();
            return;
        }

        SelectedMode = ScreenshotMode.FullScreen;
        DialogResult = true;
        Close();
    }

    private void AreaButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (IsVideoModeArmed)
        {
            Hide();
            try
            {
                var area = screenshotAreaSelectionService.SelectArea();
                Show();
                Activate();

                if (area is null)
                {
                    VideoStatusTextBlock.Text = "Area selection canceled. Choose Area again or switch back to Full Screen.";
                    return;
                }

                videoTargetMode = ScreenshotMode.Area;
                videoTargetArea = area.Value;
                VideoStatusTextBlock.Text = $"Area selected ({area.Value.Width} x {area.Value.Height}). Press the screenshot hotkey to start recording.";
                RefreshVideoUi();
                return;
            }
            finally
            {
                if (!IsVisible)
                {
                    Show();
                }

                Activate();
            }
        }

        SelectedMode = ScreenshotMode.Area;
        DialogResult = true;
        Close();
    }

    private void VideoRecordingCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        if (suppressVideoToggleEvents)
        {
            return;
        }

        videoTargetMode = null;
        videoTargetArea = null;
        RefreshVideoUi();
        VideoStatusTextBlock.Text = "Video mode armed. Choose Full Screen or Area, then press the screenshot hotkey to start recording.";
    }

    private void VideoRecordingCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (suppressVideoToggleEvents)
        {
            return;
        }

        if (screenshotCaptureService.IsVideoCaptureRunning)
        {
            suppressVideoToggleEvents = true;
            VideoRecordingCheckBox.IsChecked = true;
            suppressVideoToggleEvents = false;
            VideoStatusTextBlock.Text = "Press the screenshot hotkey again to stop recording first.";
            return;
        }

        videoTargetMode = null;
        videoTargetArea = null;
        RefreshVideoUi();
        VideoStatusTextBlock.Text = "Video mode turned off.";
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (screenshotCaptureService.IsVideoCaptureRunning)
        {
            VideoStatusTextBlock.Text = "Recording is still running. Press the screenshot hotkey again to stop and save it first.";
            return;
        }

        DialogResult = false;
        Close();
    }

    private void ScreenshotOptionsWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (screenshotCaptureService.IsVideoCaptureRunning)
        {
            e.Cancel = true;
            VideoStatusTextBlock.Text = "Recording is still running. Press the screenshot hotkey again to stop and save it first.";
        }
    }

    private async Task StopRecordingAndCloseAsync()
    {
        try
        {
            var filePath = await screenshotCaptureService.StopVideoCaptureAsync();
            WasHandledInDialog = true;
            VideoStatusTextBlock.Text = string.IsNullOrWhiteSpace(filePath)
                ? "Video recording stopped."
                : $"Saved video to {Path.GetFileName(filePath)}.";
            DialogResult = false;
            Close();
        }
        catch (Exception ex)
        {
            VideoStatusTextBlock.Text = $"Unable to stop video recording: {ex.Message}";
            RefreshVideoUi();
        }
    }

    private void RefreshVideoUi()
    {
        suppressVideoToggleEvents = true;
        VideoRecordingCheckBox.IsChecked = IsVideoModeArmed || screenshotCaptureService.IsVideoCaptureRunning;
        suppressVideoToggleEvents = false;

        var isRecording = screenshotCaptureService.IsVideoCaptureRunning;
        var isArmed = IsVideoModeArmed;

        FullScreenButton.IsEnabled = !isRecording;
        AreaButton.IsEnabled = !isRecording;
        CancelButton.Content = isRecording ? "Waiting..." : "Cancel";

        if (isRecording)
        {
            VideoStatusTextBlock.Text = "Recording started. Press the screenshot hotkey again to stop and save the video.";
            return;
        }

        if (!isArmed)
        {
            VideoStatusTextBlock.Text = "Video recording is off.";
            return;
        }

        VideoStatusTextBlock.Text = videoTargetMode switch
        {
            ScreenshotMode.FullScreen => "Video target set to Full Screen. Press the screenshot hotkey to start recording.",
            ScreenshotMode.Area when videoTargetArea is not null => $"Area selected ({videoTargetArea.Value.Width} x {videoTargetArea.Value.Height}). Press the screenshot hotkey to start recording.",
            _ => "Video mode armed. Choose Full Screen or Area, then press the screenshot hotkey to start recording.",
        };
    }
}
