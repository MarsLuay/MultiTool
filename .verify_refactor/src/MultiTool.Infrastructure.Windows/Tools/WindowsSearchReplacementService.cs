using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

public sealed class WindowsSearchReplacementService : IWindowsSearchReplacementService
{
    private const string FlowLauncherPackageId = "Flow-Launcher.Flow-Launcher";
    private const string EverythingPackageId = "voidtools.Everything";
    private const string AutoHotkeyPackageId = "AutoHotkey.AutoHotkey";
    private const string WindowsSearchServiceName = "WSearch";
    private const string ScriptFileName = "FlowLauncherSearchReplacement.ahk";
    private const string StartupShortcutFileName = "MultiTool Flow Search Replacement.lnk";
    private const string StateFileName = "windows-search-state.json";
    private const string ReplacementDisplayName = "Flow Launcher + Everything search replacement";

    private readonly IInstallerService installerService;
    private readonly Func<string?> flowLauncherPathResolver;
    private readonly Func<string?> everythingPathResolver;
    private readonly Func<string?> autoHotkeyPathResolver;
    private readonly Func<(bool Exists, string StartupType, bool IsRunning)> windowsSearchServiceProbe;
    private readonly Func<string, bool, CancellationToken, Task> windowsSearchServiceConfigurer;
    private readonly Action<string, string, string, string, string> startupShortcutWriter;
    private readonly Action<string, string> scriptStarter;
    private readonly Action<string> scriptStopper;
    private readonly string searchReplacementDirectory;
    private readonly string startupShortcutDirectory;

    public WindowsSearchReplacementService(
        IInstallerService installerService,
        Func<string?>? flowLauncherPathResolver = null,
        Func<string?>? everythingPathResolver = null,
        Func<string?>? autoHotkeyPathResolver = null,
        Func<(bool Exists, string StartupType, bool IsRunning)>? windowsSearchServiceProbe = null,
        Func<string, bool, CancellationToken, Task>? windowsSearchServiceConfigurer = null,
        Action<string, string, string, string, string>? startupShortcutWriter = null,
        Action<string, string>? scriptStarter = null,
        Action<string>? scriptStopper = null,
        string? searchReplacementDirectory = null,
        string? startupShortcutDirectory = null)
    {
        this.installerService = installerService ?? throw new ArgumentNullException(nameof(installerService));
        this.flowLauncherPathResolver = flowLauncherPathResolver ?? ResolveFlowLauncherExecutablePath;
        this.everythingPathResolver = everythingPathResolver ?? ResolveEverythingExecutablePath;
        this.autoHotkeyPathResolver = autoHotkeyPathResolver ?? ResolveAutoHotkeyExecutablePath;
        this.windowsSearchServiceProbe = windowsSearchServiceProbe ?? ProbeWindowsSearchService;
        this.windowsSearchServiceConfigurer = windowsSearchServiceConfigurer ?? ConfigureWindowsSearchServiceAsync;
        this.startupShortcutWriter = startupShortcutWriter ?? WriteShortcut;
        this.scriptStarter = scriptStarter ?? StartSearchReplacementScript;
        this.scriptStopper = scriptStopper ?? StopSearchReplacementScript;
        this.searchReplacementDirectory = searchReplacementDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MultiTool", "SearchReplacement");
        this.startupShortcutDirectory = startupShortcutDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Startup");
    }

    public WindowsSearchReplacementStatus GetStatus()
    {
        var flowLauncherPath = flowLauncherPathResolver();
        var everythingPath = everythingPathResolver();
        var autoHotkeyPath = autoHotkeyPathResolver();
        var hasScript = File.Exists(GetScriptPath());
        var hasStartupShortcut = File.Exists(GetStartupShortcutPath());
        var searchServiceState = windowsSearchServiceProbe();
        var isWindowsSearchDisabled =
            !searchServiceState.Exists ||
            string.Equals(searchServiceState.StartupType, "Disabled", StringComparison.OrdinalIgnoreCase);
        var isConfigured =
            !string.IsNullOrWhiteSpace(flowLauncherPath) &&
            !string.IsNullOrWhiteSpace(everythingPath) &&
            !string.IsNullOrWhiteSpace(autoHotkeyPath) &&
            hasScript &&
            hasStartupShortcut &&
            isWindowsSearchDisabled;

        return new WindowsSearchReplacementStatus(
            isConfigured,
            !string.IsNullOrWhiteSpace(flowLauncherPath),
            !string.IsNullOrWhiteSpace(everythingPath),
            !string.IsNullOrWhiteSpace(autoHotkeyPath),
            hasScript && hasStartupShortcut,
            isWindowsSearchDisabled,
            BuildStatusMessage(
                !string.IsNullOrWhiteSpace(flowLauncherPath),
                !string.IsNullOrWhiteSpace(everythingPath),
                !string.IsNullOrWhiteSpace(autoHotkeyPath),
                hasScript && hasStartupShortcut,
                isWindowsSearchDisabled,
                searchServiceState.Exists));
    }

