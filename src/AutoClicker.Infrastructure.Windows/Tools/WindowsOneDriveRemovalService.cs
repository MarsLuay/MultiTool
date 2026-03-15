using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using Microsoft.Win32;

namespace AutoClicker.Infrastructure.Windows.Tools;

public delegate Task<OneDriveCommandResult> OneDriveCommandRunner(ProcessStartInfo startInfo, CancellationToken cancellationToken);

public delegate OneDriveEnvironmentStatus OneDriveEnvironmentProbe();

public sealed record OneDriveCommandResult(int ExitCode, string StandardOutput, string StandardError);

public sealed record OneDriveEnvironmentStatus(bool IsInstalled, string? UninstallerPath, string Message);

public sealed class WindowsOneDriveRemovalService : IOneDriveRemovalService
{
    private readonly OneDriveEnvironmentProbe environmentProbe;
    private readonly OneDriveCommandRunner commandRunner;

    public WindowsOneDriveRemovalService()
        : this(ProbeEnvironment, RunProcessAsync)
    {
    }

    public WindowsOneDriveRemovalService(OneDriveEnvironmentProbe environmentProbe, OneDriveCommandRunner commandRunner)
    {
        this.environmentProbe = environmentProbe;
        this.commandRunner = commandRunner;
    }

    public OneDriveRemovalStatus GetStatus()
    {
        var environment = environmentProbe();
        return new OneDriveRemovalStatus(environment.IsInstalled, environment.Message);
    }

    public async Task<OneDriveRemovalResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        var before = environmentProbe();
        if (!before.IsInstalled)
        {
            return new OneDriveRemovalResult(true, false, "OneDrive is already removed or not detected.");
        }

        if (string.IsNullOrWhiteSpace(before.UninstallerPath))
        {
            return new OneDriveRemovalResult(false, false, "OneDrive appears to be installed, but no Windows uninstall command was found.");
        }

        try
        {
            var result = await commandRunner(
                new ProcessStartInfo
                {
                    FileName = before.UninstallerPath,
                    Arguments = "/uninstall",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                var detail = FirstNonEmpty(result.StandardError, result.StandardOutput, $"Exit code {result.ExitCode}.");
                return new OneDriveRemovalResult(false, false, $"OneDrive uninstall failed: {detail}");
            }

            var after = environmentProbe();
            if (!after.IsInstalled)
            {
                return new OneDriveRemovalResult(true, true, "OneDrive removal completed. OneDrive is no longer detected.");
            }

            return new OneDriveRemovalResult(
                false,
                false,
                "The OneDrive uninstaller ran, but OneDrive still appears to be installed. A sign-out or restart may be required.");
        }
        catch (Win32Exception)
        {
            return new OneDriveRemovalResult(false, false, "Windows could not start the OneDrive uninstaller.");
        }
    }

    private static OneDriveEnvironmentStatus ProbeEnvironment()
    {
        var installedEvidence = FindInstalledEvidence();
        var uninstallerPath = FindUninstallerPath();

        if (!string.IsNullOrWhiteSpace(installedEvidence))
        {
            var message = string.IsNullOrWhiteSpace(uninstallerPath)
                ? $"OneDrive appears to be installed ({installedEvidence}), but no built-in uninstaller path was found."
                : $"OneDrive appears to be installed ({installedEvidence}).";
            return new OneDriveEnvironmentStatus(true, uninstallerPath, message);
        }

        var statusMessage = string.IsNullOrWhiteSpace(uninstallerPath)
            ? "OneDrive is not detected on this PC."
            : "OneDrive is not currently detected. The Windows uninstall helper is available if it is added back later.";
        return new OneDriveEnvironmentStatus(false, uninstallerPath, statusMessage);
    }

    private static async Task<OneDriveCommandResult> RunProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new OneDriveCommandResult(
            process.ExitCode,
            await standardOutputTask.ConfigureAwait(false),
            await standardErrorTask.ConfigureAwait(false));
    }

    private static string? FindInstalledEvidence()
    {
        foreach (var executablePath in GetCandidateExecutablePaths())
        {
            if (File.Exists(executablePath))
            {
                return executablePath;
            }
        }

        try
        {
            if (Process.GetProcessesByName("OneDrive").Length > 0)
            {
                return "running OneDrive process";
            }
        }
        catch
        {
        }

        return FindInstalledRegistryEvidence();
    }

    private static string? FindInstalledRegistryEvidence()
    {
        foreach (var (root, path) in GetUninstallRegistryRoots())
        {
            using var uninstallKey = root.OpenSubKey(path);
            if (uninstallKey is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var packageKey = uninstallKey.OpenSubKey(subKeyName);
                var displayName = packageKey?.GetValue("DisplayName") as string;
                if (string.Equals(displayName, "Microsoft OneDrive", StringComparison.OrdinalIgnoreCase))
                {
                    return $@"registry:{root.Name}\{path}\{subKeyName}";
                }
            }
        }

        using var perUserKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive");
        return perUserKey is null ? null : @"registry:HKEY_CURRENT_USER\Software\Microsoft\OneDrive";
    }

    private static string? FindUninstallerPath()
    {
        foreach (var candidate in GetCandidateUninstallerPaths())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidateExecutablePaths()
    {
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "OneDrive",
            "OneDrive.exe");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Microsoft OneDrive",
            "OneDrive.exe");

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            yield return Path.Combine(programFilesX86, "Microsoft OneDrive", "OneDrive.exe");
        }
    }

    private static IEnumerable<string> GetCandidateUninstallerPaths()
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrWhiteSpace(windowsDirectory))
        {
            yield return Path.Combine(windowsDirectory, "System32", "OneDriveSetup.exe");
            yield return Path.Combine(windowsDirectory, "SysWOW64", "OneDriveSetup.exe");
        }

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "OneDrive",
            "OneDriveSetup.exe");
    }

    private static IEnumerable<(RegistryKey Root, string Path)> GetUninstallRegistryRoots()
    {
        yield return (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall");
        yield return (Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Uninstall");
        yield return (Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
