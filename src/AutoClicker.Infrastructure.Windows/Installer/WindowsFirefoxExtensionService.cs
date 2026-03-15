using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using Microsoft.Win32;

namespace AutoClicker.Infrastructure.Windows.Installer;

public sealed class WindowsFirefoxExtensionService : IFirefoxExtensionService
{
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

    public WindowsFirefoxExtensionService()
        : this(ResolveFirefoxInstallDirectory)
    {
    }

    public WindowsFirefoxExtensionService(Func<string?> installDirectoryResolver)
    {
        this.installDirectoryResolver = installDirectoryResolver;
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
                var directory = Path.GetDirectoryName(policiesPath)
                    ?? throw new InvalidOperationException("Firefox policy path is invalid.");
                Directory.CreateDirectory(directory);
                await File.WriteAllTextAsync(policiesPath, afterJson, cancellationToken).ConfigureAwait(false);
            }

            if (selectedItems.Length == 0)
            {
                return changed
                    ?
                    [
                        new InstallerOperationResult(
                            "firefox-extension-sync",
                            "Firefox add-ons",
                            true,
                            true,
                            "Cleared Firefox add-on auto-install settings.",
                            afterJson),
                    ]
                    : [];
            }

            var message = changed
                ? "Configured for automatic install. Restart Firefox if it is already open."
                : "Already configured for automatic install.";

            return
            [
                .. selectedItems.Select(
                    item => new InstallerOperationResult(
                        item.OptionId,
                        $"Firefox: {item.DisplayName}",
                        true,
                        changed,
                        message,
                        afterJson)),
            ];
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

    private sealed record FirefoxExtensionCatalogItem(
        string OptionId,
        string DisplayName,
        string Description,
        string PolicyId,
        string InstallUrl);
}
