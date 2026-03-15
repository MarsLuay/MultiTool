using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using Microsoft.Win32;

namespace AutoClicker.Infrastructure.Windows.Installer;

public sealed class WindowsFirefoxExtensionService : IFirefoxExtensionService
{
    private const int FirefoxRestartWaitMilliseconds = 5000;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly IReadOnlyList<FirefoxExtensionCatalogItem> Catalog =
    [
        new(
            "ublock-origin",
            "uBlock Origin",
            "Auto-install the Firefox content blocker from Mozilla Add-ons.",
            "uBlock0@raymondhill.net",
            "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin/latest.xpi"),
        new(
            "privacy-badger",
            "Privacy Badger",
            "Auto-install EFF's tracker blocking extension from Mozilla Add-ons.",
            "jid1-MnnxcxisBPnSXQ@jetpack",
            "https://addons.mozilla.org/firefox/downloads/latest/privacy-badger17/latest.xpi"),
    ];

    private readonly Func<string?> installDirectoryResolver;
    private readonly Func<bool> firefoxRunningDetector;
    private readonly Func<string, CancellationToken, Task<bool>> firefoxRestarter;
    private readonly Func<string, string, CancellationToken, Task> policyWriter;
    private readonly Func<string, string, CancellationToken, Task<bool>> elevatedPolicyWriter;

    public WindowsFirefoxExtensionService()
        : this(
            ResolveFirefoxInstallDirectory,
            IsFirefoxRunning,
            RestartFirefoxAsync,
            WritePolicyFileAsync,
            WritePolicyFileElevatedAsync)
    {
    }

    public WindowsFirefoxExtensionService(
        Func<string?> installDirectoryResolver,
        Func<bool>? firefoxRunningDetector = null,
        Func<string, CancellationToken, Task<bool>>? firefoxRestarter = null,
        Func<string, string, CancellationToken, Task>? policyWriter = null,
        Func<string, string, CancellationToken, Task<bool>>? elevatedPolicyWriter = null)
    {
        this.installDirectoryResolver = installDirectoryResolver;
        this.firefoxRunningDetector = firefoxRunningDetector ?? IsFirefoxRunning;
        this.firefoxRestarter = firefoxRestarter ?? RestartFirefoxAsync;
        this.policyWriter = policyWriter ?? WritePolicyFileAsync;
        this.elevatedPolicyWriter = elevatedPolicyWriter ?? WritePolicyFileElevatedAsync;
    }

    public IReadOnlyList<InstallerOptionDefinition> GetCatalog() =>
        [
            .. Catalog.Select(
                static item => new InstallerOptionDefinition(
                    item.OptionId,
                    item.DisplayName,
                    item.Description)),
        ];

