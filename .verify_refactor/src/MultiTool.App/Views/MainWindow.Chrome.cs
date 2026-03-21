using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using MultiTool.App.Localization;
using MultiTool.App.Services;
using MultiTool.App.ViewModels;
using MultiTool.Core.Results;
using MultiTool.Core.Services;
using System.Runtime.InteropServices;

namespace MultiTool.App.Views;

public partial class MainWindow : Window
{
    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        if (!IsLoaded || isTransitioningToTray || WindowState != WindowState.Minimized)
        {
            return;
        }

        HideToTray();
        viewModel.SetStatus(AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.TrayMinimizedStatus));
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ClearPendingTitleBarDrag(sender as UIElement);
            ToggleMaximizeRestore();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            if (WindowState == WindowState.Maximized)
            {
                pendingMaximizedTitleBarDragPoint = e.GetPosition(this);
                (sender as UIElement)?.CaptureMouse();
                return;
            }

            DragMove();
        }
    }

    private void TitleBar_OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (WindowState != WindowState.Maximized
            || pendingMaximizedTitleBarDragPoint is null
            || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentPoint = e.GetPosition(this);
        var dragStartPoint = pendingMaximizedTitleBarDragPoint.Value;
        if (Math.Abs(currentPoint.X - dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(currentPoint.Y - dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        RestoreFromMaximizedForDrag(sender as FrameworkElement, dragStartPoint);
        ClearPendingTitleBarDrag(sender as UIElement);

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }

        e.Handled = true;
    }

    private void TitleBar_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearPendingTitleBarDrag(sender as UIElement);
    }

    private void TitleBar_OnLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ClearPendingTitleBarDrag(sender as UIElement, releaseCapture: false);
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void HideToTray()
    {
        isTransitioningToTray = true;

        try
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Hide();
        }
        finally
        {
            isTransitioningToTray = false;
        }
    }

    private void ShowFromTray()
    {
        isTransitioningToTray = true;

        try
        {
            if (!IsVisible)
            {
                Show();
            }

            if (WindowState != WindowState.Normal)
            {
                WindowState = WindowState.Normal;
            }

            Show();

            var handle = new WindowInteropHelper(this).EnsureHandle();
            if (handle != nint.Zero)
            {
                _ = NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);
                _ = NativeMethods.BringWindowToTop(handle);
                _ = NativeMethods.SetForegroundWindow(handle);
            }

            Activate();
            Focus();
        }
        finally
        {
            isTransitioningToTray = false;
        }
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void RestoreFromMaximizedForDrag(FrameworkElement? titleBar, System.Windows.Point dragStartPoint)
    {
        var restoreBounds = RestoreBounds;
        if (restoreBounds.Width <= 0 || restoreBounds.Height <= 0)
        {
            WindowState = WindowState.Normal;
            return;
        }

        var screenPoint = PointToScreen(dragStartPoint);
        var restorePoint = ConvertFromDevicePoint(screenPoint);
        var horizontalRatio = ActualWidth <= 0
            ? 0.5d
            : Clamp(dragStartPoint.X / ActualWidth, 0d, 1d);
        var restoredLeft = restorePoint.X - (restoreBounds.Width * horizontalRatio);
        var titleBarHeight = titleBar is { ActualHeight: > 0 } ? titleBar.ActualHeight : DefaultTitleBarHeight;
        var restoredTop = restorePoint.Y - Math.Min(dragStartPoint.Y, titleBarHeight);

        WindowState = WindowState.Normal;
        Left = Clamp(
            restoredLeft,
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
        Top = Clamp(
            restoredTop,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - titleBarHeight);
    }

    private System.Windows.Point ConvertFromDevicePoint(System.Windows.Point devicePoint)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
        {
            return devicePoint;
        }

        return source.CompositionTarget.TransformFromDevice.Transform(devicePoint);
    }

    private void ClearPendingTitleBarDrag(UIElement? titleBar, bool releaseCapture = true)
    {
        pendingMaximizedTitleBarDragPoint = null;
        if (releaseCapture && titleBar?.IsMouseCaptured == true)
        {
            titleBar.ReleaseMouseCapture();
        }
    }

    private static class NativeMethods
    {
        internal const int SwRestore = 9;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BringWindowToTop(nint hWnd);
    }


}
