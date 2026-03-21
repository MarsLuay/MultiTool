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
    private async Task<InstallerPackageStatus?> GetCustomPackageStatusAsync(
        string packageId,
        bool includeUpdateCheck,
        CancellationToken cancellationToken)
    {
        if (string.Equals(packageId, Automatic1111PackageId, StringComparison.OrdinalIgnoreCase))
        {
            return includeUpdateCheck
                ? await GetAutomatic1111PackageStatusAsync(cancellationToken).ConfigureAwait(false)
                : GetAutomatic1111InstalledStatus();
        }

        if (string.Equals(packageId, OpenWebUiPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return includeUpdateCheck
                ? await GetOpenWebUiPackageStatusAsync(cancellationToken).ConfigureAwait(false)
                : GetOpenWebUiInstalledStatus();
        }

        if (string.Equals(packageId, RyubingRyujinxPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return includeUpdateCheck
                ? await GetRyubingRyujinxPackageStatusAsync(cancellationToken).ConfigureAwait(false)
                : GetRyubingRyujinxInstalledStatus();
        }

        if (string.Equals(packageId, AzaharPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return includeUpdateCheck
                ? await GetAzaharPackageStatusAsync(cancellationToken).ConfigureAwait(false)
                : GetAzaharInstalledStatus();
        }

        if (string.Equals(packageId, MacriumReflectPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return includeUpdateCheck
                ? await GetMacriumReflectPackageStatusAsync(cancellationToken).ConfigureAwait(false)
                : GetMacriumReflectInstalledStatus();
        }

        if (string.Equals(packageId, Rpcs3PackageId, StringComparison.OrdinalIgnoreCase))
        {
            return includeUpdateCheck
                ? await GetRpcs3PackageStatusAsync(cancellationToken).ConfigureAwait(false)
                : GetRpcs3InstalledStatus();
        }

        return TryGetStaticCustomPackageStatus(packageId, out var status)
            ? status
            : null;
    }

    private async Task<InstallerPackageStatus> GetAutomatic1111PackageStatusAsync(CancellationToken cancellationToken)
    {
        var installedStatus = GetAutomatic1111InstalledStatus();
        if (!installedStatus.IsInstalled)
        {
            return installedStatus;
        }

        var installDirectory = automatic1111InstallDirectoryResolver();
        var gitExecutablePath = gitExecutableResolver();
        var gitDirectory = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, ".git");
        if (string.IsNullOrWhiteSpace(gitExecutablePath) || string.IsNullOrWhiteSpace(gitDirectory) || !Directory.Exists(gitDirectory))
        {
            return installedStatus;
        }

        try
        {
            var fetchResult = await commandRunner(
                CreateCommandProcessStartInfo(
                    gitExecutablePath,
                    $"-C {QuoteArgument(installDirectory)} fetch origin --quiet"),
                cancellationToken).ConfigureAwait(false);
            if (fetchResult.ExitCode != 0)
            {
                return installedStatus;
            }

            var behindCountResult = await commandRunner(
                CreateCommandProcessStartInfo(
                    gitExecutablePath,
                    $"-C {QuoteArgument(installDirectory)} rev-list --count HEAD..@{{upstream}}"),
                cancellationToken).ConfigureAwait(false);
            if (behindCountResult.ExitCode != 0)
            {
                return installedStatus;
            }

            var behindCountOutput = NormalizeOutput(behindCountResult);
            var firstLine = behindCountOutput
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            var hasUpdateAvailable = int.TryParse(firstLine, out var behindCount) && behindCount > 0;

            return new InstallerPackageStatus(
                Automatic1111PackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : installedStatus.StatusText);
        }
        catch
        {
            return installedStatus;
        }
    }

    private async Task<InstallerPackageStatus> GetOpenWebUiPackageStatusAsync(CancellationToken cancellationToken)
    {
        var installDirectory = openWebUiInstallDirectoryResolver();
        var installedStatus = GetOpenWebUiInstalledStatus();
        if (!installedStatus.IsInstalled)
        {
            return installedStatus;
        }

        var pythonExecutablePath = Path.Combine(installDirectory, "venv", "Scripts", "python.exe");

        try
        {
            var outdatedResult = await commandRunner(
                CreateCommandProcessStartInfo(
                    pythonExecutablePath,
                    "-m pip list --outdated --format=json"),
                cancellationToken).ConfigureAwait(false);
            if (outdatedResult.ExitCode != 0)
            {
                return new InstallerPackageStatus(OpenWebUiPackageId, true, false, "Installed (custom)");
            }

            var outdatedOutput = NormalizeOutput(outdatedResult);
            var hasUpdateAvailable = PipOutdatedOutputIncludesPackage(outdatedOutput, "open-webui");
            return new InstallerPackageStatus(
                OpenWebUiPackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : installedStatus.StatusText);
        }
        catch
        {
            return installedStatus;
        }
    }

    private async Task<InstallerPackageStatus> GetRyubingRyujinxPackageStatusAsync(CancellationToken cancellationToken)
    {
        var installDirectory = ryubingRyujinxInstallDirectoryResolver();
        var installedStatus = GetRyubingRyujinxInstalledStatus();
        if (!installedStatus.IsInstalled)
        {
            return installedStatus;
        }

        try
        {
            var releaseAsset = await ryubingRyujinxReleaseResolver(cancellationToken).ConfigureAwait(false);
            if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
            {
                return new InstallerPackageStatus(RyubingRyujinxPackageId, true, false, "Installed (custom)");
            }

            var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
            var installedReleaseMarker = ReadInstalledReleaseMarker(installDirectory);
            var hasUpdateAvailable =
                !string.IsNullOrWhiteSpace(installedReleaseMarker) &&
                !string.Equals(installedReleaseMarker, releaseMarker, StringComparison.OrdinalIgnoreCase);

            return new InstallerPackageStatus(
                RyubingRyujinxPackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : installedStatus.StatusText);
        }
        catch
        {
            return installedStatus;
        }
    }

    private async Task<InstallerPackageStatus> GetAzaharPackageStatusAsync(CancellationToken cancellationToken)
    {
        var installDirectory = azaharInstallDirectoryResolver();
        var installedStatus = GetAzaharInstalledStatus();
        if (!installedStatus.IsInstalled)
        {
            return installedStatus;
        }

        try
        {
            var releaseAsset = await azaharReleaseResolver(cancellationToken).ConfigureAwait(false);
            if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
            {
                return new InstallerPackageStatus(AzaharPackageId, true, false, "Installed (custom)");
            }

            var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
            var installedReleaseMarker = ReadInstalledReleaseMarker(installDirectory);
            var hasUpdateAvailable =
                !string.IsNullOrWhiteSpace(installedReleaseMarker) &&
                !string.Equals(installedReleaseMarker, releaseMarker, StringComparison.OrdinalIgnoreCase);

            return new InstallerPackageStatus(
                AzaharPackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : installedStatus.StatusText);
        }
        catch
        {
            return installedStatus;
        }
    }

    private async Task<InstallerPackageStatus> GetMacriumReflectPackageStatusAsync(CancellationToken cancellationToken)
    {
        var installedStatus = GetMacriumReflectInstalledStatus();
        if (!installedStatus.IsInstalled)
        {
            return installedStatus;
        }

        var workingDirectory = macriumReflectWorkingDirectoryResolver();
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return installedStatus;
        }

        try
        {
            var releaseAsset = await macriumReflectReleaseResolver(cancellationToken).ConfigureAwait(false);
            if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
            {
                return new InstallerPackageStatus(MacriumReflectPackageId, true, false, "Installed (custom)");
            }

            var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
            var installedReleaseMarker = ReadInstalledReleaseMarker(workingDirectory);
            var hasUpdateAvailable =
                !string.IsNullOrWhiteSpace(installedReleaseMarker) &&
                !string.Equals(installedReleaseMarker, releaseMarker, StringComparison.OrdinalIgnoreCase);

            return new InstallerPackageStatus(
                MacriumReflectPackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : installedStatus.StatusText);
        }
        catch
        {
            return installedStatus;
        }
    }

    private async Task<InstallerPackageStatus> GetRpcs3PackageStatusAsync(CancellationToken cancellationToken)
    {
        var installDirectory = rpcs3InstallDirectoryResolver();
        var installedStatus = GetRpcs3InstalledStatus();
        if (!installedStatus.IsInstalled)
        {
            return installedStatus;
        }

        try
        {
            var releaseAsset = await rpcs3ReleaseResolver(cancellationToken).ConfigureAwait(false);
            if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
            {
                return new InstallerPackageStatus(Rpcs3PackageId, true, false, "Installed (custom)");
            }

            var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
            var installedReleaseMarker = ReadInstalledReleaseMarker(installDirectory);
            var hasUpdateAvailable =
                !string.IsNullOrWhiteSpace(installedReleaseMarker) &&
                !string.Equals(installedReleaseMarker, releaseMarker, StringComparison.OrdinalIgnoreCase);

            return new InstallerPackageStatus(
                Rpcs3PackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : installedStatus.StatusText);
        }
        catch
        {
            return installedStatus;
        }
    }

    private InstallerPackageStatus GetAutomatic1111InstalledStatus()
    {
        var installDirectory = automatic1111InstallDirectoryResolver();
        var isInstalled =
            !string.IsNullOrWhiteSpace(installDirectory) &&
            File.Exists(Path.Combine(installDirectory, "webui-user.bat")) &&
            File.Exists(Path.Combine(installDirectory, "webui.bat"));
        return CreateCustomInstalledStatus(Automatic1111PackageId, isInstalled);
    }

    private InstallerPackageStatus GetOpenWebUiInstalledStatus()
    {
        var installDirectory = openWebUiInstallDirectoryResolver();
        var venvPythonPath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, "venv", "Scripts", "python.exe");
        var openWebUiExecutablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, "venv", "Scripts", "open-webui.exe");
        var launcherPath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, OpenWebUiLauncherFileName);
        var isInstalled =
            !string.IsNullOrWhiteSpace(installDirectory) &&
            !string.IsNullOrWhiteSpace(venvPythonPath) &&
            !string.IsNullOrWhiteSpace(openWebUiExecutablePath) &&
            !string.IsNullOrWhiteSpace(launcherPath) &&
            File.Exists(venvPythonPath) &&
            File.Exists(openWebUiExecutablePath) &&
            File.Exists(launcherPath);
        return CreateCustomInstalledStatus(OpenWebUiPackageId, isInstalled);
    }

    private InstallerPackageStatus GetRyubingRyujinxInstalledStatus()
    {
        var installDirectory = ryubingRyujinxInstallDirectoryResolver();
        var executablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : ResolveRyubingRyujinxExecutablePath(installDirectory);
        return CreateCustomInstalledStatus(RyubingRyujinxPackageId, !string.IsNullOrWhiteSpace(executablePath));
    }

    private InstallerPackageStatus GetAzaharInstalledStatus()
    {
        var installDirectory = azaharInstallDirectoryResolver();
        var executablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : ResolveAzaharExecutablePath(installDirectory);
        return CreateCustomInstalledStatus(AzaharPackageId, !string.IsNullOrWhiteSpace(executablePath));
    }

    private InstallerPackageStatus GetMacriumReflectInstalledStatus() =>
        CreateCustomInstalledStatus(MacriumReflectPackageId, !string.IsNullOrWhiteSpace(macriumReflectExecutableResolver()));

    private InstallerPackageStatus GetRpcs3InstalledStatus()
    {
        var installDirectory = rpcs3InstallDirectoryResolver();
        var executablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : ResolveRpcs3ExecutablePath(installDirectory);
        return CreateCustomInstalledStatus(Rpcs3PackageId, !string.IsNullOrWhiteSpace(executablePath));
    }

    private bool TryGetStaticCustomPackageStatus(string packageId, out InstallerPackageStatus status)
    {
        status = new InstallerPackageStatus(packageId, false, false, "Status unavailable");
        return false;
    }

    private void ApplyLocalFallbackStatuses(
        IDictionary<string, InstallerPackageStatus> statusesById,
        IEnumerable<string> packageIds)
    {
        foreach (var packageId in packageIds)
        {
            if (statusesById.TryGetValue(packageId, out var status) && status.IsInstalled)
            {
                continue;
            }

            var fallbackStatus = localPackageStatusResolver(packageId);
            if (fallbackStatus is { IsInstalled: true })
            {
                statusesById[packageId] = fallbackStatus;
            }
        }
    }

    private InstallerPackageStatus? TryGetLocalPackageStatus(string packageId)
    {
        if (string.Equals(packageId, FirefoxPackageId, StringComparison.OrdinalIgnoreCase))
        {
            var firefoxStatus = GetFirefoxPackageFallbackStatus();
            return firefoxStatus.IsInstalled ? firefoxStatus : null;
        }

        if (string.Equals(packageId, GitPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(gitExecutableResolver()));
        }

        if (string.Equals(packageId, PowerShellPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolvePowerShellExecutablePath()));
        }

        if (string.Equals(packageId, DiscordPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveDiscordExecutablePath()));
        }

        if (string.Equals(packageId, VencordPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveVencordInstallDirectory()));
        }

        if (string.Equals(packageId, QbittorrentPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveQbittorrentExecutablePath()));
        }

        if (string.Equals(packageId, EverythingPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveEverythingExecutablePath()));
        }

        if (string.Equals(packageId, TorBrowserPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveTorBrowserExecutablePath()));
        }

        return null;
    }

    private static InstallerPackageStatus? CreateLocalDetectedPackageStatus(string packageId, bool isInstalled) =>
        isInstalled
            ? new InstallerPackageStatus(packageId, true, false, "Installed (detected locally)")
            : null;

    private static InstallerPackageStatus CreateCustomInstalledStatus(string packageId, bool isInstalled) =>
        new(
            packageId,
            isInstalled,
            false,
            isInstalled ? "Installed (custom)" : "Not installed");

    private InstallerPackageStatus GetFirefoxPackageFallbackStatus()
    {
        var installDirectory = firefoxInstallDirectoryResolver();
        var firefoxExecutablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, "firefox.exe");
        var isInstalled =
            !string.IsNullOrWhiteSpace(firefoxExecutablePath) &&
            File.Exists(firefoxExecutablePath);

        return isInstalled
            ? new InstallerPackageStatus(FirefoxPackageId, true, false, "Installed (detected locally)")
            : new InstallerPackageStatus(FirefoxPackageId, false, false, "Not installed");
    }

    private static bool PipOutdatedOutputIncludesPackage(string output, string packageName)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(output);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var nameElement))
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.Equals(name, packageName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

}
