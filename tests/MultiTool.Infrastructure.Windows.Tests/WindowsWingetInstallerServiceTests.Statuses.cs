using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;


public sealed partial class WindowsWingetInstallerServiceTests
{
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
    public async Task GetPackageStatusesAsync_WhenUpdateCheckIsDeferred_ShouldSkipUpgradeQuery()
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
                        Name             Id               Version Source
                        ------------------------------------------------
                        Git              Git.Git          2.53.0  winget
                        Mozilla Firefox  Mozilla.Firefox  136.0   winget
                        """,
                        string.Empty),
                    _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
                };

                return Task.FromResult(result);
            });

        var statuses = await service.GetPackageStatusesAsync(
            ["Git.Git", "Mozilla.Firefox"],
            includeUpdateCheck: false);

        commands.Should().ContainSingle().Which.Should().Be("list --accept-source-agreements --disable-interactivity");
        statuses.Should().ContainEquivalentOf(new { PackageId = "Git.Git", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "Mozilla.Firefox", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed" });
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
}