    public async Task<WindowsSearchReplacementResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var flowLauncherPath = flowLauncherPathResolver();
        var everythingPath = everythingPathResolver();
        var autoHotkeyPath = autoHotkeyPathResolver();
        var missingPackageIds = new List<string>();

        if (string.IsNullOrWhiteSpace(flowLauncherPath))
        {
            missingPackageIds.Add(FlowLauncherPackageId);
        }

        if (string.IsNullOrWhiteSpace(everythingPath))
        {
            missingPackageIds.Add(EverythingPackageId);
        }

        if (string.IsNullOrWhiteSpace(autoHotkeyPath))
        {
            missingPackageIds.Add(AutoHotkeyPackageId);
        }

        var installedAnything = false;
        if (missingPackageIds.Count > 0)
        {
            var installResults = await installerService.InstallPackagesAsync(missingPackageIds, cancellationToken).ConfigureAwait(false);
            var failedInstall = installResults.FirstOrDefault(result => !result.Succeeded);
            installedAnything = installResults.Any(result => result.Changed);
            if (failedInstall is not null)
            {
                return new WindowsSearchReplacementResult(
                    false,
                    installedAnything,
                    $"{failedInstall.DisplayName} could not be installed: {failedInstall.Message}");
            }
        }

        flowLauncherPath = flowLauncherPathResolver();
        everythingPath = everythingPathResolver();
        autoHotkeyPath = autoHotkeyPathResolver();
        if (string.IsNullOrWhiteSpace(flowLauncherPath)
            || string.IsNullOrWhiteSpace(everythingPath)
            || string.IsNullOrWhiteSpace(autoHotkeyPath))
        {
            return new WindowsSearchReplacementResult(
                false,
                installedAnything,
                "MultiTool installed the required packages, but could not locate Flow Launcher, Everything, or AutoHotkey on disk yet.");
        }

        Directory.CreateDirectory(searchReplacementDirectory);
        Directory.CreateDirectory(startupShortcutDirectory);

        var scriptPath = GetScriptPath();
        var shortcutPath = GetStartupShortcutPath();
        var statePath = GetStatePath();
        var hadHotkeyRemap = File.Exists(scriptPath) && File.Exists(shortcutPath);

        await PersistOriginalSearchServiceStateIfMissingAsync(statePath, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            scriptPath,
            BuildSearchReplacementScript(flowLauncherPath, everythingPath),
            cancellationToken).ConfigureAwait(false);

        startupShortcutWriter(
            shortcutPath,
            autoHotkeyPath,
            $"\"{scriptPath}\"",
            searchReplacementDirectory,
            flowLauncherPath);

        scriptStarter(autoHotkeyPath, scriptPath);

