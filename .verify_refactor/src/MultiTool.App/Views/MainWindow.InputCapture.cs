using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MultiTool.App.Views;

public partial class MainWindow : Window
{
    private void MainWindow_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        PauseAutoClickerForInAppInteraction();

        var originalSource = e.OriginalSource as DependencyObject;
        if (GetCaptureHosts().Any(host => host.ContainsCaptureElement(originalSource)))
        {
            return;
        }

        foreach (var host in GetCaptureHosts())
        {
            host.ClearCaptureState(MainRootGrid);
        }
    }

    private void MainWindow_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        PauseAutoClickerForInAppInteraction();

        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        if (!viewModel.IsCtrlWheelResizeEnabled)
        {
            return;
        }

        var stepCount = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
        if (Math.Abs(stepCount) < double.Epsilon)
        {
            return;
        }

        ApplyMainContentScaleStep(stepCount);
        e.Handled = true;
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control
            || ShouldIgnoreZoomShortcut(e.OriginalSource as DependencyObject))
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var stepCount = key switch
        {
            Key.OemPlus => 1d,
            Key.Add => 1d,
            Key.OemMinus => -1d,
            Key.Subtract => -1d,
            _ => 0d,
        };

        if (Math.Abs(stepCount) < double.Epsilon)
        {
            return;
        }

        ApplyMainContentScaleStep(stepCount);
        e.Handled = true;
    }

    private bool ShouldIgnoreZoomShortcut(DependencyObject? originalSource) =>
        GetCaptureHosts().Any(host => host.ShouldIgnoreZoomShortcut(originalSource));

    private void ApplyMainContentScaleStep(double stepCount)
    {
        var currentScale = MainContentScaleTransform.ScaleX;
        var requestedScale = currentScale * Math.Pow(UiScaleStep, stepCount);
        var targetScale = Clamp(requestedScale, MinimumUiScale, MaximumUiScale);
        if (Math.Abs(targetScale - currentScale) < 0.0001d)
        {
            return;
        }

        MainContentScaleTransform.ScaleX = targetScale;
        MainContentScaleTransform.ScaleY = targetScale;
    }

    private void PauseAutoClickerForInAppInteraction()
    {
        if (!viewModel.IsRunning)
        {
            return;
        }

        autoClickerController.SuspendFor(InAppInteractionClickerPause);
    }

    private bool ShouldSuppressHotkeyExecution()
    {
        if (viewModel.ShouldSuppressGlobalHotkeys)
        {
            return true;
        }

        return GetCaptureHosts().Any(host => host.HasCaptureInteraction);
    }

    private IEnumerable<IMainWindowCaptureHost> GetCaptureHosts()
    {
        if (ClickerTabContent is IMainWindowCaptureHost clickerHost)
        {
            yield return clickerHost;
        }

        if (ScreenshotTabContent is IMainWindowCaptureHost screenshotHost)
        {
            yield return screenshotHost;
        }

        if (MacroTabContent is IMainWindowCaptureHost macroHost)
        {
            yield return macroHost;
        }

        if (ToolsTabContent is IMainWindowCaptureHost toolsHost)
        {
            yield return toolsHost;
        }
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(maximum, Math.Max(minimum, value));
}
