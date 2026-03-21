using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;


public sealed partial class WindowsWingetInstallerServiceTests
{
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
}