        try
        {
            var searchServiceState = windowsSearchServiceProbe();
            if (searchServiceState.Exists
                && (!string.Equals(searchServiceState.StartupType, "Disabled", StringComparison.OrdinalIgnoreCase)
                    || searchServiceState.IsRunning))
            {
                await windowsSearchServiceConfigurer("Disabled", false, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            return new WindowsSearchReplacementResult(
                false,
                installedAnything || !hadHotkeyRemap,
                $"{ReplacementDisplayName} is mostly ready, but Windows Search could not be disabled: {ex.Message}");
        }

        return new WindowsSearchReplacementResult(
            true,
            installedAnything || !hadHotkeyRemap,
            "Flow Launcher + Everything is now set up as the primary Win + S search flow, and Windows Search indexing is disabled.");
    }

    public async Task<WindowsSearchReplacementResult> RestoreAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scriptPath = GetScriptPath();
        var shortcutPath = GetStartupShortcutPath();
        var statePath = GetStatePath();
        var hadScript = File.Exists(scriptPath);
        var hadShortcut = File.Exists(shortcutPath);
        var hadState = File.Exists(statePath);
        var originalSearchState = await LoadStateAsync(statePath, cancellationToken).ConfigureAwait(false);

        if (hadScript)
        {
            scriptStopper(scriptPath);
        }

        DeleteIfExists(shortcutPath);
        DeleteIfExists(scriptPath);

        try
        {
            var searchServiceState = windowsSearchServiceProbe();
            if (searchServiceState.Exists)
            {
                var targetStartupType = NormalizeStartupType(originalSearchState?.StartupType);
                var shouldBeRunning = originalSearchState?.WasRunning ?? true;
                await windowsSearchServiceConfigurer(targetStartupType, shouldBeRunning, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            return new WindowsSearchReplacementResult(
                false,
                hadScript || hadShortcut || hadState,
                $"Removed the Win + S replacement, but Windows Search could not be restored cleanly: {ex.Message}");
        }

        DeleteIfExists(statePath);
        TryDeleteDirectory(searchReplacementDirectory);

        return new WindowsSearchReplacementResult(
            true,
            hadScript || hadShortcut || hadState,
            "Windows Search has been restored, and the Flow Launcher Win + S replacement helper was removed.");
    }

    private string GetScriptPath() => Path.Combine(searchReplacementDirectory, ScriptFileName);

    private string GetStartupShortcutPath() => Path.Combine(startupShortcutDirectory, StartupShortcutFileName);

    private string GetStatePath() => Path.Combine(searchReplacementDirectory, StateFileName);

    private async Task PersistOriginalSearchServiceStateIfMissingAsync(string statePath, CancellationToken cancellationToken)
    {
        if (File.Exists(statePath))
        {
            return;
        }

        var serviceState = windowsSearchServiceProbe();
        if (!serviceState.Exists)
        {
            return;
        }

        var state = new WindowsSearchReplacementState
        {
            StartupType = NormalizeStartupType(serviceState.StartupType),
            WasRunning = serviceState.IsRunning,
        };

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(statePath, json, cancellationToken).ConfigureAwait(false);
    }

    private async Task<WindowsSearchReplacementState?> LoadStateAsync(string statePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(statePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(statePath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<WindowsSearchReplacementState>(json);
    }

    private static string BuildStatusMessage(
        bool hasFlowLauncher,
        bool hasEverything,
        bool hasAutoHotkey,
        bool hasHotkeyRemap,
        bool isWindowsSearchDisabled,
        bool hasSearchService)
    {
        if (hasFlowLauncher && hasEverything && hasAutoHotkey && hasHotkeyRemap && isWindowsSearchDisabled)
        {
            return "Flow Launcher + Everything is replacing Win + S, and Windows Search indexing is disabled.";
        }

        var missingParts = new List<string>();
        if (!hasFlowLauncher)
        {
            missingParts.Add("Flow Launcher is not installed");
        }

        if (!hasEverything)
        {
            missingParts.Add("Everything is not installed");
        }

        if (!hasAutoHotkey)
        {
            missingParts.Add("AutoHotkey is not installed");
        }

        if (!hasHotkeyRemap)
        {
            missingParts.Add("the Win + S replacement helper is not set up");
        }

        if (hasSearchService && !isWindowsSearchDisabled)
        {
            missingParts.Add("Windows Search is still enabled");
        }

        return missingParts.Count == 0
            ? "Flow Launcher + Everything is ready, but Windows Search service state could not be confirmed."
            : $"{string.Join("; ", missingParts)}.";
    }

    private static string BuildSearchReplacementScript(string flowLauncherPath, string everythingPath)
    {
        var escapedFlowLauncherPath = EscapeAhkString(flowLauncherPath);
        var escapedEverythingPath = EscapeAhkString(everythingPath);

        return $$"""
#Requires AutoHotkey v2.0
#SingleInstance Force

flowPath := "{{escapedFlowLauncherPath}}"
everythingPath := "{{escapedEverythingPath}}"

EnsureProcessRunning(processName, executablePath)
{
    if !FileExist(executablePath)
    {
        return false
    }

    if ProcessExist(processName)
    {
        return true
    }

    Run '"' executablePath '"'
    return true
}

EnsureProcessRunning("Everything.exe", everythingPath)
EnsureProcessRunning("Flow.Launcher.exe", flowPath)

#s::
{
    EnsureProcessRunning("Everything.exe", everythingPath)

    if !EnsureProcessRunning("Flow.Launcher.exe", flowPath)
    {
        MsgBox "Flow Launcher was not found at:`n" flowPath, "MultiTool Search Replacement", "Iconx"
        return
    }

    Run '"' flowPath '"'
}
""";
    }

    private static string EscapeAhkString(string value) => value.Replace("\"", "\"\"");

    private static string NormalizeStartupType(string? startupType)
    {
        if (string.IsNullOrWhiteSpace(startupType))
        {
            return "Automatic";
        }

        return startupType.Trim().ToUpperInvariant() switch
        {
            "AUTO" => "Automatic",
            "AUTOMATIC" => "Automatic",
            "MANUAL" => "Manual",
            "DISABLED" => "Disabled",
            _ => "Automatic",
        };
    }

    private static string? ResolveFlowLauncherExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Flow Launcher", "Flow.Launcher.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Flow Launcher", "Flow.Launcher.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Flow Launcher", "Flow.Launcher.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "FlowLauncher", "Flow.Launcher.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowLauncher", "Flow.Launcher.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowLauncher", "Flow.Launcher.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        foreach (var root in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowLauncher"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Flow Launcher"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "FlowLauncher"),
                 })
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            try
            {
                var resolvedPath = Directory
                    .EnumerateFiles(root, "Flow.Launcher.exe", SearchOption.AllDirectories)
                    .OrderBy(path => path.Length)
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(resolvedPath))
                {
                    return resolvedPath;
                }
            }
            catch
            {
            }
        }

