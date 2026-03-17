using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AutoClicker.App.Localization;
using AutoClicker.App.Services;
using AutoClicker.App.ViewModels;
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
    private MainWindowViewModel? mainWindowViewModel;

    private bool IsCatTranslatorEnabled =>
        System.Windows.Application.Current?.MainWindow?.DataContext is MainWindowViewModel viewModel
        && viewModel.IsSillyModeEnabled;

    private AppLanguage CurrentLanguage => IsCatTranslatorEnabled ? AppLanguage.CatSpeak : AppLanguage.English;

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
        Closed += ScreenshotOptionsWindow_Closed;
    }

    public ScreenshotMode? SelectedMode { get; private set; }

    public bool WasHandledInDialog { get; private set; }

    private bool IsVideoModeArmed => VideoRecordingCheckBox.IsChecked == true;

    private void ScreenshotOptionsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AttachMainWindowViewModel();
        ApplyLocalizedText();
        Keyboard.Focus(FullScreenButton);
        RefreshVideoUi();
    }

    private void ScreenshotOptionsWindow_Closed(object? sender, EventArgs e)
    {
        if (mainWindowViewModel is not null)
        {
            mainWindowViewModel.PropertyChanged -= MainWindowViewModel_PropertyChanged;
            mainWindowViewModel = null;
        }
    }

    private void AttachMainWindowViewModel()
    {
        if (mainWindowViewModel is not null)
        {
            return;
        }

        if (System.Windows.Application.Current?.MainWindow?.DataContext is MainWindowViewModel vm)
        {
            mainWindowViewModel = vm;
            mainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
        }
    }

    private void MainWindowViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.IsSillyModeEnabled))
        {
            return;
        }

        ApplyLocalizedText();
        RefreshVideoUi();
    }

    public async Task<bool> HandleCaptureHotkeyAsync()
    {
        if (!IsVisible)
        {
            return false;
        }

        Activate();

        if (screenshotCaptureService.IsVideoCaptureRunning)
        {
            await StopRecordingAndCloseAsync();
            return true;
        }

        if (!IsVideoModeArmed)
        {
            VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusOptionsOpen);
            return true;
        }

        if (videoTargetMode is null)
        {
            VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusChooseModeFirst);
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
            VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusRecordingStarted);
        }
        catch (Exception ex)
        {
            VideoStatusTextBlock.Text = LF(AppLanguageKeys.ScreenshotStatusUnableToStart, ex.Message);
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
            VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusVideoTargetFullScreen);
            RefreshVideoUi();
            return;
        }

        SelectedMode = ScreenshotMode.FullScreen;
        CloseWithDialogResult(true);
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
                    VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusAreaSelectionCanceled);
                    return;
                }

                videoTargetMode = ScreenshotMode.Area;
                videoTargetArea = area.Value;
                VideoStatusTextBlock.Text = LF(AppLanguageKeys.ScreenshotStatusAreaSelected, area.Value.Width, area.Value.Height);
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
        CloseWithDialogResult(true);
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
        VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusVideoModeArmed);
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
            VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusStopRecordingFirst);
            return;
        }

        videoTargetMode = null;
        videoTargetArea = null;
        RefreshVideoUi();
        VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusVideoModeOff);
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (screenshotCaptureService.IsVideoCaptureRunning)
        {
            VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusStillRecordingSaveFirst);
            return;
        }

        CloseWithDialogResult(false);
    }

    private void ScreenshotOptionsWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (screenshotCaptureService.IsVideoCaptureRunning)
        {
            e.Cancel = true;
            VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusStillRecordingSaveFirst);
        }
    }

    private async Task StopRecordingAndCloseAsync()
    {
        try
        {
            var filePath = await screenshotCaptureService.StopVideoCaptureAsync();
            WasHandledInDialog = true;
            VideoStatusTextBlock.Text = string.IsNullOrWhiteSpace(filePath)
                ? L(AppLanguageKeys.ScreenshotStatusRecordingStopped)
                : LF(AppLanguageKeys.ScreenshotStatusSavedVideoTo, Path.GetFileName(filePath));
            CloseWithDialogResult(false);
        }
        catch (Exception ex)
        {
            VideoStatusTextBlock.Text = LF(AppLanguageKeys.ScreenshotStatusUnableToStop, ex.Message);
            RefreshVideoUi();
        }
    }

    private void CloseWithDialogResult(bool result)
    {
        // Hotkey and button events can race with window lifecycle; avoid throwing if WPF no longer treats this as modal.
        try
        {
            DialogResult = result;
            return;
        }
        catch (InvalidOperationException)
        {
        }

        Close();
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
        CancelButton.Content = isRecording
            ? L(AppLanguageKeys.ScreenshotWaiting)
            : L(AppLanguageKeys.ScreenshotCancel);

        if (isRecording)
        {
            VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusRecordingStarted);
            return;
        }

        if (!isArmed)
        {
            VideoStatusTextBlock.Text = L(AppLanguageKeys.ScreenshotStatusVideoOff);
            return;
        }

        VideoStatusTextBlock.Text = videoTargetMode switch
        {
            ScreenshotMode.FullScreen => L(AppLanguageKeys.ScreenshotStatusVideoTargetFullScreen),
            ScreenshotMode.Area when videoTargetArea is not null => LF(AppLanguageKeys.ScreenshotStatusAreaSelected, videoTargetArea.Value.Width, videoTargetArea.Value.Height),
            _ => L(AppLanguageKeys.ScreenshotStatusVideoModeArmed),
        };
    }

    private void ApplyLocalizedText()
    {
        Title = L(AppLanguageKeys.ScreenshotOptionsTitle);

        if (FullScreenButton.Content is System.Windows.Controls.StackPanel fullScreenPanel
            && fullScreenPanel.Children.Count >= 2
            && fullScreenPanel.Children[0] is System.Windows.Controls.TextBlock fullScreenHeader
            && fullScreenPanel.Children[1] is System.Windows.Controls.TextBlock fullScreenDescription)
        {
            fullScreenHeader.Text = L(AppLanguageKeys.ScreenshotFullScreenHeader);
            fullScreenDescription.Text = L(AppLanguageKeys.ScreenshotFullScreenDescription);
        }

        if (AreaButton.Content is System.Windows.Controls.StackPanel areaPanel
            && areaPanel.Children.Count >= 2
            && areaPanel.Children[0] is System.Windows.Controls.TextBlock areaHeader
            && areaPanel.Children[1] is System.Windows.Controls.TextBlock areaDescription)
        {
            areaHeader.Text = L(AppLanguageKeys.ScreenshotAreaHeader);
            areaDescription.Text = L(AppLanguageKeys.ScreenshotAreaDescription);
        }

        VideoRecordingCheckBox.Content = L(AppLanguageKeys.ScreenshotRecordVideo);
        CancelButton.Content = L(AppLanguageKeys.ScreenshotCancel);
    }

    private string L(string key) => AppLanguageStrings.Get(key, CurrentLanguage);

    private string LF(string key, params object[] args) => AppLanguageStrings.Format(key, CurrentLanguage, args);
}