    public async Task<IReadOnlyList<InstallerOperationResult>> SyncExtensionSelectionsAsync(
        IEnumerable<string> selectedOptionIds,
        CancellationToken cancellationToken = default)
    {
        var normalizedSelectedOptionIds = NormalizeOptionIds(selectedOptionIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedItems = Catalog
            .Where(item => normalizedSelectedOptionIds.Contains(item.OptionId))
            .ToArray();

        if (selectedItems.Length == 0 && !HasManagedPolicyFile())
        {
            return [];
        }

        var installDirectory = installDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
        {
            if (selectedItems.Length == 0)
            {
                return [];
            }

            return
            [
                .. selectedItems.Select(
                    static item => new InstallerOperationResult(
                        item.OptionId,
                        $"Firefox: {item.DisplayName}",
                        false,
                        false,
                        "Firefox could not be located. Install Firefox first, then try again.",
                        string.Empty)),
            ];
        }

        var policiesPath = Path.Combine(installDirectory, "distribution", "policies.json");
        var firefoxExecutablePath = Path.Combine(installDirectory, "firefox.exe");

        try
        {
            var root = await LoadPolicyRootAsync(policiesPath, cancellationToken).ConfigureAwait(false);
            var beforeJson = root.ToJsonString(SerializerOptions);

            var policies = GetOrCreateObject(root, "policies");
            var extensionSettings = GetOrCreateObject(policies, "ExtensionSettings");
            var selectedIds = selectedItems
                .Select(static item => item.OptionId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var extension in Catalog)
            {
                if (selectedIds.Contains(extension.OptionId))
                {
                    extensionSettings[extension.PolicyId] = new JsonObject
                    {
                        ["installation_mode"] = "normal_installed",
                        ["install_url"] = extension.InstallUrl,
                    };
                }
                else
                {
                    extensionSettings.Remove(extension.PolicyId);
                }
            }

            if (extensionSettings.Count == 0)
            {
                policies.Remove("ExtensionSettings");
            }

            if (policies.Count == 0)
            {
                root.Remove("policies");
            }

            var afterJson = root.ToJsonString(SerializerOptions);
            var changed = !string.Equals(beforeJson, afterJson, StringComparison.Ordinal);

            if (changed)
            {
                var restartNeeded = firefoxRunningDetector();
                var writeSucceeded = await TryWritePoliciesAsync(policiesPath, afterJson, cancellationToken).ConfigureAwait(false);
                if (!writeSucceeded)
                {
                    return BuildFailureResults(
                        selectedItems,
                        "Firefox add-ons need administrator permission to update Firefox's policies. Approve the Windows prompt and try again.",
                        afterJson);
                }

                var restarted = await TryRestartFirefoxAsync(firefoxExecutablePath, restartNeeded, cancellationToken).ConfigureAwait(false);
                var message = BuildSuccessMessage(changed, restartNeeded, restarted, clearingSelections: selectedItems.Length == 0);
                return BuildSuccessResults(selectedItems, message, afterJson, changed);
            }

            if (selectedItems.Length == 0)
            {
                return [];
            }

            return BuildSuccessResults(
                selectedItems,
                "Already configured for automatic install.",
                afterJson,
                changed: false);
        }
        catch (UnauthorizedAccessException ex)
        {
            return BuildFailureResults(
                selectedItems,
                "Firefox add-ons need permission to write Firefox's policies.json file.",
                ex.ToString());
        }
        catch (Exception ex)
        {
            return BuildFailureResults(
                selectedItems,
                ex.Message,
                ex.ToString());
        }
    }

    private async Task<bool> TryWritePoliciesAsync(string policiesPath, string policyJson, CancellationToken cancellationToken)
    {
        try
        {
            await policyWriter(policiesPath, policyJson, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return await elevatedPolicyWriter(policiesPath, policyJson, cancellationToken).ConfigureAwait(false);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return false;
        }
    }

    private async Task<bool> TryRestartFirefoxAsync(string firefoxExecutablePath, bool restartNeeded, CancellationToken cancellationToken)
    {
        if (!restartNeeded || !File.Exists(firefoxExecutablePath))
        {
            return false;
        }

        try
        {
            return await firefoxRestarter(firefoxExecutablePath, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    private bool HasManagedPolicyFile()
    {
        var installDirectory = installDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
        {
            return false;
        }

        var policiesPath = Path.Combine(installDirectory, "distribution", "policies.json");
        return File.Exists(policiesPath);
    }

    private static IReadOnlyList<string> NormalizeOptionIds(IEnumerable<string> optionIds)
    {
        var normalizedIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var optionId in optionIds)
        {
            if (string.IsNullOrWhiteSpace(optionId))
            {
                continue;
            }

            var normalized = optionId.Trim();
            if (seen.Add(normalized))
            {
                normalizedIds.Add(normalized);
            }
        }

        return normalizedIds;
    }

    private static IReadOnlyList<InstallerOperationResult> BuildFailureResults(
        IReadOnlyList<FirefoxExtensionCatalogItem> selectedItems,
        string message,
        string details)
    {
        if (selectedItems.Count == 0)
        {
            return
            [
                new InstallerOperationResult(
                    "firefox-extension-sync",
                    "Firefox add-ons",
                    false,
                    false,
                    message,
                    details),
            ];
        }

        return
        [
            .. selectedItems.Select(
                item => new InstallerOperationResult(
                    item.OptionId,
                    $"Firefox: {item.DisplayName}",
                    false,
                    false,
                    message,
                    details)),
        ];
    }

    private static IReadOnlyList<InstallerOperationResult> BuildSuccessResults(
        IReadOnlyList<FirefoxExtensionCatalogItem> selectedItems,
        string message,
        string output,
        bool changed)
    {
        if (selectedItems.Count == 0)
        {
            return
            [
                new InstallerOperationResult(
                    "firefox-extension-sync",
                    "Firefox add-ons",
                    true,
                    changed,
                    message,
                    output),
            ];
        }

        return
        [
            .. selectedItems.Select(
                item => new InstallerOperationResult(
                    item.OptionId,
                    $"Firefox: {item.DisplayName}",
                    true,
                    changed,
                    message,
                    output)),
        ];
    }

    private static string BuildSuccessMessage(bool changed, bool restartNeeded, bool restarted, bool clearingSelections)
    {
        if (!changed)
        {
            return clearingSelections
                ? "Firefox add-on auto-install settings were already cleared."
                : "Already configured for automatic install.";
        }

        if (clearingSelections)
        {
            if (restartNeeded && restarted)
            {
                return "Cleared Firefox add-on auto-install settings and restarted Firefox.";
            }

            if (restartNeeded)
            {
                return "Cleared Firefox add-on auto-install settings, but Firefox could not be restarted automatically. Restart Firefox to finish applying the change.";
            }

            return "Cleared Firefox add-on auto-install settings.";
        }

        if (restartNeeded && restarted)
        {
            return "Configured for automatic install and restarted Firefox.";
        }

        if (restartNeeded)
        {
            return "Configured for automatic install, but Firefox could not be restarted automatically. Restart Firefox to finish applying the add-ons.";
        }

        return "Configured for automatic install.";
    }

    private static async Task<JsonObject> LoadPolicyRootAsync(string policiesPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(policiesPath))
        {
            return new JsonObject();
        }

        var json = await File.ReadAllTextAsync(policiesPath, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        var node = JsonNode.Parse(json);
        if (node is null)
        {
            return new JsonObject();
        }

        if (node is not JsonObject jsonObject)
        {
            throw new InvalidDataException("Firefox policies.json must contain a JSON object.");
        }

        return jsonObject;
    }

    private static JsonObject GetOrCreateObject(JsonObject root, string propertyName)
    {
        if (root[propertyName] is JsonObject existingObject)
        {
            return existingObject;
        }

        var created = new JsonObject();
        root[propertyName] = created;
        return created;
    }

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

    private static async Task WritePolicyFileAsync(string policiesPath, string policyJson, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(policiesPath)
            ?? throw new InvalidOperationException("Firefox policy path is invalid.");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(policiesPath, policyJson, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> WritePolicyFileElevatedAsync(string policiesPath, string policyJson, CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "MultiTool", "firefox-policy", Guid.NewGuid().ToString("N"));
        var tempPolicyPath = Path.Combine(tempDirectory, "policies.json");
        var tempScriptPath = Path.Combine(tempDirectory, "write-firefox-policy.ps1");

        Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(tempPolicyPath, policyJson, cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(
                tempScriptPath,
                """
                param(
                    [Parameter(Mandatory = $true)][string]$SourcePath,
                    [Parameter(Mandatory = $true)][string]$DestinationPath
                )

                $directory = Split-Path -Parent $DestinationPath
                if (-not [string]::IsNullOrWhiteSpace($directory))
                {
                    New-Item -ItemType Directory -Path $directory -Force | Out-Null
                }

                Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
                """,
                cancellationToken).ConfigureAwait(false);

            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScriptPath}\" -SourcePath \"{tempPolicyPath}\" -DestinationPath \"{policiesPath}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                });

            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return false;
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

    private static bool IsFirefoxRunning() =>
        Process.GetProcessesByName("firefox").Length > 0;

    private static async Task<bool> RestartFirefoxAsync(string firefoxExecutablePath, CancellationToken cancellationToken)
    {
        var processes = Process.GetProcessesByName("firefox");
        try
        {
            if (processes.Length == 0)
            {
                return false;
            }

            foreach (var process in processes.Where(process => !process.HasExited))
            {
                try
                {
                    process.CloseMainWindow();
                }
                catch
                {
                }
            }

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            foreach (var process in processes.Where(process => !process.HasExited))
            {
                try
                {
                    if (!process.WaitForExit(FirefoxRestartWaitMilliseconds))
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(FirefoxRestartWaitMilliseconds);
                    }
                }
                catch
                {
                }
            }

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = firefoxExecutablePath,
                    WorkingDirectory = Path.GetDirectoryName(firefoxExecutablePath) ?? string.Empty,
                    UseShellExecute = true,
                });

            return true;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private sealed record FirefoxExtensionCatalogItem(
        string OptionId,
        string DisplayName,
        string Description,
        string PolicyId,
        string InstallUrl);
}
