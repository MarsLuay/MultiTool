using System.Windows;

namespace MultiTool.App.Views;

internal interface IMainWindowCaptureHost
{
    bool HasCaptureInteraction { get; }

    bool ContainsCaptureElement(DependencyObject? source);

    bool ShouldIgnoreZoomShortcut(DependencyObject? source);

    void ClearCaptureState(UIElement fallbackFocusTarget);
}
