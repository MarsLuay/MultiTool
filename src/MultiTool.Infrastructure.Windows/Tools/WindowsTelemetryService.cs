using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using Microsoft.Win32;

namespace MultiTool.Infrastructure.Windows.Tools;

public delegate Task<TelemetryCommandResult> TelemetryCommandRunner(ProcessStartInfo startInfo, CancellationToken cancellationToken);

public sealed record TelemetryCommandResult(int ExitCode, string StandardOutput, string StandardError);

public sealed class WindowsTelemetryService : IWindowsTelemetryService
{
    private const string DataCollectionPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
    private const string AllowTelemetryValueName = "AllowTelemetry";

    private static readonly IReadOnlyDictionary<string, string> TelemetryServiceDefaultStartModes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DiagTrack"] = "auto",
            ["dmwappushservice"] = "demand",
        };

    private static readonly IReadOnlyList<string> TelemetryServices =
    [
        "DiagTrack",
        "dmwappushservice",
    ];

    private static readonly IReadOnlyList<string> TelemetryTasks =
    [
        @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
        @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
        @"\Microsoft\Windows\Autochk\Proxy",
        @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
        @"\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask",
        @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
        @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
        @"\Microsoft\Windows\Feedback\Siuf\DmClient",
        @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
    ];

    private readonly TelemetryCommandRunner commandRunner;
    private readonly Func<int> readAllowTelemetryPolicy;
    private readonly Func<int, bool> setAllowTelemetryPolicy;
    private readonly Func<bool> clearAllowTelemetryPolicy;
    private readonly Func<string, ServiceState> getServiceState;

    public WindowsTelemetryService()
        : this(null, null, null, null, null)
    {
    }

    public WindowsTelemetryService(TelemetryCommandRunner commandRunner)
        : this(commandRunner, null, null, null, null)
    {
    }

    internal WindowsTelemetryService(
        TelemetryCommandRunner? commandRunner = null,
        Func<int>? readAllowTelemetryPolicy = null,
        Func<int, bool>? setAllowTelemetryPolicy = null,
        Func<bool>? clearAllowTelemetryPolicy = null,
        Func<string, ServiceState>? getServiceState = null)
    {
        this.commandRunner = commandRunner ?? RunProcessAsync;
        this.readAllowTelemetryPolicy = readAllowTelemetryPolicy ?? ReadAllowTelemetryPolicyCore;
        this.setAllowTelemetryPolicy = setAllowTelemetryPolicy ?? SetAllowTelemetryPolicyCore;
        this.clearAllowTelemetryPolicy = clearAllowTelemetryPolicy ?? ClearAllowTelemetryPolicyCore;
        this.getServiceState = getServiceState ?? GetServiceStateCore;
    }

    public WindowsTelemetryStatus GetStatus()
    {
        var allowTelemetry = readAllowTelemetryPolicy();
        var telemetryServiceStates = TelemetryServices
            .Select(serviceName => new TelemetryServiceSnapshot(serviceName, getServiceState(serviceName)))
            .ToArray();
        var telemetryServiceRunning = telemetryServiceStates
            .Where(static service => service.State.IsRunning)
            .Select(static service => service.Name)
            .ToArray();

        var isReduced = allowTelemetry == 0 && telemetryServiceRunning.Length == 0;

        if (isReduced)
        {
            return new WindowsTelemetryStatus(true, "Windows telemetry is reduced: policy is set to minimum and telemetry services are stopped.");
        }

        if (allowTelemetry < 0 && IsTelemetryDefaultConfiguration(telemetryServiceStates))
        {
            return new WindowsTelemetryStatus(
                false,
                "Windows telemetry defaults are restored: the AllowTelemetry policy override is removed and telemetry services use their default startup modes.");
        }

        var parts = new List<string>();
        if (allowTelemetry != 0)
        {
            parts.Add("AllowTelemetry policy is not set to 0");
        }

        if (telemetryServiceRunning.Length > 0)
        {
            parts.Add($"Running services: {string.Join(", ", telemetryServiceRunning)}");
        }

        return new WindowsTelemetryStatus(false, $"Telemetry hardening not fully applied. {string.Join(". ", parts)}.");
    }

    public async Task<WindowsTelemetryResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var changed = await ApplyWithoutElevationAsync(cancellationToken).ConfigureAwait(false);
            return changed
                ? new WindowsTelemetryResult(true, true, "Applied telemetry reduction policy, disabled common telemetry tasks, and disabled telemetry services.")
                : new WindowsTelemetryResult(true, false, "Telemetry reduction settings were already applied.");
        }
        catch (UnauthorizedAccessException)
        {
            return await ApplyElevatedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SecurityException)
        {
            return await ApplyElevatedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<WindowsTelemetryResult> RestoreAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var changed = await RestoreWithoutElevationAsync(cancellationToken).ConfigureAwait(false);
            return changed
                ? new WindowsTelemetryResult(true, true, "Restored telemetry defaults by removing the policy override, re-enabling telemetry tasks, and restoring telemetry service startup defaults.")
                : new WindowsTelemetryResult(true, false, "Telemetry settings are already at their default configuration.");
        }
        catch (UnauthorizedAccessException)
        {
            return await RestoreElevatedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SecurityException)
        {
            return await RestoreElevatedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> ApplyWithoutElevationAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var changed = false;
        changed |= setAllowTelemetryPolicy(0);

        foreach (var serviceName in TelemetryServices)
        {
            changed |= await StopAndDisableServiceAsync(serviceName, cancellationToken).ConfigureAwait(false);
        }

        foreach (var taskName in TelemetryTasks)
        {
            changed |= await DisableScheduledTaskAsync(taskName, cancellationToken).ConfigureAwait(false);
        }

        return changed;
    }

    private async Task<WindowsTelemetryResult> ApplyElevatedAsync(CancellationToken cancellationToken)
    {
        return await RunElevatedScriptAsync(
            scriptName: "apply-telemetry-policy.ps1",
            scriptContent: BuildTelemetryScript(),
            successMessage: "Applied telemetry reduction policy through elevated helper.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> RestoreWithoutElevationAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var changed = false;
        changed |= clearAllowTelemetryPolicy();

        foreach (var serviceName in TelemetryServices)
        {
            changed |= await RestoreServiceDefaultAsync(serviceName, cancellationToken).ConfigureAwait(false);
        }

        foreach (var taskName in TelemetryTasks)
        {
            changed |= await EnableScheduledTaskAsync(taskName, cancellationToken).ConfigureAwait(false);
        }

        return changed;
    }

    private async Task<WindowsTelemetryResult> RestoreElevatedAsync(CancellationToken cancellationToken)
    {
        return await RunElevatedScriptAsync(
            scriptName: "restore-telemetry-policy.ps1",
            scriptContent: BuildTelemetryRestoreScript(),
            successMessage: "Restored telemetry defaults through elevated helper.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<WindowsTelemetryResult> RunElevatedScriptAsync(
        string scriptName,
        string scriptContent,
        string successMessage,
        CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "MultiTool", "telemetry", Guid.NewGuid().ToString("N"));
        var tempScriptPath = Path.Combine(tempDirectory, scriptName);

        Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(tempScriptPath, scriptContent, cancellationToken).ConfigureAwait(false);

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
                return new WindowsTelemetryResult(false, false, "Windows could not start the elevated telemetry helper.");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                return new WindowsTelemetryResult(false, false, $"The elevated telemetry helper exited with code {process.ExitCode}.");
            }

            return new WindowsTelemetryResult(true, true, successMessage);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new WindowsTelemetryResult(false, false, "Administrator permission was canceled.");
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

    private static string BuildTelemetryScript()
    {
        var lines = new List<string>
        {
            "$ErrorActionPreference = 'SilentlyContinue'",
            "$policyPath = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection'",
            "if (-not (Test-Path $policyPath)) { New-Item -Path $policyPath -Force | Out-Null }",
            "New-ItemProperty -Path $policyPath -Name AllowTelemetry -PropertyType DWord -Value 0 -Force | Out-Null",
        };

        foreach (var service in TelemetryServices)
        {
            lines.Add($"sc.exe stop {service} | Out-Null");
            lines.Add($"sc.exe config {service} start= disabled | Out-Null");
        }

        foreach (var task in TelemetryTasks)
        {
            lines.Add($"schtasks.exe /Change /TN \"{task}\" /Disable | Out-Null");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildTelemetryRestoreScript()
    {
        var lines = new List<string>
        {
            "$ErrorActionPreference = 'SilentlyContinue'",
            "$policyPath = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection'",
            "if (Test-Path $policyPath) { Remove-ItemProperty -Path $policyPath -Name AllowTelemetry -ErrorAction SilentlyContinue }",
        };

        foreach (var service in TelemetryServices)
        {
            var startMode = TelemetryServiceDefaultStartModes.TryGetValue(service, out var mode)
                ? mode
                : "demand";

            lines.Add($"sc.exe config {service} start= {startMode} | Out-Null");
            lines.Add($"sc.exe start {service} | Out-Null");
        }

        foreach (var task in TelemetryTasks)
        {
            lines.Add($"schtasks.exe /Change /TN \"{task}\" /Enable | Out-Null");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool IsTelemetryDefaultConfiguration(IEnumerable<TelemetryServiceSnapshot> services)
    {
        var knownServices = services
            .Where(static service => service.State.Exists)
            .ToArray();

        return knownServices.Length > 0
            && knownServices.All(service => IsServiceUsingDefaultStartMode(service.Name, service.State));
    }

    private static bool IsServiceUsingDefaultStartMode(string serviceName, ServiceState state)
    {
        var defaultStartMode = TelemetryServiceDefaultStartModes.TryGetValue(serviceName, out var configuredMode)
            ? configuredMode
            : "demand";

        return state.StartMode.Equals(defaultStartMode, StringComparison.OrdinalIgnoreCase);
    }

    private static int ReadAllowTelemetryPolicyCore()
    {
        using var key = Registry.LocalMachine.OpenSubKey(DataCollectionPolicyPath, writable: false);
        if (key is null)
        {
            return -1;
        }

        var value = key.GetValue(AllowTelemetryValueName);
        return value is null ? -1 : Convert.ToInt32(value);
    }

    private static bool SetAllowTelemetryPolicyCore(int value)
    {
        using var key = Registry.LocalMachine.CreateSubKey(DataCollectionPolicyPath, writable: true)
            ?? throw new IOException("The machine-level data collection policy key could not be opened.");

        var current = key.GetValue(AllowTelemetryValueName);
        var currentValue = current is null ? -1 : Convert.ToInt32(current);
        if (currentValue == value)
        {
            return false;
        }

        key.SetValue(AllowTelemetryValueName, value, RegistryValueKind.DWord);
        return true;
    }

    private static bool ClearAllowTelemetryPolicyCore()
    {
        using var key = Registry.LocalMachine.OpenSubKey(DataCollectionPolicyPath, writable: true);
        if (key is null)
        {
            return false;
        }

        var value = key.GetValue(AllowTelemetryValueName);
        if (value is null)
        {
            return false;
        }

        key.DeleteValue(AllowTelemetryValueName, throwOnMissingValue: false);
        return true;
    }

    private async Task<bool> StopAndDisableServiceAsync(string serviceName, CancellationToken cancellationToken)
    {
        var stateBefore = getServiceState(serviceName);
        var changed = stateBefore.IsRunning || !stateBefore.IsDisabled;

        await commandRunner(
            new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"stop {serviceName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            cancellationToken).ConfigureAwait(false);

        await commandRunner(
            new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"config {serviceName} start= disabled",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            cancellationToken).ConfigureAwait(false);

        return changed;
    }

    private async Task<bool> DisableScheduledTaskAsync(string taskName, CancellationToken cancellationToken)
    {
        var queryResult = await commandRunner(
            new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Query /TN \"{taskName}\" /FO LIST /V",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            cancellationToken).ConfigureAwait(false);

        if (queryResult.ExitCode != 0)
        {
            return false;
        }

        var wasDisabled = queryResult.StandardOutput.Contains("Disabled", StringComparison.OrdinalIgnoreCase);

        await commandRunner(
            new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Change /TN \"{taskName}\" /Disable",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            cancellationToken).ConfigureAwait(false);

        return !wasDisabled;
    }

    private async Task<bool> RestoreServiceDefaultAsync(string serviceName, CancellationToken cancellationToken)
    {
        var stateBefore = getServiceState(serviceName);
        var defaultStartMode = TelemetryServiceDefaultStartModes.TryGetValue(serviceName, out var configuredMode)
            ? configuredMode
            : "demand";

        var shouldStart = defaultStartMode.Equals("auto", StringComparison.OrdinalIgnoreCase);
        var changed = stateBefore.IsDisabled || (shouldStart && !stateBefore.IsRunning);

        await commandRunner(
            new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"config {serviceName} start= {defaultStartMode}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            cancellationToken).ConfigureAwait(false);

        if (shouldStart)
        {
            await commandRunner(
                new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"start {serviceName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                cancellationToken).ConfigureAwait(false);
        }

        return changed;
    }

    private async Task<bool> EnableScheduledTaskAsync(string taskName, CancellationToken cancellationToken)
    {
        var queryResult = await commandRunner(
            new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Query /TN \"{taskName}\" /FO LIST /V",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            cancellationToken).ConfigureAwait(false);

        if (queryResult.ExitCode != 0)
        {
            return false;
        }

        var wasEnabled = queryResult.StandardOutput.Contains("Enabled", StringComparison.OrdinalIgnoreCase);

        await commandRunner(
            new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Change /TN \"{taskName}\" /Enable",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            cancellationToken).ConfigureAwait(false);

        return !wasEnabled;
    }

    private static ServiceState GetServiceStateCore(string serviceName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, State, StartMode FROM Win32_Service WHERE Name = '" + serviceName.Replace("'", "''") + "'");

            foreach (ManagementObject service in searcher.Get())
            {
                var state = Convert.ToString(service["State"]) ?? string.Empty;
                var startMode = Convert.ToString(service["StartMode"]) ?? string.Empty;
                return new ServiceState(
                    true,
                    state.Equals("Running", StringComparison.OrdinalIgnoreCase),
                    startMode);
            }
        }
        catch
        {
        }

        return new ServiceState(false, false, string.Empty);
    }

    private static async Task<TelemetryCommandResult> RunProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new TelemetryCommandResult(
            process.ExitCode,
            await standardOutputTask.ConfigureAwait(false),
            await standardErrorTask.ConfigureAwait(false));
    }

    internal readonly record struct ServiceState(bool Exists, bool IsRunning, string StartMode)
    {
        public bool IsDisabled => StartMode.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record TelemetryServiceSnapshot(string Name, ServiceState State);
}
