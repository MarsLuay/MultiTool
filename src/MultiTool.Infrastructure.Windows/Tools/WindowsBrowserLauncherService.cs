using System.Diagnostics;
using System.IO;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using Microsoft.Win32;

namespace MultiTool.Infrastructure.Windows.Tools;

public sealed class WindowsBrowserLauncherService : IBrowserLauncherService
{
    private readonly Action<ProcessStartInfo> processStarter;
    private readonly Func<string?> torBrowserExecutablePathResolver;

    public WindowsBrowserLauncherService()
        : this(
            static startInfo => Process.Start(startInfo),
            ResolveTorBrowserExecutablePath)
    {
    }

    public WindowsBrowserLauncherService(
        Action<ProcessStartInfo> processStarter,
        Func<string?> torBrowserExecutablePathResolver)
    {
        this.processStarter = processStarter;
        this.torBrowserExecutablePathResolver = torBrowserExecutablePathResolver;
    }

    public BrowserLaunchResult OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("The selected site does not have a valid URL.");
        }

        if (RequiresTorBrowser(uri))
        {
            var torBrowserExecutablePath = torBrowserExecutablePathResolver();
            if (string.IsNullOrWhiteSpace(torBrowserExecutablePath) || !File.Exists(torBrowserExecutablePath))
            {
                throw new InvalidOperationException("Tor Browser could not be located. Install it first, then try again.");
            }

            processStarter(
                new ProcessStartInfo
                {
                    FileName = torBrowserExecutablePath,
                    Arguments = uri.AbsoluteUri,
                    WorkingDirectory = Path.GetDirectoryName(torBrowserExecutablePath) ?? string.Empty,
                    UseShellExecute = true,
                });

            return new BrowserLaunchResult("Tor Browser");
        }

        processStarter(
            new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true,
            });

        return new BrowserLaunchResult("default browser");
    }

    private static bool RequiresTorBrowser(Uri uri) =>
        uri.Host.EndsWith(".onion", StringComparison.OrdinalIgnoreCase);

    private static string? ResolveTorBrowserExecutablePath()
    {
        foreach (var registryView in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            var installPath = ResolveTorBrowserExecutablePathFromUninstallRegistry(RegistryHive.CurrentUser, registryView)
                ?? ResolveTorBrowserExecutablePathFromUninstallRegistry(RegistryHive.LocalMachine, registryView)
                ?? ResolveTorBrowserExecutablePathFromAppPaths(RegistryHive.CurrentUser, registryView)
                ?? ResolveTorBrowserExecutablePathFromAppPaths(RegistryHive.LocalMachine, registryView);

            if (!string.IsNullOrWhiteSpace(installPath))
            {
                return installPath;
            }
        }

        foreach (var candidate in GetCommonTorBrowserExecutablePaths())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveTorBrowserExecutablePathFromUninstallRegistry(RegistryHive hive, RegistryView view)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        if (uninstallKey is null)
        {
            return null;
        }

        foreach (var subKeyName in uninstallKey.GetSubKeyNames())
        {
            using var packageKey = uninstallKey.OpenSubKey(subKeyName);
            var displayName = packageKey?.GetValue("DisplayName") as string;
            if (string.IsNullOrWhiteSpace(displayName) ||
                displayName.IndexOf("Tor Browser", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            var executablePath = TryResolveTorExecutableFromDirectory(packageKey?.GetValue("InstallLocation") as string)
                ?? TryResolveExecutablePath(packageKey?.GetValue("DisplayIcon") as string)
                ?? TryResolveExecutablePath(packageKey?.GetValue("InstallSource") as string);

            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                return executablePath;
            }
        }

        return null;
    }

    private static string? ResolveTorBrowserExecutablePathFromAppPaths(RegistryHive hive, RegistryView view)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var appPathsKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe");
        var path = appPathsKey?.GetValue(string.Empty) as string;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return path.IndexOf("Tor Browser", StringComparison.OrdinalIgnoreCase) >= 0 && File.Exists(path)
            ? path
            : null;
    }

    private static IEnumerable<string> GetCommonTorBrowserExecutablePaths()
    {
        var directCandidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Tor Browser", "Browser", "firefox.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tor Browser", "Browser", "firefox.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Tor Browser", "Browser", "firefox.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Tor Browser", "Browser", "firefox.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Tor Browser", "Browser", "firefox.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Tor Browser", "Browser", "firefox.exe"),
        };

        foreach (var candidate in directCandidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                yield return candidate;
            }
        }

        foreach (var parentDirectory in GetTorBrowserSearchRoots())
        {
            IEnumerable<string> matchingDirectories;
            try
            {
                if (!Directory.Exists(parentDirectory))
                {
                    continue;
                }

                matchingDirectories = Directory.EnumerateDirectories(parentDirectory, "Tor Browser*", SearchOption.TopDirectoryOnly);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var torDirectory in matchingDirectories)
            {
                yield return Path.Combine(torDirectory, "Browser", "firefox.exe");
            }
        }
    }

    private static IEnumerable<string> GetTorBrowserSearchRoots()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
        };

        return roots
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? TryResolveTorExecutableFromDirectory(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return null;
        }

        var candidates = new[]
        {
            Path.Combine(directoryPath, "Browser", "firefox.exe"),
            Path.Combine(directoryPath, "firefox.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? TryResolveExecutablePath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        var trimmedPath = rawPath.Trim();
        if (trimmedPath.StartsWith('"'))
        {
            var closingQuoteIndex = trimmedPath.IndexOf('"', 1);
            if (closingQuoteIndex > 1)
            {
                trimmedPath = trimmedPath[1..closingQuoteIndex];
            }
        }
        else
        {
            var separatorIndex = trimmedPath.IndexOf(',');
            if (separatorIndex >= 0)
            {
                trimmedPath = trimmedPath[..separatorIndex];
            }
        }

        trimmedPath = trimmedPath.Trim();
        if (!trimmedPath.EndsWith("firefox.exe", StringComparison.OrdinalIgnoreCase) ||
            trimmedPath.IndexOf("Tor Browser", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return null;
        }

        return File.Exists(trimmedPath) ? trimmedPath : null;
    }
}
