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
    private async Task<(bool Succeeded, string Message, string Output)> EnsurePackageInstalledAsync(
        InstallerCatalogItem package,
        string dependentDisplayName,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingStatus = (await GetPackageStatusesAsync([package.PackageId], cancellationToken).ConfigureAwait(false))
                .FirstOrDefault();
            if (existingStatus?.IsInstalled == true)
            {
                return (true, $"{package.DisplayName} is already installed.", existingStatus.StatusText);
            }

            var installResult = await RunWingetAsync(BuildPackageCommand("install", package), cancellationToken).ConfigureAwait(false);
            var interpretedResult = InterpretInstallResult(package, installResult);
            return interpretedResult.Succeeded
                ? (true, $"{package.DisplayName} is ready.", interpretedResult.Output)
                : (false, $"{package.DisplayName} is required before {dependentDisplayName} can be installed. {interpretedResult.Message}", interpretedResult.Output);
        }
        catch (Win32Exception)
        {
            return (false, $"{package.DisplayName} is required before {dependentDisplayName} can be installed, and winget is unavailable to install it automatically.", string.Empty);
        }
    }

    private async Task<(bool Succeeded, string? ExecutablePath, string Message, string Output)> EnsureExecutableAsync(
        InstallerCatalogItem package,
        string dependentDisplayName,
        Func<string?> executableResolver,
        CancellationToken cancellationToken)
    {
        var executablePath = executableResolver();
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            return (true, executablePath, $"{package.DisplayName} is ready.", executablePath);
        }

        try
        {
            var installResult = await RunWingetAsync(BuildPackageCommand("install", package), cancellationToken).ConfigureAwait(false);
            var interpretedResult = InterpretInstallResult(package, installResult);
            if (!interpretedResult.Succeeded)
            {
                return (false, null, $"{package.DisplayName} is required before {dependentDisplayName} can be installed. {interpretedResult.Message}", interpretedResult.Output);
            }
        }
        catch (Win32Exception)
        {
            return (false, null, $"{package.DisplayName} is required before {dependentDisplayName} can be installed, and winget is unavailable to install it automatically.", string.Empty);
        }

        executablePath = executableResolver();
        return string.IsNullOrWhiteSpace(executablePath)
            ? (false, null, $"{package.DisplayName} installed, but its executable could not be located automatically.", string.Empty)
            : (true, executablePath, $"{package.DisplayName} is ready.", executablePath);
    }

    private static ProcessStartInfo CreateCommandProcessStartInfo(string fileName, string arguments) =>
        new()
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

    private static string? ReadInstalledReleaseMarker(string installDirectory)
    {
        var markerPath = GetInstalledReleaseMarkerPath(installDirectory);
        return File.Exists(markerPath)
            ? File.ReadAllText(markerPath).Trim()
            : null;
    }

    private static async Task WriteInstalledReleaseMarkerAsync(
        string installDirectory,
        InstallerReleaseAsset releaseAsset,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            GetInstalledReleaseMarkerPath(installDirectory),
            BuildInstalledReleaseMarkerValue(releaseAsset),
            cancellationToken).ConfigureAwait(false);
    }

    private static string GetInstalledReleaseMarkerPath(string installDirectory) =>
        Path.Combine(installDirectory, InstalledReleaseMarkerFileName);

    private static string BuildInstalledReleaseMarkerValue(InstallerReleaseAsset releaseAsset) =>
        !string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl)
            ? releaseAsset.DownloadUrl.Trim()
            : releaseAsset.FileName.Trim();

    private static async Task WriteAutomatic1111LauncherAsync(
        string launcherPath,
        string installDirectory,
        string pythonExecutablePath,
        string gitExecutablePath,
        CancellationToken cancellationToken)
    {
        var launcherContents = $@"@echo off
setlocal
set ""PYTHON={pythonExecutablePath}""
set ""GIT={gitExecutablePath}""
set ""STABLE_DIFFUSION_REPO={Automatic1111StableDiffusionRepositoryUrl}""
set ""VENV_DIR=""
set ""COMMANDLINE_ARGS=""
pushd ""{installDirectory}""
call webui-user.bat
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteOpenWebUiLauncherAsync(
        string launcherPath,
        string installDirectory,
        string dataDirectory,
        string openWebUiExecutablePath,
        CancellationToken cancellationToken)
    {
        var launcherContents = $@"@echo off
setlocal
set ""DATA_DIR={dataDirectory}""
if not exist ""%DATA_DIR%"" mkdir ""%DATA_DIR%""
pushd ""{installDirectory}""
""{openWebUiExecutablePath}"" serve
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteRyubingRyujinxLauncherAsync(
        string launcherPath,
        string executablePath,
        CancellationToken cancellationToken)
    {
        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            throw new InvalidOperationException("Ryujinx (Ryubing) executable path is invalid.");
        }

        var launcherContents = $@"@echo off
setlocal
pushd ""{executableDirectory}""
start """" ""{executablePath}""
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteAzaharLauncherAsync(
        string launcherPath,
        string executablePath,
        CancellationToken cancellationToken)
    {
        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            throw new InvalidOperationException("Lime3DS (Azahar) executable path is invalid.");
        }

        var launcherContents = $@"@echo off
setlocal
pushd ""{executableDirectory}""
start """" ""{executablePath}""
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteMacriumReflectLauncherAsync(
        string launcherPath,
        string targetExecutablePath,
        CancellationToken cancellationToken)
    {
        var targetDirectory = Path.GetDirectoryName(targetExecutablePath);
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new InvalidOperationException("Macrium Reflect target path is invalid.");
        }

        var launcherContents = $@"@echo off
setlocal
pushd ""{targetDirectory}""
start """" ""{targetExecutablePath}""
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteRpcs3LauncherAsync(
        string launcherPath,
        string executablePath,
        CancellationToken cancellationToken)
    {
        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            throw new InvalidOperationException("RPCS3 executable path is invalid.");
        }

        var launcherContents = $@"@echo off
setlocal
pushd ""{executableDirectory}""
start """" ""{executablePath}""
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InstallerOperationResult> LaunchAutomatic1111Async(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                Automatic1111PackageId,
                "Stable Diffusion WebUI (AUTOMATIC1111)",
                false,
                false,
                "The launch script for Stable Diffusion WebUI was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                Automatic1111PackageId,
                "Stable Diffusion WebUI (AUTOMATIC1111)",
                true,
                true,
                "Launched the first-run setup window.",
                output)
            : new InstallerOperationResult(
                Automatic1111PackageId,
                "Stable Diffusion WebUI (AUTOMATIC1111)",
                false,
                false,
                SummarizeFailure(output, "The first-run setup could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> LaunchOpenWebUiAsync(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                OpenWebUiPackageId,
                "Open WebUI",
                false,
                false,
                "The launch script for Open WebUI was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                OpenWebUiPackageId,
                "Open WebUI",
                true,
                true,
                "Launched the Open WebUI server window.",
                output)
            : new InstallerOperationResult(
                OpenWebUiPackageId,
                "Open WebUI",
                false,
                false,
                SummarizeFailure(output, "The Open WebUI server could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> LaunchRyubingRyujinxAsync(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                RyubingRyujinxPackageId,
                "Ryujinx (Ryubing)",
                false,
                false,
                "The launch script for Ryujinx (Ryubing) was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                RyubingRyujinxPackageId,
                "Ryujinx (Ryubing)",
                true,
                true,
                "Launched Ryujinx (Ryubing).",
                output)
            : new InstallerOperationResult(
                RyubingRyujinxPackageId,
                "Ryujinx (Ryubing)",
                false,
                false,
                SummarizeFailure(output, "Ryujinx (Ryubing) could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> LaunchAzaharAsync(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                AzaharPackageId,
                "Lime3DS (Azahar)",
                false,
                false,
                "The launch script for Lime3DS (Azahar) was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                AzaharPackageId,
                "Lime3DS (Azahar)",
                true,
                true,
                "Launched Lime3DS (Azahar).",
                output)
            : new InstallerOperationResult(
                AzaharPackageId,
                "Lime3DS (Azahar)",
                false,
                false,
                SummarizeFailure(output, "Lime3DS (Azahar) could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> LaunchMacriumReflectAsync(
        string launcherPath,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                false,
                false,
                "The launch script for Macrium Reflect was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(workingDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                true,
                true,
                "Launched Macrium Reflect.",
                output)
            : new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                false,
                false,
                SummarizeFailure(output, "Macrium Reflect could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> RunMacriumReflectInstallerSilentlyAsync(
        string installerPath,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(installerPath))
        {
            return new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                false,
                false,
                "The Macrium Reflect installer could not be found after download.",
                installerPath);
        }

        var logsDirectory = Path.Combine(workingDirectory, "Logs");
        Directory.CreateDirectory(logsDirectory);
        var logPath = Path.Combine(logsDirectory, MacriumReflectUpdateLogFileName);
        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }

        var installResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /wait {QuoteArgument(installerPath)} /qn /norestart /l {QuoteArgument(logPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(installResult);
        var details = string.Join(
            Environment.NewLine,
            new[]
            {
                output,
                $"Installer: {installerPath}",
                $"Log: {logPath}",
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        if (installResult.ExitCode == 0)
        {
            return new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                true,
                true,
                "Updated Macrium Reflect silently with the latest installer.",
                details);
        }

        if (installResult.ExitCode == RestartRequiredExitCode)
        {
            return new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                true,
                true,
                "Updated Macrium Reflect silently. Restart Windows to finish the upgrade.",
                details);
        }

        return new InstallerOperationResult(
            MacriumReflectPackageId,
            "Macrium Reflect",
            false,
            false,
            $"Macrium Reflect silent update failed. Review the installer log at {logPath}.",
            details);
    }

    private async Task<InstallerOperationResult> LaunchRpcs3Async(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                Rpcs3PackageId,
                "RPCS3",
                false,
                false,
                "The launch script for RPCS3 was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                Rpcs3PackageId,
                "RPCS3",
                true,
                true,
                "Launched RPCS3.",
                output)
            : new InstallerOperationResult(
                Rpcs3PackageId,
                "RPCS3",
                false,
                false,
                SummarizeFailure(output, "RPCS3 could not be launched."),
                output);
    }

    private static bool UsesGuidedInstall(InstallerCatalogItem package) =>
        !string.IsNullOrWhiteSpace(package.InstallUrl) && !package.TrackStatusWithWinget;

    private static bool UsesGuidedUpdate(InstallerCatalogItem package) =>
        (!string.IsNullOrWhiteSpace(package.UpdateUrl) || UsesGuidedInstall(package))
        && !package.TrackStatusWithWinget;

    private static string QuoteArgument(string value) => $"\"{value}\"";

    private static bool IsAutomatic1111Package(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, Automatic1111PackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsOpenWebUiPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, OpenWebUiPackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsRyubingRyujinxPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, RyubingRyujinxPackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsAzaharPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, AzaharPackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsMacriumReflectPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, MacriumReflectPackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsRpcs3Package(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, Rpcs3PackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsSpotifyPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, SpotifyPackageId, StringComparison.OrdinalIgnoreCase);

    private static string SummarizeFailure(string output, string fallbackMessage)
    {
        var summarizedFailure = TrySummarizeFailure(output);
        if (!string.IsNullOrWhiteSpace(summarizedFailure))
        {
            return summarizedFailure;
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            return fallbackMessage;
        }

        var interestingLine = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(
                line => !line.StartsWith("Found ", StringComparison.OrdinalIgnoreCase)
                     && !line.StartsWith("This application is licensed", StringComparison.OrdinalIgnoreCase)
                     && !line.StartsWith("Microsoft is not responsible", StringComparison.OrdinalIgnoreCase)
                     && !line.StartsWith("Starting package install", StringComparison.OrdinalIgnoreCase));

        return interestingLine ?? fallbackMessage;
    }

    private static string? TrySummarizeFailure(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var lines = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => line.Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!IsFailureSummaryLine(line))
            {
                continue;
            }

            if (IsGenericFailureLeadIn(line))
            {
                var detailLines = lines
                    .Skip(index + 1)
                    .Where(IsFailureSummaryLine)
                    .Take(2)
                    .ToArray();

                if (detailLines.Length > 0)
                {
                    return string.Join(" ", detailLines);
                }

                continue;
            }

            if (IsFailureCodeLine(line) && index > 0)
            {
                var previousLine = lines[index - 1];
                if (IsFailureSummaryLine(previousLine) && !IsGenericFailureLeadIn(previousLine))
                {
                    return $"{previousLine} {line}";
                }
            }

            return line;
        }

        return null;
    }

    private static bool IsFailureSummaryLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || IsInformationalSummaryLine(line))
        {
            return false;
        }

        return line.Contains("error", StringComparison.OrdinalIgnoreCase)
            || line.Contains("failed", StringComparison.OrdinalIgnoreCase)
            || line.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
            || line.Contains("denied", StringComparison.OrdinalIgnoreCase)
            || line.Contains("not success", StringComparison.OrdinalIgnoreCase)
            || line.Contains("could not", StringComparison.OrdinalIgnoreCase)
            || line.Contains("cannot", StringComparison.OrdinalIgnoreCase)
            || line.Contains("can't", StringComparison.OrdinalIgnoreCase)
            || line.Contains("0x", StringComparison.OrdinalIgnoreCase)
            || line.Contains("hash does not match", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericFailureLeadIn(string line) =>
        line.StartsWith("An unexpected error occurred", StringComparison.OrdinalIgnoreCase);

    private static bool IsFailureCodeLine(string line) =>
        line.Contains("0x", StringComparison.OrdinalIgnoreCase);

    private static bool IsInformationalSummaryLine(string line) =>
        line.StartsWith("Found ", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("This application is licensed", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Microsoft is not responsible", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Starting package install", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Downloading ", StringComparison.OrdinalIgnoreCase);

}