        return ResolveExecutableFromPath("Flow.Launcher.exe");
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

    private static string? ResolveAutoHotkeyExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AutoHotkey", "v2", "AutoHotkey64.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AutoHotkey", "AutoHotkey64.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AutoHotkey", "AutoHotkey.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "AutoHotkey", "v2", "AutoHotkey64.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "AutoHotkey", "AutoHotkeyU64.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "AutoHotkey", "AutoHotkey.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("AutoHotkey64.exe")
            ?? ResolveExecutableFromPath("AutoHotkey.exe")
            ?? ResolveExecutableFromPath("AutoHotkeyU64.exe");
    }

    private static string? ResolveExecutableFromPath(string executableName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue.Split(Path.PathSeparator))
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

    private static (bool Exists, string StartupType, bool IsRunning) ProbeWindowsSearchService()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                new ManagementScope(@"\\.\root\cimv2"),
                new ObjectQuery($"SELECT StartMode, State FROM Win32_Service WHERE Name='{WindowsSearchServiceName}'"));
            foreach (ManagementObject service in searcher.Get())
            {
                var startupType = NormalizeStartupType(Convert.ToString(service["StartMode"]));
                var isRunning = string.Equals(Convert.ToString(service["State"]), "Running", StringComparison.OrdinalIgnoreCase);
                return (true, startupType, isRunning);
            }
        }
        catch
        {
        }

        return (false, "Automatic", false);
    }

    private static async Task ConfigureWindowsSearchServiceAsync(string startupType, bool shouldBeRunning, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tempDirectory = Path.Combine(Path.GetTempPath(), "MultiTool", "search-replacement", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(tempDirectory, "set-windows-search-state.ps1");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(
                scriptPath,
                BuildSearchServicePowerShellScript(),
                cancellationToken).ConfigureAwait(false);

            var directResult = await RunPowerShellScriptAsync(
                scriptPath,
                startupType,
                shouldBeRunning,
                useShellExecute: false,
                runElevated: false,
                cancellationToken).ConfigureAwait(false);

            if (directResult.ExitCode == 0)
            {
                return;
            }

            if (!ContainsAccessDeniedMessage(directResult.StandardError, directResult.StandardOutput))
            {
                var detail = FirstNonEmpty(directResult.StandardError, directResult.StandardOutput, $"Exit code {directResult.ExitCode}.");
                throw new IOException(detail);
            }

            var elevatedResult = await RunPowerShellScriptAsync(
                scriptPath,
                startupType,
                shouldBeRunning,
                useShellExecute: true,
                runElevated: true,
                cancellationToken).ConfigureAwait(false);

            if (elevatedResult.ExitCode != 0)
            {
                throw new IOException($"The elevated Windows Search helper exited with code {elevatedResult.ExitCode}.");
            }
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            throw new IOException("Administrator permission was canceled.");
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static string BuildSearchServicePowerShellScript() =>
        """
        param(
            [Parameter(Mandatory = $true)][ValidateSet('Automatic', 'Manual', 'Disabled')][string]$StartupType,
            [Parameter(Mandatory = $true)][bool]$ShouldBeRunning
        )

        $serviceName = 'WSearch'
        $service = Get-Service -Name $serviceName -ErrorAction Stop
        Set-Service -Name $serviceName -StartupType $StartupType -ErrorAction Stop

        if ($ShouldBeRunning)
        {
            if ($service.Status -ne 'Running')
            {
                Start-Service -Name $serviceName -ErrorAction Stop
            }
        }
        else
        {
            if ($service.Status -ne 'Stopped')
            {
                Stop-Service -Name $serviceName -Force -ErrorAction Stop
            }
        }
        """;

    private static async Task<CommandResult> RunPowerShellScriptAsync(
        string scriptPath,
        string startupType,
        bool shouldBeRunning,
        bool useShellExecute,
        bool runElevated,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments =
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -StartupType {startupType} -ShouldBeRunning:${(shouldBeRunning ? "true" : "false")}",
            UseShellExecute = useShellExecute,
        };

        if (useShellExecute)
        {
            if (runElevated)
            {
                startInfo.Verb = "runas";
            }
        }
        else
        {
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
        }

        using var process = Process.Start(startInfo)
            ?? throw new IOException("Windows could not start the Windows Search helper.");

        if (!useShellExecute)
        {
            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return new CommandResult(
                process.ExitCode,
                await standardOutputTask.ConfigureAwait(false),
                await standardErrorTask.ConfigureAwait(false));
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return new CommandResult(process.ExitCode, string.Empty, string.Empty);
    }

    private static bool ContainsAccessDeniedMessage(params string[] values) =>
        values.Any(
            value =>
                !string.IsNullOrWhiteSpace(value) &&
                (value.Contains("Access is denied", StringComparison.OrdinalIgnoreCase)
                 || value.Contains("OpenService FAILED 5", StringComparison.OrdinalIgnoreCase)
                 || value.Contains("requires elevated rights", StringComparison.OrdinalIgnoreCase)
                 || value.Contains("permission", StringComparison.OrdinalIgnoreCase)));

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static void WriteShortcut(string shortcutPath, string targetPath, string arguments, string workingDirectory, string iconLocation)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is unavailable on this PC.");
        object? shell = null;
        object? shortcut = null;

        try
        {
            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Windows could not create a shortcut helper.");
            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: [shortcutPath]);
            if (shortcut is null)
            {
                throw new InvalidOperationException("Windows could not create the startup shortcut.");
            }

            SetShortcutProperty(shortcut, "TargetPath", targetPath);
            SetShortcutProperty(shortcut, "Arguments", arguments);
            SetShortcutProperty(shortcut, "WorkingDirectory", workingDirectory);
            SetShortcutProperty(shortcut, "IconLocation", $"{iconLocation},0");
            shortcut.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, args: null);
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static void SetShortcutProperty(object shortcut, string propertyName, object value)
    {
        shortcut.GetType().InvokeMember(
            propertyName,
            BindingFlags.SetProperty,
            binder: null,
            target: shortcut,
            args: [value]);
    }

    private static void ReleaseComObject(object? value)
    {
        try
        {
            if (value is not null && Marshal.IsComObject(value))
            {
                Marshal.ReleaseComObject(value);
            }
        }
        catch
        {
        }
    }

    private static void StartSearchReplacementScript(string autoHotkeyPath, string scriptPath)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = autoHotkeyPath,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? string.Empty,
                UseShellExecute = true,
            });
    }

    private static void StopSearchReplacementScript(string scriptPath)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                new ManagementScope(@"\\.\root\cimv2"),
                new ObjectQuery("SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name LIKE 'AutoHotkey%.exe'"));
            foreach (ManagementObject process in searcher.Get())
            {
                var commandLine = Convert.ToString(process["CommandLine"]);
                if (string.IsNullOrWhiteSpace(commandLine)
                    || !commandLine.Contains(scriptPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                process.InvokeMethod("Terminate", null);
            }
        }
        catch
        {
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path);
            }
        }
        catch
        {
        }
    }

    private sealed class WindowsSearchReplacementState
    {
        public string StartupType { get; set; } = "Automatic";

        public bool WasRunning { get; set; }
    }

    private sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
}
