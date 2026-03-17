using AutoClicker.App.Models;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using AutoClicker.App.Views;

namespace AutoClicker.App.Services;

public sealed class ScreenshotOptionsDialogService : IScreenshotOptionsDialogService
{
    private readonly IThemeService themeService;
    private readonly IScreenshotCaptureService screenshotCaptureService;
    private readonly IScreenshotAreaSelectionService screenshotAreaSelectionService;
    private ScreenshotOptionsWindow? currentWindow;

    public ScreenshotOptionsDialogService(
        IThemeService themeService,
        IScreenshotCaptureService screenshotCaptureService,
        IScreenshotAreaSelectionService screenshotAreaSelectionService)
    {
        this.themeService = themeService;
        this.screenshotCaptureService = screenshotCaptureService;
        this.screenshotAreaSelectionService = screenshotAreaSelectionService;
    }

    public ScreenshotOptionsResult SelectMode(ScreenshotSettings settings)
    {
        if (currentWindow is { IsVisible: true })
        {
            if (currentWindow.WindowState == System.Windows.WindowState.Minimized)
            {
                currentWindow.WindowState = System.Windows.WindowState.Normal;
            }

            currentWindow.Activate();
            currentWindow.Topmost = true;
            currentWindow.Topmost = false;
            currentWindow.Focus();
            return ScreenshotOptionsResult.IgnoredBecauseAlreadyOpen;
        }

        var window = new ScreenshotOptionsWindow(screenshotCaptureService, screenshotAreaSelectionService, settings);
        currentWindow = window;
        if (System.Windows.Application.Current?.MainWindow is System.Windows.Window owner)
        {
            window.Owner = owner;
        }

        window.SourceInitialized += (_, _) => themeService.ApplyThemeToWindow(window);
        window.ContentRendered += (_, _) => themeService.ApplyThemeToWindow(window);
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(currentWindow, window))
            {
                currentWindow = null;
            }
        };

        _ = window.ShowDialog();
        return window.SelectedMode is ScreenshotMode mode
            ? new ScreenshotOptionsResult(mode)
            : window.WasHandledInDialog
                ? ScreenshotOptionsResult.HandledInDialog
                : ScreenshotOptionsResult.Canceled;
    }

    public Task<bool> TryHandleCaptureHotkeyAsync()
    {
        if (currentWindow is { IsVisible: true })
        {
            return currentWindow.HandleCaptureHotkeyAsync();
        }

        if (!screenshotCaptureService.IsVideoCaptureRunning)
        {
            return Task.FromResult(false);
        }

        return StopVideoCaptureFromHotkeyAsync();
    }

    private async Task<bool> StopVideoCaptureFromHotkeyAsync()
    {
        await screenshotCaptureService.StopVideoCaptureAsync();
        return true;
    }
}
