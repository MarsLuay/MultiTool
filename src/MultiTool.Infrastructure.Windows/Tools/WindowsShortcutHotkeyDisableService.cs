using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

public sealed class WindowsShortcutHotkeyDisableService : IShortcutHotkeyDisableService
{
    private readonly Func<string, bool> shortcutHotkeyClearer;

    public WindowsShortcutHotkeyDisableService()
        : this(ClearShortcutHotkey)
    {
    }

    internal WindowsShortcutHotkeyDisableService(Func<string, bool> shortcutHotkeyClearer)
    {
        this.shortcutHotkeyClearer = shortcutHotkeyClearer ?? throw new ArgumentNullException(nameof(shortcutHotkeyClearer));
    }

    public Task<ShortcutHotkeyDisableResult> DisableAsync(
        IReadOnlyList<ShortcutHotkeyInfo> shortcuts,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                var warnings = new List<string>();
                var disabledCount = 0;
                var supportedCount = 0;
                var unsupportedCount = 0;
                var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var shortcut in shortcuts ?? [])
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!CanDisableShortcut(shortcut))
                    {
                        unsupportedCount++;
                        continue;
                    }

                    var shortcutPath = Path.GetFullPath(shortcut.ShortcutPath);
                    if (!seenPaths.Add(shortcutPath))
                    {
                        continue;
                    }

                    supportedCount++;

                    if (!File.Exists(shortcutPath))
                    {
                        warnings.Add($"Shortcut file no longer exists: {shortcutPath}");
                        continue;
                    }

                    try
                    {
                        if (shortcutHotkeyClearer(shortcutPath))
                        {
                            disabledCount++;
                        }
                        else
                        {
                            warnings.Add($"Couldn't clear the shortcut hotkey in {shortcutPath}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"Couldn't clear the shortcut hotkey in {shortcutPath}: {ex.Message}");
                    }
                }

                return new ShortcutHotkeyDisableResult(disabledCount, supportedCount, unsupportedCount, warnings);
            },
            cancellationToken);
    }

    internal static bool CanDisableShortcut(ShortcutHotkeyInfo shortcut) =>
        shortcut is not null
        && shortcut.CanDisable
        && !shortcut.IsReferenceShortcut
        && !string.IsNullOrWhiteSpace(shortcut.ShortcutPath)
        && shortcut.ShortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);

    private static bool ClearShortcutHotkey(string shortcutPath)
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
                return false;
            }

            shortcut.GetType().InvokeMember(
                "Hotkey",
                BindingFlags.SetProperty,
                binder: null,
                target: shortcut,
                args: [string.Empty]);
            shortcut.GetType().InvokeMember(
                "Save",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shortcut,
                args: null);
            return true;
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
