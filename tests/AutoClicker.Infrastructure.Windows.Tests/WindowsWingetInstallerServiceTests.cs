using AutoClicker.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

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
                CreateRunner(),
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
    public async Task GetPackageStatusesAsync_ShouldMarkRyubingRyujinxAsInstalledWhenExecutableExists()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "Ryujinx.exe"), string.Empty);

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                ryubingRyujinxInstallDirectoryResolver: () => installDirectory);

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
                azaharInstallDirectoryResolver: () => installDirectory);

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

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                macriumReflectExecutableResolver: () => executablePath);

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
    public async Task GetPackageStatusesAsync_ShouldMarkRpcs3AsInstalledWhenExecutableExists()
    {
        var installDirectory = CreateTemporaryDirectory();

        try
        {
            Directory.CreateDirectory(installDirectory);
            File.WriteAllText(Path.Combine(installDirectory, "rpcs3.exe"), string.Empty);

            var service = new WindowsWingetInstallerService(
                CreateRunner(),
                rpcs3InstallDirectoryResolver: () => installDirectory);

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
    public async Task InstallPackagesAsync_ShouldInstallDiscordBeforeVencord()
    {
        var commands = new List<string>();
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                commands.Add(startInfo.Arguments);
                var packageId = startInfo.Arguments.Contains("Vendicated.Vencord", StringComparison.Ordinal)
                    ? "Vendicated.Vencord"
                    : "Discord.Discord";
                return Task.FromResult(
                    new InstallerCommandResult(
                        0,
                        $"Successfully installed {packageId}",
                        string.Empty));
            });

        var results = await service.InstallPackagesAsync(["Vendicated.Vencord"]);

        commands.Should().ContainInOrder(
            "install --exact --id \"Discord.Discord\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent",
            "install --exact --id \"Vendicated.Vencord\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
        results.Select(result => result.PackageId).Should().ContainInOrder("Discord.Discord", "Vendicated.Vencord");
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
    public async Task UpgradePackagesAsync_ShouldOpenGuidedUpdatePageForExternalEntries()
    {
        var openedTargets = new List<string>();
        var service = new WindowsWingetInstallerService(
            CreateRunner(),
            (target, _) =>
            {
                openedTargets.Add(target);
                return Task.CompletedTask;
            });

        var results = await service.UpgradePackagesAsync(["AUTOMATIC1111.StableDiffusionWebUI"]);

        openedTargets.Should().ContainSingle()
            .Which.Should().Be("https://github.com/AUTOMATIC1111/stable-diffusion-webui");
        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
        results[0].Message.Should().Be("Opened the official update page.");
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
        var path = Path.Combine(Path.GetTempPath(), "AutoClicker.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
