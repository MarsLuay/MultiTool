using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

public sealed class Windows11EeaMediaService : IWindows11EeaMediaService, IDisposable
{
    private const string Windows11DownloadPageUrl = "https://www.microsoft.com/software-download/windows11";
    private const string MediaCreationToolUrl = "https://go.microsoft.com/fwlink/?linkid=2156295";
    private const string WorkspaceFolderName = "Windows11-EEA";
    private const string MediaCreationToolFileName = "MediaCreationToolW11.exe";
    private const string AnswerFileName = "autounattend.xml";
    private const string ReadmeFileName = "README.txt";
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();

    private readonly Func<string, string, CancellationToken, Task> mediaCreationToolDownloader;
    private readonly Action<ProcessStartInfo> processStarter;
    private readonly Func<IEnumerable<string>> removableDriveRootProvider;
    private readonly Func<TimeSpan, CancellationToken, Task> delayAsync;
    private readonly string workspaceDirectory;
    private readonly TimeSpan usbWatchTimeout;
    private readonly TimeSpan usbWatchPollInterval;
    private readonly object watcherLock = new();
    private CancellationTokenSource? usbWatchCancellationTokenSource;

    public Windows11EeaMediaService(
        Func<string, string, CancellationToken, Task>? mediaCreationToolDownloader = null,
        Action<ProcessStartInfo>? processStarter = null,
        Func<IEnumerable<string>>? removableDriveRootProvider = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        string? workspaceDirectory = null,
        TimeSpan? usbWatchTimeout = null,
        TimeSpan? usbWatchPollInterval = null)
    {
        this.mediaCreationToolDownloader = mediaCreationToolDownloader ?? DownloadFileAsync;
        this.processStarter = processStarter ?? (static startInfo => Process.Start(startInfo));
        this.removableDriveRootProvider = removableDriveRootProvider ?? GetRemovableDriveRoots;
        this.delayAsync = delayAsync ?? Task.Delay;
        this.workspaceDirectory = workspaceDirectory ?? GetDefaultWorkspaceDirectory();
        this.usbWatchTimeout = usbWatchTimeout ?? TimeSpan.FromMinutes(45);
        this.usbWatchPollInterval = usbWatchPollInterval ?? TimeSpan.FromSeconds(2);
    }

    public event EventHandler<string>? StatusChanged;

    public async Task<Windows11EeaMediaPreparationResult> PrepareAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(workspaceDirectory);

        var mediaCreationToolPath = Path.Combine(workspaceDirectory, MediaCreationToolFileName);
        var answerFilePath = Path.Combine(workspaceDirectory, AnswerFileName);
        var readmePath = Path.Combine(workspaceDirectory, ReadmeFileName);

        var hadCachedTool = File.Exists(mediaCreationToolPath);
        Exception? downloadException = null;

