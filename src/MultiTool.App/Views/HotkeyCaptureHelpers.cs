using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using UserControl = System.Windows.Controls.UserControl;

namespace MultiTool.App.Views;

internal static class HotkeyCaptureHelpers
{
    public static bool IsModifierKey(Key key) =>
        key is Key.LeftShift
            or Key.RightShift
            or Key.LeftCtrl
            or Key.RightCtrl
            or Key.LeftAlt
            or Key.RightAlt
            or Key.LWin
            or Key.RWin;

    public static void ArmCaptureBox(ToggleButton button)
    {
        button.IsChecked = true;
    }

    public static void DisarmCaptureBox(ToggleButton button)
    {
        button.IsChecked = false;
    }

    public static void FocusFallback(UIElement fallbackFocusTarget)
    {
        fallbackFocusTarget.Focus();
        Keyboard.Focus(fallbackFocusTarget);
    }

    public static UIElement ResolveFallbackFocusTarget(UserControl control) =>
        Window.GetWindow(control) is MainWindow mainWindow
            ? mainWindow.MainRootGrid
            : control;

    public static bool IsDescendantOf(DependencyObject? source, DependencyObject? target)
    {
        if (target is null)
        {
            return false;
        }

        while (source is not null)
        {
            if (ReferenceEquals(source, target))
            {
                return true;
            }

            source = source switch
            {
                Visual or Visual3D => VisualTreeHelper.GetParent(source),
                _ => LogicalTreeHelper.GetParent(source),
            };
        }

        return false;
    }
}
