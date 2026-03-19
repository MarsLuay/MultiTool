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
    private async Task<InstallerCommandResult> RunWingetAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "winget",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        return await commandRunner(startInfo, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InstallerOperationResult> LaunchGuidedPackageAsync(
        InstallerCatalogItem package,
        string? target,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "No official install link is configured for this app.",
                string.Empty);
        }

        await guidedInstallerLauncher(target, cancellationToken).ConfigureAwait(false);
        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            successMessage,
            target);
    }

    private static InstallerOperationResult InterpretInstallResult(InstallerCatalogItem package, InstallerCommandResult result)
    {
        var output = NormalizeOutput(result);

        if (Contains(output, "Successfully installed"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Installed successfully.", output);
        }

        if (Contains(output, "Found an existing package already installed"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, false, "Already installed and up to date.", output);
        }

        if (Contains(output, "No package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, "winget could not find this package.", output);
        }

        var failureSummary = TrySummarizeFailure(output);
        if (!string.IsNullOrWhiteSpace(failureSummary))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, failureSummary, output);
        }

        if (result.ExitCode == 0)
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Install completed.", output);
        }

        return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, SummarizeFailure(output, "Install failed."), output);
    }

    private static InstallerOperationResult InterpretUpgradeResult(InstallerCatalogItem package, InstallerCommandResult result)
    {
        var output = NormalizeOutput(result);

        if (Contains(output, "Successfully installed") || Contains(output, "Successfully upgraded"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Updated successfully.", output);
        }

        if (Contains(output, "No available upgrade found") || Contains(output, "No newer package versions are available"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, false, "Already up to date.", output);
        }

        if (Contains(output, "No installed package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, "This app is not installed yet.", output);
        }

        if (Contains(output, "No package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, "winget could not find this package.", output);
        }

        var failureSummary = TrySummarizeFailure(output);
        if (!string.IsNullOrWhiteSpace(failureSummary))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, failureSummary, output);
        }

        if (result.ExitCode == 0)
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Update completed.", output);
        }

        return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, SummarizeFailure(output, "Update failed."), output);
    }

    private static InstallerOperationResult InterpretUninstallResult(InstallerCatalogItem package, InstallerCommandResult result)
    {
        var output = NormalizeOutput(result);

        if (Contains(output, "Successfully uninstalled") || Contains(output, "Successfully removed"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Removed successfully.", output);
        }

        if (Contains(output, "No installed package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, false, "Already missing on this PC.", output);
        }

        if (Contains(output, "No package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, "Windows does not expose this app as removable on this PC.", output);
        }

        var failureSummary = TrySummarizeFailure(output);
        if (!string.IsNullOrWhiteSpace(failureSummary))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, failureSummary, output);
        }

        if (result.ExitCode == 0)
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Removal completed.", output);
        }

        return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, SummarizeFailure(output, "Removal failed."), output);
    }

    private static InstallerOperationResult ApplyGuidance(
        InstallerCatalogItem package,
        InstallerPackageAction action,
        InstallerOperationResult result)
    {
        var guidance = string.Empty;
        var requiresManualStep = false;
        var output = result.Output ?? string.Empty;
        var message = result.Message ?? string.Empty;

        if (result.Succeeded)
        {
            if (message.Contains("Restart Windows", StringComparison.OrdinalIgnoreCase))
            {
                guidance = "Restart Windows to finish the change cleanly.";
                requiresManualStep = true;
            }
            else if (!result.Changed && action == InstallerPackageAction.Update)
            {
                guidance = "Use Reinstall if the app still feels broken even though it is current.";
            }
            else if (!result.Changed && action is InstallerPackageAction.Install or InstallerPackageAction.InstallInteractive)
            {
                guidance = "Use Update or Reinstall if you want to force setup to run again.";
            }
        }
        else
        {
            if (message.Contains("not installed yet", StringComparison.OrdinalIgnoreCase))
            {
                guidance = "Install the app first, then run update.";
            }
            else if (message.Contains("Already installed", StringComparison.OrdinalIgnoreCase))
            {
                guidance = "Use Update or Reinstall instead of Install.";
            }
            else if (message.Contains("could not find this package", StringComparison.OrdinalIgnoreCase))
            {
                guidance = "Refresh the installer status, then try again. If it still fails, winget's source list may be stale.";
                requiresManualStep = true;
            }
            else if (Contains(output, "hash does not match"))
            {
                guidance = "The package source looks out of sync. Try again later or use the vendor page instead.";
                requiresManualStep = true;
            }
            else if (Contains(output, "Access is denied")
                     || Contains(output, "administrator")
                     || Contains(output, "0x80070005")
                     || Contains(output, "0x8a15000f"))
            {
                var capabilities = BuildPackageCapabilities(package);
                guidance = capabilities.SupportsInteractiveInstall
                    || capabilities.SupportsInteractiveUpdate
                    ? "Try the interactive action so the installer can show its own prompts, or run MultiTool as administrator if Windows blocks access."
                    : "Run MultiTool as administrator or switch to the vendor installer flow if this app needs a different context.";
                requiresManualStep = true;
            }
            else if (Contains(output, "403")
                     || Contains(output, "1603")
                     || Contains(output, "forbidden")
                     || message.Contains("fallback", StringComparison.OrdinalIgnoreCase))
            {
                guidance = !string.IsNullOrWhiteSpace(package.UpdateUrl) || !string.IsNullOrWhiteSpace(package.InstallUrl)
                    ? "Use the official page action if the quiet installer path keeps failing."
                    : "Try the interactive action so the installer can surface its own prompts.";
                requiresManualStep = true;
            }
            else if ((action is InstallerPackageAction.Install or InstallerPackageAction.Update or InstallerPackageAction.Reinstall)
                     && (BuildPackageCapabilities(package).SupportsInteractiveInstall
                         || BuildPackageCapabilities(package).SupportsInteractiveUpdate))
            {
                guidance = "Try the interactive action next so the installer can prompt for anything it could not do silently.";
            }
            else if (!string.IsNullOrWhiteSpace(package.UpdateUrl) || !string.IsNullOrWhiteSpace(package.InstallUrl))
            {
                guidance = "Use the official page action if the quiet installer path keeps failing.";
            }
        }

        return result with
        {
            Guidance = guidance,
            RequiresManualStep = requiresManualStep,
        };
    }

    private static InstallerPackageCapabilities BuildPackageCapabilities(InstallerCatalogItem package)
    {
        var supportsInteractiveWinget = package.TrackStatusWithWinget
            && !package.UsesCustomInstallFlow
            && package.Dependencies is null;
        var supportsOpenInstallPage = !string.IsNullOrWhiteSpace(package.InstallUrl);
        var supportsOpenUpdatePage = !string.IsNullOrWhiteSpace(package.UpdateUrl) || supportsOpenInstallPage;

        return new InstallerPackageCapabilities(
            SupportsInstall: true,
            SupportsUpdate: true,
            SupportsUninstall: CleanupCatalog.Any(
                cleanup => string.Equals(cleanup.PackageId, package.PackageId, StringComparison.OrdinalIgnoreCase)),
            SupportsInteractiveInstall: supportsInteractiveWinget,
            SupportsInteractiveUpdate: supportsInteractiveWinget,
            SupportsReinstall: package.TrackStatusWithWinget && !package.UsesCustomInstallFlow,
            SupportsOpenInstallPage: supportsOpenInstallPage,
            SupportsOpenUpdatePage: supportsOpenUpdatePage,
            UsesWinget: package.TrackStatusWithWinget,
            UsesCustomFlow: package.UsesCustomInstallFlow,
            HasGuidedFallback: IsSpotifyPackage(package) || UsesGuidedInstall(package));
    }

    private HashSet<string> FindPackageIdsInOutput(string output, IReadOnlyList<string> packageIds)
    {
        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(output))
        {
            return matches;
        }

        foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var packageId in packageIds)
            {
                if (MatchesWingetOutputLine(line, packageId))
                {
                    matches.Add(packageId);
                }
            }
        }

        return matches;
    }

    private bool MatchesWingetOutputLine(string line, string packageId)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (line.Contains(packageId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var matchToken in GetWingetOutputMatchTokens(packageId))
        {
            if (StartsWithWingetNameToken(line, matchToken))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<string> GetWingetOutputMatchTokens(string packageId)
    {
        if (WingetOutputAliases.TryGetValue(packageId, out var aliases))
        {
            foreach (var alias in aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    yield return alias;
                }
            }

            yield break;
        }

        if (catalogById.TryGetValue(packageId, out var package) && !string.IsNullOrWhiteSpace(package.DisplayName))
        {
            yield return package.DisplayName;
        }
    }

    private static bool StartsWithWingetNameToken(string line, string token)
    {
        if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var trimmedLine = line.TrimStart();
        if (!trimmedLine.StartsWith(token, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trimmedLine.Length == token.Length)
        {
            return true;
        }

        var nextCharacter = trimmedLine[token.Length];
        return char.IsWhiteSpace(nextCharacter)
            || nextCharacter is '(' or '[' or '{' or '-' or '_' or '.';
    }

    private static IReadOnlyList<string> NormalizePackageIds(IEnumerable<string> packageIds)
    {
        var normalizedIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageId in packageIds)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                continue;
            }

            var normalized = packageId.Trim();
            if (seen.Add(normalized))
            {
                normalizedIds.Add(normalized);
            }
        }

        return normalizedIds;
    }

    private async Task<IReadOnlyList<string>> ExpandPackageIdsForInstallAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken)
    {
        var explicitPackageIds = NormalizePackageIds(packageIds);
        var expandedIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageId in explicitPackageIds)
        {
            ExpandPackageWithDependencies(packageId, expandedIds, seen);
        }

        var explicitPackageSet = explicitPackageIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var autoIncludedDependencyIds = expandedIds
            .Where(packageId => !explicitPackageSet.Contains(packageId))
            .ToArray();
        if (autoIncludedDependencyIds.Length == 0)
        {
            return expandedIds;
        }

        var installedDependencyIds = (await GetPackageStatusesAsync(autoIncludedDependencyIds, cancellationToken).ConfigureAwait(false))
            .Where(static status => status.IsInstalled)
            .Select(static status => status.PackageId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return
        [
            .. expandedIds.Where(packageId => explicitPackageSet.Contains(packageId) || !installedDependencyIds.Contains(packageId)),
        ];
    }

    private IReadOnlyList<string> ExpandPackageIdsForInstall(IEnumerable<string> packageIds)
    {
        var expandedIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageId in NormalizePackageIds(packageIds))
        {
            ExpandPackageWithDependencies(packageId, expandedIds, seen);
        }

        return expandedIds;
    }

    private void ExpandPackageWithDependencies(string packageId, List<string> expandedIds, HashSet<string> seen)
    {
        if (!catalogById.TryGetValue(packageId, out var package))
        {
            if (seen.Add(packageId))
            {
                expandedIds.Add(packageId);
            }

            return;
        }

        if (package.Dependencies is not null)
        {
            foreach (var dependencyId in package.Dependencies)
            {
                ExpandPackageWithDependencies(dependencyId, expandedIds, seen);
            }
        }

        if (seen.Add(packageId))
        {
            expandedIds.Add(packageId);
        }
    }

    private static string BuildPackageCommand(
        string verb,
        InstallerCatalogItem package,
        bool interactive = false,
        bool force = false,
        bool noUpgrade = false)
    {
        var sourceSegment = string.IsNullOrWhiteSpace(package.Source)
            ? string.Empty
            : $" --source {QuoteArgument(package.Source)}";

        var packageAgreementSegment = verb.Equals("uninstall", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : " --accept-package-agreements";
        var forceSegment = force ? " --force" : string.Empty;
        var noUpgradeSegment = noUpgrade && verb.Equals("install", StringComparison.OrdinalIgnoreCase)
            ? " --no-upgrade"
            : string.Empty;
        var executionModeSegment = interactive
            ? " --interactive"
            : " --disable-interactivity --silent";

        return $"{verb} --exact --id {QuoteArgument(package.PackageId)}{sourceSegment}{packageAgreementSegment}{forceSegment}{noUpgradeSegment} --accept-source-agreements{executionModeSegment}";
    }

    private static string NormalizeOutput(InstallerCommandResult result)
    {
        var combined = $"{result.StandardOutput}{Environment.NewLine}{result.StandardError}";
        var lines = combined
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !IsProgressLine(line))
            .Where(line => !string.Equals(line, "Failed in attempting to update the source: winget", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return string.Join(Environment.NewLine, lines);
    }

    private static bool IsProgressLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return true;
        }

        if (line.Contains("KB /", StringComparison.OrdinalIgnoreCase)
            || line.Contains("MB /", StringComparison.OrdinalIgnoreCase)
            || line.Contains("GB /", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (line.EndsWith("%", StringComparison.Ordinal) && line.Any(char.IsDigit))
        {
            return true;
        }

        foreach (var character in line)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            if (character is '-' or '\\' or '|' or '/' or '█' or '▒' or '▓' or '░')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool Contains(string output, string value) =>
        output.Contains(value, StringComparison.OrdinalIgnoreCase);

}
