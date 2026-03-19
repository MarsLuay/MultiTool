using System.Windows;
using System.Windows.Controls;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.App.Services;
using MultiTool.Core.Models;

namespace MultiTool.App.Views;

public enum ScreenshotAreaSelectionWindowMode
{
    AreaCapture,
    VideoCapture,
}

public partial class ScreenshotAreaSelectionWindow : Window
{
    private readonly ScreenshotAreaSelectionWindowMode mode;
    private System.Windows.Point? dragStart;
    private global::System.Drawing.Point? dragStartScreen;
    private global::System.Drawing.Point? dragCurrentScreen;
    private bool isAreaSelectionEnabled;

    public ScreenshotAreaSelectionWindow(ScreenshotAreaSelectionWindowMode mode)
    {
        InitializeComponent();
        this.mode = mode;

        ConfigureMode();

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;

        AppLog.Info($"AreaSelectionWindow opened. Mode={mode} WindowBounds=({Left},{Top},{Width}x{Height}) VirtualScreen=({SystemParameters.VirtualScreenLeft},{SystemParameters.VirtualScreenTop},{SystemParameters.VirtualScreenWidth}x{SystemParameters.VirtualScreenHeight}) Monitors={DescribeMonitors()}");
    }

    public ScreenRectangle? SelectedArea { get; private set; }

    public VideoCaptureSelection? SelectedVideoCapture { get; private set; }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (!isAreaSelectionEnabled || IsDescendantOf(e.OriginalSource as DependencyObject, VideoOptionsPanel))
        {
            return;
        }

