using MultiTool.Core.Models;
using MultiTool.Core.Results;
using MultiTool.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;


public sealed partial class WindowsWingetInstallerServiceTests
{
    [Fact]
    public async Task RunPackageOperationAsync_ShouldUseInteractiveWingetInstallCommand()
    {
        var commands = new List<string>();
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                commands.Add(startInfo.Arguments);

                var result = startInfo.Arguments switch
                {
                    "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(0, "Name Id Version Source", string.Empty),
                    "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(0, "Name Id Version Available Source", string.Empty),
                    "install --exact --id \"Microsoft.VisualStudioCode\" --accept-package-agreements --accept-source-agreements --interactive" => new InstallerCommandResult(0, "Successfully installed", string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            localPackageStatusResolver: _ => null);

        var results = await service.RunPackageOperationAsync("Microsoft.VisualStudioCode", InstallerPackageAction.InstallInteractive);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
        commands.Should().Contain("install --exact --id \"Microsoft.VisualStudioCode\" --accept-package-agreements --accept-source-agreements --interactive");
        commands.Should().NotContain(argument => argument.Contains("--silent", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunPackageOperationAsync_ShouldUseForceAndNoUpgradeForReinstall()
    {
        var commands = new List<string>();
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                commands.Add(startInfo.Arguments);

                var result = startInfo.Arguments switch
                {
                    "install --exact --id \"Microsoft.VisualStudioCode\" --accept-package-agreements --force --no-upgrade --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(0, "Successfully installed", string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            });

        var results = await service.RunPackageOperationAsync("Microsoft.VisualStudioCode", InstallerPackageAction.Reinstall);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
        results[0].Message.Should().Be("Reinstall completed.");
        commands.Should().ContainSingle();
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldAddInteractiveGuidanceForAccessDeniedFailures()
    {
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                var result = startInfo.Arguments switch
                {
                    "upgrade --exact --id \"Microsoft.VisualStudioCode\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(
                        1,
                        """
                        An unexpected error occurred while executing the command:
                        Access is denied.
                        0x80070005
                        """,
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            });

        var results = await service.UpgradePackagesAsync(["Microsoft.VisualStudioCode"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeFalse();
        results[0].Guidance.Should().Contain("interactive action");
        results[0].RequiresManualStep.Should().BeTrue();
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldTreatExistingInstallAsSuccessfulNoOp()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var results = await service.InstallPackagesAsync(["Microsoft.VisualStudioCode"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeFalse();
        results[0].Message.Should().Be("Already installed and up to date.");
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldSkipInstalledWingetPackageBeforeRunningInstall()
    {
        var commands = new List<string>();
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                commands.Add(startInfo.Arguments);

                var result = startInfo.Arguments switch
                {
                    "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name  Id       Version Source
                        --------------------------------
                        Git   Git.Git  2.53.0  winget
                        """,
                        string.Empty),
                    "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name  Id       Version  Available  Source
                        -----------------------------------------
                        Git   Git.Git  2.53.0   2.53.0.2   winget
                        """,
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            localPackageStatusResolver: _ => null);

        var results = await service.InstallPackagesAsync(["Git.Git"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeFalse();
        results[0].Message.Should().Be("Already installed. Use Update to upgrade.");
        commands.Should().NotContain(argument => argument.StartsWith("install ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldReportMissingInstall()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var results = await service.UpgradePackagesAsync(["Roblox.Roblox"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeFalse();
        results[0].Changed.Should().BeFalse();
        results[0].Message.Should().Be("This app is not installed yet.");
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldFallbackToOfficialSpotifyInstallerWhenWingetUpgradeFails()
    {
        var launchedInstallers = new List<(string FileName, string Arguments, bool PreferUnelevated)>();
        var downloadedFiles = new List<(string Url, string DestinationPath)>();
        var commandCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                commandCounts[startInfo.Arguments] = commandCounts.GetValueOrDefault(startInfo.Arguments) + 1;

                var result = startInfo.Arguments switch
                {
                    "upgrade --exact --id \"Spotify.Spotify\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(
                        1,
                        """
                        Found Spotify [Spotify.Spotify] Version 1.2.85.513.g45f09625
                        Downloading https://upgrade.scdn.co/upgrade/client/win32-x86_64/spotify_installer-1.2.85.513.g45f09625-3679.exe
                        An unexpected error occurred while executing the command:
                        Download request status is not success.
                        0x80190193 : Forbidden (403).
                        """,
                        string.Empty),
                    "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name     Id               Version  Source
                        -----------------------------------------
                        Spotify  Spotify.Spotify  1.2.85   winget
                        """,
                        string.Empty),
                    "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name  Id  Version  Available  Source
                        ------------------------------------
                        """,
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            fileDownloader: (url, destinationPath, _) =>
            {
                downloadedFiles.Add((url, destinationPath));
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.WriteAllText(destinationPath, "spotify");
                return Task.CompletedTask;
            },
            installerExecutableLauncher: (startInfo, preferUnelevated, _) =>
            {
                launchedInstallers.Add((startInfo.FileName, startInfo.Arguments, preferUnelevated));
                return Task.CompletedTask;
            },
            delayAsync: (_, _) => Task.CompletedTask,
            localPackageStatusResolver: _ => null);

        var results = await service.UpgradePackagesAsync(["Spotify.Spotify"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
        results[0].Message.Should().Be("Updated successfully through Spotify's official installer fallback.");
        downloadedFiles.Should().ContainSingle();
        downloadedFiles[0].Url.Should().Be("https://download.scdn.co/SpotifySetup.exe");
        launchedInstallers.Should().ContainSingle();
        launchedInstallers[0].PreferUnelevated.Should().BeTrue();
        launchedInstallers[0].FileName.Should().EndWith("SpotifySetup.exe");
        launchedInstallers[0].Arguments.Should().Contain("/silent /skip-app-launch");
        commandCounts["list --accept-source-agreements --disable-interactivity"].Should().Be(1);
        commandCounts["list --upgrade-available --accept-source-agreements --disable-interactivity"].Should().Be(1);
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldEmitDownloadProgressEventsForWingetManagedUpdates()
    {
        var progressEvents = new List<InstallerOperationProgressChangedEventArgs>();
        var service = new WindowsWingetInstallerService(
            (_, _) => throw new InvalidOperationException("The non-progress command runner should not be used for the winget update path in this test."),
            localPackageStatusResolver: _ => null,
            progressCommandRunner: (startInfo, _, outputSegmentHandler, errorSegmentHandler) =>
            {
                startInfo.FileName.Should().Be("winget");
                startInfo.Arguments.Should().Be("upgrade --exact --id \"KiCad.KiCad\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");

                outputSegmentHandler?.Invoke("\u001b[2KDownloading https://downloads.kicad.org/kicad-setup.exe");
                outputSegmentHandler?.Invoke("\u001b[2K12.0 MiB / 60.0 MiB");
                outputSegmentHandler?.Invoke("\u001b[2K24.0 MiB / 60.0 MiB");
                errorSegmentHandler?.Invoke("\u001b[2KSuccessfully verified installer hash");
                outputSegmentHandler?.Invoke("\u001b[2KStarting package upgrade");

                return Task.FromResult(new InstallerCommandResult(0, "Successfully upgraded", string.Empty));
            });
        service.OperationProgressChanged += (_, args) => progressEvents.Add(args);

        var results = await service.UpgradePackagesAsync(["KiCad.KiCad"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        progressEvents.Select(args => $"{args.StatusText}|{(args.Percent?.ToString() ?? "null")}")
            .Should()
            .ContainInOrder(
                "Updating...|null",
                "Downloading...|null",
                "Downloading 20%...|20",
                "Downloading 40%...|40",
                "Verified installer hash.|null",
                "Running installer...|null");
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldSurfaceWingetDownloadFailureDetails()
    {
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                var result = startInfo.Arguments switch
                {
                    "upgrade --exact --id \"Spotify.Spotify\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(
                        1,
                        """
                        Found Spotify [Spotify.Spotify] Version 1.2.85.513.g45f09625
                        This application is licensed to you by its owner.
                        Microsoft is not responsible for, nor does it grant any licenses to, third-party packages.
                        Downloading https://upgrade.scdn.co/upgrade/client/win32-x86_64/spotify_installer.exe
                        An unexpected error occurred while executing the command:
                        Download request status is not success.
                        0x80190193 : Forbidden (403).
                        """,
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            fileDownloader: (_, _, _) => throw new InvalidOperationException("Fallback download blocked."),
            localPackageStatusResolver: _ => null);

        var results = await service.UpgradePackagesAsync(["Spotify.Spotify"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeFalse();
        results[0].Changed.Should().BeFalse();
        results[0].Message.Should().Be("Spotify fallback download failed after winget failed: Download request status is not success. 0x80190193 : Forbidden (403). Direct installer error: Fallback download blocked.");
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldTreatWingetErrorOutputAsFailureEvenWhenExitCodeIsZero()
    {
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                var result = startInfo.Arguments switch
                {
                    "upgrade --exact --id \"Spotify.Spotify\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(
                        0,
                        """
                        Found Spotify [Spotify.Spotify] Version 1.2.85.513.g45f09625
                        Downloading https://upgrade.scdn.co/upgrade/client/win32-x86_64/spotify_installer.exe
                        An unexpected error occurred while executing the command:
                        Download request status is not success.
                        0x80190193 : Forbidden (403).
                        """,
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            fileDownloader: (_, _, _) => throw new InvalidOperationException("Fallback download blocked."),
            localPackageStatusResolver: _ => null);

        var results = await service.UpgradePackagesAsync(["Spotify.Spotify"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeFalse();
        results[0].Changed.Should().BeFalse();
        results[0].Message.Should().Be("Spotify fallback download failed after winget failed: Download request status is not success. 0x80190193 : Forbidden (403). Direct installer error: Fallback download blocked.");
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldInstallDiscordBeforeVencord()
    {
        var commands = new List<string>();
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                commands.Add(startInfo.Arguments);

                var result = startInfo.Arguments switch
                {
                    "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name  Id  Version Source
                        ------------------------
                        """,
                        string.Empty),
                    "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name  Id  Version  Available  Source
                        ------------------------------------
                        """,
                        string.Empty),
                    _ =>
                        new InstallerCommandResult(
                            0,
                            $"Successfully installed {(startInfo.Arguments.Contains("Vendicated.Vencord", StringComparison.Ordinal) ? "Vendicated.Vencord" : "Discord.Discord")}",
                            string.Empty),
                };

                return Task.FromResult(result);
            },
            localPackageStatusResolver: _ => null);

        var results = await service.InstallPackagesAsync(["Vendicated.Vencord"]);

        commands.Should().ContainInOrder(
            "install --exact --id \"Discord.Discord\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent",
            "install --exact --id \"Vendicated.Vencord\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
        results.Select(result => result.PackageId).Should().ContainInOrder("Discord.Discord", "Vendicated.Vencord");
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldSkipAutoIncludedDependencyWhenAlreadyInstalled()
    {
        var commands = new List<string>();
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                commands.Add(startInfo.Arguments);

                var result = startInfo.Arguments switch
                {
                    "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name     Id               Version Source
                        ----------------------------------------
                        Discord  Discord.Discord  1.0.0   winget
                        """,
                        string.Empty),
                    "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name  Id  Version  Available  Source
                        ------------------------------------
                        """,
                        string.Empty),
                    "install --exact --id \"Vendicated.Vencord\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(
                        0,
                        "Successfully installed Vendicated.Vencord",
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            localPackageStatusResolver: _ => null);

        var results = await service.InstallPackagesAsync(["Vendicated.Vencord"]);

        results.Should().ContainSingle();
        results[0].PackageId.Should().Be("Vendicated.Vencord");
        commands.Should().NotContain(argument => argument.Contains("\"Discord.Discord\"", StringComparison.Ordinal));
        commands.Should().Contain("install --exact --id \"Vendicated.Vencord\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldIncludeMsStoreSourceForStorePackages()
    {
        string? capturedArguments = null;
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                capturedArguments = startInfo.Arguments;
                return Task.FromResult(new InstallerCommandResult(0, "Successfully installed", string.Empty));
            });

        await service.InstallPackagesAsync(["9NRX63209R7B"]);

        capturedArguments.Should().Be("install --exact --id \"9NRX63209R7B\" --source \"msstore\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
    }

    [Fact]
    public async Task UninstallPackagesAsync_ShouldUseMsStoreSourceForCleanupPackages()
    {
        string? capturedArguments = null;
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                capturedArguments = startInfo.Arguments;
                return Task.FromResult(new InstallerCommandResult(0, "Successfully uninstalled", string.Empty));
            });

        var results = await service.UninstallPackagesAsync(["XP8BT8DW290MPQ"]);

        capturedArguments.Should().Be("uninstall --exact --id \"XP8BT8DW290MPQ\" --source \"msstore\" --accept-source-agreements --disable-interactivity --silent");
        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
    }
}
