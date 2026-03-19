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
    private static string ResolveAutomatic1111InstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "stable-diffusion-webui");

    private static string ResolveSpotifyWorkingDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "spotify-installer");

    private static string ResolveOpenWebUiInstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "open-webui");

    private static string? ResolveFirefoxInstallDirectory()
    {
        foreach (var registryView in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            var installDirectory = ResolveFirefoxInstallDirectoryFromRegistry(RegistryHive.LocalMachine, registryView)
                ?? ResolveFirefoxInstallDirectoryFromRegistry(RegistryHive.CurrentUser, registryView);

            if (!string.IsNullOrWhiteSpace(installDirectory))
            {
                return installDirectory;
            }
        }

        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Mozilla Firefox"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mozilla Firefox"),
                 })
        {
            if (File.Exists(Path.Combine(candidate, "firefox.exe")))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveFirefoxInstallDirectoryFromRegistry(RegistryHive hive, RegistryView view)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var appPathsKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe");
        var path = appPathsKey?.GetValue(string.Empty) as string;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.GetDirectoryName(path);
    }

    private static string ResolveRyubingRyujinxInstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "ryujinx-ryubing");

    private static string ResolveAzaharInstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "azahar");

    private static string ResolveMacriumReflectWorkingDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "macrium-reflect");

    private static string ResolveRpcs3InstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "rpcs3");

    private static string? ResolveGitExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "git.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "cmd", "git.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin", "git.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("git.exe");
    }

    private static string? ResolvePowerShellExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7-preview", "pwsh.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PowerShell", "7", "pwsh.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "pwsh.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("pwsh.exe");
    }

    private static string? ResolvePython310ExecutablePath()
    {
        return ResolvePythonExecutablePath("3.10", "Python310", "python3.10.exe");
    }

    private static string? ResolvePython311ExecutablePath()
    {
        return ResolvePythonExecutablePath("3.11", "Python311", "python3.11.exe");
    }

    private static string? ResolveSevenZipExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("7z.exe");
    }

    private static string? ResolvePythonExecutablePath(string version, string folderName, string versionedExecutableName)
    {
        foreach (var registryView in new[] { Microsoft.Win32.RegistryView.Registry64, Microsoft.Win32.RegistryView.Registry32 })
        {
            var installDirectory = ResolvePythonInstallDirectoryFromRegistry(Microsoft.Win32.RegistryHive.CurrentUser, registryView, version)
                ?? ResolvePythonInstallDirectoryFromRegistry(Microsoft.Win32.RegistryHive.LocalMachine, registryView, version);
            if (!string.IsNullOrWhiteSpace(installDirectory))
            {
                var candidate = Path.Combine(installDirectory!, "python.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", folderName, "python.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), folderName, "python.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), folderName, "python.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath(versionedExecutableName);
    }

    private static string? ResolvePythonInstallDirectoryFromRegistry(Microsoft.Win32.RegistryHive hive, Microsoft.Win32.RegistryView view, string version)
    {
        using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(hive, view);
        using var installPathKey = baseKey.OpenSubKey($@"SOFTWARE\Python\PythonCore\{version}\InstallPath");
        return installPathKey?.GetValue(string.Empty) as string;
    }

    private static string? ResolveExecutableFromPath(string executableName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue!.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            try
            {
                var candidate = Path.Combine(directory.Trim(), executableName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static string? ResolveDiscordExecutablePath()
    {
        var installDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord");
        if (!Directory.Exists(installDirectory))
        {
            return ResolveExecutableFromPath("Discord.exe");
        }

        var updateExecutablePath = Path.Combine(installDirectory, "Update.exe");
        if (File.Exists(updateExecutablePath))
        {
            return updateExecutablePath;
        }

        return Directory
            .EnumerateFiles(installDirectory, "Discord.exe", SearchOption.AllDirectories)
            .OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string? ResolveVencordInstallDirectory()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VencordDesktop"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vencord"),
                 })
        {
            if (!Directory.Exists(candidate))
            {
                continue;
            }

            if (File.Exists(Path.Combine(candidate, "settings", "settings.json"))
                || File.Exists(Path.Combine(candidate, "dist", "patcher.js"))
                || Directory.EnumerateFileSystemEntries(candidate).Any())
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveQbittorrentExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "qBittorrent", "qbittorrent.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "qBittorrent", "qbittorrent.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "qBittorrent", "qbittorrent.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("qbittorrent.exe");
    }

    private static string? ResolveEverythingExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything", "Everything.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything", "Everything.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Everything", "Everything.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything", "Everything.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("Everything.exe");
    }

    private static string? ResolveTorBrowserExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Tor Browser", "Browser", "firefox.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveRpcs3ExecutablePath(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
        {
            return null;
        }

        var directPath = Path.Combine(installDirectory, "rpcs3.exe");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        return Directory
            .EnumerateFiles(installDirectory, "rpcs3.exe", SearchOption.AllDirectories)
            .FirstOrDefault(path => !path.Contains($"{Path.DirectorySeparatorChar}downloads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveRyubingRyujinxExecutablePath(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
        {
            return null;
        }

        foreach (var candidateFileName in new[] { "Ryujinx.exe", "Ryujinx.Ava.exe" })
        {
            var directPath = Path.Combine(installDirectory, candidateFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }
        }

        return Directory
            .EnumerateFiles(installDirectory, "Ryujinx*.exe", SearchOption.AllDirectories)
            .FirstOrDefault(
                path =>
                    !path.Contains($"{Path.DirectorySeparatorChar}downloads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileName(path).Contains("Updater", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveAzaharExecutablePath(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
        {
            return null;
        }

        foreach (var candidateFileName in new[] { "azahar.exe", "Azahar.exe" })
        {
            var directPath = Path.Combine(installDirectory, candidateFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }
        }

        return Directory
            .EnumerateFiles(installDirectory, "*azahar*.exe", SearchOption.AllDirectories)
            .FirstOrDefault(
                path =>
                    !path.Contains($"{Path.DirectorySeparatorChar}downloads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileName(path).Contains("updater", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileName(path).Contains("room", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveMacriumReflectExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Macrium", "Reflect", "ReflectBin.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Macrium", "Reflect", "Reflect.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Macrium", "Reflect", "ReflectBin.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Macrium", "Reflect", "Reflect.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

}
