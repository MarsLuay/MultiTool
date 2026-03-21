using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;


public sealed partial class WindowsWingetInstallerServiceTests
{
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
}