        try
        {
            await mediaCreationToolDownloader(MediaCreationToolUrl, mediaCreationToolPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (hadCachedTool)
        {
            downloadException = ex;
        }
        catch (Exception ex)
        {
            downloadException = ex;
        }

        await File.WriteAllTextAsync(answerFilePath, BuildAutounattendXml(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(readmePath, BuildReadme(mediaCreationToolPath, answerFilePath), Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        TryOpenFolder(workspaceDirectory);

        if (File.Exists(mediaCreationToolPath))
        {
            StartUsbWatch(answerFilePath);

            var launchException = TryLaunchMediaCreationTool(mediaCreationToolPath);
            var message = BuildSuccessMessage(workspaceDirectory, hadCachedTool, downloadException, launchException);
            return new Windows11EeaMediaPreparationResult(
                launchException is null,
                true,
                workspaceDirectory,
                mediaCreationToolPath,
                answerFilePath,
                message);
        }

        TryOpenWindows11DownloadPage();
        var fallbackMessage =
            $"MultiTool prepared the EEA setup files in {workspaceDirectory}, but it could not download Microsoft's Media Creation Tool. The official Windows 11 download page was opened instead. Download error: {downloadException?.Message ?? "Unknown error."}";

        return new Windows11EeaMediaPreparationResult(
            false,
            true,
            workspaceDirectory,
            mediaCreationToolPath,
            answerFilePath,
            fallbackMessage);
    }

    public void Dispose()
    {
        StopUsbWatch();
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MultiTool/1.0");
        client.Timeout = TimeSpan.FromMinutes(20);
        return client;
    }

    private static async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        var tempPath = destinationPath + ".download";
        DeleteIfExists(tempPath);

        using var response = await SharedHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using (var destinationStream = File.Create(tempPath))
        await using (var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            await responseStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
        }

        DeleteIfExists(destinationPath);
        File.Move(tempPath, destinationPath);
    }

    private static string GetDefaultWorkspaceDirectory()
    {
        var downloadsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        return Path.Combine(downloadsDirectory, "MultiTool", WorkspaceFolderName);
    }

    private static IEnumerable<string> GetRemovableDriveRoots()
    {
        var systemDriveRoot = Path.GetPathRoot(Environment.SystemDirectory);

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady
                || (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.Fixed)
                || string.Equals(drive.RootDirectory.FullName, systemDriveRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return drive.RootDirectory.FullName;
        }
    }

    private void StartUsbWatch(string answerFilePath)
    {
        StopUsbWatch();

        CancellationTokenSource cancellationTokenSource;
        lock (watcherLock)
        {
            cancellationTokenSource = new CancellationTokenSource();
            usbWatchCancellationTokenSource = cancellationTokenSource;
        }

        RaiseStatusChanged(
            $"Prepared the EEA answer file at {answerFilePath}. Leave MultiTool open while Media Creation Tool builds the USB and MultiTool will try to copy {AnswerFileName} to it automatically.");

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await WatchForInstallerUsbAsync(answerFilePath, cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    RaiseStatusChanged($"Windows 11 EEA USB watch failed: {ex.Message}");
                }
            },
            CancellationToken.None);
    }

    private void StopUsbWatch()
    {
        lock (watcherLock)
        {
            usbWatchCancellationTokenSource?.Cancel();
            usbWatchCancellationTokenSource?.Dispose();
            usbWatchCancellationTokenSource = null;
        }
    }

    private async Task WatchForInstallerUsbAsync(string answerFilePath, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + usbWatchTimeout;
        var answerFileBytes = await File.ReadAllBytesAsync(answerFilePath, cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            foreach (var driveRoot in removableDriveRootProvider())
            {
                if (!IsWindowsInstallerMediaRoot(driveRoot))
                {
                    continue;
                }

                var destinationPath = Path.Combine(driveRoot, AnswerFileName);

                try
                {
                    await File.WriteAllBytesAsync(destinationPath, answerFileBytes, cancellationToken).ConfigureAwait(false);
                    RaiseStatusChanged(
                        $"Copied {AnswerFileName} to {destinationPath}. Windows Setup will start with Ireland as the EEA regional default.");
                    StopUsbWatch();
                    return;
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            await delayAsync(usbWatchPollInterval, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        RaiseStatusChanged(
            $"Stopped waiting for the Windows 11 USB after {usbWatchTimeout.TotalMinutes:0} minutes. {AnswerFileName} is still ready in {workspaceDirectory} if you want to copy it manually.");
    }

    private static bool IsWindowsInstallerMediaRoot(string driveRoot)
    {
        if (string.IsNullOrWhiteSpace(driveRoot) || !Directory.Exists(driveRoot))
        {
            return false;
        }

        return File.Exists(Path.Combine(driveRoot, "setup.exe"))
            && Directory.Exists(Path.Combine(driveRoot, "sources"))
            && Directory.Exists(Path.Combine(driveRoot, "boot"))
            && Directory.Exists(Path.Combine(driveRoot, "efi"));
    }

    private Exception? TryLaunchMediaCreationTool(string mediaCreationToolPath)
    {
        try
        {
            processStarter(
                new ProcessStartInfo
                {
                    FileName = mediaCreationToolPath,
                    WorkingDirectory = Path.GetDirectoryName(mediaCreationToolPath),
                    UseShellExecute = true,
                });

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private void TryOpenFolder(string path)
    {
        try
        {
            processStarter(
                new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                });
        }
        catch
        {
        }
    }

    private void TryOpenWindows11DownloadPage()
    {
        try
        {
            processStarter(
                new ProcessStartInfo
                {
                    FileName = Windows11DownloadPageUrl,
                    UseShellExecute = true,
                });
        }
        catch
        {
        }
    }

    private string BuildSuccessMessage(
        string workspacePath,
        bool hadCachedTool,
        Exception? downloadException,
        Exception? launchException)
    {
        var builder = new StringBuilder();
        builder.Append($"Prepared the Windows 11 EEA media files in {workspacePath}.");

        if (downloadException is null)
        {
            builder.Append(" Downloaded Microsoft's latest Media Creation Tool.");
        }
        else if (hadCachedTool)
        {
            builder.Append($" Reused the cached Media Creation Tool because the latest download failed: {downloadException.Message}.");
        }

        if (launchException is null)
        {
            builder.Append(" Media Creation Tool was launched, and MultiTool is now watching for the finished USB so it can copy autounattend.xml automatically.");
        }
        else
        {
            builder.Append($" MultiTool could not launch Media Creation Tool automatically: {launchException.Message}. The prepared files are still ready in the folder that was opened.");
        }

        builder.Append(" If you want English International media, choose it in Microsoft's language step.");
        return builder.ToString();
    }

    private static string BuildAutounattendXml() =>
        """
        <?xml version="1.0" encoding="utf-8"?>
        <unattend xmlns="urn:schemas-microsoft-com:unattend">
          <settings pass="oobeSystem">
            <component name="Microsoft-Windows-International-Core" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS">
              <UserLocale>en-IE</UserLocale>
            </component>
          </settings>
        </unattend>
        """;

    private static string BuildReadme(string mediaCreationToolPath, string answerFilePath)
    {
        var builder = new StringBuilder();
        builder.AppendLine("MultiTool Windows 11 EEA Media Prep");
        builder.AppendLine($"Prepared: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();
        builder.AppendLine("Microsoft does not publish a separate Europe-only Windows 11 image.");
        builder.AppendLine("This folder keeps the official Media Creation Tool and an answer file that sets Ireland (en-IE) as the Windows Setup user locale.");
        builder.AppendLine();
        builder.AppendLine("What MultiTool already prepared");
        builder.AppendLine($"- Media Creation Tool: {mediaCreationToolPath}");
        builder.AppendLine($"- Answer file: {answerFilePath}");
        builder.AppendLine();
        builder.AppendLine("Recommended flow");
        builder.AppendLine("1. Leave MultiTool open.");
        builder.AppendLine("2. In Media Creation Tool, choose Create installation media for another PC.");
        builder.AppendLine("3. If you want English International media, choose it when Microsoft asks for the language.");
        builder.AppendLine("4. Choose USB flash drive.");
        builder.AppendLine("5. When the USB is done, MultiTool will try to copy autounattend.xml onto the USB root automatically.");
        builder.AppendLine("6. Boot from that USB and install Windows normally.");
        builder.AppendLine();
        builder.AppendLine("If you choose ISO instead of USB");
        builder.AppendLine("- Keep this folder.");
        builder.AppendLine("- Copy autounattend.xml into the root of the extracted installer media before you boot from it.");
        builder.AppendLine();
        builder.AppendLine("Official references");
        builder.AppendLine($"- Windows 11 download page: {Windows11DownloadPageUrl}");
        builder.AppendLine("- Automate OOBE: https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/automate-oobe");
        builder.AppendLine("- UserLocale: https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-international-core-userlocale");
        return builder.ToString().TrimEnd();
    }

    private void RaiseStatusChanged(string message)
    {
        StatusChanged?.Invoke(this, message);
    }

    private static void DeleteIfExists(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        File.Delete(path);
    }
}
