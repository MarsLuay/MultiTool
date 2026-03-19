using System.Windows;
using System.Windows.Controls;
using MultiTool.App.Localization;
using MultiTool.App.Services;
using MultiTool.Core.Models;

namespace MultiTool.App.Views;

public partial class ScreenshotAreaSelectionWindow : Window
{
    private System.Windows.Point? dragStart;
    private global::System.Drawing.Point? dragStartScreen;
    private global::System.Drawing.Point? dragCurrentScreen;

    public ScreenshotAreaSelectionWindow()
    {
        InitializeComponent();

        AreaInstructionTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AreaSelectionInstruction);
        AreaInstructionHintTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.AreaSelectionEscHint);

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;

        AppLog.Info($"AreaSelectionWindow opened. WindowBounds=({Left},{Top},{Width}x{Height}) VirtualScreen=({SystemParameters.VirtualScreenLeft},{SystemParameters.VirtualScreenTop},{SystemParameters.VirtualScreenWidth}x{SystemParameters.VirtualScreenHeight}) Monitors={DescribeMonitors()}");
    }

    public ScreenRectangle? SelectedArea { get; private set; }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
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

        if (dragStart is null)
        {
            return;
        }

        dragCurrentScreen = global::System.Windows.Forms.Cursor.Position;
        UpdateSelection(dragStart.Value, e.GetPosition(this));
    }

    protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (dragStart is null)
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
            // Hide immediately so the translucent overlay does not leak into the next screen capture frame.
            Opacity = 0;
            DialogResult = true;
            Close();
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

        AppLog.Info("AreaSelection canceled via Escape key.");
        Opacity = 0;
        DialogResult = false;
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
