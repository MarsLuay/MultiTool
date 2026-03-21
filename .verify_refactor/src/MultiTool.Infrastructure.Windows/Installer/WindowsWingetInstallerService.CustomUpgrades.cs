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
    private async Task<InstallerOperationResult> UpgradeAutomatic1111Async(InstallerCatalogItem package, CancellationToken cancellationToken)
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
        if (!File.Exists(webUiBatchPath) || !File.Exists(webUiCoreBatchPath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
        }

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

        var launcherPath = Path.Combine(installDirectory, Automatic1111LauncherFileName);
        await WriteAutomatic1111LauncherAsync(
            launcherPath,
            installDirectory,
            pythonReady.ExecutablePath,
            gitReady.ExecutablePath,
            cancellationToken).ConfigureAwait(false);

        var pullResult = await commandRunner(
            CreateCommandProcessStartInfo(
                gitReady.ExecutablePath,
                $"-C {QuoteArgument(installDirectory)} pull"),
            cancellationToken).ConfigureAwait(false);
        var pullOutput = NormalizeOutput(pullResult);
        if (pullResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(pullOutput, "Stable Diffusion WebUI could not be updated."),
                pullOutput);
        }

        if (Contains(pullOutput, "Already up to date.") || Contains(pullOutput, "Already up to date"))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                pullOutput);
        }

        var launchResult = await LaunchAutomatic1111Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = $"{pullOutput}{Environment.NewLine}{launchResult.Output}".Trim(),
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Pulled the latest Stable Diffusion WebUI changes in {installDirectory} and launched it.",
            $"{pullOutput}{Environment.NewLine}{launchResult.Output}".Trim());
    }

    private async Task<InstallerOperationResult> UpgradeOpenWebUiAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
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
        if (!File.Exists(venvPythonPath) || !File.Exists(openWebUiExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
        }

        var installResult = await commandRunner(
            CreateCommandProcessStartInfo(
                venvPythonPath,
                "-m pip install -U open-webui"),
            cancellationToken).ConfigureAwait(false);
        var installOutput = NormalizeOutput(installResult);
        if (installResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(installOutput, "Open WebUI could not be updated."),
                installOutput);
        }

        if (!File.Exists(openWebUiExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Open WebUI updated, but its launcher executable could not be located automatically.",
                venvDirectory);
        }

        var dataDirectory = Path.Combine(installDirectory, "data");
        Directory.CreateDirectory(dataDirectory);
        var launcherPath = Path.Combine(installDirectory, OpenWebUiLauncherFileName);
        await WriteOpenWebUiLauncherAsync(
            launcherPath,
            installDirectory,
            dataDirectory,
            openWebUiExecutablePath,
            cancellationToken).ConfigureAwait(false);

        if (Contains(installOutput, "Requirement already satisfied"))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                installOutput);
        }

        var launchResult = await LaunchOpenWebUiAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = $"{installOutput}{Environment.NewLine}{launchResult.Output}".Trim(),
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Updated Open WebUI in {installDirectory} and launched the local server.",
            $"{installOutput}{Environment.NewLine}{launchResult.Output}".Trim());
    }

    private async Task<InstallerOperationResult> UpgradeRyubingRyujinxAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(ResolveRyubingRyujinxExecutablePath(installDirectory)))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
        }

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

        var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
        if (string.Equals(ReadInstalledReleaseMarker(installDirectory), releaseMarker, StringComparison.OrdinalIgnoreCase))
        {
            var launcherPath = Path.Combine(installDirectory, RyubingRyujinxLauncherFileName);
            var executablePath = ResolveRyubingRyujinxExecutablePath(installDirectory);
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                await WriteRyubingRyujinxLauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);
            }

            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                releaseMarker);
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

        var updatedExecutablePath = ResolveRyubingRyujinxExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(updatedExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Ryujinx (Ryubing) was updated, but Ryujinx.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);

        var updatedLauncherPath = Path.Combine(installDirectory, RyubingRyujinxLauncherFileName);
        await WriteRyubingRyujinxLauncherAsync(updatedLauncherPath, updatedExecutablePath, cancellationToken).ConfigureAwait(false);

        var launchResult = await LaunchRyubingRyujinxAsync(updatedLauncherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
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
            $"Updated Ryujinx (Ryubing) in {installDirectory} and launched it.",
            archivePath);
    }

    private async Task<InstallerOperationResult> UpgradeAzaharAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(ResolveAzaharExecutablePath(installDirectory)))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
        }

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

        var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
        if (string.Equals(ReadInstalledReleaseMarker(installDirectory), releaseMarker, StringComparison.OrdinalIgnoreCase))
        {
            var launcherPath = Path.Combine(installDirectory, AzaharLauncherFileName);
            var executablePath = ResolveAzaharExecutablePath(installDirectory);
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                await WriteAzaharLauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);
            }

            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                releaseMarker);
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

        var updatedExecutablePath = ResolveAzaharExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(updatedExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Lime3DS (Azahar) was updated, but azahar.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);

        var updatedLauncherPath = Path.Combine(installDirectory, AzaharLauncherFileName);
        await WriteAzaharLauncherAsync(updatedLauncherPath, updatedExecutablePath, cancellationToken).ConfigureAwait(false);

        var launchResult = await LaunchAzaharAsync(updatedLauncherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
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
            $"Updated Lime3DS (Azahar) in {installDirectory} and launched it.",
            archivePath);
    }

    private async Task<InstallerOperationResult> UpgradeMacriumReflectAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(macriumReflectExecutableResolver()))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                workingDirectory);
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

        var releaseMarker = BuildInstalledReleaseMarkerValue(installerAsset);
        if (string.Equals(ReadInstalledReleaseMarker(workingDirectory), releaseMarker, StringComparison.OrdinalIgnoreCase))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                releaseMarker);
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

        var installResult = await RunMacriumReflectInstallerSilentlyAsync(installerPath, workingDirectory, cancellationToken).ConfigureAwait(false);
        if (!installResult.Succeeded)
        {
            return installResult;
        }

        await WriteInstalledReleaseMarkerAsync(workingDirectory, installerAsset, cancellationToken).ConfigureAwait(false);
        return installResult;
    }

    private async Task<InstallerOperationResult> UpgradeRpcs3Async(InstallerCatalogItem package, CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(ResolveRpcs3ExecutablePath(installDirectory)))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
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

        var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
        if (string.Equals(ReadInstalledReleaseMarker(installDirectory), releaseMarker, StringComparison.OrdinalIgnoreCase))
        {
            var existingLauncherPath = Path.Combine(installDirectory, Rpcs3LauncherFileName);
            var executablePath = ResolveRpcs3ExecutablePath(installDirectory);
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                await WriteRpcs3LauncherAsync(existingLauncherPath, executablePath, cancellationToken).ConfigureAwait(false);
            }

            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                releaseMarker);
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

        var updatedExecutablePath = ResolveRpcs3ExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(updatedExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "RPCS3 was updated, but rpcs3.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);

        var launcherPath = Path.Combine(installDirectory, Rpcs3LauncherFileName);
        await WriteRpcs3LauncherAsync(launcherPath, updatedExecutablePath, cancellationToken).ConfigureAwait(false);

        var launchResult = await LaunchRpcs3Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
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
            $"Updated RPCS3 in {installDirectory} and launched it.",
            extractOutput);
    }

}
