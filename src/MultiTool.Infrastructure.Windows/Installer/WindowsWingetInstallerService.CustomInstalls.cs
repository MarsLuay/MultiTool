using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using Microsoft.Win32;

namespace MultiTool.Infrastructure.Windows.Installer;

public sealed partial class WindowsWingetInstallerService : IInstallerService
{
    private async Task<InstallerOperationResult> InstallAutomatic1111Async(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = automatic1111InstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Stable Diffusion WebUI is invalid.",
                string.Empty);
        }

        var webUiBatchPath = Path.Combine(installDirectory, "webui-user.bat");
        var webUiCoreBatchPath = Path.Combine(installDirectory, "webui.bat");
        var launcherPath = Path.Combine(installDirectory, Automatic1111LauncherFileName);

        var gitReady = await EnsureExecutableAsync(
            catalogById["Git.Git"],
            package.DisplayName,
            gitExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!gitReady.Succeeded || string.IsNullOrWhiteSpace(gitReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                gitReady.Message,
                gitReady.Output);
        }

        var pythonReady = await EnsureExecutableAsync(
            Automatic1111PythonPackage,
            package.DisplayName,
            python310ExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!pythonReady.Succeeded || string.IsNullOrWhiteSpace(pythonReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                pythonReady.Message,
                pythonReady.Output);
        }

        if (File.Exists(webUiBatchPath) && File.Exists(webUiCoreBatchPath))
        {
            await WriteAutomatic1111LauncherAsync(
                launcherPath,
                installDirectory,
                pythonReady.ExecutablePath,
                gitReady.ExecutablePath,
                cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchAutomatic1111Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        if (Directory.Exists(installDirectory))
        {
            if (Directory.EnumerateFileSystemEntries(installDirectory).Any())
            {
                return new InstallerOperationResult(
                    package.PackageId,
                    package.DisplayName,
                    false,
                    false,
                    $"The install folder already exists and is not empty: {installDirectory}",
                    installDirectory);
            }

            Directory.Delete(installDirectory, recursive: false);
        }

        var parentDirectory = Path.GetDirectoryName(installDirectory);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Stable Diffusion WebUI is invalid.",
                installDirectory);
        }

        Directory.CreateDirectory(parentDirectory);

        var cloneResult = await commandRunner(
            CreateCommandProcessStartInfo(
                gitReady.ExecutablePath,
                $"clone {QuoteArgument(Automatic1111RepositoryUrl)} {QuoteArgument(installDirectory)}"),
            cancellationToken).ConfigureAwait(false);
        var cloneOutput = NormalizeOutput(cloneResult);
        if (cloneResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(cloneOutput, "Git clone failed."),
                cloneOutput);
        }

        await WriteAutomatic1111LauncherAsync(
            launcherPath,
            installDirectory,
            pythonReady.ExecutablePath,
            gitReady.ExecutablePath,
            cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchAutomatic1111Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = $"{cloneOutput}{Environment.NewLine}{launchSetupResult.Output}".Trim(),
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Cloned the official repo to {installDirectory} and launched first-run setup.",
            cloneOutput);
    }

    private async Task<InstallerOperationResult> InstallOpenWebUiAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = openWebUiInstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Open WebUI is invalid.",
                string.Empty);
        }

        var venvDirectory = Path.Combine(installDirectory, "venv");
        var venvPythonPath = Path.Combine(venvDirectory, "Scripts", "python.exe");
        var openWebUiExecutablePath = Path.Combine(venvDirectory, "Scripts", "open-webui.exe");
        var dataDirectory = Path.Combine(installDirectory, "data");
        var launcherPath = Path.Combine(installDirectory, OpenWebUiLauncherFileName);

        var pythonReady = await EnsureExecutableAsync(
            OpenWebUiPythonPackage,
            package.DisplayName,
            python311ExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!pythonReady.Succeeded || string.IsNullOrWhiteSpace(pythonReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                pythonReady.Message,
                pythonReady.Output);
        }

        if (File.Exists(venvPythonPath) && File.Exists(openWebUiExecutablePath))
        {
            Directory.CreateDirectory(dataDirectory);
            await WriteOpenWebUiLauncherAsync(
                launcherPath,
                installDirectory,
                dataDirectory,
                openWebUiExecutablePath,
                cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchOpenWebUiAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        if (Directory.Exists(installDirectory) && Directory.EnumerateFileSystemEntries(installDirectory).Any())
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"The install folder already exists and is not empty: {installDirectory}",
                installDirectory);
        }

        Directory.CreateDirectory(installDirectory);

        var createVenvResult = await commandRunner(
            CreateCommandProcessStartInfo(
                pythonReady.ExecutablePath,
                $"-m venv {QuoteArgument(venvDirectory)}"),
            cancellationToken).ConfigureAwait(false);
        var createVenvOutput = NormalizeOutput(createVenvResult);
        if (createVenvResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(createVenvOutput, "Python could not create the Open WebUI virtual environment."),
                createVenvOutput);
        }

        if (!File.Exists(venvPythonPath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Python created the environment, but the virtual environment executable is missing.",
                venvDirectory);
        }

        var installResult = await commandRunner(
            CreateCommandProcessStartInfo(
                venvPythonPath,
                "-m pip install open-webui"),
            cancellationToken).ConfigureAwait(false);
        var installOutput = NormalizeOutput(installResult);
        if (installResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(installOutput, "Open WebUI could not be installed into the virtual environment."),
                installOutput);
        }

        if (!File.Exists(openWebUiExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Open WebUI installed, but its launcher executable could not be located automatically.",
                venvDirectory);
        }

        Directory.CreateDirectory(dataDirectory);
        await WriteOpenWebUiLauncherAsync(
            launcherPath,
            installDirectory,
            dataDirectory,
            openWebUiExecutablePath,
            cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchOpenWebUiAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = $"{createVenvOutput}{Environment.NewLine}{installOutput}{Environment.NewLine}{launchSetupResult.Output}".Trim(),
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Installed Open WebUI into {installDirectory} and launched the local server.",
            $"{createVenvOutput}{Environment.NewLine}{installOutput}".Trim());
    }

    private async Task<InstallerOperationResult> InstallRyubingRyujinxAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = ryubingRyujinxInstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Ryujinx (Ryubing) is invalid.",
                string.Empty);
        }

        var launcherPath = Path.Combine(installDirectory, RyubingRyujinxLauncherFileName);
        var existingExecutablePath = ResolveRyubingRyujinxExecutablePath(installDirectory);
        if (!string.IsNullOrWhiteSpace(existingExecutablePath))
        {
            await WriteRyubingRyujinxLauncherAsync(launcherPath, existingExecutablePath, cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchRyubingRyujinxAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        if (Directory.Exists(installDirectory) && Directory.EnumerateFileSystemEntries(installDirectory).Any())
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"The install folder already exists and is not empty: {installDirectory}",
                installDirectory);
        }

        Directory.CreateDirectory(installDirectory);

        InstallerReleaseAsset? releaseAsset;
        try
        {
            releaseAsset = await ryubingRyujinxReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Ryujinx (Ryubing) release lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest Ryujinx (Ryubing) Windows build could not be resolved automatically.",
                string.Empty);
        }

        var downloadsDirectory = Path.Combine(installDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var archivePath = Path.Combine(downloadsDirectory, releaseAsset.FileName);

        try
        {
            await fileDownloader(releaseAsset.DownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Ryujinx (Ryubing) download failed: {ex.Message}",
                ex.ToString());
        }

        try
        {
            ZipFile.ExtractToDirectory(archivePath, installDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Ryujinx (Ryubing) could not be extracted: {ex.Message}",
                ex.ToString());
        }

        var executablePath = ResolveRyubingRyujinxExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Ryujinx (Ryubing) was extracted, but Ryujinx.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);
        await WriteRyubingRyujinxLauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchRyubingRyujinxAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = archivePath,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Installed Ryujinx (Ryubing) into {installDirectory} and launched it.",
            archivePath);
    }

    private async Task<InstallerOperationResult> InstallAzaharAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = azaharInstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Lime3DS (Azahar) is invalid.",
                string.Empty);
        }

        var launcherPath = Path.Combine(installDirectory, AzaharLauncherFileName);
        var existingExecutablePath = ResolveAzaharExecutablePath(installDirectory);
        if (!string.IsNullOrWhiteSpace(existingExecutablePath))
        {
            await WriteAzaharLauncherAsync(launcherPath, existingExecutablePath, cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchAzaharAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        if (Directory.Exists(installDirectory) && Directory.EnumerateFileSystemEntries(installDirectory).Any())
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"The install folder already exists and is not empty: {installDirectory}",
                installDirectory);
        }

        Directory.CreateDirectory(installDirectory);

        InstallerReleaseAsset? releaseAsset;
        try
        {
            releaseAsset = await azaharReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Lime3DS (Azahar) release lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest Lime3DS (Azahar) Windows build could not be resolved automatically.",
                string.Empty);
        }

        var downloadsDirectory = Path.Combine(installDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var archivePath = Path.Combine(downloadsDirectory, releaseAsset.FileName);

        try
        {
            await fileDownloader(releaseAsset.DownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Lime3DS (Azahar) download failed: {ex.Message}",
                ex.ToString());
        }

        try
        {
            ZipFile.ExtractToDirectory(archivePath, installDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Lime3DS (Azahar) could not be extracted: {ex.Message}",
                ex.ToString());
        }

        var executablePath = ResolveAzaharExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Lime3DS (Azahar) was extracted, but azahar.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);
        await WriteAzaharLauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchAzaharAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = archivePath,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Installed Lime3DS (Azahar) into {installDirectory} and launched it.",
            archivePath);
    }

    private async Task<InstallerOperationResult> InstallMacriumReflectAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var workingDirectory = macriumReflectWorkingDirectoryResolver();
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The working directory for Macrium Reflect is invalid.",
                string.Empty);
        }

        var launcherPath = Path.Combine(workingDirectory, MacriumReflectLauncherFileName);
        var installedExecutablePath = macriumReflectExecutableResolver();
        if (!string.IsNullOrWhiteSpace(installedExecutablePath))
        {
            await WriteMacriumReflectLauncherAsync(launcherPath, installedExecutablePath, cancellationToken).ConfigureAwait(false);
            var existingLaunchResult = await LaunchMacriumReflectAsync(launcherPath, workingDirectory, cancellationToken).ConfigureAwait(false);
            return existingLaunchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        Directory.CreateDirectory(workingDirectory);

        InstallerReleaseAsset? installerAsset;
        try
        {
            installerAsset = await macriumReflectReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Macrium Reflect installer lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (installerAsset is null || string.IsNullOrWhiteSpace(installerAsset.FileName) || string.IsNullOrWhiteSpace(installerAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest Macrium Reflect installer could not be resolved automatically.",
                string.Empty);
        }

        var downloadsDirectory = Path.Combine(workingDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var installerPath = Path.Combine(downloadsDirectory, installerAsset.FileName);

        try
        {
            await fileDownloader(installerAsset.DownloadUrl, installerPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Macrium Reflect download failed: {ex.Message}",
                ex.ToString());
        }

        await WriteMacriumReflectLauncherAsync(launcherPath, installerPath, cancellationToken).ConfigureAwait(false);

        var launchResult = await LaunchMacriumReflectAsync(launcherPath, workingDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = installerPath,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Downloaded the latest Macrium Reflect installer to {installerPath} and launched it.",
            installerPath);
    }

    private async Task<InstallerOperationResult> InstallRpcs3Async(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = rpcs3InstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for RPCS3 is invalid.",
                string.Empty);
        }

        var launcherPath = Path.Combine(installDirectory, Rpcs3LauncherFileName);
        var existingExecutablePath = ResolveRpcs3ExecutablePath(installDirectory);
        if (!string.IsNullOrWhiteSpace(existingExecutablePath))
        {
            await WriteRpcs3LauncherAsync(launcherPath, existingExecutablePath, cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchRpcs3Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        var visualCppReady = await EnsurePackageInstalledAsync(
            Rpcs3VisualCppPackage,
            package.DisplayName,
            cancellationToken).ConfigureAwait(false);
        if (!visualCppReady.Succeeded)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                visualCppReady.Message,
                visualCppReady.Output);
        }

        var sevenZipReady = await EnsureExecutableAsync(
            SevenZipPackage,
            package.DisplayName,
            sevenZipExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!sevenZipReady.Succeeded || string.IsNullOrWhiteSpace(sevenZipReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                sevenZipReady.Message,
                sevenZipReady.Output);
        }

        if (Directory.Exists(installDirectory) && Directory.EnumerateFileSystemEntries(installDirectory).Any())
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"The install folder already exists and is not empty: {installDirectory}",
                installDirectory);
        }

        Directory.CreateDirectory(installDirectory);

        InstallerReleaseAsset? releaseAsset;
        try
        {
            releaseAsset = await rpcs3ReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"RPCS3 release lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest RPCS3 Windows build could not be resolved automatically.",
                string.Empty);
        }

        var downloadsDirectory = Path.Combine(installDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var archivePath = Path.Combine(downloadsDirectory, releaseAsset.FileName);

        try
        {
            await fileDownloader(releaseAsset.DownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"RPCS3 download failed: {ex.Message}",
                ex.ToString());
        }

        var extractResult = await commandRunner(
            CreateCommandProcessStartInfo(
                sevenZipReady.ExecutablePath,
                $"x -y {QuoteArgument(archivePath)} -o{QuoteArgument(installDirectory)}"),
            cancellationToken).ConfigureAwait(false);
        var extractOutput = NormalizeOutput(extractResult);
        if (extractResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(extractOutput, "RPCS3 could not be extracted."),
                extractOutput);
        }

        var executablePath = ResolveRpcs3ExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "RPCS3 was extracted, but rpcs3.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);
        await WriteRpcs3LauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchRpcs3Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = extractOutput,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Installed RPCS3 into {installDirectory} and launched it.",
            extractOutput);
    }

}
