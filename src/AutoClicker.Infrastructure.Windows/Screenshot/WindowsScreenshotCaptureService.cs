using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using AutoClicker.Infrastructure.Windows.Interop;

namespace AutoClicker.Infrastructure.Windows.Screenshot;

public sealed class WindowsScreenshotCaptureService : IScreenshotCaptureService, IDisposable
{
    private readonly object syncRoot = new();
    private readonly SemaphoreSlim videoLock = new(1, 1);
    private Process? videoCaptureProcess;
    private string? currentVideoPath;

    public bool IsVideoCaptureRunning
    {
        get
        {
            lock (syncRoot)
            {
                return videoCaptureProcess is { HasExited: false };
            }
        }
    }

    public Task<string> CaptureDesktopAsync(string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default)
    {
        var bounds = SystemInformation.VirtualScreen;
        return CaptureBitmapAsync(
            new Rectangle(bounds.Left, bounds.Top, bounds.Width, bounds.Height),
            outputDirectory,
            fileNamePrefix,
            cancellationToken);
    }

    public Task<string> CaptureAreaAsync(ScreenRectangle area, string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken = default) =>
        CaptureBitmapAsync(
            new Rectangle(area.X, area.Y, area.Width, area.Height),
            outputDirectory,
            fileNamePrefix,
            cancellationToken);

    public async Task StartVideoCaptureAsync(string outputDirectory, string fileNamePrefix, ScreenRectangle? area = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await videoLock.WaitAsync(cancellationToken);

        try
        {
            if (videoCaptureProcess is { HasExited: false })
            {
                throw new InvalidOperationException("Video recording is already running.");
            }

            Directory.CreateDirectory(outputDirectory);

            var safePrefix = SanitizeFileName(string.IsNullOrWhiteSpace(fileNamePrefix) ? "Screenshot" : fileNamePrefix.Trim());
            currentVideoPath = Path.Combine(outputDirectory, $"{safePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.mp4");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ResolveFfmpegExecutable(),
                    Arguments = BuildFfmpegArguments(currentVideoPath, area),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    WorkingDirectory = AppContext.BaseDirectory,
                },
                EnableRaisingEvents = true,
            };

            process.ErrorDataReceived += (_, _) => { };
            process.OutputDataReceived += (_, _) => { };

            if (!process.Start())
            {
                throw new InvalidOperationException("FFmpeg did not start.");
            }

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            lock (syncRoot)
            {
                videoCaptureProcess = process;
            }
        }
        catch (Win32Exception ex)
        {
            currentVideoPath = null;
            throw new InvalidOperationException(
                "FFmpeg was not found. Install ffmpeg or place ffmpeg.exe next to MultiTool.exe.",
                ex);
        }
        finally
        {
            videoLock.Release();
        }
    }

    public async Task<string?> StopVideoCaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await videoLock.WaitAsync(cancellationToken);

        try
        {
            Process? process;
            lock (syncRoot)
            {
                process = videoCaptureProcess;
            }

            if (process is null)
            {
                return null;
            }

            if (!process.HasExited)
            {
                try
                {
                    await process.StandardInput.WriteLineAsync("q");
                    await process.StandardInput.FlushAsync();
                }
                catch
                {
                    // If stdin is gone already, fall back to waiting / killing below.
                }

                var exited = process.WaitForExit(5000);
                if (!exited && !process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit();
                }
            }

            process.Dispose();
            lock (syncRoot)
            {
                videoCaptureProcess = null;
            }

            var savedPath = currentVideoPath;
            currentVideoPath = null;
            return savedPath;
        }
        finally
        {
            videoLock.Release();
        }
    }

    public void Dispose()
    {
        Process? process;
        lock (syncRoot)
        {
            process = videoCaptureProcess;
            videoCaptureProcess = null;
        }

        if (process is { HasExited: false })
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(1000);
            }
            catch
            {
            }
        }

        process?.Dispose();
        videoLock.Dispose();
    }

    private static Task<string> CaptureBitmapAsync(Rectangle bounds, string outputDirectory, string fileNamePrefix, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(outputDirectory);

        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

        var safePrefix = SanitizeFileName(string.IsNullOrWhiteSpace(fileNamePrefix) ? "Screenshot" : fileNamePrefix.Trim());
        var fileName = $"{safePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.png";
        var filePath = Path.Combine(outputDirectory, fileName);

        bitmap.Save(filePath, ImageFormat.Png);
        CopyToClipboard(bitmap);
        return Task.FromResult(filePath);
    }

    private static string ResolveFfmpegExecutable()
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        return File.Exists(bundled) ? bundled : "ffmpeg";
    }

    private static string BuildFfmpegArguments(string outputPath, ScreenRectangle? area)
    {
        if (area is { Width: > 1, Height: > 1 } selectedArea)
        {
            return $"-y -f gdigrab -framerate 30 -offset_x {selectedArea.X} -offset_y {selectedArea.Y} -video_size {selectedArea.Width}x{selectedArea.Height} -draw_mouse 1 -i desktop -c:v libx264 -preset ultrafast -pix_fmt yuv420p \"{outputPath}\"";
        }

        return $"-y -f gdigrab -framerate 30 -draw_mouse 1 -i desktop -c:v libx264 -preset ultrafast -pix_fmt yuv420p \"{outputPath}\"";
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
        }

        return builder.ToString();
    }

    private static void CopyToClipboard(Bitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();

        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                handle,
                nint.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();

            System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Clipboard.SetImage(source));
        }
        finally
        {
            Gdi32.DeleteObject(handle);
        }
    }
}
