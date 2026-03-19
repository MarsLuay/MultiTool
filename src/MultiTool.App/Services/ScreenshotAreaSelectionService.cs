using MultiTool.App.Models;
using MultiTool.App.Views;
using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public sealed class ScreenshotAreaSelectionService : IScreenshotAreaSelectionService
{
    public Task<ScreenRectangle?> SelectAreaAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            AppLog.Info("SelectAreaAsync canceled before window creation.");
            return Task.FromResult<ScreenRectangle?>(null);
        }

        var mainWindow = System.Windows.Application.Current?.MainWindow;
        var restoreTopmost = mainWindow?.Topmost ?? false;

        AppLog.Info($"SelectAreaAsync requested. MainWindowTopmostBefore={restoreTopmost}");
        CancellationTokenRegistration cancellationRegistration = default;

        try
        {
            if (mainWindow is not null)
            {
                // A topmost main window can steal z-order and distort area selection behavior.
                mainWindow.Topmost = false;
                AppLog.Info("SelectAreaAsync temporarily disabled MainWindow.Topmost.");
            }

            var window = new ScreenshotAreaSelectionWindow(ScreenshotAreaSelectionWindowMode.AreaCapture);
            if (mainWindow is not null)
            {
                window.Owner = mainWindow;
            }

            cancellationRegistration = RegisterCancellation(cancellationToken, window);

            var selectedArea = window.ShowDialog() == true ? window.SelectedArea : null;
            if (selectedArea is null)
            {
                AppLog.Info("SelectAreaAsync completed with null area.");
            }
            else
            {
                AppLog.Info($"SelectAreaAsync completed with area=({selectedArea.Value.X},{selectedArea.Value.Y},{selectedArea.Value.Width}x{selectedArea.Value.Height}).");
            }

            return Task.FromResult(selectedArea);
        }
        finally
        {
            cancellationRegistration.Dispose();

            if (mainWindow is not null)
            {
                mainWindow.Topmost = restoreTopmost;
                AppLog.Info($"SelectAreaAsync restored MainWindow.Topmost={restoreTopmost}");
            }
        }
    }

    public Task<VideoCaptureSelection?> SelectVideoCaptureAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            AppLog.Info("SelectVideoCaptureAsync canceled before window creation.");
            return Task.FromResult<VideoCaptureSelection?>(null);
        }

        var mainWindow = System.Windows.Application.Current?.MainWindow;
        var restoreTopmost = mainWindow?.Topmost ?? false;

        AppLog.Info($"SelectVideoCaptureAsync requested. MainWindowTopmostBefore={restoreTopmost}");
        CancellationTokenRegistration cancellationRegistration = default;

        try
        {
            if (mainWindow is not null)
            {
                // A topmost main window can steal z-order and distort area/video selection behavior.
                mainWindow.Topmost = false;
                AppLog.Info("SelectVideoCaptureAsync temporarily disabled MainWindow.Topmost.");
            }

            var window = new ScreenshotAreaSelectionWindow(ScreenshotAreaSelectionWindowMode.VideoCapture);
            if (mainWindow is not null)
            {
                window.Owner = mainWindow;
            }

            cancellationRegistration = RegisterCancellation(cancellationToken, window);

            var selection = window.ShowDialog() == true ? window.SelectedVideoCapture : null;
            if (selection is null)
            {
                AppLog.Info("SelectVideoCaptureAsync completed with null selection.");
            }
            else
            {
                var areaText = selection.Area is null
                    ? "AllScreens"
                    : $"{selection.Area.Value.X},{selection.Area.Value.Y},{selection.Area.Value.Width}x{selection.Area.Value.Height}";
                AppLog.Info($"SelectVideoCaptureAsync completed with selection={selection.Kind} area={areaText}.");
            }

            return Task.FromResult(selection);
        }
        finally
        {
            cancellationRegistration.Dispose();

            if (mainWindow is not null)
            {
                mainWindow.Topmost = restoreTopmost;
                AppLog.Info($"SelectVideoCaptureAsync restored MainWindow.Topmost={restoreTopmost}");
            }
        }
    }

    private static CancellationTokenRegistration RegisterCancellation(
        CancellationToken cancellationToken,
        ScreenshotAreaSelectionWindow window) =>
        cancellationToken.Register(
            static state =>
            {
                if (state is not ScreenshotAreaSelectionWindow selectionWindow)
                {
                    return;
                }

                try
                {
                    _ = selectionWindow.Dispatcher.BeginInvoke(
                        () => selectionWindow.CancelSelection("Cancellation requested by screenshot hotkey flow."));
                }
                catch (InvalidOperationException)
                {
                }
            },
            window);
}
