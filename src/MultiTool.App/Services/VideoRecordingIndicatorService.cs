using System.Drawing;
using System.Windows.Forms;
using MultiTool.App.Views;
using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public sealed class VideoRecordingIndicatorService : IVideoRecordingIndicatorService
{
    private const double IndicatorMargin = 24d;
    private readonly List<VideoRecordingIndicatorWindow> windows = [];
    private bool disposed;

    public void ShowForRecordingArea(ScreenRectangle? captureArea)
    {
        ExecuteOnUiThread(
            () =>
            {
                if (disposed)
                {
                    return;
                }

                HideCore();

                var targetScreens = GetTargetScreens(captureArea);
                foreach (var screen in targetScreens)
                {
                    var window = new VideoRecordingIndicatorWindow();
                    PositionWindow(window, screen);
                    window.Show();

                    if (!window.IsCaptureExcluded)
                    {
                        window.Close();
                        continue;
                    }

                    windows.Add(window);
                }

                AppLog.Info($"Video recording indicator shown on {windows.Count} screen(s). CaptureArea={(captureArea is null ? "FullDesktop" : $"{captureArea.Value.X},{captureArea.Value.Y},{captureArea.Value.Width}x{captureArea.Value.Height}")}.");
            });
    }

    public void Hide()
    {
        ExecuteOnUiThread(HideCore);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Hide();
    }

    private void HideCore()
    {
        foreach (var window in windows)
        {
            window.Close();
        }

        windows.Clear();
    }

    private static IReadOnlyList<Screen> GetTargetScreens(ScreenRectangle? captureArea)
    {
        if (captureArea is not { Width: > 0, Height: > 0 } selectedArea)
        {
            return Screen.AllScreens;
        }

        var area = new Rectangle(selectedArea.X, selectedArea.Y, selectedArea.Width, selectedArea.Height);
        var targetScreens = Screen.AllScreens
            .Where(screen => screen.Bounds.IntersectsWith(area))
            .ToArray();

        return targetScreens.Length == 0 ? Screen.AllScreens : targetScreens;
    }

    private static void PositionWindow(VideoRecordingIndicatorWindow window, Screen screen)
    {
        var bounds = screen.WorkingArea;
        window.Left = bounds.Right - window.Width - IndicatorMargin;
        window.Top = bounds.Top + IndicatorMargin;
    }

    private static void ExecuteOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
