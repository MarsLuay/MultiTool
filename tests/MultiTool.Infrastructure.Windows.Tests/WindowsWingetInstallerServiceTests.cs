using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsWingetInstallerServiceTests
{
    [Fact]
    public void GetCatalog_ShouldIncludeAnyDesk()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Adobe.Acrobat.Reader.64-bit",
                DisplayName = "Adobe Acrobat Reader",
                Category = "Productivity",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "AnyDesk.AnyDesk",
                DisplayName = "AnyDesk",
                Category = "Remote Access",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Audacity.Audacity",
                DisplayName = "Audacity",
                Category = "Creator",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "BlenderFoundation.Blender",
                DisplayName = "Blender",
                Category = "Creative",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Bitwarden.Bitwarden",
                DisplayName = "Bitwarden",
                Category = "Security",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Cloudflare.Warp",
                DisplayName = "Cloudflare WARP",
                Category = "Security",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "9N0866FS04W8",
                DisplayName = "Dolby Access",
                Category = "Media",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Microsoft.Teams",
                DisplayName = "Microsoft Teams",
                Category = "Communication",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "KiCad.KiCad",
                DisplayName = "KiCad",
                Category = "CAD",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "FreeCAD.FreeCAD",
                DisplayName = "FreeCAD",
                Category = "CAD",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "PCSX2Team.PCSX2",
                DisplayName = "PCSX2",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Vita3K.Vita3K",
                DisplayName = "Vita3K",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "DolphinEmulator.Dolphin",
                DisplayName = "Dolphin",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Cemu.Cemu",
                DisplayName = "Cemu",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Libretro.RetroArch",
                DisplayName = "RetroArch",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Ryubing.Ryujinx",
                DisplayName = "Ryujinx (Ryubing)",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "RPCS3.RPCS3",
                DisplayName = "RPCS3",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "AzaharEmu.Azahar",
                DisplayName = "Lime3DS (Azahar)",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Anki.Anki",
                DisplayName = "Anki",
                Category = "Learning",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "AUTOMATIC1111.StableDiffusionWebUI",
                DisplayName = "Stable Diffusion WebUI (AUTOMATIC1111)",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Comfy.ComfyUI-Desktop",
                DisplayName = "ComfyUI",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "OpenJS.NodeJS.LTS",
                DisplayName = "Node.js",
                Category = "Developer",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Microsoft.DotNet.SDK.10",
                DisplayName = "Microsoft .NET SDK",
                Category = "Developer",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "9MV0B5HZVK9Z",
                DisplayName = "Xbox",
                Category = "Games",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Neovim.Neovim",
                DisplayName = "Neovim",
                Category = "Developer",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Ollama.Ollama",
                DisplayName = "Ollama",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "ElementLabs.LMStudio",
                DisplayName = "LM Studio",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "OpenWebUI.OpenWebUI",
                DisplayName = "Open WebUI",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Macrium.Reflect",
                DisplayName = "Macrium Reflect",
                Category = "Utilities",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Guru3D.Afterburner",
                DisplayName = "MSI Afterburner",
                Category = "Utilities",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "TechPowerUp.NVCleanstall",
                DisplayName = "NVCleanstall",
                Category = "Utilities",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "OpenRGB.OpenRGB",
                DisplayName = "OpenRGB",
                Category = "Utilities",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "TorProject.TorBrowser",
                DisplayName = "Tor Browser",
                Category = "Browsers",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Microsoft.WSL",
                DisplayName = "WSL",
                Category = "Developer",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "WiresharkFoundation.Wireshark",
                DisplayName = "Wireshark",
                Category = "Networking",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Python.Python.3.14",
                DisplayName = "Python",
                Category = "Developer",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Guided.SKiDL",
                DisplayName = "SKiDL",
                Category = "CAD",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Guided.PCBFlow",
                DisplayName = "PCBFlow",
                Category = "CAD",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Guided.build123d",
                DisplayName = "build123d",
                Category = "CAD",
            });
    }

    [Fact]
    public void GetPackageCapabilities_ShouldExposeInteractiveAndReinstallForWingetPackages()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var capabilities = service.GetPackageCapabilities("Microsoft.VisualStudioCode");

        capabilities.SupportsInstall.Should().BeTrue();
        capabilities.SupportsUpdate.Should().BeTrue();
        capabilities.SupportsInteractiveInstall.Should().BeTrue();
        capabilities.SupportsInteractiveUpdate.Should().BeTrue();
        capabilities.SupportsReinstall.Should().BeTrue();
        capabilities.UsesWinget.Should().BeTrue();
        capabilities.UsesCustomFlow.Should().BeFalse();
    }

    [Fact]
    public void GetPackageCapabilities_ShouldExposeOfficialPageForCustomPackages()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var capabilities = service.GetPackageCapabilities("AUTOMATIC1111.StableDiffusionWebUI");

        capabilities.SupportsInteractiveInstall.Should().BeFalse();
        capabilities.SupportsReinstall.Should().BeFalse();
        capabilities.SupportsOpenInstallPage.Should().BeTrue();
        capabilities.SupportsOpenUpdatePage.Should().BeTrue();
        capabilities.UsesCustomFlow.Should().BeTrue();
    }

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
    public async Task GetPackageStatusesAsync_ShouldMarkAutomatic1111AsInstalledWhenLocalCloneExists()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "webui-user.bat"), "@echo off");
            File.WriteAllText(Path.Combine(installDirectory, "webui.bat"), "@echo off");

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                automatic1111InstallDirectoryResolver: () => installDirectory);

            var statuses = await service.GetPackageStatusesAsync(["AUTOMATIC1111.StableDiffusionWebUI"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkAutomatic1111AsUpgradeableWhenGitReportsBehindRemote()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            Directory.CreateDirectory(Path.Combine(installDirectory, ".git"));
            File.WriteAllText(Path.Combine(installDirectory, "webui-user.bat"), "@echo off");
            File.WriteAllText(Path.Combine(installDirectory, "webui.bat"), "@echo off");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (string.Equals(startInfo.FileName, @"C:\Program Files\Git\cmd\git.exe", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(startInfo.Arguments, $"-C \"{installDirectory}\" fetch origin --quiet", StringComparison.Ordinal))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    if (string.Equals(startInfo.FileName, @"C:\Program Files\Git\cmd\git.exe", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(startInfo.Arguments, $"-C \"{installDirectory}\" rev-list --count HEAD..@{{upstream}}", StringComparison.Ordinal))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, "3", string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                automatic1111InstallDirectoryResolver: () => installDirectory,
                gitExecutableResolver: () => @"C:\Program Files\Git\cmd\git.exe");

            var statuses = await service.GetPackageStatusesAsync(["AUTOMATIC1111.StableDiffusionWebUI"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeTrue();
            statuses[0].StatusText.Should().Be("Update available");
            commands.Should().ContainInOrder(
                (@"C:\Program Files\Git\cmd\git.exe", $"-C \"{installDirectory}\" fetch origin --quiet"),
                (@"C:\Program Files\Git\cmd\git.exe", $"-C \"{installDirectory}\" rev-list --count HEAD..@{{upstream}}"));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldKeepAutomatic1111AsInstalledWhenGitReportsNoRemoteChanges()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(installDirectory, ".git"));
            File.WriteAllText(Path.Combine(installDirectory, "webui-user.bat"), "@echo off");
            File.WriteAllText(Path.Combine(installDirectory, "webui.bat"), "@echo off");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    if (string.Equals(startInfo.FileName, @"C:\Program Files\Git\cmd\git.exe", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(startInfo.Arguments, $"-C \"{installDirectory}\" fetch origin --quiet", StringComparison.Ordinal))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    if (string.Equals(startInfo.FileName, @"C:\Program Files\Git\cmd\git.exe", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(startInfo.Arguments, $"-C \"{installDirectory}\" rev-list --count HEAD..@{{upstream}}", StringComparison.Ordinal))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, "0", string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                automatic1111InstallDirectoryResolver: () => installDirectory,
                gitExecutableResolver: () => @"C:\Program Files\Git\cmd\git.exe");

            var statuses = await service.GetPackageStatusesAsync(["AUTOMATIC1111.StableDiffusionWebUI"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkOpenWebUiAsInstalledWhenLocalInstallExists()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(installDirectory, "venv", "Scripts"));
            File.WriteAllText(Path.Combine(installDirectory, "venv", "Scripts", "python.exe"), string.Empty);
            File.WriteAllText(Path.Combine(installDirectory, "venv", "Scripts", "open-webui.exe"), string.Empty);
            File.WriteAllText(Path.Combine(installDirectory, "launch-open-webui-multitool.bat"), "@echo off");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    if (string.Equals(startInfo.FileName, Path.Combine(installDirectory, "venv", "Scripts", "python.exe"), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(startInfo.Arguments, "-m pip list --outdated --format=json", StringComparison.Ordinal))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, "[]", string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                openWebUiInstallDirectoryResolver: () => installDirectory);

            var statuses = await service.GetPackageStatusesAsync(["OpenWebUI.OpenWebUI"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkOpenWebUiAsUpgradeableWhenPipReportsOutdatedPackage()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(installDirectory, "venv", "Scripts"));
            File.WriteAllText(Path.Combine(installDirectory, "venv", "Scripts", "python.exe"), string.Empty);
            File.WriteAllText(Path.Combine(installDirectory, "venv", "Scripts", "open-webui.exe"), string.Empty);
            File.WriteAllText(Path.Combine(installDirectory, "launch-open-webui-multitool.bat"), "@echo off");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    if (string.Equals(startInfo.FileName, Path.Combine(installDirectory, "venv", "Scripts", "python.exe"), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(startInfo.Arguments, "-m pip list --outdated --format=json", StringComparison.Ordinal))
                    {
                        return Task.FromResult(
                            new InstallerCommandResult(
                                0,
                                """[{"name":"open-webui","version":"0.6.5","latest_version":"0.6.6","latest_filetype":"wheel"}]""",
                                string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                openWebUiInstallDirectoryResolver: () => installDirectory);

            var statuses = await service.GetPackageStatusesAsync(["OpenWebUI.OpenWebUI"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeTrue();
            statuses[0].StatusText.Should().Be("Update available");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkRyubingRyujinxAsInstalledWhenExecutableExists()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "Ryujinx.exe"), string.Empty);

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                ryubingRyujinxInstallDirectoryResolver: () => installDirectory,
                ryubingRyujinxReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(null));

            var statuses = await service.GetPackageStatusesAsync(["Ryubing.Ryujinx"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkRyubingRyujinxAsUpgradeableWhenReleaseMarkerDiffers()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "Ryujinx.exe"), string.Empty);
            File.WriteAllText(
                Path.Combine(installDirectory, ".multitool-installed-release.txt"),
                "https://git.ryujinx.app/api/v4/projects/1/packages/generic/Ryubing/1.3.3/ryujinx-1.3.3-win_x64.zip");

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                ryubingRyujinxInstallDirectoryResolver: () => installDirectory,
                ryubingRyujinxReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "ryujinx-1.3.4-win_x64.zip",
                    "https://git.ryujinx.app/api/v4/projects/1/packages/generic/Ryubing/1.3.4/ryujinx-1.3.4-win_x64.zip")));

            var statuses = await service.GetPackageStatusesAsync(["Ryubing.Ryujinx"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeTrue();
            statuses[0].StatusText.Should().Be("Update available");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldKeepRyubingRyujinxAsInstalledWhenReleaseMarkerMatches()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "Ryujinx.exe"), string.Empty);
            File.WriteAllText(
                Path.Combine(installDirectory, ".multitool-installed-release.txt"),
                "https://git.ryujinx.app/api/v4/projects/1/packages/generic/Ryubing/1.3.4/ryujinx-1.3.4-win_x64.zip");

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                ryubingRyujinxInstallDirectoryResolver: () => installDirectory,
                ryubingRyujinxReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "ryujinx-1.3.4-win_x64.zip",
                    "https://git.ryujinx.app/api/v4/projects/1/packages/generic/Ryubing/1.3.4/ryujinx-1.3.4-win_x64.zip")));

            var statuses = await service.GetPackageStatusesAsync(["Ryubing.Ryujinx"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkAzaharAsInstalledWhenExecutableExists()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "azahar.exe"), string.Empty);

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                azaharInstallDirectoryResolver: () => installDirectory,
                azaharReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(null));

            var statuses = await service.GetPackageStatusesAsync(["AzaharEmu.Azahar"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkAzaharAsUpgradeableWhenReleaseMarkerDiffers()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "azahar.exe"), string.Empty);
            File.WriteAllText(
                Path.Combine(installDirectory, ".multitool-installed-release.txt"),
                "https://github.com/azahar-emu/azahar/releases/download/2124.3/azahar-2124.3-windows-msys2.zip");

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                azaharInstallDirectoryResolver: () => installDirectory,
                azaharReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "azahar-2124.4-windows-msys2.zip",
                    "https://github.com/azahar-emu/azahar/releases/download/2124.4/azahar-2124.4-windows-msys2.zip")));

            var statuses = await service.GetPackageStatusesAsync(["AzaharEmu.Azahar"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeTrue();
            statuses[0].StatusText.Should().Be("Update available");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldKeepAzaharAsInstalledWhenReleaseMarkerMatches()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "azahar.exe"), string.Empty);
            File.WriteAllText(
                Path.Combine(installDirectory, ".multitool-installed-release.txt"),
                "https://github.com/azahar-emu/azahar/releases/download/2124.4/azahar-2124.4-windows-msys2.zip");

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                azaharInstallDirectoryResolver: () => installDirectory,
                azaharReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "azahar-2124.4-windows-msys2.zip",
                    "https://github.com/azahar-emu/azahar/releases/download/2124.4/azahar-2124.4-windows-msys2.zip")));

            var statuses = await service.GetPackageStatusesAsync(["AzaharEmu.Azahar"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkMacriumReflectAsInstalledWhenExecutableExists()
    {
        var executablePath = Path.Combine(CreateTemporaryDirectory(), "ReflectBin.exe");

        try
        {
            File.WriteAllText(executablePath, string.Empty);
            var workingDirectory = Path.GetDirectoryName(executablePath)!;

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                macriumReflectWorkingDirectoryResolver: () => workingDirectory,
                macriumReflectExecutableResolver: () => executablePath,
                macriumReflectReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(null));

            var statuses = await service.GetPackageStatusesAsync(["Macrium.Reflect"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            var directory = Path.GetDirectoryName(executablePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkMacriumReflectAsUpgradeableWhenReleaseMarkerDiffers()
    {
        var workingDirectory = CreateTemporaryDirectory();
        var executablePath = Path.Combine(workingDirectory, "ReflectBin.exe");

        try
        {
            File.WriteAllText(executablePath, string.Empty);
            File.WriteAllText(
                Path.Combine(workingDirectory, ".multitool-installed-release.txt"),
                "https://download.macrium.com/reflect/v10/v10.0.8750/reflect_home_setup_x64.exe");

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                macriumReflectWorkingDirectoryResolver: () => workingDirectory,
                macriumReflectExecutableResolver: () => executablePath,
                macriumReflectReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "reflect_home_setup_x64.exe",
                    "https://download.macrium.com/reflect/v10/v10.0.8751/reflect_home_setup_x64.exe")));

            var statuses = await service.GetPackageStatusesAsync(["Macrium.Reflect"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeTrue();
            statuses[0].StatusText.Should().Be("Update available");
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldKeepMacriumReflectAsInstalledWhenReleaseMarkerMatches()
    {
        var workingDirectory = CreateTemporaryDirectory();
        var executablePath = Path.Combine(workingDirectory, "ReflectBin.exe");

        try
        {
            File.WriteAllText(executablePath, string.Empty);
            File.WriteAllText(
                Path.Combine(workingDirectory, ".multitool-installed-release.txt"),
                "https://download.macrium.com/reflect/v10/v10.0.8751/reflect_home_setup_x64.exe");

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                macriumReflectWorkingDirectoryResolver: () => workingDirectory,
                macriumReflectExecutableResolver: () => executablePath,
                macriumReflectReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "reflect_home_setup_x64.exe",
                    "https://download.macrium.com/reflect/v10/v10.0.8751/reflect_home_setup_x64.exe")));

            var statuses = await service.GetPackageStatusesAsync(["Macrium.Reflect"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkRpcs3AsInstalledWhenExecutableExists()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "rpcs3.exe"), string.Empty);

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                rpcs3InstallDirectoryResolver: () => installDirectory,
                rpcs3ReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(null));

            var statuses = await service.GetPackageStatusesAsync(["RPCS3.RPCS3"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkRpcs3AsUpgradeableWhenReleaseMarkerDiffers()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "rpcs3.exe"), string.Empty);
            File.WriteAllText(
                Path.Combine(installDirectory, ".multitool-installed-release.txt"),
                "https://github.com/RPCS3/rpcs3-binaries-win/releases/download/build-123/rpcs3-v0.0.40-18950-16277576_win64_msvc.7z");

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                rpcs3InstallDirectoryResolver: () => installDirectory,
                rpcs3ReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "rpcs3-v0.0.41-19000-abcdef12_win64_msvc.7z",
                    "https://github.com/RPCS3/rpcs3-binaries-win/releases/download/build-124/rpcs3-v0.0.41-19000-abcdef12_win64_msvc.7z")));

            var statuses = await service.GetPackageStatusesAsync(["RPCS3.RPCS3"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeTrue();
            statuses[0].StatusText.Should().Be("Update available");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldKeepRpcs3AsInstalledWhenReleaseMarkerMatches()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "rpcs3.exe"), string.Empty);
            File.WriteAllText(
                Path.Combine(installDirectory, ".multitool-installed-release.txt"),
                "https://github.com/RPCS3/rpcs3-binaries-win/releases/download/build-124/rpcs3-v0.0.41-19000-abcdef12_win64_msvc.7z");

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                rpcs3InstallDirectoryResolver: () => installDirectory,
                rpcs3ReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "rpcs3-v0.0.41-19000-abcdef12_win64_msvc.7z",
                    "https://github.com/RPCS3/rpcs3-binaries-win/releases/download/build-124/rpcs3-v0.0.41-19000-abcdef12_win64_msvc.7z")));

            var statuses = await service.GetPackageStatusesAsync(["RPCS3.RPCS3"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (custom)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMapInstalledAndUpgradeablePackages()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var statuses = await service.GetPackageStatusesAsync(
            [
                "Git.Git",
                "Mozilla.Firefox",
                "Google.Chrome",
            ]);

        statuses.Should().ContainEquivalentOf(new { PackageId = "Git.Git", IsInstalled = true, HasUpdateAvailable = true, StatusText = "Update available" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "Mozilla.Firefox", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "Google.Chrome", IsInstalled = false, HasUpdateAvailable = false, StatusText = "Not installed" });
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkFirefoxAsInstalledWhenWingetMissesButLocalInstallExists()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(Path.Combine(installDirectory, "firefox.exe"), "stub");
            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    var result = startInfo.Arguments switch
                    {
                        "--version" => new InstallerCommandResult(0, "v1.28.220", string.Empty),
                        "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                            0,
                            """
                            Name    Id       Version  Source
                            --------------------------------
                            Git     Git.Git  2.53.0   winget
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
                firefoxInstallDirectoryResolver: () => installDirectory);

            var statuses = await service.GetPackageStatusesAsync(["Mozilla.Firefox"]);

            statuses.Should().ContainSingle();
            statuses[0].IsInstalled.Should().BeTrue();
            statuses[0].HasUpdateAvailable.Should().BeFalse();
            statuses[0].StatusText.Should().Be("Installed (detected locally)");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMatchArpAndMsixEntriesByDisplayName()
    {
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                var result = startInfo.Arguments switch
                {
                    "--version" => new InstallerCommandResult(0, "v1.28.220", string.Empty),
                    "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name                        Id                                               Version
                        -------------------------------------------------------------------------------
                        Git                         ARP\Machine\X64\Git_is1                           2.52.0
                        Discord                     ARP\User\X64\Discord                              1.0.9228
                        Everything 1.4.1.1032 (x86) ARP\Machine\X86\Everything                        1.4.1.1032
                        Dolby Access                MSIX\DolbyLaboratories.DolbyAccess_123            3.27.7470.0
                        Outlook for Windows         MSIX\Microsoft.OutlookForWindows_123              1.2026.225.400
                        """,
                        string.Empty),
                    "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name        Id                      Version Available Source
                        ------------------------------------------------------------
                        qBittorrent qBittorrent.qBittorrent 5.1.2   5.1.4     winget
                        """,
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            localPackageStatusResolver: _ => null);

        var statuses = await service.GetPackageStatusesAsync(
            [
                "Git.Git",
                "Discord.Discord",
                "voidtools.Everything",
                "9N0866FS04W8",
                "9NRX63209R7B",
                "qBittorrent.qBittorrent",
            ]);

        statuses.Should().ContainEquivalentOf(new { PackageId = "Git.Git", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "Discord.Discord", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "voidtools.Everything", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "9N0866FS04W8", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "9NRX63209R7B", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "qBittorrent.qBittorrent", IsInstalled = true, HasUpdateAvailable = true, StatusText = "Update available" });
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldNotConfuseEverythingWithDateEverything()
    {
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                var result = startInfo.Arguments switch
                {
                    "--version" => new InstallerCommandResult(0, "v1.28.220", string.Empty),
                    "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name                    Id                                  Version
                        -------------------------------------------------------------------
                        Date Everything!        ARP\Machine\X64\Steam App 2201320   Unknown
                        """,
                        string.Empty),
                    "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name Id Version Available Source
                        --------------------------------
                        """,
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            localPackageStatusResolver: _ => null);

        var statuses = await service.GetPackageStatusesAsync(["voidtools.Everything"]);

        statuses.Should().ContainSingle();
        statuses[0].IsInstalled.Should().BeFalse();
        statuses[0].HasUpdateAvailable.Should().BeFalse();
        statuses[0].StatusText.Should().Be("Not installed");
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldNotConfuseLatestPythonWithDifferentInstalledPythonVersions()
    {
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                var result = startInfo.Arguments switch
                {
                    "--version" => new InstallerCommandResult(0, "v1.28.220", string.Empty),
                    "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name                    Id                 Version
                        ----------------------------------------------------------
                        Python 3.10.11 (64-bit) Python.Python.3.10 3.10.11
                        Python 3.14.3 (64-bit)  Python.Python.3.14 3.14.3
                        """,
                        string.Empty),
                    "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                        0,
                        """
                        Name                    Id                 Version Available Source
                        -------------------------------------------------------------------
                        Python 3.10.11 (64-bit) Python.Python.3.10 3.10.11 3.10.12  winget
                        """,
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            localPackageStatusResolver: _ => null);

        var statuses = await service.GetPackageStatusesAsync(["Python.Python.3.10", "Python.Python.3.14"]);

        statuses.Should().ContainEquivalentOf(new
        {
            PackageId = "Python.Python.3.10",
            IsInstalled = true,
            HasUpdateAvailable = true,
            StatusText = "Update available",
        });
        statuses.Should().ContainEquivalentOf(new
        {
            PackageId = "Python.Python.3.14",
            IsInstalled = true,
            HasUpdateAvailable = false,
            StatusText = "Installed",
        });
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldUseLocalFallbackForManualTorAndVencordInstalls()
    {
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                var result = startInfo.Arguments switch
                {
                    "--version" => new InstallerCommandResult(0, "v1.28.220", string.Empty),
                    "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(0, "Name Id Version Source", string.Empty),
                    "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(0, "Name Id Version Available Source", string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            },
            localPackageStatusResolver: packageId =>
                packageId switch
                {
                    "TorProject.TorBrowser" => new InstallerPackageStatus(packageId, true, false, "Installed (detected locally)"),
                    "Vendicated.Vencord" => new InstallerPackageStatus(packageId, true, false, "Installed (detected locally)"),
                    _ => null,
                });

        var statuses = await service.GetPackageStatusesAsync(["TorProject.TorBrowser", "Vendicated.Vencord"]);

        statuses.Should().ContainEquivalentOf(new { PackageId = "TorProject.TorBrowser", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed (detected locally)" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "Vendicated.Vencord", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed (detected locally)" });
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkMissingCustomEntriesAsNotInstalled()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var statuses = await service.GetPackageStatusesAsync(
            [
                "Macrium.Reflect",
            ]);

        statuses.Should().ContainEquivalentOf(new { PackageId = "Macrium.Reflect", IsInstalled = false, HasUpdateAvailable = false, StatusText = "Not installed" });
    }

    [Fact]
    public void CatalogEntries_ShouldExposeShortDescriptions()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        service.GetCatalog().Should().OnlyContain(
            item => !string.IsNullOrWhiteSpace(item.Description)
                && item.Description.Length <= 80);
        service.GetCleanupCatalog().Should().OnlyContain(
            item => !string.IsNullOrWhiteSpace(item.Description)
                && item.Description.Length <= 80);
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
    public async Task InstallPackagesAsync_ShouldSkipInstalledCustomPackageBeforeRunningInstall()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<string>();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "webui-user.bat"), "@echo off");
            File.WriteAllText(Path.Combine(installDirectory, "webui.bat"), "@echo off");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add($"{startInfo.FileName} {startInfo.Arguments}".Trim());
                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                automatic1111InstallDirectoryResolver: () => installDirectory);

            var results = await service.InstallPackagesAsync(["AUTOMATIC1111.StableDiffusionWebUI"]);

            results.Should().ContainSingle();
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeFalse();
            results[0].Message.Should().Be("Already installed. Skipped.");
            commands.Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
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

    [Fact]
    public async Task InstallPackagesAsync_ShouldAutomateMacriumReflectSetup()
    {
        var workingDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-macrium-reflect-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                macriumReflectWorkingDirectoryResolver: () => workingDirectory,
                macriumReflectReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "reflect_home_setup_x64.exe",
                    "https://download.macrium.com/reflect/v10/v10.0.8750/reflect_home_setup_x64.exe")),
                fileDownloader: (_, destinationPath, _) =>
                {
                    File.WriteAllText(destinationPath, "fake installer");
                    return Task.CompletedTask;
                });

            var results = await service.InstallPackagesAsync(["Macrium.Reflect"]);

            results.Should().ContainSingle();
            results[0].PackageId.Should().Be("Macrium.Reflect");
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Downloaded the latest Macrium Reflect installer");
            commands.Should().ContainSingle(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase) &&
                command.Arguments.Contains("launch-macrium-reflect-multitool.bat", StringComparison.OrdinalIgnoreCase));

            var installerPath = Path.Combine(workingDirectory, "downloads", "reflect_home_setup_x64.exe");
            File.Exists(installerPath).Should().BeTrue();
            var launcherPath = Path.Combine(workingDirectory, "launch-macrium-reflect-multitool.bat");
            File.Exists(launcherPath).Should().BeTrue();
            var launcherContents = File.ReadAllText(launcherPath);
            launcherContents.Should().Contain(installerPath);
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }
        }
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldAutomateAutomatic1111Setup()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (startInfo.FileName.Contains("git.exe", StringComparison.OrdinalIgnoreCase) &&
                        startInfo.Arguments.StartsWith("clone ", StringComparison.Ordinal))
                    {
                        Directory.CreateDirectory(installDirectory);
                        File.WriteAllText(Path.Combine(installDirectory, "webui-user.bat"), "@echo off");
                        File.WriteAllText(Path.Combine(installDirectory, "webui.bat"), "@echo off");
                        return Task.FromResult(new InstallerCommandResult(0, "Cloning into stable-diffusion-webui", string.Empty));
                    }

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase) &&
                        startInfo.Arguments.Contains("launch-webui-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    var result = startInfo.Arguments switch
                    {
                        "install --exact --id \"Git.Git\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" =>
                            new InstallerCommandResult(0, "Successfully installed Git.Git", string.Empty),
                        "install --exact --id \"Python.Python.3.10\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" =>
                            new InstallerCommandResult(0, "Successfully installed Python.Python.3.10", string.Empty),
                        _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                    };

                    return Task.FromResult(result);
                },
                automatic1111InstallDirectoryResolver: () => installDirectory,
                gitExecutableResolver: () => commands.Any(command => command.Arguments.Contains("\"Git.Git\"", StringComparison.Ordinal))
                    ? @"C:\Program Files\Git\cmd\git.exe"
                    : null,
                python310ExecutableResolver: () => commands.Any(command => command.Arguments.Contains("\"Python.Python.3.10\"", StringComparison.Ordinal))
                    ? @"C:\Users\Test\AppData\Local\Programs\Python\Python310\python.exe"
                    : null);

            var results = await service.InstallPackagesAsync(["AUTOMATIC1111.StableDiffusionWebUI"]);

            results.Should().ContainSingle();
            results[0].PackageId.Should().Be("AUTOMATIC1111.StableDiffusionWebUI");
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Cloned the official repo");
            commands.Select(command => command.Arguments).Should().ContainInOrder(
                "install --exact --id \"Git.Git\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent",
                "install --exact --id \"Python.Python.3.10\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
            commands.Should().Contain(command =>
                command.FileName.Contains("git.exe", StringComparison.OrdinalIgnoreCase) &&
                command.Arguments.Contains("https://github.com/AUTOMATIC1111/stable-diffusion-webui.git", StringComparison.Ordinal));
            commands.Should().Contain(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase) &&
                command.Arguments.Contains("launch-webui-multitool.bat", StringComparison.OrdinalIgnoreCase));
            var launcherPath = Path.Combine(installDirectory, "launch-webui-multitool.bat");
            File.Exists(launcherPath).Should().BeTrue();
            var launcherContents = File.ReadAllText(launcherPath);
            launcherContents.Should().Contain(@"set ""PYTHON=C:\Users\Test\AppData\Local\Programs\Python\Python310\python.exe""");
            launcherContents.Should().Contain(@"set ""GIT=C:\Program Files\Git\cmd\git.exe""");
            launcherContents.Should().Contain(@"set ""STABLE_DIFFUSION_REPO=https://github.com/w-e-w/stablediffusion.git""");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldAutomateOpenWebUiSetup()
    {
        var installDirectory = CreateTemporaryDirectory();
        var venvPythonPath = Path.Combine(installDirectory, "venv", "Scripts", "python.exe");
        var openWebUiExecutablePath = Path.Combine(installDirectory, "venv", "Scripts", "open-webui.exe");
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (startInfo.FileName.Equals(@"C:\Users\Test\AppData\Local\Programs\Python\Python311\python.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.StartsWith("-m venv ", StringComparison.Ordinal))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(venvPythonPath)!);
                        File.WriteAllText(venvPythonPath, string.Empty);
                        return Task.FromResult(new InstallerCommandResult(0, "Created virtual environment", string.Empty));
                    }

                    if (startInfo.FileName.Equals(venvPythonPath, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(startInfo.Arguments, "-m pip install open-webui", StringComparison.Ordinal))
                    {
                        File.WriteAllText(openWebUiExecutablePath, string.Empty);
                        return Task.FromResult(new InstallerCommandResult(0, "Successfully installed open-webui", string.Empty));
                    }

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase) &&
                        startInfo.Arguments.Contains("launch-open-webui-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    var result = startInfo.Arguments switch
                    {
                        "install --exact --id \"Python.Python.3.11\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" =>
                            new InstallerCommandResult(0, "Successfully installed Python.Python.3.11", string.Empty),
                        _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                    };

                    return Task.FromResult(result);
                },
                openWebUiInstallDirectoryResolver: () => installDirectory,
                python311ExecutableResolver: () => commands.Any(command => command.Arguments.Contains("\"Python.Python.3.11\"", StringComparison.Ordinal))
                    ? @"C:\Users\Test\AppData\Local\Programs\Python\Python311\python.exe"
                    : null);

            var results = await service.InstallPackagesAsync(["OpenWebUI.OpenWebUI"]);

            results.Should().ContainSingle();
            results[0].PackageId.Should().Be("OpenWebUI.OpenWebUI");
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Installed Open WebUI");
            commands.Select(command => command.Arguments).Should().Contain(
                "install --exact --id \"Python.Python.3.11\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
            commands.Should().Contain(command =>
                command.FileName.Equals(@"C:\Users\Test\AppData\Local\Programs\Python\Python311\python.exe", StringComparison.OrdinalIgnoreCase) &&
                command.Arguments.StartsWith("-m venv ", StringComparison.Ordinal));
            commands.Should().Contain(command =>
                command.FileName.Equals(venvPythonPath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(command.Arguments, "-m pip install open-webui", StringComparison.Ordinal));
            commands.Should().Contain(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase) &&
                command.Arguments.Contains("launch-open-webui-multitool.bat", StringComparison.OrdinalIgnoreCase));

            var launcherPath = Path.Combine(installDirectory, "launch-open-webui-multitool.bat");
            File.Exists(launcherPath).Should().BeTrue();
            var launcherContents = File.ReadAllText(launcherPath);
            launcherContents.Should().Contain($@"set ""DATA_DIR={Path.Combine(installDirectory, "data")}""");
            launcherContents.Should().Contain($@"""{openWebUiExecutablePath}"" serve");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldAutomateRyubingRyujinxSetup()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-ryujinx-ryubing-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                ryubingRyujinxInstallDirectoryResolver: () => installDirectory,
                ryubingRyujinxReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "ryujinx-1.3.3-win_x64.zip",
                    "https://git.ryujinx.app/api/v4/projects/1/packages/generic/Ryubing/1.3.3/ryujinx-1.3.3-win_x64.zip")),
                fileDownloader: (_, destinationPath, _) =>
                {
                    var tempSourceDirectory = CreateTemporaryDirectory();

                    try
                    {
                        File.WriteAllText(Path.Combine(tempSourceDirectory, "Ryujinx.exe"), string.Empty);
                        System.IO.Compression.ZipFile.CreateFromDirectory(tempSourceDirectory, destinationPath);
                    }
                    finally
                    {
                        if (Directory.Exists(tempSourceDirectory))
                        {
                            Directory.Delete(tempSourceDirectory, true);
                        }
                    }

                    return Task.CompletedTask;
                });

            var results = await service.InstallPackagesAsync(["Ryubing.Ryujinx"]);

            results.Should().ContainSingle();
            results[0].PackageId.Should().Be("Ryubing.Ryujinx");
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Installed Ryujinx (Ryubing)");
            commands.Should().ContainSingle(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase) &&
                command.Arguments.Contains("launch-ryujinx-ryubing-multitool.bat", StringComparison.OrdinalIgnoreCase));

            File.Exists(Path.Combine(installDirectory, "Ryujinx.exe")).Should().BeTrue();
            File.ReadAllText(Path.Combine(installDirectory, ".multitool-installed-release.txt"))
                .Should().Be("https://git.ryujinx.app/api/v4/projects/1/packages/generic/Ryubing/1.3.3/ryujinx-1.3.3-win_x64.zip");
            var launcherPath = Path.Combine(installDirectory, "launch-ryujinx-ryubing-multitool.bat");
            File.Exists(launcherPath).Should().BeTrue();
            var launcherContents = File.ReadAllText(launcherPath);
            launcherContents.Should().Contain(Path.Combine(installDirectory, "Ryujinx.exe"));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldAutomateAzaharSetup()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-azahar-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                azaharInstallDirectoryResolver: () => installDirectory,
                azaharReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "azahar-2124.3-windows-msys2.zip",
                    "https://github.com/azahar-emu/azahar/releases/download/2124.3/azahar-2124.3-windows-msys2.zip")),
                fileDownloader: (_, destinationPath, _) =>
                {
                    var tempSourceDirectory = CreateTemporaryDirectory();

                    try
                    {
                        File.WriteAllText(Path.Combine(tempSourceDirectory, "azahar.exe"), string.Empty);
                        System.IO.Compression.ZipFile.CreateFromDirectory(tempSourceDirectory, destinationPath);
                    }
                    finally
                    {
                        if (Directory.Exists(tempSourceDirectory))
                        {
                            Directory.Delete(tempSourceDirectory, true);
                        }
                    }

                    return Task.CompletedTask;
                });

            var results = await service.InstallPackagesAsync(["AzaharEmu.Azahar"]);

            results.Should().ContainSingle();
            results[0].PackageId.Should().Be("AzaharEmu.Azahar");
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Installed Lime3DS (Azahar)");
            commands.Should().ContainSingle(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase) &&
                command.Arguments.Contains("launch-azahar-multitool.bat", StringComparison.OrdinalIgnoreCase));

            File.Exists(Path.Combine(installDirectory, "azahar.exe")).Should().BeTrue();
            File.ReadAllText(Path.Combine(installDirectory, ".multitool-installed-release.txt"))
                .Should().Be("https://github.com/azahar-emu/azahar/releases/download/2124.3/azahar-2124.3-windows-msys2.zip");
            var launcherPath = Path.Combine(installDirectory, "launch-azahar-multitool.bat");
            File.Exists(launcherPath).Should().BeTrue();
            var launcherContents = File.ReadAllText(launcherPath);
            launcherContents.Should().Contain(Path.Combine(installDirectory, "azahar.exe"));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldAutomateRpcs3Setup()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (startInfo.FileName.Equals(@"C:\Program Files\7-Zip\7z.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.StartsWith("x -y ", StringComparison.Ordinal))
                    {
                        File.WriteAllText(Path.Combine(installDirectory, "rpcs3.exe"), string.Empty);
                        return Task.FromResult(new InstallerCommandResult(0, "Everything is Ok", string.Empty));
                    }

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-rpcs3-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

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
                        "install --exact --id \"Microsoft.VCRedist.2015+.x64\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" =>
                            new InstallerCommandResult(0, "Successfully installed Microsoft.VCRedist.2015+.x64", string.Empty),
                        "install --exact --id \"7zip.7zip\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" =>
                            new InstallerCommandResult(0, "Successfully installed 7zip.7zip", string.Empty),
                        _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                    };

                    return Task.FromResult(result);
                },
                rpcs3InstallDirectoryResolver: () => installDirectory,
                sevenZipExecutableResolver: () => commands.Any(command => command.Arguments.Contains("\"7zip.7zip\"", StringComparison.Ordinal))
                    ? @"C:\Program Files\7-Zip\7z.exe"
                    : null,
                rpcs3ReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "rpcs3-v0.0.40-18950-16277576_win64_msvc.7z",
                    "https://github.com/RPCS3/rpcs3-binaries-win/releases/download/build-123/rpcs3-v0.0.40-18950-16277576_win64_msvc.7z")),
                fileDownloader: (_, destinationPath, _) =>
                {
                    File.WriteAllText(destinationPath, "fake archive");
                    return Task.CompletedTask;
                });

            var results = await service.InstallPackagesAsync(["RPCS3.RPCS3"]);

            results.Should().ContainSingle();
            results[0].PackageId.Should().Be("RPCS3.RPCS3");
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Installed RPCS3");
            commands.Select(command => command.Arguments).Should().Contain(
                "install --exact --id \"Microsoft.VCRedist.2015+.x64\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
            commands.Select(command => command.Arguments).Should().Contain(
                "install --exact --id \"7zip.7zip\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
            commands.Should().Contain(command =>
                command.FileName.Equals(@"C:\Program Files\7-Zip\7z.exe", StringComparison.OrdinalIgnoreCase) &&
                command.Arguments.Contains("rpcs3-v0.0.40-18950-16277576_win64_msvc.7z", StringComparison.Ordinal));
            commands.Should().Contain(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase) &&
                command.Arguments.Contains("launch-rpcs3-multitool.bat", StringComparison.OrdinalIgnoreCase));

            var launcherPath = Path.Combine(installDirectory, "launch-rpcs3-multitool.bat");
            File.Exists(launcherPath).Should().BeTrue();
            File.ReadAllText(Path.Combine(installDirectory, ".multitool-installed-release.txt"))
                .Should().Be("https://github.com/RPCS3/rpcs3-binaries-win/releases/download/build-123/rpcs3-v0.0.40-18950-16277576_win64_msvc.7z");
            var launcherContents = File.ReadAllText(launcherPath);
            launcherContents.Should().Contain(@"start """" """);
            launcherContents.Should().Contain(Path.Combine(installDirectory, "rpcs3.exe"));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldSkipDetectedVisualCppDependencyForRpcs3()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (startInfo.Arguments == "list --accept-source-agreements --disable-interactivity")
                    {
                        return Task.FromResult(
                            new InstallerCommandResult(
                                0,
                                """
                                Name                           Id                              Version Source
                                --------------------------------------------------------------------------------
                                Visual C++ Redistributable     Microsoft.VCRedist.2015+.x64   14.42   winget
                                """,
                                string.Empty));
                    }

                    if (startInfo.Arguments == "list --upgrade-available --accept-source-agreements --disable-interactivity")
                    {
                        return Task.FromResult(
                            new InstallerCommandResult(
                                0,
                                """
                                Name  Id  Version  Available  Source
                                ------------------------------------
                                """,
                                string.Empty));
                    }

                    if (startInfo.FileName.Equals(@"C:\Program Files\7-Zip\7z.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.StartsWith("x -y ", StringComparison.Ordinal))
                    {
                        File.WriteAllText(Path.Combine(installDirectory, "rpcs3.exe"), string.Empty);
                        return Task.FromResult(new InstallerCommandResult(0, "Everything is Ok", string.Empty));
                    }

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-rpcs3-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                rpcs3InstallDirectoryResolver: () => installDirectory,
                sevenZipExecutableResolver: () => @"C:\Program Files\7-Zip\7z.exe",
                rpcs3ReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "rpcs3-v0.0.40-18950-16277576_win64_msvc.7z",
                    "https://github.com/RPCS3/rpcs3-binaries-win/releases/download/build-123/rpcs3-v0.0.40-18950-16277576_win64_msvc.7z")),
                fileDownloader: (_, destinationPath, _) =>
                {
                    File.WriteAllText(destinationPath, "fake archive");
                    return Task.CompletedTask;
                });

            var results = await service.InstallPackagesAsync(["RPCS3.RPCS3"]);

            results.Should().ContainSingle();
            results[0].Succeeded.Should().BeTrue();
            commands.Should().NotContain(command => command.Arguments.Contains("\"Microsoft.VCRedist.2015+.x64\"", StringComparison.Ordinal));
            commands.Should().Contain(command =>
                command.FileName.Equals(@"C:\Program Files\7-Zip\7z.exe", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldUpdateAutomatic1111FromExistingClone()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "webui-user.bat"), "@echo off");
            File.WriteAllText(Path.Combine(installDirectory, "webui.bat"), "@echo off");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (startInfo.FileName.Equals(@"C:\Program Files\Git\cmd\git.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains(" pull", StringComparison.Ordinal))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, "Updating 0123456..89abcde", string.Empty));
                    }

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-webui-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                automatic1111InstallDirectoryResolver: () => installDirectory,
                gitExecutableResolver: () => @"C:\Program Files\Git\cmd\git.exe",
                python310ExecutableResolver: () => @"C:\Users\Test\AppData\Local\Programs\Python\Python310\python.exe");

            var results = await service.UpgradePackagesAsync(["AUTOMATIC1111.StableDiffusionWebUI"]);

            results.Should().ContainSingle();
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Pulled the latest Stable Diffusion WebUI changes");
            commands.Should().Contain(command =>
                command.FileName.Equals(@"C:\Program Files\Git\cmd\git.exe", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains(" pull", StringComparison.Ordinal));
            commands.Should().Contain(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("launch-webui-multitool.bat", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldUpdateOpenWebUiInExistingVenv()
    {
        var installDirectory = CreateTemporaryDirectory();
        var venvPythonPath = Path.Combine(installDirectory, "venv", "Scripts", "python.exe");
        var openWebUiExecutablePath = Path.Combine(installDirectory, "venv", "Scripts", "open-webui.exe");
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(venvPythonPath)!);
            File.WriteAllText(venvPythonPath, string.Empty);
            File.WriteAllText(openWebUiExecutablePath, string.Empty);

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (startInfo.FileName.Equals(venvPythonPath, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(startInfo.Arguments, "-m pip install -U open-webui", StringComparison.Ordinal))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, "Successfully installed open-webui-0.6.6", string.Empty));
                    }

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-open-webui-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                openWebUiInstallDirectoryResolver: () => installDirectory);

            var results = await service.UpgradePackagesAsync(["OpenWebUI.OpenWebUI"]);

            results.Should().ContainSingle();
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Updated Open WebUI");
            commands.Should().Contain(command =>
                command.FileName.Equals(venvPythonPath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(command.Arguments, "-m pip install -U open-webui", StringComparison.Ordinal));
            commands.Should().Contain(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("launch-open-webui-multitool.bat", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldUpdateRyubingRyujinxInPlace()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "Ryujinx.exe"), string.Empty);
            File.WriteAllText(Path.Combine(installDirectory, ".multitool-installed-release.txt"), "old-release");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-ryujinx-ryubing-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                ryubingRyujinxInstallDirectoryResolver: () => installDirectory,
                ryubingRyujinxReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "ryujinx-1.3.4-win_x64.zip",
                    "https://git.ryujinx.app/api/v4/projects/1/packages/generic/Ryubing/1.3.4/ryujinx-1.3.4-win_x64.zip")),
                fileDownloader: (_, destinationPath, _) =>
                {
                    CreateZipArchiveWithFiles(destinationPath, ("Ryujinx.exe", string.Empty));
                    return Task.CompletedTask;
                });

            var results = await service.UpgradePackagesAsync(["Ryubing.Ryujinx"]);

            results.Should().ContainSingle();
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Updated Ryujinx (Ryubing)");
            File.ReadAllText(Path.Combine(installDirectory, ".multitool-installed-release.txt"))
                .Should().Be("https://git.ryujinx.app/api/v4/projects/1/packages/generic/Ryubing/1.3.4/ryujinx-1.3.4-win_x64.zip");
            commands.Should().ContainSingle(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("launch-ryujinx-ryubing-multitool.bat", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldUpdateAzaharInPlace()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "azahar.exe"), string.Empty);
            File.WriteAllText(Path.Combine(installDirectory, ".multitool-installed-release.txt"), "old-release");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-azahar-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                azaharInstallDirectoryResolver: () => installDirectory,
                azaharReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "azahar-2124.4-windows-msys2.zip",
                    "https://github.com/azahar-emu/azahar/releases/download/2124.4/azahar-2124.4-windows-msys2.zip")),
                fileDownloader: (_, destinationPath, _) =>
                {
                    CreateZipArchiveWithFiles(destinationPath, ("azahar.exe", string.Empty));
                    return Task.CompletedTask;
                });

            var results = await service.UpgradePackagesAsync(["AzaharEmu.Azahar"]);

            results.Should().ContainSingle();
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Updated Lime3DS (Azahar)");
            File.ReadAllText(Path.Combine(installDirectory, ".multitool-installed-release.txt"))
                .Should().Be("https://github.com/azahar-emu/azahar/releases/download/2124.4/azahar-2124.4-windows-msys2.zip");
            commands.Should().ContainSingle(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("launch-azahar-multitool.bat", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldUpdateMacriumReflectSilentlyWithLatestInstaller()
    {
        var workingDirectory = CreateTemporaryDirectory();
        var installedExecutablePath = Path.Combine(workingDirectory, "ReflectBin.exe");
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            File.WriteAllText(installedExecutablePath, string.Empty);

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("reflect_home_setup_x64.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("/qn", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("/norestart", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("macrium-reflect-update.log", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                macriumReflectWorkingDirectoryResolver: () => workingDirectory,
                macriumReflectExecutableResolver: () => installedExecutablePath,
                macriumReflectReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "reflect_home_setup_x64.exe",
                    "https://download.macrium.com/reflect/v10/v10.0.8751/reflect_home_setup_x64.exe")),
                fileDownloader: (_, destinationPath, _) =>
                {
                    File.WriteAllText(destinationPath, "fake installer");
                    return Task.CompletedTask;
                });

            var results = await service.UpgradePackagesAsync(["Macrium.Reflect"]);

            results.Should().ContainSingle();
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Be("Updated Macrium Reflect silently with the latest installer.");
            File.Exists(Path.Combine(workingDirectory, "downloads", "reflect_home_setup_x64.exe")).Should().BeTrue();
            File.ReadAllText(Path.Combine(workingDirectory, ".multitool-installed-release.txt"))
                .Should().Be("https://download.macrium.com/reflect/v10/v10.0.8751/reflect_home_setup_x64.exe");
            commands.Should().ContainSingle(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("reflect_home_setup_x64.exe", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("/qn", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("/norestart", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("macrium-reflect-update.log", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }
        }
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldSkipMacriumReflectWhenLatestInstallerMarkerMatches()
    {
        var workingDirectory = CreateTemporaryDirectory();
        var installedExecutablePath = Path.Combine(workingDirectory, "ReflectBin.exe");

        try
        {
            File.WriteAllText(installedExecutablePath, string.Empty);
            File.WriteAllText(
                Path.Combine(workingDirectory, ".multitool-installed-release.txt"),
                "https://download.macrium.com/reflect/v10/v10.0.8751/reflect_home_setup_x64.exe");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                macriumReflectWorkingDirectoryResolver: () => workingDirectory,
                macriumReflectExecutableResolver: () => installedExecutablePath,
                macriumReflectReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "reflect_home_setup_x64.exe",
                    "https://download.macrium.com/reflect/v10/v10.0.8751/reflect_home_setup_x64.exe")),
                fileDownloader: (_, _, _) => throw new InvalidOperationException("The installer should not be downloaded when the latest release marker already matches."));

            var results = await service.UpgradePackagesAsync(["Macrium.Reflect"]);

            results.Should().ContainSingle();
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeFalse();
            results[0].Message.Should().Be("Already up to date.");
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }
        }
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldUpdateRpcs3InPlace()
    {
        var installDirectory = CreateTemporaryDirectory();
        var commands = new List<(string FileName, string Arguments)>();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "rpcs3.exe"), string.Empty);
            File.WriteAllText(Path.Combine(installDirectory, ".multitool-installed-release.txt"), "old-release");

            var service = new WindowsWingetInstallerService(
                (startInfo, _) =>
                {
                    commands.Add((startInfo.FileName, startInfo.Arguments));

                    if (startInfo.FileName.Equals(@"C:\Program Files\7-Zip\7z.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.StartsWith("x -y ", StringComparison.Ordinal))
                    {
                        File.WriteAllText(Path.Combine(installDirectory, "rpcs3.exe"), string.Empty);
                        return Task.FromResult(new InstallerCommandResult(0, "Everything is Ok", string.Empty));
                    }

                    if (string.Equals(startInfo.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                        && startInfo.Arguments.Contains("launch-rpcs3-multitool.bat", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(new InstallerCommandResult(0, string.Empty, string.Empty));
                    }

                    throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}");
                },
                rpcs3InstallDirectoryResolver: () => installDirectory,
                sevenZipExecutableResolver: () => @"C:\Program Files\7-Zip\7z.exe",
                rpcs3ReleaseResolver: _ => Task.FromResult<InstallerReleaseAsset?>(new InstallerReleaseAsset(
                    "rpcs3-v0.0.41-19000-abcdef12_win64_msvc.7z",
                    "https://github.com/RPCS3/rpcs3-binaries-win/releases/download/build-124/rpcs3-v0.0.41-19000-abcdef12_win64_msvc.7z")),
                fileDownloader: (_, destinationPath, _) =>
                {
                    File.WriteAllText(destinationPath, "fake archive");
                    return Task.CompletedTask;
                });

            var results = await service.UpgradePackagesAsync(["RPCS3.RPCS3"]);

            results.Should().ContainSingle();
            results[0].Succeeded.Should().BeTrue();
            results[0].Changed.Should().BeTrue();
            results[0].Message.Should().Contain("Updated RPCS3");
            File.ReadAllText(Path.Combine(installDirectory, ".multitool-installed-release.txt"))
                .Should().Be("https://github.com/RPCS3/rpcs3-binaries-win/releases/download/build-124/rpcs3-v0.0.41-19000-abcdef12_win64_msvc.7z");
            commands.Should().Contain(command =>
                command.FileName.Equals(@"C:\Program Files\7-Zip\7z.exe", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("rpcs3-v0.0.41-19000-abcdef12_win64_msvc.7z", StringComparison.Ordinal));
            commands.Should().Contain(command =>
                string.Equals(command.FileName, "cmd.exe", StringComparison.OrdinalIgnoreCase)
                && command.Arguments.Contains("launch-rpcs3-multitool.bat", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(installDirectory))
            {
                Directory.Delete(installDirectory, true);
            }
        }
    }

    private static InstallerCommandRunner CreateRunner() =>
        (startInfo, _) =>
        {
            var result = startInfo.Arguments switch
            {
                "--version" => new InstallerCommandResult(0, "v1.28.220", string.Empty),
                "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                    0,
                    """
                    Failed in attempting to update the source: winget
                    Name             Id               Version Source
                    ------------------------------------------------
                    Git              Git.Git          2.53.0  winget
                    Mozilla Firefox  Mozilla.Firefox  136.0   winget
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
                "install --exact --id \"Microsoft.VisualStudioCode\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(
                    1,
                    """
                    Found an existing package already installed. Trying to upgrade the installed package...
                    No available upgrade found.
                    No newer package versions are available from the configured sources.
                    """,
                    string.Empty),
                "upgrade --exact --id \"Roblox.Roblox\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(
                    1,
                    "No installed package found matching input criteria.",
                    string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
            };

            return Task.FromResult(result);
        };

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "MultiTool.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CreateZipArchiveWithFiles(string destinationPath, params (string RelativePath, string Contents)[] files)
    {
        var tempSourceDirectory = CreateTemporaryDirectory();

        try
        {
            foreach (var (relativePath, contents) in files)
            {
                var fullPath = Path.Combine(tempSourceDirectory, relativePath);
                var parentDirectory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                File.WriteAllText(fullPath, contents);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(tempSourceDirectory, destinationPath);
        }
        finally
        {
            if (Directory.Exists(tempSourceDirectory))
            {
                Directory.Delete(tempSourceDirectory, true);
            }
        }
    }
}
