using System.Windows;
using System.Windows.Controls;
using AutoClicker.Core.Models;

namespace AutoClicker.App.Views;

public partial class ScreenshotAreaSelectionWindow : Window
{
    private System.Windows.Point? dragStart;

    public ScreenshotAreaSelectionWindow()
    {
        InitializeComponent();

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;
    }

    public ScreenRectangle? SelectedArea { get; private set; }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        dragStart = e.GetPosition(this);
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
        SelectedArea = BuildScreenRectangle(dragStart.Value, end);
        dragStart = null;

        if (SelectedArea is { Width: > 1, Height: > 1 })
        {
            DialogResult = true;
            Close();
            return;
        }

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

    private ScreenRectangle BuildScreenRectangle(System.Windows.Point start, System.Windows.Point end)
    {
        var left = Math.Min(start.X, end.X) + Left;
        var top = Math.Min(start.Y, end.Y) + Top;
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);

        return new ScreenRectangle(
            (int)Math.Round(left),
            (int)Math.Round(top),
            (int)Math.Round(width),
            (int)Math.Round(height));
    }
}
