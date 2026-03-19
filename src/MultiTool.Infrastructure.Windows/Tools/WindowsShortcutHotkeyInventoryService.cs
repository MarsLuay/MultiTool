using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

public sealed class WindowsShortcutHotkeyInventoryService : IShortcutHotkeyInventoryService
{
    private const int AppShortcutConfigSearchDepth = 6;

    private static readonly HashSet<string> IgnoredShortcutConfigDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "AC",
        "AppDataCache",
        "blob_storage",
        "Cache",
        "Caches",
        "Code Cache",
        "CrashDumps",
        "Crashpad",
        "extensions",
        "GPUCache",
        "IndexedDB",
        "Local Storage",
        "Logs",
        "node_modules",
        "Service Worker",
        "Session Storage",
        "Temp",
        "TempState",
        "tmp",
    };

    private static readonly HashSet<string> GenericShortcutConfigPathSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        ".config",
        "AppData",
        "Config",
        "Configuration",
        "Data",
        "Default",
        "Defaults",
        "keymap",
        "keymaps",
        "Local",
        "LocalState",
        "Packages",
        "Profile",
        "Profiles",
        "Roaming",
        "Setting",
        "Settings",
        "Storage",
        "User",
        "Users",
    };

    private readonly Func<IEnumerable<string>> scanRootResolver;
    private readonly Func<string, (string Hotkey, string TargetPath)?> shortcutMetadataReader;
    private readonly Func<string, int?> exactFolderCountResolver;
    private readonly Func<IReadOnlyList<ShortcutHotkeyInfo>> referenceShortcutResolver;
    private readonly Func<IReadOnlyList<ShortcutHotkeyInfo>> knownApplicationShortcutResolver;

    public WindowsShortcutHotkeyInventoryService()
        : this(
            ResolveScanRoots,
            ReadShortcutMetadata,
            NtfsFolderCountProvider.TryGetExactFolderCount,
            BuildReferenceShortcuts,
            BuildKnownApplicationShortcuts)
    {
    }

    public WindowsShortcutHotkeyInventoryService(
        Func<IEnumerable<string>> scanRootResolver,
        Func<string, (string Hotkey, string TargetPath)?> shortcutMetadataReader)
        : this(scanRootResolver, shortcutMetadataReader, _ => null, static () => [], static () => [])
    {
    }

    public WindowsShortcutHotkeyInventoryService(
        Func<IEnumerable<string>> scanRootResolver,
        Func<string, (string Hotkey, string TargetPath)?> shortcutMetadataReader,
        Func<string, int?> exactFolderCountResolver)
        : this(scanRootResolver, shortcutMetadataReader, exactFolderCountResolver, static () => [], static () => [])
    {
    }

    public WindowsShortcutHotkeyInventoryService(
        Func<IEnumerable<string>> scanRootResolver,
        Func<string, (string Hotkey, string TargetPath)?> shortcutMetadataReader,
        Func<string, int?> exactFolderCountResolver,
        Func<IReadOnlyList<ShortcutHotkeyInfo>> referenceShortcutResolver,
        Func<IReadOnlyList<ShortcutHotkeyInfo>>? knownApplicationShortcutResolver = null)
    {
        this.scanRootResolver = scanRootResolver ?? throw new ArgumentNullException(nameof(scanRootResolver));
        this.shortcutMetadataReader = shortcutMetadataReader ?? throw new ArgumentNullException(nameof(shortcutMetadataReader));
        this.exactFolderCountResolver = exactFolderCountResolver ?? throw new ArgumentNullException(nameof(exactFolderCountResolver));
        this.referenceShortcutResolver = referenceShortcutResolver ?? throw new ArgumentNullException(nameof(referenceShortcutResolver));
        this.knownApplicationShortcutResolver = knownApplicationShortcutResolver ?? (() => Array.Empty<ShortcutHotkeyInfo>());
    }

    public Task<ShortcutHotkeyScanResult> ScanAsync(
        IProgress<ShortcutHotkeyScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                var shortcuts = new List<ShortcutHotkeyInfo>();
                var warnings = new List<string>();
                var seenShortcutPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var progressState = new ScanProgressState(progress);

                foreach (var rootPath in scanRootResolver()
                             .Where(static path => !string.IsNullOrWhiteSpace(path))
                             .Select(static path => Path.GetFullPath(path))
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    int? exactFolderCount;
                    try
                    {
                        exactFolderCount = exactFolderCountResolver(rootPath);
                    }
                    catch
                    {
                        exactFolderCount = null;
                    }

                    progressState.RegisterRoot(rootPath, exactFolderCount);

                    foreach (var shortcutPath in EnumerateShortcutFiles(rootPath, warnings, progressState, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progressState.MarkShortcutScanned(Path.GetDirectoryName(shortcutPath) ?? rootPath);

                        if (!seenShortcutPaths.Add(shortcutPath))
                        {
                            continue;
                        }

                        try
                        {
                            var metadata = shortcutMetadataReader(shortcutPath);
                            if (metadata is null || string.IsNullOrWhiteSpace(metadata.Value.Hotkey))
                            {
                                continue;
                            }

                            var targetPath = metadata.Value.TargetPath?.Trim() ?? string.Empty;
                            shortcuts.Add(
                                new ShortcutHotkeyInfo(
                                    NormalizeHotkey(metadata.Value.Hotkey),
                                    Path.GetFileNameWithoutExtension(shortcutPath),
                                    shortcutPath,
                                    Path.GetDirectoryName(shortcutPath) ?? string.Empty,
                                    targetPath,
                                    PathExists(targetPath),
                                    "Detected shortcut file",
                                    "Windows .lnk hotkey",
                                    string.IsNullOrWhiteSpace(targetPath)
                                        ? "Assigned on a Windows shortcut file."
                                        : $"Target: {targetPath}",
                                    CanDisable: true));
                        }
                        catch (Exception ex)
                        {
                            warnings.Add($"Skipped shortcut metadata in {shortcutPath}: {ex.Message}");
                        }
                    }
                }

                shortcuts.AddRange(referenceShortcutResolver());
                shortcuts.AddRange(knownApplicationShortcutResolver());
                shortcuts = DeduplicateShortcuts(shortcuts);

                shortcuts.Sort(
                    static (left, right) =>
                    {
                        var hotkeyComparison = StringComparer.OrdinalIgnoreCase.Compare(left.Hotkey, right.Hotkey);
                        if (hotkeyComparison != 0)
                        {
                            return hotkeyComparison;
                        }

                        var nameComparison = StringComparer.OrdinalIgnoreCase.Compare(left.ShortcutName, right.ShortcutName);
                        return nameComparison != 0
                            ? nameComparison
                            : StringComparer.OrdinalIgnoreCase.Compare(left.ShortcutPath, right.ShortcutPath);
                    });

                var shortcutsWithConflicts = ApplyConflictMetadata(shortcuts);
                var conflictingShortcuts = shortcutsWithConflicts.Count(shortcut => shortcut.HasConflict);
                var conflictGroups = shortcutsWithConflicts
                    .Where(shortcut => shortcut.HasConflict)
                    .Select(shortcut => shortcut.Hotkey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                return new ShortcutHotkeyScanResult(
                    shortcutsWithConflicts,
                    seenShortcutPaths.Count,
                    warnings,
                    conflictGroups,
                    conflictingShortcuts);
            },
            cancellationToken);
    }

    private static IEnumerable<string> EnumerateShortcutFiles(
        string rootPath,
        ICollection<string> warnings,
        ScanProgressState progressState,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            yield break;
        }

        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentPath = pendingDirectories.Pop();

            try
            {
                string[] files;
                try
                {
                    files = Directory.GetFiles(currentPath, "*.lnk");
                }
                catch (Exception ex)
                {
                    warnings.Add($"Skipped shortcut files in {currentPath}: {ex.Message}");
                    continue;
                }

                foreach (var filePath in files)
                {
                    yield return filePath;
                }

                string[] subdirectories;
                try
                {
                    subdirectories = Directory.GetDirectories(currentPath);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Skipped folders in {currentPath}: {ex.Message}");
                    continue;
                }

                progressState.AddDiscoveredDirectories(subdirectories.Length, currentPath);

                foreach (var subdirectoryPath in subdirectories)
                {
                    pendingDirectories.Push(subdirectoryPath);
                }
            }
            finally
            {
                progressState.MarkDirectoryCompleted(currentPath);
            }
        }
    }

    private static IEnumerable<string> ResolveScanRoots()
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
            {
                continue;
            }

            yield return drive.RootDirectory.FullName;
        }
    }

    private static (string Hotkey, string TargetPath)? ReadShortcutMetadata(string shortcutPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            throw new InvalidOperationException("Windows Script Host is unavailable on this PC.");
        }

        object? shell = null;
        object? shortcut = null;

        try
        {
            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Windows Script Host could not be created.");
            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: [shortcutPath]);

            if (shortcut is null)
            {
                return null;
            }

            var hotkey = ReadShortcutProperty(shortcut, "Hotkey");
            if (string.IsNullOrWhiteSpace(hotkey))
            {
                return null;
            }

            return (hotkey.Trim(), ReadShortcutProperty(shortcut, "TargetPath")?.Trim() ?? string.Empty);
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static string? ReadShortcutProperty(object shortcut, string propertyName)
    {
        var value = shortcut.GetType().InvokeMember(
            propertyName,
            BindingFlags.GetProperty,
            binder: null,
            target: shortcut,
            args: null);
        return value as string;
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }

    private static bool PathExists(string path) =>
        !string.IsNullOrWhiteSpace(path)
        && (File.Exists(path) || Directory.Exists(path));

    private static string NormalizeHotkey(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return string.Empty;
        }

        return string.Join(
            " + ",
            hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(
                    static token => token.ToUpperInvariant() switch
                    {
                        "CTRL" => "Ctrl",
                        "ALT" => "Alt",
                        "SHIFT" => "Shift",
                        "EXT" => "Ext",
                        _ => token,
                    }));
    }

    private static List<ShortcutHotkeyInfo> DeduplicateShortcuts(List<ShortcutHotkeyInfo> shortcuts)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduped = new List<ShortcutHotkeyInfo>(shortcuts.Count);

        foreach (var shortcut in shortcuts)
        {
            var dedupeKey = string.Join(
                "|",
                shortcut.Hotkey.Trim(),
                shortcut.ShortcutName.Trim(),
                shortcut.ShortcutPath.Trim(),
                shortcut.SourceLabel.Trim());

            if (seen.Add(dedupeKey))
            {
                deduped.Add(shortcut);
            }
        }

        return deduped;
    }

    private static IReadOnlyList<ShortcutHotkeyInfo> ApplyConflictMetadata(IReadOnlyList<ShortcutHotkeyInfo> shortcuts)
    {
        var conflictsByHotkey = shortcuts
            .GroupBy(shortcut => shortcut.Hotkey, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        if (conflictsByHotkey.Count == 0)
        {
            return shortcuts;
        }

        return shortcuts
            .Select(
                shortcut =>
                {
                    if (!conflictsByHotkey.TryGetValue(shortcut.Hotkey, out var conflictingGroup))
                    {
                        return shortcut;
                    }

                    var conflictingNames = conflictingGroup
                        .Where(conflictingShortcut => !string.Equals(conflictingShortcut.ShortcutPath, shortcut.ShortcutPath, StringComparison.OrdinalIgnoreCase))
                        .Select(conflictingShortcut => conflictingShortcut.ShortcutName)
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    return shortcut with
                    {
                        HasConflict = conflictingNames.Length > 0,
                        ConflictCount = conflictingNames.Length,
                        ConflictSummary = BuildConflictSummary(conflictingNames),
                    };
                })
            .ToArray();
    }

    private static string BuildConflictSummary(IReadOnlyList<string> conflictingNames)
    {
        if (conflictingNames.Count == 0)
        {
            return string.Empty;
        }

        if (conflictingNames.Count == 1)
        {
            return $"Also assigned to {conflictingNames[0]}";
        }

        if (conflictingNames.Count == 2)
        {
            return $"Also assigned to {conflictingNames[0]} and {conflictingNames[1]}";
        }

        return $"Also assigned to {conflictingNames[0]}, {conflictingNames[1]}, and {conflictingNames.Count - 2} other shortcut{(conflictingNames.Count - 2 == 1 ? string.Empty : "s")}";
    }

    private static IReadOnlyList<ShortcutHotkeyInfo> BuildReferenceShortcuts() =>
    [
        CreateReferenceShortcut("Ctrl + C", "Copy", "Most apps and File Explorer", "Copies the selected text, file, or item."),
        CreateReferenceShortcut("Ctrl + V", "Paste", "Most apps and File Explorer", "Pastes the clipboard contents."),
        CreateReferenceShortcut("Ctrl + X", "Cut", "Most apps and File Explorer", "Cuts the selected text, file, or item."),
        CreateReferenceShortcut("Ctrl + Z", "Undo", "Most apps", "Reverses the last change."),
        CreateReferenceShortcut("Ctrl + Y", "Redo", "Most apps", "Reapplies the last undone change."),
        CreateReferenceShortcut("Ctrl + A", "Select all", "Most apps and File Explorer", "Selects everything in the current view."),
        CreateReferenceShortcut("Ctrl + S", "Save", "Most apps", "Saves the current file or document."),
        CreateReferenceShortcut("Ctrl + O", "Open", "Many desktop apps", "Opens a file chooser or existing item."),
        CreateReferenceShortcut("Ctrl + N", "New", "Many desktop apps", "Creates a new item, file, or window."),
        CreateReferenceShortcut("Ctrl + P", "Print", "Many desktop apps", "Opens the print flow."),
        CreateReferenceShortcut("Ctrl + F", "Find", "Most apps and browsers", "Searches in the current page, file, or view."),
        CreateReferenceShortcut("Ctrl + W", "Close tab or document", "Browsers and many apps", "Closes the current tab, pane, or document."),
        CreateReferenceShortcut("Ctrl + T", "New tab", "Browsers and tabbed apps", "Opens a new tab."),
        CreateReferenceShortcut("Ctrl + Shift + T", "Reopen closed tab", "Browsers and some tabbed apps", "Restores the last closed tab."),
        CreateReferenceShortcut("Alt + Tab", "Switch apps", "Windows", "Cycles through open apps."),
        CreateReferenceShortcut("Alt + F4", "Close current window", "Windows", "Closes the active window or app."),
        CreateReferenceShortcut("Ctrl + Shift + Esc", "Task Manager", "Windows", "Opens Task Manager directly."),
        CreateReferenceShortcut("Win + E", "File Explorer", "Windows", "Opens File Explorer."),
        CreateReferenceShortcut("Win + R", "Run", "Windows", "Opens the Run dialog."),
        CreateReferenceShortcut("Win + D", "Show desktop", "Windows", "Minimizes or restores open windows."),
        CreateReferenceShortcut("Win + L", "Lock PC", "Windows", "Locks the current Windows session."),
        CreateReferenceShortcut("Win + V", "Clipboard history", "Windows", "Opens clipboard history if enabled."),
        CreateReferenceShortcut("Win + Shift + S", "Screen snip", "Windows", "Starts the Snipping Tool capture overlay."),
        CreateReferenceShortcut("Win + I", "Settings", "Windows", "Opens Windows Settings."),
        CreateReferenceShortcut("Win + Tab", "Task view", "Windows", "Opens Task View and virtual desktops."),
    ];

    private static ShortcutHotkeyInfo CreateReferenceShortcut(
        string hotkey,
        string shortcutName,
        string appliesTo,
        string details) =>
        new(
            hotkey,
            shortcutName,
            string.Empty,
            string.Empty,
            string.Empty,
            TargetExists: false,
            "Windows / common shortcut",
            appliesTo,
            details,
            IsReferenceShortcut: true);

    private static IReadOnlyList<ShortcutHotkeyInfo> BuildKnownApplicationShortcuts()
    {
        var shortcuts = new List<ShortcutHotkeyInfo>();
        var configRoots = ResolveCompatibleAppConfigRoots().ToArray();

        shortcuts.AddRange(ReadVsCodeLikeShortcuts(ResolveVsCodeLikeKeybindingFiles(configRoots)));
        shortcuts.AddRange(ReadHotkeysJsonShortcuts(ResolveHotkeysJsonFiles(configRoots)));
        shortcuts.AddRange(ReadJetBrainsShortcuts(ResolveJetBrainsKeymapFiles(configRoots)));
        shortcuts.AddRange(ReadFlowLauncherShortcuts(ResolveFlowLauncherSettingsFiles()));
        shortcuts.AddRange(ReadWindowsTerminalShortcuts(ResolveWindowsTerminalSettingsFiles()));
        shortcuts.AddRange(ReadAutoHotkeyScriptShortcuts(ResolveAutoHotkeyScriptFiles()));

        return shortcuts;
    }

    internal static IReadOnlyList<ShortcutHotkeyInfo> ReadVsCodeLikeShortcuts(IEnumerable<string> keybindingFilePaths)
    {
        var shortcuts = new List<ShortcutHotkeyInfo>();

        foreach (var filePath in keybindingFilePaths
                     .Where(static path => !string.IsNullOrWhiteSpace(path))
                     .Select(static path => Path.GetFullPath(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            using var document = TryReadJsonDocument(filePath);
            if (document is null)
            {
                continue;
            }

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!element.TryGetProperty("key", out var keyElement) || keyElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var rawKey = keyElement.GetString();
                if (string.IsNullOrWhiteSpace(rawKey))
                {
                    continue;
                }

                var command = element.TryGetProperty("command", out var commandElement) && commandElement.ValueKind == JsonValueKind.String
                    ? commandElement.GetString()
                    : null;
                var whenClause = element.TryGetProperty("when", out var whenElement) && whenElement.ValueKind == JsonValueKind.String
                    ? whenElement.GetString()
                    : null;
                var normalizedHotkey = NormalizeVsCodeHotkey(rawKey);
                if (string.IsNullOrWhiteSpace(normalizedHotkey))
                {
                    continue;
                }

                var details = string.IsNullOrWhiteSpace(whenClause)
                    ? $"Detected from {Path.GetFileName(filePath)}"
                    : $"Detected from {Path.GetFileName(filePath)}. When: {Truncate(whenClause, 120)}";

                shortcuts.Add(new ShortcutHotkeyInfo(
                    normalizedHotkey,
                    string.IsNullOrWhiteSpace(command) ? "User keybinding" : command,
                    filePath,
                    Path.GetDirectoryName(filePath) ?? string.Empty,
                    string.Empty,
                    TargetExists: false,
                    "Detected app keymap",
                    InferShortcutApplicationName(filePath, "Desktop app"),
                    details));
            }
        }

        return shortcuts;
    }

    internal static IReadOnlyList<ShortcutHotkeyInfo> ReadHotkeysJsonShortcuts(IEnumerable<string> hotkeyFilePaths)
    {
        var shortcuts = new List<ShortcutHotkeyInfo>();

        foreach (var filePath in hotkeyFilePaths
                     .Where(static path => !string.IsNullOrWhiteSpace(path))
                     .Select(static path => Path.GetFullPath(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            using var document = TryReadJsonDocument(filePath);
            if (document is null)
            {
                continue;
            }

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var commandProperty in document.RootElement.EnumerateObject())
            {
                if (commandProperty.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var keybinding in commandProperty.Value.EnumerateArray())
                {
                    var hotkey = TryParseObsidianHotkey(keybinding);
                    if (string.IsNullOrWhiteSpace(hotkey))
                    {
                        continue;
                    }

                    shortcuts.Add(new ShortcutHotkeyInfo(
                        hotkey,
                        commandProperty.Name,
                        filePath,
                        Path.GetDirectoryName(filePath) ?? string.Empty,
                        string.Empty,
                        TargetExists: false,
                        "Detected app keymap",
                        InferShortcutApplicationName(filePath, "Desktop app"),
                        $"Detected from {Path.GetFileName(filePath)}"));
                }
            }
        }

        return shortcuts;
    }

    internal static IReadOnlyList<ShortcutHotkeyInfo> ReadJetBrainsShortcuts(IEnumerable<string> keymapFilePaths)
    {
        var shortcuts = new List<ShortcutHotkeyInfo>();

        foreach (var filePath in keymapFilePaths
                     .Where(static path => !string.IsNullOrWhiteSpace(path))
                     .Select(static path => Path.GetFullPath(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            XDocument? document = null;
            try
            {
                document = XDocument.Load(filePath);
                foreach (var actionElement in document.Descendants("action"))
                {
                    var actionId = actionElement.Attribute("id")?.Value?.Trim();
                    if (string.IsNullOrWhiteSpace(actionId))
                    {
                        continue;
                    }

                    foreach (var shortcutElement in actionElement.Elements("keyboard-shortcut"))
                    {
                        var first = shortcutElement.Attribute("first-keystroke")?.Value;
                        var second = shortcutElement.Attribute("second-keystroke")?.Value;
                        var hotkey = NormalizeJetBrainsShortcut(first, second);

                        if (string.IsNullOrWhiteSpace(hotkey))
                        {
                            continue;
                        }

                        shortcuts.Add(new ShortcutHotkeyInfo(
                            hotkey,
                            actionId,
                            filePath,
                            Path.GetDirectoryName(filePath) ?? string.Empty,
                            string.Empty,
                            TargetExists: false,
                            "Detected app keymap",
                            InferShortcutApplicationName(filePath, "JetBrains IDE"),
                            "Detected from keymap XML"));
                    }
                }
            }
            catch
            {
            }
            finally
            {
                document?.Root?.Remove();
            }
        }

        return shortcuts;
    }

    internal static IReadOnlyList<ShortcutHotkeyInfo> ReadWindowsTerminalShortcuts(IEnumerable<string> settingsFilePaths)
    {
        var shortcuts = new List<ShortcutHotkeyInfo>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in settingsFilePaths
                     .Where(static path => !string.IsNullOrWhiteSpace(path))
                     .Select(static path => Path.GetFullPath(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!seenFiles.Add(filePath) || !File.Exists(filePath))
            {
                continue;
            }

            using var document = TryReadJsonDocument(filePath);
            if (document is null || document.RootElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var actionsElement = document.RootElement.TryGetProperty("actions", out var actions)
                ? actions
                : document.RootElement.TryGetProperty("keybindings", out var legacyActions)
                    ? legacyActions
                    : default;

            if (actionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var actionElement in actionsElement.EnumerateArray())
            {
                if (actionElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var keys = ReadWindowsTerminalKeys(actionElement);
                if (keys.Count == 0)
                {
                    continue;
                }

                var commandName = GetWindowsTerminalCommandName(actionElement);
                var details = GetWindowsTerminalDetails(actionElement);

                foreach (var hotkey in keys)
                {
                    shortcuts.Add(new ShortcutHotkeyInfo(
                        hotkey,
                        commandName,
                        filePath,
                        Path.GetDirectoryName(filePath) ?? string.Empty,
                        string.Empty,
                        TargetExists: false,
                        "Detected app keymap",
                        "Windows Terminal",
                        details));
                }
            }
        }

        return shortcuts;
    }

    internal static IReadOnlyList<ShortcutHotkeyInfo> ReadFlowLauncherShortcuts(IEnumerable<string> settingsFilePaths)
    {
        var shortcuts = new List<ShortcutHotkeyInfo>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in settingsFilePaths
                     .Where(static path => !string.IsNullOrWhiteSpace(path))
                     .Select(static path => Path.GetFullPath(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!seenFiles.Add(filePath) || !File.Exists(filePath))
            {
                continue;
            }

            using var document = TryReadJsonDocument(filePath);
            if (document is null || document.RootElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var root = document.RootElement;
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "Hotkey", "Open Flow Launcher", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "PreviewHotkey", "Preview selected result", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "AutoCompleteHotkey", "Autocomplete selected result", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "AutoCompleteHotkey2", "Autocomplete selected result (alternate)", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "SelectNextItemHotkey", "Select next result", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "SelectNextItemHotkey2", "Select next result (alternate)", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "SelectPrevItemHotkey", "Select previous result", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "SelectPrevItemHotkey2", "Select previous result (alternate)", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "SettingWindowHotkey", "Open settings", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "OpenHistoryHotkey", "Open history", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "OpenContextMenuHotkey", "Open result context menu", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "SelectNextPageHotkey", "Select next page", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "SelectPrevPageHotkey", "Select previous page", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "CycleHistoryUpHotkey", "Cycle history up", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "CycleHistoryDownHotkey", "Cycle history down", filePath);
            AddFlowLauncherShortcutIfPresent(shortcuts, root, "DialogJumpHotkey", "Dialog jump", filePath);

            var showOpenResultHotkey = !root.TryGetProperty("ShowOpenResultHotkey", out var showOpenResultHotkeyElement)
                || showOpenResultHotkeyElement.ValueKind != JsonValueKind.False;
            if (showOpenResultHotkey
                && root.TryGetProperty("OpenResultModifiers", out var openResultModifiersElement)
                && openResultModifiersElement.ValueKind == JsonValueKind.String)
            {
                var openResultModifiers = openResultModifiersElement.GetString();
                if (!string.IsNullOrWhiteSpace(openResultModifiers))
                {
                    for (var index = 1; index <= 10; index++)
                    {
                        var digit = index == 10 ? "0" : index.ToString();
                        var hotkey = NormalizeFlowLauncherHotkey($"{openResultModifiers} + {digit}");
                        if (string.IsNullOrWhiteSpace(hotkey))
                        {
                            continue;
                        }

                        shortcuts.Add(new ShortcutHotkeyInfo(
                            hotkey,
                            $"Open result {index}",
                            filePath,
                            Path.GetDirectoryName(filePath) ?? string.Empty,
                            string.Empty,
                            TargetExists: false,
                            "Detected app settings",
                            "Flow Launcher",
                            "Detected from Flow Launcher Settings.json (OpenResultModifiers)."));
                    }
                }
            }

            if (!root.TryGetProperty("CustomPluginHotkeys", out var customPluginHotkeysElement)
                || customPluginHotkeysElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var customHotkeyElement in customPluginHotkeysElement.EnumerateArray())
            {
                if (customHotkeyElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var rawHotkey = customHotkeyElement.TryGetProperty("Hotkey", out var customHotkeyValue)
                    && customHotkeyValue.ValueKind == JsonValueKind.String
                    ? customHotkeyValue.GetString()
                    : null;
                var hotkey = NormalizeFlowLauncherHotkey(rawHotkey);
                if (string.IsNullOrWhiteSpace(hotkey))
                {
                    continue;
                }

                var actionKeyword = customHotkeyElement.TryGetProperty("ActionKeyword", out var actionKeywordElement)
                    && actionKeywordElement.ValueKind == JsonValueKind.String
                    ? actionKeywordElement.GetString()?.Trim()
                    : null;
                var shortcutName = string.IsNullOrWhiteSpace(actionKeyword)
                    ? "Custom query hotkey"
                    : $"Query \"{Truncate(actionKeyword, 48)}\"";
                var details = string.IsNullOrWhiteSpace(actionKeyword)
                    ? "Detected from Flow Launcher Settings.json (CustomPluginHotkeys)."
                    : $"Detected from Flow Launcher Settings.json (CustomPluginHotkeys). Action keyword: {Truncate(actionKeyword, 64)}";

                shortcuts.Add(new ShortcutHotkeyInfo(
                    hotkey,
                    shortcutName,
                    filePath,
                    Path.GetDirectoryName(filePath) ?? string.Empty,
                    string.Empty,
                    TargetExists: false,
                    "Detected app settings",
                    "Flow Launcher",
                    details));
            }
        }

        return shortcuts;
    }

    internal static IReadOnlyList<ShortcutHotkeyInfo> ReadAutoHotkeyScriptShortcuts(IEnumerable<string> scriptFilePaths)
    {
        var shortcuts = new List<ShortcutHotkeyInfo>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in scriptFilePaths
                     .Where(static path => !string.IsNullOrWhiteSpace(path))
                     .Select(static path => Path.GetFullPath(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!seenFiles.Add(filePath) || !File.Exists(filePath))
            {
                continue;
            }

            try
            {
                var currentContext = string.Empty;
                var lineNumber = 0;

                foreach (var rawLine in File.ReadLines(filePath))
                {
                    lineNumber++;
                    var line = StripAutoHotkeyComment(rawLine).Trim();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (TryParseAutoHotkeyContextDirective(line, out var parsedContext))
                    {
                        currentContext = parsedContext;
                        continue;
                    }

                    if (line.StartsWith(':'))
                    {
                        continue;
                    }

                    var separatorIndex = line.IndexOf("::", StringComparison.Ordinal);
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    var rawHotkey = line[..separatorIndex].Trim();
                    var hotkey = NormalizeAutoHotkeyHotkey(rawHotkey);
                    if (string.IsNullOrWhiteSpace(hotkey))
                    {
                        continue;
                    }

                    var inlineAction = line[(separatorIndex + 2)..].Trim();
                    shortcuts.Add(new ShortcutHotkeyInfo(
                        hotkey,
                        BuildAutoHotkeyShortcutName(inlineAction, lineNumber),
                        filePath,
                        Path.GetDirectoryName(filePath) ?? string.Empty,
                        string.Empty,
                        TargetExists: false,
                        "Detected AutoHotkey script",
                        BuildAutoHotkeyAppliesTo(currentContext),
                        BuildAutoHotkeyDetails(currentContext, inlineAction, lineNumber)));
                }
            }
            catch
            {
            }
        }

        return shortcuts;
    }

    private static string NormalizeVsCodeHotkey(string hotkey)
    {
        var chordParts = hotkey.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var normalizedChordParts = chordParts
            .Select(
                static part =>
                    string.Join(
                        " + ",
                        part.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(NormalizeModifierOrKeyToken)))
            .Where(static part => !string.IsNullOrWhiteSpace(part));

        return string.Join(", ", normalizedChordParts);
    }

    private static string? TryParseObsidianHotkey(JsonElement keybinding)
    {
        if (keybinding.ValueKind == JsonValueKind.String)
        {
            var value = keybinding.GetString();
            return string.IsNullOrWhiteSpace(value)
                ? null
                : NormalizeVsCodeHotkey(value);
        }

        if (keybinding.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var key = keybinding.TryGetProperty("key", out var keyElement) && keyElement.ValueKind == JsonValueKind.String
            ? keyElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var parts = new List<string>();
        if (keybinding.TryGetProperty("modifiers", out var modifiersElement) && modifiersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var modifierElement in modifiersElement.EnumerateArray())
            {
                if (modifierElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var modifier = modifierElement.GetString();
                if (!string.IsNullOrWhiteSpace(modifier))
                {
                    parts.Add(NormalizeModifierOrKeyToken(modifier));
                }
            }
        }

        parts.Add(NormalizeModifierOrKeyToken(key));
        return string.Join(" + ", parts.Where(static part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string NormalizeJetBrainsShortcut(string? firstKeystroke, string? secondKeystroke)
    {
        var first = NormalizeJetBrainsKeystroke(firstKeystroke);
        if (string.IsNullOrWhiteSpace(first))
        {
            return string.Empty;
        }

        var second = NormalizeJetBrainsKeystroke(secondKeystroke);
        return string.IsNullOrWhiteSpace(second)
            ? first
            : $"{first}, {second}";
    }

    private static IEnumerable<string> ResolveWindowsTerminalSettingsFiles()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            return [];
        }

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(localAppData, "Microsoft", "Windows Terminal", "settings.json"),
            Path.Combine(localAppData, "Microsoft", "Windows Terminal Preview", "settings.json"),
        };

        var packagesRoot = Path.Combine(localAppData, "Packages");
        if (Directory.Exists(packagesRoot))
        {
            try
            {
                foreach (var packageDirectory in Directory.EnumerateDirectories(packagesRoot, "Microsoft.WindowsTerminal*"))
                {
                    candidates.Add(Path.Combine(packageDirectory, "LocalState", "settings.json"));
                }
            }
            catch
            {
            }
        }

        return candidates;
    }

    private static IEnumerable<string> ResolveFlowLauncherSettingsFiles()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rootPath in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowLauncher"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowLauncher"),
                 }.Where(static path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path)))
        {
            try
            {
                foreach (var filePath in Directory.EnumerateFiles(rootPath, "Settings.json", SearchOption.AllDirectories))
                {
                    if (filePath.EndsWith($"{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Settings.json", StringComparison.OrdinalIgnoreCase))
                    {
                        candidates.Add(filePath);
                    }
                }
            }
            catch
            {
            }
        }

        return candidates;
    }

    private static IEnumerable<string> ResolveCompatibleAppConfigRoots()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                string.IsNullOrWhiteSpace(userProfile) ? string.Empty : Path.Combine(userProfile, ".config"),
            }
            .Where(static path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            .Select(static path => Path.GetFullPath(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    internal static IEnumerable<string> ResolveVsCodeLikeKeybindingFiles(IEnumerable<string> rootPaths)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileName in new[] { "keybindings.json", "keyboard-shortcuts.json" })
        {
            foreach (var filePath in EnumerateNamedFiles(rootPaths, fileName, AppShortcutConfigSearchDepth))
            {
                if (ShouldIncludeVsCodeLikeKeybindingFile(filePath))
                {
                    candidates.Add(filePath);
                }
            }
        }

        return candidates;
    }

    internal static IEnumerable<string> ResolveHotkeysJsonFiles(IEnumerable<string> rootPaths) =>
        EnumerateNamedFiles(rootPaths, "hotkeys.json", AppShortcutConfigSearchDepth)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    internal static IEnumerable<string> ResolveJetBrainsKeymapFiles(IEnumerable<string> rootPaths)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directoryName in new[] { "keymaps", "keymap" })
        {
            foreach (var keymapDirectory in EnumerateNamedDirectories(rootPaths, directoryName, AppShortcutConfigSearchDepth))
            {
                try
                {
                    foreach (var filePath in Directory.EnumerateFiles(keymapDirectory, "*.xml", SearchOption.AllDirectories))
                    {
                        candidates.Add(Path.GetFullPath(filePath));
                    }
                }
                catch
                {
                }
            }
        }

        return candidates;
    }

    private static IEnumerable<string> ResolveAutoHotkeyScriptFiles()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var scriptPath in EnumerateRunningAutoHotkeyScriptPaths())
        {
            candidates.Add(scriptPath);
        }

        foreach (var directoryPath in ResolveAutoHotkeyDirectories())
        {
            if (!Directory.Exists(directoryPath))
            {
                continue;
            }

            try
            {
                foreach (var scriptPath in Directory.EnumerateFiles(directoryPath, "*.ahk", SearchOption.AllDirectories))
                {
                    candidates.Add(scriptPath);
                }
            }
            catch
            {
            }
        }

        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documentsPath))
        {
            candidates.Add(Path.Combine(documentsPath, "AutoHotkey.ahk"));
        }

        return candidates;
    }

    private static IEnumerable<string> ResolveAutoHotkeyDirectories()
    {
        var startupDirectories = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
        };

        foreach (var directoryPath in startupDirectories)
        {
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                yield return directoryPath;
            }
        }

        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documentsPath))
        {
            yield return Path.Combine(documentsPath, "AutoHotkey");
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrWhiteSpace(appData))
        {
            yield return Path.Combine(appData, "MultiTool", "SearchReplacement");
        }
    }

    private static IEnumerable<string> EnumerateRunningAutoHotkeyScriptPaths()
    {
        var scriptPaths = new List<string>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                new ManagementScope(@"\\.\root\cimv2"),
                new ObjectQuery("SELECT CommandLine FROM Win32_Process WHERE Name LIKE 'AutoHotkey%.exe'"));

            foreach (ManagementObject process in searcher.Get())
            {
                var commandLine = Convert.ToString(process["CommandLine"]);
                var scriptPath = ExtractAutoHotkeyScriptPath(commandLine);
                if (!string.IsNullOrWhiteSpace(scriptPath))
                {
                    scriptPaths.Add(scriptPath);
                }
            }
        }
        catch
        {
        }

        return scriptPaths;
    }

    private static string? ExtractAutoHotkeyScriptPath(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return null;
        }

        var matches = Regex.Matches(commandLine, "\"([^\"]+)\"|(\\S+)");
        if (matches.Count <= 1)
        {
            return null;
        }

        foreach (Match match in matches.Cast<Match>().Skip(1))
        {
            var candidate = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(candidate) || candidate.StartsWith('/') || candidate.StartsWith('-'))
            {
                continue;
            }

            if (candidate.EndsWith(".ahk", StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateNamedFiles(IEnumerable<string> rootPaths, string fileName, int maxDepth)
    {
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rootPath in rootPaths
                     .Where(static path => !string.IsNullOrWhiteSpace(path))
                     .Select(static path => Path.GetFullPath(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(rootPath))
            {
                continue;
            }

            var pendingDirectories = new Stack<(string Path, int Depth)>();
            pendingDirectories.Push((rootPath, 0));

            while (pendingDirectories.Count > 0)
            {
                var (currentPath, depth) = pendingDirectories.Pop();

                string[] files;
                try
                {
                    files = Directory.GetFiles(currentPath, fileName, SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    files = [];
                }

                foreach (var filePath in files)
                {
                    var fullPath = Path.GetFullPath(filePath);
                    if (seenFiles.Add(fullPath))
                    {
                        yield return fullPath;
                    }
                }

                if (depth >= maxDepth)
                {
                    continue;
                }

                string[] subdirectories;
                try
                {
                    subdirectories = Directory.GetDirectories(currentPath);
                }
                catch
                {
                    continue;
                }

                foreach (var subdirectoryPath in subdirectories)
                {
                    if (!ShouldSkipShortcutConfigDirectory(subdirectoryPath))
                    {
                        pendingDirectories.Push((subdirectoryPath, depth + 1));
                    }
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateNamedDirectories(IEnumerable<string> rootPaths, string directoryName, int maxDepth)
    {
        var seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rootPath in rootPaths
                     .Where(static path => !string.IsNullOrWhiteSpace(path))
                     .Select(static path => Path.GetFullPath(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(rootPath))
            {
                continue;
            }

            var pendingDirectories = new Stack<(string Path, int Depth)>();
            pendingDirectories.Push((rootPath, 0));

            while (pendingDirectories.Count > 0)
            {
                var (currentPath, depth) = pendingDirectories.Pop();

                if (Path.GetFileName(currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                        .Equals(directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    var fullPath = Path.GetFullPath(currentPath);
                    if (seenDirectories.Add(fullPath))
                    {
                        yield return fullPath;
                    }
                }

                if (depth >= maxDepth)
                {
                    continue;
                }

                string[] subdirectories;
                try
                {
                    subdirectories = Directory.GetDirectories(currentPath);
                }
                catch
                {
                    continue;
                }

                foreach (var subdirectoryPath in subdirectories)
                {
                    if (!ShouldSkipShortcutConfigDirectory(subdirectoryPath))
                    {
                        pendingDirectories.Push((subdirectoryPath, depth + 1));
                    }
                }
            }
        }
    }

    private static bool ShouldSkipShortcutConfigDirectory(string directoryPath)
    {
        var directoryName = Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(directoryName)
            || IgnoredShortcutConfigDirectoryNames.Contains(directoryName);
    }

    private static bool ShouldIncludeVsCodeLikeKeybindingFile(string filePath) =>
        !PathContainsSegment(filePath, "extensions")
        && !PathContainsSegment(filePath, "node_modules");

    private static string InferShortcutApplicationName(string filePath, string fallbackName)
    {
        var knownApplicationName = TryGetKnownShortcutApplicationName(filePath);
        if (!string.IsNullOrWhiteSpace(knownApplicationName))
        {
            return knownApplicationName;
        }

        var userName = Environment.UserName;
        var directoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;
        var segments = GetPathSegments(directoryPath);

        for (var index = segments.Count - 1; index >= 0; index--)
        {
            var segment = segments[index];
            if (string.IsNullOrWhiteSpace(segment)
                || GenericShortcutConfigPathSegments.Contains(segment)
                || string.Equals(segment, userName, StringComparison.OrdinalIgnoreCase)
                || (segment.Length == 2 && segment[1] == ':'))
            {
                continue;
            }

            var normalizedSegment = NormalizeApplicationSegment(segment);
            if (!string.IsNullOrWhiteSpace(normalizedSegment))
            {
                return normalizedSegment;
            }
        }

        return fallbackName;
    }

    private static string TryGetKnownShortcutApplicationName(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        var separator = Path.DirectorySeparatorChar;

        return normalizedPath switch
        {
            var path when path.Contains($"{separator}Code{separator}User{separator}keybindings.json", StringComparison.OrdinalIgnoreCase) => "Visual Studio Code",
            var path when path.Contains($"{separator}Code - Insiders{separator}User{separator}keybindings.json", StringComparison.OrdinalIgnoreCase) => "VS Code Insiders",
            var path when path.Contains($"{separator}VSCodium{separator}User{separator}keybindings.json", StringComparison.OrdinalIgnoreCase) => "VSCodium",
            var path when path.Contains($"{separator}Cursor{separator}User{separator}keybindings.json", StringComparison.OrdinalIgnoreCase) => "Cursor",
            var path when path.Contains($"{separator}Windsurf{separator}User{separator}keybindings.json", StringComparison.OrdinalIgnoreCase) => "Windsurf",
            var path when path.Contains($"{separator}obsidian{separator}hotkeys.json", StringComparison.OrdinalIgnoreCase)
                || path.Contains($"{separator}Obsidian{separator}hotkeys.json", StringComparison.OrdinalIgnoreCase) => "Obsidian",
            var path when path.Contains("Microsoft.WindowsTerminal", StringComparison.OrdinalIgnoreCase)
                || path.Contains($"{separator}Windows Terminal{separator}", StringComparison.OrdinalIgnoreCase)
                || path.Contains($"{separator}Windows Terminal Preview{separator}", StringComparison.OrdinalIgnoreCase) => "Windows Terminal",
            var path when PathContainsSegment(path, "FlowLauncher") => "Flow Launcher",
            var path when PathContainsSegment(path, "JetBrains") => "JetBrains IDE",
            _ => string.Empty,
        };
    }

    private static IReadOnlyList<string> GetPathSegments(string path) =>
        path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool PathContainsSegment(string path, string segment) =>
        GetPathSegments(path).Any(pathSegment => string.Equals(pathSegment, segment, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeApplicationSegment(string segment)
    {
        var value = segment.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var underscoreIndex = value.IndexOf('_');
        if (underscoreIndex > 0)
        {
            var suffix = value[(underscoreIndex + 1)..];
            if (suffix.All(static character => char.IsLetterOrDigit(character)))
            {
                value = value[..underscoreIndex];
            }
        }

        var parts = Regex.Split(value, @"[\.\-_]+")
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(
                static part =>
                {
                    var trimmed = part.Trim();
                    if (trimmed.All(static character => char.IsLower(character) || char.IsDigit(character)))
                    {
                        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
                    }

                    return trimmed;
                })
            .ToArray();

        return parts.Length == 0
            ? string.Empty
            : string.Join(" ", parts);
    }

    private static JsonDocument? TryReadJsonDocument(string filePath)
    {
        try
        {
            return JsonDocument.Parse(
                File.ReadAllText(filePath),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                });
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ReadWindowsTerminalKeys(JsonElement actionElement)
    {
        if (!actionElement.TryGetProperty("keys", out var keysElement))
        {
            return [];
        }

        if (keysElement.ValueKind == JsonValueKind.String)
        {
            var hotkey = keysElement.GetString();
            return string.IsNullOrWhiteSpace(hotkey)
                ? []
                : [NormalizeVsCodeHotkey(hotkey)];
        }

        if (keysElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return keysElement.EnumerateArray()
            .Where(static keyElement => keyElement.ValueKind == JsonValueKind.String)
            .Select(static keyElement => NormalizeVsCodeHotkey(keyElement.GetString() ?? string.Empty))
            .Where(static hotkey => !string.IsNullOrWhiteSpace(hotkey))
            .ToArray();
    }

    private static string GetWindowsTerminalCommandName(JsonElement actionElement)
    {
        if (!actionElement.TryGetProperty("command", out var commandElement))
        {
            return "Terminal action";
        }

        if (commandElement.ValueKind == JsonValueKind.String)
        {
            var command = commandElement.GetString();
            return string.IsNullOrWhiteSpace(command)
                ? "Terminal action"
                : command;
        }

        if (commandElement.ValueKind == JsonValueKind.Object
            && commandElement.TryGetProperty("action", out var actionNameElement)
            && actionNameElement.ValueKind == JsonValueKind.String)
        {
            var actionName = actionNameElement.GetString();
            return string.IsNullOrWhiteSpace(actionName)
                ? "Terminal action"
                : actionName;
        }

        return "Terminal action";
    }

    private static string GetWindowsTerminalDetails(JsonElement actionElement)
    {
        if (!actionElement.TryGetProperty("command", out var commandElement))
        {
            return "Detected from Windows Terminal settings.json";
        }

        if (commandElement.ValueKind != JsonValueKind.Object)
        {
            return "Detected from Windows Terminal settings.json";
        }

        var arguments = commandElement.EnumerateObject()
            .Where(static property => !string.Equals(property.Name, "action", StringComparison.OrdinalIgnoreCase))
            .Select(static property => $"{property.Name}={FormatJsonValue(property.Value)}")
            .ToArray();

        return arguments.Length == 0
            ? "Detected from Windows Terminal settings.json"
            : $"Detected from Windows Terminal settings.json. {string.Join(", ", arguments)}";
    }

    private static string FormatJsonValue(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => value.GetRawText(),
            _ => value.GetRawText(),
        };

    private static void AddFlowLauncherShortcutIfPresent(
        ICollection<ShortcutHotkeyInfo> shortcuts,
        JsonElement root,
        string propertyName,
        string shortcutName,
        string filePath)
    {
        if (!root.TryGetProperty(propertyName, out var propertyElement) || propertyElement.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var hotkey = NormalizeFlowLauncherHotkey(propertyElement.GetString());
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return;
        }

        shortcuts.Add(new ShortcutHotkeyInfo(
            hotkey,
            shortcutName,
            filePath,
            Path.GetDirectoryName(filePath) ?? string.Empty,
            string.Empty,
            TargetExists: false,
            "Detected app settings",
            "Flow Launcher",
            $"Detected from Flow Launcher Settings.json ({propertyName})."));
    }

    private static string NormalizeFlowLauncherHotkey(string? hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return string.Empty;
        }

        var chordParts = hotkey
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(
                static chord =>
                    string.Join(
                        " + ",
                        Regex.Split(chord.Trim(), @"\s*\+\s*")
                            .Where(static token => !string.IsNullOrWhiteSpace(token))
                            .Select(NormalizeFlowLauncherHotkeyToken)
                            .Where(static token => !string.IsNullOrWhiteSpace(token))))
            .Where(static chord => !string.IsNullOrWhiteSpace(chord));

        return string.Join(", ", chordParts);
    }

    private static string NormalizeFlowLauncherHotkeyToken(string token) =>
        token.Trim().ToUpperInvariant() switch
        {
            "D0" => "0",
            "D1" => "1",
            "D2" => "2",
            "D3" => "3",
            "D4" => "4",
            "D5" => "5",
            "D6" => "6",
            "D7" => "7",
            "D8" => "8",
            "D9" => "9",
            _ => NormalizeModifierOrKeyToken(token),
        };

    private static string StripAutoHotkeyComment(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return string.Empty;
        }

        for (var index = 0; index < line.Length; index++)
        {
            if (line[index] == ';' && (index == 0 || char.IsWhiteSpace(line[index - 1])))
            {
                return line[..index];
            }
        }

        return line;
    }

    private static bool TryParseAutoHotkeyContextDirective(string line, out string context)
    {
        context = string.Empty;

        if (TryParseAutoHotkeyDirective(line, "#HotIf", out context)
            || TryParseAutoHotkeyDirective(line, "#IfWinActive", out context)
            || TryParseAutoHotkeyDirective(line, "#IfWinExist", out context)
            || TryParseAutoHotkeyDirective(line, "#If", out context))
        {
            return true;
        }

        return false;
    }

    private static bool TryParseAutoHotkeyDirective(string line, string directive, out string context)
    {
        context = string.Empty;
        if (!line.StartsWith(directive, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        context = line[directive.Length..].Trim().TrimStart(',');
        return true;
    }

    private static string NormalizeAutoHotkeyHotkey(string hotkeyDefinition)
    {
        if (string.IsNullOrWhiteSpace(hotkeyDefinition) || hotkeyDefinition.StartsWith(':'))
        {
            return string.Empty;
        }

        var ampersandIndex = hotkeyDefinition.IndexOf('&');
        if (ampersandIndex >= 0)
        {
            var left = NormalizeAutoHotkeyHotkeyPart(hotkeyDefinition[..ampersandIndex]);
            var right = NormalizeAutoHotkeyHotkeyPart(hotkeyDefinition[(ampersandIndex + 1)..]);
            return string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)
                ? string.Empty
                : $"{left} & {right}";
        }

        return NormalizeAutoHotkeyHotkeyPart(hotkeyDefinition);
    }

    private static string NormalizeAutoHotkeyHotkeyPart(string hotkeyPart)
    {
        var part = hotkeyPart.Trim();
        if (string.IsNullOrWhiteSpace(part))
        {
            return string.Empty;
        }

        var isKeyUp = part.EndsWith(" Up", StringComparison.OrdinalIgnoreCase);
        if (isKeyUp)
        {
            part = part[..^3].TrimEnd();
        }

        var tokens = new List<string>();
        while (!string.IsNullOrWhiteSpace(part))
        {
            if (part.StartsWith("<^>!", StringComparison.Ordinal))
            {
                tokens.Add("AltGr");
                part = part[4..];
                continue;
            }

            switch (part[0])
            {
                case '*':
                case '~':
                case '$':
                case '<':
                case '>':
                    part = part[1..];
                    continue;
                case '^':
                    tokens.Add("Ctrl");
                    part = part[1..];
                    continue;
                case '!':
                    tokens.Add("Alt");
                    part = part[1..];
                    continue;
                case '+':
                    tokens.Add("Shift");
                    part = part[1..];
                    continue;
                case '#':
                    tokens.Add("Win");
                    part = part[1..];
                    continue;
                default:
                    part = part.Trim();
                    goto DoneParsing;
            }
        }

    DoneParsing:
        if (part.StartsWith('{') && part.EndsWith('}') && part.Length > 2)
        {
            part = part[1..^1];
        }

        var keyToken = NormalizeAutoHotkeyKeyToken(part);
        if (string.IsNullOrWhiteSpace(keyToken))
        {
            return string.Empty;
        }

        tokens.Add(keyToken);
        var hotkey = string.Join(" + ", tokens);
        return isKeyUp
            ? $"{hotkey} (Up)"
            : hotkey;
    }

    private static string NormalizeAutoHotkeyKeyToken(string token)
    {
        var normalized = token.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return normalized.ToUpperInvariant() switch
        {
            "ESC" or "ESCAPE" => "Esc",
            "RETURN" => "Enter",
            "DEL" => "Delete",
            "BS" => "Backspace",
            "PGUP" => "PgUp",
            "PGDN" => "PgDn",
            "APPSKEY" => "Menu",
            "LWIN" or "RWIN" => "Win",
            "LCTRL" or "RCTRL" => "Ctrl",
            "LALT" or "RALT" => "Alt",
            "LSHIFT" or "RSHIFT" => "Shift",
            _ when Regex.IsMatch(normalized, "^(vk|sc)[0-9A-F]+$", RegexOptions.IgnoreCase) => normalized.ToUpperInvariant(),
            _ => NormalizeModifierOrKeyToken(normalized),
        };
    }

    private static string BuildAutoHotkeyShortcutName(string inlineAction, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(inlineAction))
        {
            return $"Script hotkey (line {lineNumber})";
        }

        return Truncate(inlineAction, 72);
    }

    private static string BuildAutoHotkeyAppliesTo(string context)
    {
        var target = TryExtractAutoHotkeyTarget(context);
        return string.IsNullOrWhiteSpace(target)
            ? "AutoHotkey script"
            : target;
    }

    private static string BuildAutoHotkeyDetails(string context, string inlineAction, int lineNumber)
    {
        var parts = new List<string> { $"Detected from AutoHotkey script (line {lineNumber})" };
        if (!string.IsNullOrWhiteSpace(context))
        {
            parts.Add($"Condition: {Truncate(context, 90)}");
        }

        if (!string.IsNullOrWhiteSpace(inlineAction))
        {
            parts.Add($"Action: {Truncate(inlineAction, 90)}");
        }

        return string.Join(". ", parts);
    }

    private static string TryExtractAutoHotkeyTarget(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return string.Empty;
        }

        var executableMatch = Regex.Match(context, @"ahk_exe\s+([^\s""')]+)", RegexOptions.IgnoreCase);
        if (executableMatch.Success)
        {
            return executableMatch.Groups[1].Value;
        }

        var classMatch = Regex.Match(context, @"ahk_class\s+([^\s""')]+)", RegexOptions.IgnoreCase);
        if (classMatch.Success)
        {
            return classMatch.Groups[1].Value;
        }

        var titleMatch = Regex.Match(context, @"ahk_id\s+([^\s""')]+)", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            return titleMatch.Groups[1].Value;
        }

        return context;
    }

    private static string Truncate(string value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maximumLength)
        {
            return value;
        }

        return $"{value[..(maximumLength - 3)]}...";
    }

    private static string NormalizeJetBrainsKeystroke(string? keystroke)
    {
        if (string.IsNullOrWhiteSpace(keystroke))
        {
            return string.Empty;
        }

        var tokens = keystroke
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeModifierOrKeyToken)
            .Where(static token => !string.IsNullOrWhiteSpace(token));

        return string.Join(" + ", tokens);
    }

    private static string NormalizeModifierOrKeyToken(string token)
    {
        var normalized = token.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return normalized.ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => "Ctrl",
            "ALT" => "Alt",
            "SHIFT" => "Shift",
            "WIN" or "WINDOWS" or "META" => "Win",
            "CMD" or "COMMAND" or "MOD" => "Ctrl",
            _ => normalized.Length == 1
                ? normalized.ToUpperInvariant()
                : char.ToUpperInvariant(normalized[0]) + normalized[1..],
        };
    }

    private sealed class ScanProgressState
    {
        private readonly IProgress<ShortcutHotkeyScanProgress>? progress;
        private int completedFolderCount;
        private int totalFolderCount;
        private int scannedShortcutCount;
        private string currentPath = string.Empty;

        public ScanProgressState(IProgress<ShortcutHotkeyScanProgress>? progress)
        {
            this.progress = progress;
        }

        private bool activeRootUsesExactFolderCount;

        public void RegisterRoot(string rootPath, int? exactFolderCount)
        {
            activeRootUsesExactFolderCount = exactFolderCount.HasValue && exactFolderCount.Value > 0;
            totalFolderCount += activeRootUsesExactFolderCount ? exactFolderCount!.Value : 1;
            currentPath = rootPath;
            Report();
        }

        public void AddDiscoveredDirectories(int count, string currentPath)
        {
            if (count <= 0 || activeRootUsesExactFolderCount)
            {
                return;
            }

            totalFolderCount += count;
            this.currentPath = currentPath;
            Report();
        }

        public void MarkShortcutScanned(string currentPath)
        {
            scannedShortcutCount++;
            this.currentPath = currentPath;
            Report();
        }

        public void MarkDirectoryCompleted(string currentPath)
        {
            completedFolderCount++;
            this.currentPath = currentPath;
            Report();
        }

        private void Report()
        {
            progress?.Report(
                new ShortcutHotkeyScanProgress(
                    completedFolderCount,
                    Math.Max(totalFolderCount, 1),
                    scannedShortcutCount,
                    currentPath));
        }
    }
}
