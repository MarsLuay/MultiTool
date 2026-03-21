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
    private async Task<IReadOnlyList<InstallerOperationResult>> RunBatchAsync(
        IReadOnlyList<string> packageIds,
        Func<InstallerCatalogItem, CancellationToken, Task<InstallerOperationResult>> executePackageAsync,
        InstallerPackageAction action,
        CancellationToken cancellationToken)
    {
        if (packageIds.Count == 0)
        {
            return [];
        }

        var results = new List<InstallerOperationResult>(packageIds.Count);

        foreach (var packageId in packageIds)
        {
            if (!catalogById.TryGetValue(packageId, out var package))
            {
                results.Add(
                    new InstallerOperationResult(
                        packageId,
                        packageId,
                        false,
                        false,
                        "This package is not in the current installer catalog.",
                        string.Empty));
                continue;
            }

            try
            {
                results.Add(ApplyGuidance(package, action, await executePackageAsync(package, cancellationToken).ConfigureAwait(false)));
            }
            catch (Exception ex)
            {
                results.Add(
                    ApplyGuidance(
                        package,
                        action,
                        new InstallerOperationResult(
                        package.PackageId,
                        package.DisplayName,
                        false,
                        false,
                        ex.Message,
                        ex.ToString())));
            }
        }

        return results;
    }

    private async Task<InstallerOperationResult> InstallPackageAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        if (IsSpotifyPackage(package))
        {
            return await InstallSpotifyAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsAutomatic1111Package(package))
        {
            return await InstallAutomatic1111Async(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsOpenWebUiPackage(package))
        {
            return await InstallOpenWebUiAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsRyubingRyujinxPackage(package))
        {
            return await InstallRyubingRyujinxAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsAzaharPackage(package))
        {
            return await InstallAzaharAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsMacriumReflectPackage(package))
        {
            return await InstallMacriumReflectAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsRpcs3Package(package))
        {
            return await InstallRpcs3Async(package, cancellationToken).ConfigureAwait(false);
        }

        if (UsesGuidedInstall(package))
        {
            return await LaunchGuidedPackageAsync(
                package,
                package.InstallUrl,
                "Opened the official install page.",
                cancellationToken).ConfigureAwait(false);
        }

        var result = await RunWingetAsync(BuildPackageCommand("install", package), cancellationToken).ConfigureAwait(false);
        return InterpretInstallResult(package, result);
    }

    private async Task<InstallerOperationResult> InstallSpotifyAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var wingetResult = await RunWingetAsync(BuildPackageCommand("install", package), cancellationToken).ConfigureAwait(false);
        var interpretedResult = InterpretInstallResult(package, wingetResult);
        if (interpretedResult.Succeeded)
        {
            return interpretedResult;
        }

        return await RunSpotifyInstallerFallbackAsync(package, interpretedResult, isUpgrade: false, cancellationToken).ConfigureAwait(false);
    }

    private static InstallerOperationResult CreateSkippedInstallResult(InstallerCatalogItem package, InstallerPackageStatus status)
    {
        var message = status.HasUpdateAvailable
            ? "Already installed. Use Update to upgrade."
            : "Already installed. Skipped.";

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            false,
            message,
            status.StatusText);
    }

    private async Task<InstallerOperationResult> UpgradePackageAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        if (IsSpotifyPackage(package))
        {
            return await UpgradeSpotifyAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsAutomatic1111Package(package))
        {
            return await UpgradeAutomatic1111Async(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsOpenWebUiPackage(package))
        {
            return await UpgradeOpenWebUiAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsRyubingRyujinxPackage(package))
        {
            return await UpgradeRyubingRyujinxAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsAzaharPackage(package))
        {
            return await UpgradeAzaharAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsMacriumReflectPackage(package))
        {
            return await UpgradeMacriumReflectAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsRpcs3Package(package))
        {
            return await UpgradeRpcs3Async(package, cancellationToken).ConfigureAwait(false);
        }

        if (UsesGuidedUpdate(package))
        {
            return await LaunchGuidedPackageAsync(
                package,
                package.UpdateUrl ?? package.InstallUrl,
                "Opened the official update page.",
                cancellationToken).ConfigureAwait(false);
        }

        var result = await RunWingetAsync(BuildPackageCommand("upgrade", package), cancellationToken).ConfigureAwait(false);
        return InterpretUpgradeResult(package, result);
    }

    private async Task<InstallerOperationResult> UpgradeSpotifyAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var wingetResult = await RunWingetAsync(BuildPackageCommand("upgrade", package), cancellationToken).ConfigureAwait(false);
        var interpretedResult = InterpretUpgradeResult(package, wingetResult);
        if (interpretedResult.Succeeded)
        {
            return interpretedResult;
        }

        return await RunSpotifyInstallerFallbackAsync(package, interpretedResult, isUpgrade: true, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InstallerOperationResult> UninstallPackageAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var result = await RunWingetAsync(BuildPackageCommand("uninstall", package), cancellationToken).ConfigureAwait(false);
        return InterpretUninstallResult(package, result);
    }

    private async Task<IReadOnlyList<InstallerOperationResult>> RunInteractiveInstallAsync(
        InstallerCatalogItem package,
        CancellationToken cancellationToken)
    {
        if (!package.TrackStatusWithWinget || package.UsesCustomInstallFlow)
        {
            return
            [
                ApplyGuidance(
                    package,
                    InstallerPackageAction.InstallInteractive,
                    new InstallerOperationResult(
                        package.PackageId,
                        package.DisplayName,
                        false,
                        false,
                        "Interactive install is only available for winget-managed packages.",
                        string.Empty)),
            ];
        }

        var status = (await GetPackageStatusesAsync(
            [package.PackageId],
            cancellationToken: cancellationToken).ConfigureAwait(false)).FirstOrDefault();
        if (status?.IsInstalled == true)
        {
            return
            [
                ApplyGuidance(
                    package,
                    InstallerPackageAction.InstallInteractive,
                    new InstallerOperationResult(
                        package.PackageId,
                        package.DisplayName,
                        true,
                        false,
                        "Already installed. Use Update or Reinstall if you want to run setup again.",
                        status.StatusText)),
            ];
        }

        var result = await RunWingetAsync(
            BuildPackageCommand(
                "install",
                package,
                interactive: true,
                force: false,
                noUpgrade: false),
            cancellationToken).ConfigureAwait(false);

        return
        [
            ApplyGuidance(package, InstallerPackageAction.InstallInteractive, InterpretInstallResult(package, result)),
        ];
    }

    private async Task<IReadOnlyList<InstallerOperationResult>> RunInteractiveUpgradeAsync(
        InstallerCatalogItem package,
        CancellationToken cancellationToken)
    {
        if (!package.TrackStatusWithWinget || package.UsesCustomInstallFlow)
        {
            return
            [
                ApplyGuidance(
                    package,
                    InstallerPackageAction.UpdateInteractive,
                    new InstallerOperationResult(
                        package.PackageId,
                        package.DisplayName,
                        false,
                        false,
                        "Interactive update is only available for winget-managed packages.",
                        string.Empty)),
            ];
        }

        var result = await RunWingetAsync(
            BuildPackageCommand(
                "upgrade",
                package,
                interactive: true,
                force: false,
                noUpgrade: false),
            cancellationToken).ConfigureAwait(false);

        return
        [
            ApplyGuidance(package, InstallerPackageAction.UpdateInteractive, InterpretUpgradeResult(package, result)),
        ];
    }

    private async Task<IReadOnlyList<InstallerOperationResult>> RunReinstallAsync(
        InstallerCatalogItem package,
        CancellationToken cancellationToken)
    {
        if (!package.TrackStatusWithWinget || package.UsesCustomInstallFlow)
        {
            return
            [
                ApplyGuidance(
                    package,
                    InstallerPackageAction.Reinstall,
                    new InstallerOperationResult(
                        package.PackageId,
                        package.DisplayName,
                        false,
                        false,
                        "Reinstall is only available for winget-managed packages.",
                        string.Empty)),
            ];
        }

        var result = await RunWingetAsync(
            BuildPackageCommand(
                "install",
                package,
                interactive: false,
                force: true,
                noUpgrade: true),
            cancellationToken).ConfigureAwait(false);

        var interpretedResult = InterpretInstallResult(package, result);
        return
        [
            ApplyGuidance(
                package,
                InstallerPackageAction.Reinstall,
                interpretedResult with
                {
                    Message = result.ExitCode == 0
                        ? "Reinstall completed."
                        : interpretedResult.Message,
                }),
        ];
    }

    private async Task<InstallerOperationResult> RunSpotifyInstallerFallbackAsync(
        InstallerCatalogItem package,
        InstallerOperationResult wingetFailure,
        bool isUpgrade,
        CancellationToken cancellationToken)
    {
        var workingDirectory = ResolveSpotifyWorkingDirectory();
        Directory.CreateDirectory(workingDirectory);

        var installerPath = Path.Combine(workingDirectory, SpotifyFallbackInstallerFileName);
        var logPath = Path.Combine(workingDirectory, SpotifyFallbackInstallerLogFileName);

        try
        {
            await fileDownloader(SpotifyFallbackInstallerUrl, installerPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Spotify fallback download failed after winget failed: {wingetFailure.Message} Direct installer error: {ex.Message}",
                wingetFailure.Output);
        }

        var installerArguments = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            SpotifyFallbackInstallerArgumentsTemplate,
            QuoteArgument(logPath));

        try
        {
            await installerExecutableLauncher(
                new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = installerArguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true,
                },
                preferUnelevated: true,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Spotify fallback launch failed after winget failed: {wingetFailure.Message} Installer launch error: {ex.Message}",
                wingetFailure.Output);
        }

        var fallbackSucceeded = await WaitForSpotifyFallbackStatusAsync(isUpgrade, cancellationToken).ConfigureAwait(false);
        if (fallbackSucceeded)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                true,
                isUpgrade
                    ? "Updated successfully through Spotify's official installer fallback."
                    : "Installed successfully through Spotify's official installer fallback.",
                $"winget failure: {wingetFailure.Message}{Environment.NewLine}fallback installer: {installerPath}");
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Started Spotify's official installer fallback after winget failed ({wingetFailure.Message}). Spotify may still be finishing the update in the background. If it still shows update ready after a minute, check {logPath}.",
            $"winget failure: {wingetFailure.Message}{Environment.NewLine}fallback installer: {installerPath}{Environment.NewLine}log: {logPath}");
    }

    private async Task<bool> WaitForSpotifyFallbackStatusAsync(bool requireNoUpdateAvailable, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < SpotifyFallbackStatusPollAttempts; attempt++)
        {
            if (attempt > 0)
            {
                await delayAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }

            var status = (await GetPackageStatusesAsync(
                [SpotifyPackageId],
                cancellationToken: cancellationToken).ConfigureAwait(false))
                .FirstOrDefault();

            if (status is null || !status.IsInstalled)
            {
                continue;
            }

            if (!requireNoUpdateAvailable || !status.HasUpdateAvailable)
            {
                return true;
            }
        }

        return false;
    }

}
