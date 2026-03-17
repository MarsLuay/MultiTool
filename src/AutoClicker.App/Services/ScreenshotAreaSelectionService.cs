using AutoClicker.App.Views;
using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public sealed class ScreenshotAreaSelectionService : IScreenshotAreaSelectionService
{
    public ScreenRectangle? SelectArea()
    {
        var mainWindow = System.Windows.Application.Current?.MainWindow;
        var restoreTopmost = mainWindow?.Topmost ?? false;

        AppLog.Info($"SelectArea requested. MainWindowTopmostBefore={restoreTopmost}");

        try
        {
            if (mainWindow is not null)
            {
                // A topmost main window can steal z-order and distort area selection behavior.
                mainWindow.Topmost = false;
                AppLog.Info("SelectArea temporarily disabled MainWindow.Topmost.");
            }

            var window = new ScreenshotAreaSelectionWindow();
            if (mainWindow is not null)
            {
                window.Owner = mainWindow;
            }

            var selectedArea = window.ShowDialog() == true ? window.SelectedArea : null;
            if (selectedArea is null)
            {
                AppLog.Info("SelectArea completed with null area.");
            }
            else
            {
                AppLog.Info($"SelectArea completed with area=({selectedArea.Value.X},{selectedArea.Value.Y},{selectedArea.Value.Width}x{selectedArea.Value.Height}).");
            }

            return selectedArea;
        }
        finally
        {
            if (mainWindow is not null)
            {
                mainWindow.Topmost = restoreTopmost;
                AppLog.Info($"SelectArea restored MainWindow.Topmost={restoreTopmost}");
            }
        }
    }
}
