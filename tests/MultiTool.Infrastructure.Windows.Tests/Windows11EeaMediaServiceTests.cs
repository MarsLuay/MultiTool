using System.Diagnostics;
using MultiTool.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class Windows11EeaMediaServiceTests : IDisposable
{
    private readonly string rootPath;
    private readonly string workspaceDirectory;
    private readonly string usbRootPath;

    public Windows11EeaMediaServiceTests()
    {
        rootPath = Path.Combine(Path.GetTempPath(), "MultiTool.Tests", "windows11-eea", Guid.NewGuid().ToString("N"));
        workspaceDirectory = Path.Combine(rootPath, "Workspace");
        usbRootPath = Path.Combine(rootPath, "UsbDrive");
        Directory.CreateDirectory(workspaceDirectory);
        Directory.CreateDirectory(usbRootPath);
    }

    [Fact]
    public async Task PrepareAsync_ShouldDownloadToolWriteFilesAndLaunchPrepFlow()
    {
        var startedProcesses = new List<ProcessStartInfo>();

        using var service = new Windows11EeaMediaService(
            mediaCreationToolDownloader: (_, destinationPath, _) =>
            {
                File.WriteAllText(destinationPath, "tool");
                return Task.CompletedTask;
            },
            processStarter: startInfo => startedProcesses.Add(startInfo),
            removableDriveRootProvider: static () => [],
            workspaceDirectory: workspaceDirectory,
            usbWatchTimeout: TimeSpan.FromMilliseconds(25),
            usbWatchPollInterval: TimeSpan.FromMilliseconds(5));

        var result = await service.PrepareAsync();

        result.Succeeded.Should().BeTrue();
        result.WorkspacePath.Should().Be(workspaceDirectory);
        File.Exists(Path.Combine(workspaceDirectory, "MediaCreationToolW11.exe")).Should().BeTrue();
        File.ReadAllText(Path.Combine(workspaceDirectory, "autounattend.xml")).Should().Contain("<UserLocale>en-IE</UserLocale>");
        File.ReadAllText(Path.Combine(workspaceDirectory, "README.txt")).Should().Contain("English International");
        startedProcesses.Should().ContainSingle(startInfo => startInfo.FileName == workspaceDirectory);
        startedProcesses.Should().ContainSingle(startInfo => startInfo.FileName == Path.Combine(workspaceDirectory, "MediaCreationToolW11.exe"));
    }

    [Fact]
    public async Task PrepareAsync_ShouldReuseCachedToolWhenFreshDownloadFails()
    {
        var cachedToolPath = Path.Combine(workspaceDirectory, "MediaCreationToolW11.exe");
        File.WriteAllText(cachedToolPath, "cached-tool");
        var startedProcesses = new List<ProcessStartInfo>();

        using var service = new Windows11EeaMediaService(
            mediaCreationToolDownloader: (_, _, _) => throw new HttpRequestException("Download blocked"),
            processStarter: startInfo => startedProcesses.Add(startInfo),
            removableDriveRootProvider: static () => [],
            workspaceDirectory: workspaceDirectory,
            usbWatchTimeout: TimeSpan.FromMilliseconds(25),
            usbWatchPollInterval: TimeSpan.FromMilliseconds(5));

        var result = await service.PrepareAsync();

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Contain("Reused the cached Media Creation Tool");
        startedProcesses.Should().ContainSingle(startInfo => startInfo.FileName == cachedToolPath);
    }

    [Fact]
    public async Task PrepareAsync_ShouldCopyAnswerFileToDetectedInstallerUsb()
    {
        File.WriteAllText(Path.Combine(usbRootPath, "setup.exe"), string.Empty);
        Directory.CreateDirectory(Path.Combine(usbRootPath, "sources"));
        Directory.CreateDirectory(Path.Combine(usbRootPath, "boot"));
        Directory.CreateDirectory(Path.Combine(usbRootPath, "efi"));
        var statusMessages = new List<string>();

        using var service = new Windows11EeaMediaService(
            mediaCreationToolDownloader: (_, destinationPath, _) =>
            {
                File.WriteAllText(destinationPath, "tool");
                return Task.CompletedTask;
            },
            processStarter: _ => { },
            removableDriveRootProvider: () => [usbRootPath],
            workspaceDirectory: workspaceDirectory,
            usbWatchTimeout: TimeSpan.FromSeconds(1),
            usbWatchPollInterval: TimeSpan.FromMilliseconds(10));

        service.StatusChanged += (_, message) =>
        {
            lock (statusMessages)
            {
                statusMessages.Add(message);
            }
        };

        await service.PrepareAsync();
        await WaitForFileAsync(Path.Combine(usbRootPath, "autounattend.xml"));

        File.ReadAllText(Path.Combine(usbRootPath, "autounattend.xml")).Should().Contain("<UserLocale>en-IE</UserLocale>");
        statusMessages.Should().Contain(message => message.Contains("Copied autounattend.xml", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
        catch
        {
        }
    }

    private static async Task WaitForFileAsync(string path)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path))
            {
                return;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException($"Timed out waiting for {path}.");
    }
}
