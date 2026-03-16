using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using AutoClicker.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class WindowsSearchReplacementServiceTests : IDisposable
{
    private readonly string rootPath;
    private readonly string searchReplacementDirectory;
    private readonly string startupDirectory;

    public WindowsSearchReplacementServiceTests()
    {
        rootPath = Path.Combine(Path.GetTempPath(), "MultiTool.Tests", "search-replacement", Guid.NewGuid().ToString("N"));
        searchReplacementDirectory = Path.Combine(rootPath, "SearchReplacement");
        startupDirectory = Path.Combine(rootPath, "Startup");
        Directory.CreateDirectory(searchReplacementDirectory);
        Directory.CreateDirectory(startupDirectory);
    }

    [Fact]
    public void GetStatus_ShouldReportConfiguredWhenReplacementIsReady()
    {
        var flowPath = CreateFile("Flow.Launcher.exe");
        var everythingPath = CreateFile("Everything.exe");
        var autoHotkeyPath = CreateFile("AutoHotkey64.exe");
        CreateReplacementArtifacts();

        var service = new WindowsSearchReplacementService(
            new StubInstallerService(),
            () => flowPath,
            () => everythingPath,
            () => autoHotkeyPath,
            () => (true, "Disabled", false),
            (_, _, _) => Task.CompletedTask,
            searchReplacementDirectory: searchReplacementDirectory,
            startupShortcutDirectory: startupDirectory);

        var status = service.GetStatus();

        status.IsConfigured.Should().BeTrue();
        status.Message.Should().Be("Flow Launcher + Everything is replacing Win + S, and Windows Search indexing is disabled.");
    }

    [Fact]
    public async Task ApplyAsync_ShouldInstallPackagesWriteScriptAndDisableWindowsSearch()
    {
        var flowPath = Path.Combine(rootPath, "Flow.Launcher.exe");
        var everythingPath = Path.Combine(rootPath, "Everything.exe");
        var autoHotkeyPath = Path.Combine(rootPath, "AutoHotkey64.exe");
        var configuredStates = new List<(string StartupType, bool ShouldBeRunning)>();
        var startedScripts = new List<(string AutoHotkeyPath, string ScriptPath)>();
        var shortcutWrites = new List<string>();

        var installerService = new StubInstallerService
        {
            InstallPackagesAsyncHandler = (packageIds, _) =>
            {
                File.WriteAllText(flowPath, string.Empty);
                File.WriteAllText(everythingPath, string.Empty);
                File.WriteAllText(autoHotkeyPath, string.Empty);
                return Task.FromResult<IReadOnlyList<InstallerOperationResult>>(
                [
                    .. packageIds.Select(packageId => new InstallerOperationResult(packageId, packageId, true, true, "Installed.", string.Empty)),
                ]);
            },
        };

        var service = new WindowsSearchReplacementService(
            installerService,
            () => File.Exists(flowPath) ? flowPath : null,
            () => File.Exists(everythingPath) ? everythingPath : null,
            () => File.Exists(autoHotkeyPath) ? autoHotkeyPath : null,
            () => (true, "Automatic", true),
            (startupType, shouldBeRunning, _) =>
            {
                configuredStates.Add((startupType, shouldBeRunning));
                return Task.CompletedTask;
            },
            (shortcutPath, _, _, _, _) =>
            {
                File.WriteAllText(shortcutPath, "shortcut");
                shortcutWrites.Add(shortcutPath);
            },
            (resolvedAutoHotkeyPath, scriptPath) => startedScripts.Add((resolvedAutoHotkeyPath, scriptPath)),
            _ => { },
            searchReplacementDirectory,
            startupDirectory);

        var result = await service.ApplyAsync();

        result.Succeeded.Should().BeTrue();
        result.Changed.Should().BeTrue();
        File.Exists(Path.Combine(searchReplacementDirectory, "FlowLauncherSearchReplacement.ahk")).Should().BeTrue();
        File.Exists(Path.Combine(startupDirectory, "MultiTool Flow Search Replacement.lnk")).Should().BeTrue();
        File.Exists(Path.Combine(searchReplacementDirectory, "windows-search-state.json")).Should().BeTrue();
        configuredStates.Should().ContainSingle().Which.Should().Be(("Disabled", false));
        startedScripts.Should().ContainSingle();
        shortcutWrites.Should().ContainSingle();
    }

    [Fact]
    public async Task RestoreAsync_ShouldRemoveArtifactsAndRestoreWindowsSearch()
    {
        CreateReplacementArtifacts();
        await File.WriteAllTextAsync(
            Path.Combine(searchReplacementDirectory, "windows-search-state.json"),
            """
            {
              "StartupType": "Manual",
              "WasRunning": true
            }
            """);

        var configuredStates = new List<(string StartupType, bool ShouldBeRunning)>();
        var stoppedScripts = new List<string>();
        var service = new WindowsSearchReplacementService(
            new StubInstallerService(),
            () => null,
            () => null,
            () => null,
            () => (true, "Disabled", false),
            (startupType, shouldBeRunning, _) =>
            {
                configuredStates.Add((startupType, shouldBeRunning));
                return Task.CompletedTask;
            },
            (shortcutPath, _, _, _, _) => File.WriteAllText(shortcutPath, "shortcut"),
            (_, _) => { },
            scriptPath => stoppedScripts.Add(scriptPath),
            searchReplacementDirectory,
            startupDirectory);

        var result = await service.RestoreAsync();

        result.Succeeded.Should().BeTrue();
        result.Changed.Should().BeTrue();
        configuredStates.Should().ContainSingle().Which.Should().Be(("Manual", true));
        File.Exists(Path.Combine(searchReplacementDirectory, "FlowLauncherSearchReplacement.ahk")).Should().BeFalse();
        File.Exists(Path.Combine(startupDirectory, "MultiTool Flow Search Replacement.lnk")).Should().BeFalse();
        File.Exists(Path.Combine(searchReplacementDirectory, "windows-search-state.json")).Should().BeFalse();
        stoppedScripts.Should().ContainSingle();
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

    private string CreateFile(string fileName)
    {
        var path = Path.Combine(rootPath, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private void CreateReplacementArtifacts()
    {
        File.WriteAllText(Path.Combine(searchReplacementDirectory, "FlowLauncherSearchReplacement.ahk"), string.Empty);
        File.WriteAllText(Path.Combine(startupDirectory, "MultiTool Flow Search Replacement.lnk"), string.Empty);
    }

    private sealed class StubInstallerService : IInstallerService
    {
        public Func<IEnumerable<string>, CancellationToken, Task<IReadOnlyList<InstallerOperationResult>>>? InstallPackagesAsyncHandler { get; set; }

        public IReadOnlyList<InstallerCatalogItem> GetCatalog() => [];

        public IReadOnlyList<InstallerCatalogItem> GetCleanupCatalog() => [];

        public InstallerPackageCapabilities GetPackageCapabilities(string packageId) =>
            new(
                SupportsInstall: true,
                SupportsUpdate: true,
                SupportsUninstall: true,
                SupportsInteractiveInstall: true,
                SupportsInteractiveUpdate: true,
                SupportsReinstall: true,
                SupportsOpenInstallPage: false,
                SupportsOpenUpdatePage: false,
                UsesWinget: true,
                UsesCustomFlow: false,
                HasGuidedFallback: false);

        public Task<InstallerEnvironmentInfo> GetEnvironmentInfoAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new InstallerEnvironmentInfo(true, "1.0.0", "winget ready"));

        public Task<IReadOnlyList<InstallerPackageStatus>> GetPackageStatusesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerPackageStatus>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> RunPackageOperationAsync(
            string packageId,
            InstallerPackageAction action,
            CancellationToken cancellationToken = default) =>
            action switch
            {
                InstallerPackageAction.Install => InstallPackagesAsync([packageId], cancellationToken),
                InstallerPackageAction.Update => UpgradePackagesAsync([packageId], cancellationToken),
                InstallerPackageAction.Uninstall => UninstallPackagesAsync([packageId], cancellationToken),
                _ => Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]),
            };

        public Task<IReadOnlyList<InstallerOperationResult>> InstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            InstallPackagesAsyncHandler?.Invoke(packageIds, cancellationToken)
            ?? Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> UpgradePackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);

        public Task<IReadOnlyList<InstallerOperationResult>> UninstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InstallerOperationResult>>([]);
    }
}
