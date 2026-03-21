using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using Microsoft.Win32;

namespace MultiTool.Infrastructure.Windows.Tools;

public delegate Task<OneDriveCommandResult> OneDriveCommandRunner(ProcessStartInfo startInfo, CancellationToken cancellationToken);

public delegate OneDriveEnvironmentStatus OneDriveEnvironmentProbe();

public delegate bool OneDrivePolicyProbe();

public delegate Task<OneDrivePolicyApplyOutcome> OneDrivePolicyApplier(CancellationToken cancellationToken);

public sealed record OneDriveCommandResult(int ExitCode, string StandardOutput, string StandardError);

public sealed record OneDriveEnvironmentStatus(bool IsInstalled, string? UninstallerPath, string Message);

public sealed record OneDrivePolicyApplyOutcome(bool Succeeded, bool Changed, string Message);

public sealed class WindowsOneDriveRemovalService : IOneDriveRemovalService
{
    private const string OneDrivePoliciesRegistryPath = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive";

    private static readonly IReadOnlyList<OneDrivePolicyValue> PolicyValues =
    [
        new("DisableFileSync", 1),
        new("DisableFileSyncNGSC", 1),
        new("DisableMeteredNetworkFileSync", 0),
        new("DisableLibrariesDefaultSaveToOneDrive", 0),
    ];

    private readonly OneDriveEnvironmentProbe environmentProbe;
    private readonly OneDriveCommandRunner commandRunner;
    private readonly OneDrivePolicyProbe policyProbe;
    private readonly OneDrivePolicyApplier policyApplier;

    public WindowsOneDriveRemovalService()
        : this(ProbeEnvironment, RunProcessAsync, IsDisablePolicyActive, ApplyDisablePolicyAsync)
    {
    }

    public WindowsOneDriveRemovalService(
        OneDriveEnvironmentProbe environmentProbe,
        OneDriveCommandRunner commandRunner,
        OneDrivePolicyProbe? policyProbe = null,
        OneDrivePolicyApplier? policyApplier = null)
    {
        this.environmentProbe = environmentProbe;
        this.commandRunner = commandRunner;
        this.policyProbe = policyProbe ?? IsDisablePolicyActive;
        this.policyApplier = policyApplier ?? ApplyDisablePolicyAsync;
    }

    public OneDriveRemovalStatus GetStatus()
    {
        var environment = environmentProbe();
        var disablePolicyActive = policyProbe();
        return new OneDriveRemovalStatus(environment.IsInstalled, BuildStatusMessage(environment.Message, disablePolicyActive));
    }

    public async Task<OneDriveRemovalResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        var before = environmentProbe();
        var policyWasActive = policyProbe();

        if (!before.IsInstalled)
        {
            var policyOutcome = await EnsureDisablePolicyAsync(policyWasActive, cancellationToken).ConfigureAwait(false);
            if (!policyOutcome.Succeeded)
            {
                return new OneDriveRemovalResult(
                    false,
                    false,
                    $"OneDrive is not detected, but the disable policy could not be applied: {policyOutcome.Message}");
            }

            return policyOutcome.Changed
                ? new OneDriveRemovalResult(true, true, "OneDrive is not detected. Applied the system policy that disables OneDrive file sync.")
                : new OneDriveRemovalResult(true, false, "OneDrive is already removed or not detected. The system OneDrive disable policy is already active.");
        }

        if (string.IsNullOrWhiteSpace(before.UninstallerPath))
        {
            var policyOutcome = await EnsureDisablePolicyAsync(policyWasActive, cancellationToken).ConfigureAwait(false);
            if (!policyOutcome.Succeeded)
            {
                return new OneDriveRemovalResult(
                    false,
                    false,
                    $"OneDrive appears to be installed, but no Windows uninstall command was found and the disable policy could not be applied: {policyOutcome.Message}");
            }

            return new OneDriveRemovalResult(
                false,
                policyOutcome.Changed,
                policyOutcome.Changed
                    ? "Applied the system OneDrive disable policy, but no Windows uninstall command was found."
                    : "OneDrive appears to be installed, but no Windows uninstall command was found.");
        }