        dragStart = e.GetPosition(this);
        dragStartScreen = global::System.Windows.Forms.Cursor.Position;
        dragCurrentScreen = dragStartScreen;
        AppLog.Info($"AreaSelection drag start. WindowPoint=({dragStart.Value.X:F2},{dragStart.Value.Y:F2}) ScreenPoint=({dragStartScreen.Value.X},{dragStartScreen.Value.Y})");
        UpdateSelection(dragStart.Value, dragStart.Value);
        SelectionBorder.Visibility = Visibility.Visible;
        CaptureMouse();
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!isAreaSelectionEnabled || dragStart is null)
        {
            return;
        }

        dragCurrentScreen = global::System.Windows.Forms.Cursor.Position;
        UpdateSelection(dragStart.Value, e.GetPosition(this));
    }

    protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (!isAreaSelectionEnabled || dragStart is null)
        {
            return;
        }

        var end = e.GetPosition(this);
        ReleaseMouseCapture();
        var endScreen = dragCurrentScreen ?? global::System.Windows.Forms.Cursor.Position;
        AppLog.Info($"AreaSelection drag end. WindowPoint=({end.X:F2},{end.Y:F2}) ScreenPoint=({endScreen.X},{endScreen.Y})");
        SelectedArea = dragStartScreen is null
            ? null
            : BuildScreenRectangle(dragStartScreen.Value, endScreen);
        dragStart = null;
        dragStartScreen = null;
        dragCurrentScreen = null;

        if (SelectedArea is { Width: > 1, Height: > 1 })
        {
            AppLog.Info($"AreaSelection accepted. SelectedArea=({SelectedArea.Value.X},{SelectedArea.Value.Y},{SelectedArea.Value.Width}x{SelectedArea.Value.Height})");
            if (mode == ScreenshotAreaSelectionWindowMode.VideoCapture)
            {
                SelectedVideoCapture = new VideoCaptureSelection(VideoCaptureSelectionKind.Area, SelectedArea);
            }

            CompleteSelection();
            return;
        }

        AppLog.Info("AreaSelection rejected (too small). Returning null area.");
        SelectionBorder.Visibility = Visibility.Collapsed;
        SelectedArea = null;
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key != System.Windows.Input.Key.Escape)
        {
            return;
        }

        CancelSelection("AreaSelection canceled via Escape key.");
    }

    public void CancelSelection(string reason)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => CancelSelection(reason));
            return;
        }

        if (!IsVisible)
        {
            return;
        }

        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        dragStart = null;
        dragStartScreen = null;
        dragCurrentScreen = null;
        SelectionBorder.Visibility = Visibility.Collapsed;
        SelectedArea = null;
        SelectedVideoCapture = null;
        AppLog.Info(reason);
        Opacity = 0;
        DialogResult = false;
        Close();
    }

    private void VideoChooseAreaButton_OnClick(object sender, RoutedEventArgs e)
    {
        EnableAreaSelectionForVideo();
        e.Handled = true;
    }

    private void VideoCurrentScreenButton_OnClick(object sender, RoutedEventArgs e)
    {
        var screenArea = GetCurrentScreenRectangle();
        SelectedArea = screenArea;
        SelectedVideoCapture = new VideoCaptureSelection(VideoCaptureSelectionKind.CurrentScreen, screenArea);
        AppLog.Info($"Video capture selection accepted. Kind={VideoCaptureSelectionKind.CurrentScreen} Area=({screenArea.X},{screenArea.Y},{screenArea.Width}x{screenArea.Height})");
        CompleteSelection();
        e.Handled = true;
    }

    private void VideoAllScreensButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedArea = null;
        SelectedVideoCapture = new VideoCaptureSelection(VideoCaptureSelectionKind.AllScreens, null);
        AppLog.Info("Video capture selection accepted. Kind=AllScreens Area=FullDesktop");
        CompleteSelection();
        e.Handled = true;
    }

    private void ConfigureMode()
    {
        switch (mode)
        {
            case ScreenshotAreaSelectionWindowMode.AreaCapture:
                Cursor = System.Windows.Input.Cursors.Cross;
                isAreaSelectionEnabled = true;
                AreaInstructionTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AreaSelectionInstruction);
                AreaInstructionHintTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AreaSelectionEscHint);
                VideoOptionsPanel.Visibility = Visibility.Collapsed;
                break;
            case ScreenshotAreaSelectionWindowMode.VideoCapture:
                Cursor = System.Windows.Input.Cursors.Arrow;
                isAreaSelectionEnabled = false;
                AreaInstructionTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.VideoSelectionInstruction);
                AreaInstructionHintTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.VideoSelectionHint);
                VideoChooseAreaButton.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.VideoSelectionChooseAreaButton);
                VideoCurrentScreenButton.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.VideoSelectionCurrentScreenButton);
                VideoAllScreensButton.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.VideoSelectionAllScreensButton);
                VideoOptionsPanel.Visibility = Visibility.Visible;
                break;
            default:
                throw new NotSupportedException($"Screenshot area selection mode {mode} is not supported.");
        }
    }

    private void EnableAreaSelectionForVideo()
    {
        isAreaSelectionEnabled = true;
        Cursor = System.Windows.Input.Cursors.Cross;
        VideoOptionsPanel.Visibility = Visibility.Collapsed;
        AreaInstructionTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AreaSelectionInstruction);
        AreaInstructionHintTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AreaSelectionEscHint);
        AppLog.Info("Video capture picker switched to area selection mode.");
    }

    private void CompleteSelection()
    {
        // Hide immediately so the translucent overlay does not leak into the next capture frame.
        Opacity = 0;
        DialogResult = true;
        Close();
    }

    private void UpdateSelection(System.Windows.Point start, System.Windows.Point end)
    {
        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);
        SelectionBorder.Width = width;
        SelectionBorder.Height = height;
    }

    private static ScreenRectangle BuildScreenRectangle(global::System.Drawing.Point start, global::System.Drawing.Point end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        return new ScreenRectangle(
            left,
            top,
            width,
            height);
    }

    private static ScreenRectangle GetCurrentScreenRectangle()
    {
        var currentScreen = global::System.Windows.Forms.Screen.FromPoint(global::System.Windows.Forms.Cursor.Position);
        var bounds = currentScreen.Bounds;
        return new ScreenRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    private static bool IsDescendantOf(DependencyObject? originalSource, DependencyObject ancestor)
    {
        while (originalSource is not null)
        {
            if (ReferenceEquals(originalSource, ancestor))
            {
                return true;
            }

            originalSource = System.Windows.Media.VisualTreeHelper.GetParent(originalSource);
        }

        return false;
    }

    private static string DescribeMonitors()
    {
        var parts = new List<string>();
        foreach (var screen in global::System.Windows.Forms.Screen.AllScreens)
        {
            var bounds = screen.Bounds;
            parts.Add($"{screen.DeviceName}:{bounds.X},{bounds.Y},{bounds.Width}x{bounds.Height}{(screen.Primary ? ":Primary" : string.Empty)}");
        }

        return string.Join(" | ", parts);
    }
}
