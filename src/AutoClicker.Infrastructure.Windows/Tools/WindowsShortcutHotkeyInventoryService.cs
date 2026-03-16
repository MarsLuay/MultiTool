using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Tools;

public sealed class WindowsShortcutHotkeyInventoryService : IShortcutHotkeyInventoryService
{
    private readonly Func<IEnumerable<string>> scanRootResolver;
    private readonly Func<string, (string Hotkey, string TargetPath)?> shortcutMetadataReader;
    private readonly Func<string, int?> exactFolderCountResolver;
    private readonly Func<IReadOnlyList<ShortcutHotkeyInfo>> referenceShortcutResolver;

    public WindowsShortcutHotkeyInventoryService()
        : this(ResolveScanRoots, ReadShortcutMetadata, NtfsFolderCountProvider.TryGetExactFolderCount, BuildReferenceShortcuts)
    {
    }

    public WindowsShortcutHotkeyInventoryService(
        Func<IEnumerable<string>> scanRootResolver,
        Func<string, (string Hotkey, string TargetPath)?> shortcutMetadataReader)
        : this(scanRootResolver, shortcutMetadataReader, _ => null, static () => [])
    {
    }

    public WindowsShortcutHotkeyInventoryService(
        Func<IEnumerable<string>> scanRootResolver,
        Func<string, (string Hotkey, string TargetPath)?> shortcutMetadataReader,
        Func<string, int?> exactFolderCountResolver)
        : this(scanRootResolver, shortcutMetadataReader, exactFolderCountResolver, static () => [])
    {
    }

    public WindowsShortcutHotkeyInventoryService(
        Func<IEnumerable<string>> scanRootResolver,
        Func<string, (string Hotkey, string TargetPath)?> shortcutMetadataReader,
        Func<string, int?> exactFolderCountResolver,
        Func<IReadOnlyList<ShortcutHotkeyInfo>> referenceShortcutResolver)
    {
        this.scanRootResolver = scanRootResolver ?? throw new ArgumentNullException(nameof(scanRootResolver));
        this.shortcutMetadataReader = shortcutMetadataReader ?? throw new ArgumentNullException(nameof(shortcutMetadataReader));
        this.exactFolderCountResolver = exactFolderCountResolver ?? throw new ArgumentNullException(nameof(exactFolderCountResolver));
        this.referenceShortcutResolver = referenceShortcutResolver ?? throw new ArgumentNullException(nameof(referenceShortcutResolver));
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
                                        : $"Target: {targetPath}"));
                        }
                        catch (Exception ex)
                        {
                            warnings.Add($"Skipped shortcut metadata in {shortcutPath}: {ex.Message}");
                        }
                    }
                }

                shortcuts.AddRange(referenceShortcutResolver());

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