        try
        {
            var uninstallResult = await commandRunner(
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

            if (uninstallResult.ExitCode != 0)
            {
                var detail = FirstNonEmpty(uninstallResult.StandardError, uninstallResult.StandardOutput, $"Exit code {uninstallResult.ExitCode}.");
                return new OneDriveRemovalResult(false, false, $"OneDrive uninstall failed: {detail}");
            }

            var after = environmentProbe();
            var policyOutcome = await EnsureDisablePolicyAsync(policyWasActive, cancellationToken).ConfigureAwait(false);

            if (!after.IsInstalled)
            {
                if (!policyOutcome.Succeeded)
                {
                    return new OneDriveRemovalResult(
                        true,
                        true,
                        $"OneDrive removal completed, but the disable policy could not be applied: {policyOutcome.Message}");
                }

                return new OneDriveRemovalResult(
                    true,
                    true,
                    policyOutcome.Changed
                        ? "OneDrive removal completed and the system disable policy is now active."
                        : "OneDrive removal completed. OneDrive is no longer detected.");
            }

            if (!policyOutcome.Succeeded)
            {
                return new OneDriveRemovalResult(
                    false,
                    false,
                    $"The OneDrive uninstaller ran, but OneDrive still appears to be installed and the disable policy could not be applied: {policyOutcome.Message}");
            }

            return new OneDriveRemovalResult(
                false,
                policyOutcome.Changed,
                policyOutcome.Changed
                    ? "The OneDrive uninstaller ran and the system disable policy was applied, but OneDrive still appears to be installed. A sign-out or restart may be required."
                    : "The OneDrive uninstaller ran, but OneDrive still appears to be installed. A sign-out or restart may be required.");
        }
        catch (Win32Exception)
        {
            return new OneDriveRemovalResult(false, false, "Windows could not start the OneDrive uninstaller.");
        }
    }

    private async Task<OneDrivePolicyApplyOutcome> EnsureDisablePolicyAsync(bool policyWasActive, CancellationToken cancellationToken)
    {
        if (policyWasActive)
        {
            return new OneDrivePolicyApplyOutcome(true, false, "The system OneDrive disable policy is already active.");
        }

        return await policyApplier(cancellationToken).ConfigureAwait(false);
    }

    private static string BuildStatusMessage(string baseMessage, bool disablePolicyActive) =>
        disablePolicyActive
            ? $"{baseMessage} The system OneDrive disable policy is active."
            : $"{baseMessage} The system OneDrive disable policy is not active.";

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

    private static bool IsDisablePolicyActive()
    {
        using var key = Registry.LocalMachine.OpenSubKey(OneDrivePoliciesRegistryPath, writable: false);
        if (key is null)
        {
            return false;
        }

        return PolicyValues.All(
            policyValue =>
                ReadRegistryInt(key.GetValue(policyValue.Name)) == policyValue.Value);
    }

    private static async Task<OneDrivePolicyApplyOutcome> ApplyDisablePolicyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var changed = await WriteDisablePolicyAsync(cancellationToken).ConfigureAwait(false);
            return new OneDrivePolicyApplyOutcome(
                true,
                changed,
                changed
                    ? "Applied the system OneDrive disable policy."
                    : "The system OneDrive disable policy was already active.");
        }
        catch (UnauthorizedAccessException)
        {
            return await ApplyDisablePolicyElevatedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SecurityException)
        {
            return await ApplyDisablePolicyElevatedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            return new OneDrivePolicyApplyOutcome(false, false, ex.Message);
        }
    }

    private static async Task<bool> WriteDisablePolicyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Task.Run(
            () =>
            {
                using var key = Registry.LocalMachine.CreateSubKey(OneDrivePoliciesRegistryPath, writable: true)
                    ?? throw new IOException("The machine-level OneDrive policy registry key could not be opened.");

                var changed = false;
                foreach (var policyValue in PolicyValues)
                {
                    var existingValue = ReadRegistryInt(key.GetValue(policyValue.Name));
                    if (existingValue == policyValue.Value)
                    {
                        continue;
                    }

                    key.SetValue(policyValue.Name, policyValue.Value, RegistryValueKind.DWord);
                    changed = true;
                }

                return changed;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<OneDrivePolicyApplyOutcome> ApplyDisablePolicyElevatedAsync(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "MultiTool", "onedrive-policy", Guid.NewGuid().ToString("N"));
        var tempScriptPath = Path.Combine(tempDirectory, "apply-onedrive-policy.ps1");

        Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(
                tempScriptPath,
                BuildElevatedPolicyScript(),
                cancellationToken).ConfigureAwait(false);

            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                });

            if (process is null)
            {
                return new OneDrivePolicyApplyOutcome(false, false, "Windows could not start the elevated policy helper.");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                return new OneDrivePolicyApplyOutcome(false, false, $"The elevated policy helper exited with code {process.ExitCode}.");
            }

            return new OneDrivePolicyApplyOutcome(
                true,
                true,
                "Applied the system OneDrive disable policy with administrator permission.");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new OneDrivePolicyApplyOutcome(false, false, "Administrator permission was canceled.");
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
            catch
            {
            }
        }
    }

    private static string BuildElevatedPolicyScript()
    {
        var lines = new List<string>
        {
            "$path = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive'",
            "New-Item -Path $path -Force | Out-Null",
        };

        foreach (var policyValue in PolicyValues)
        {
            lines.Add(
                $"New-ItemProperty -Path $path -Name '{policyValue.Name}' -PropertyType DWord -Value {policyValue.Value.ToString(CultureInfo.InvariantCulture)} -Force | Out-Null");
        }

        return string.Join(Environment.NewLine, lines);
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

    private static int ReadRegistryInt(object? value) =>
        value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            string stringValue when int.TryParse(stringValue, out var parsedValue) => parsedValue,
            _ => int.MinValue,
        };

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private sealed record OneDrivePolicyValue(string Name, int Value);
}
