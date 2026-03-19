using System.IO;
using Microsoft.Win32;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Startup;

public sealed class WindowsRunAtStartupService : IRunAtStartupService
{
    internal const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string RunRegistryValueName = "MultiTool";
    internal const string StartupShortcutFileName = "Launch MultiTool.lnk";
    internal const string LegacyStartupShortcutFileName = "Launch AutoClicker.lnk";

    private readonly Func<string?> processPathResolver;
    private readonly Func<string?> runValueReader;
    private readonly Action<string> runValueWriter;
    private readonly Action runValueRemover;
    private readonly Func<string> startupDirectoryResolver;
    private readonly Func<string, bool> fileExists;
    private readonly Action<string> fileDeleter;

    public WindowsRunAtStartupService()
        : this(
            () => Environment.ProcessPath,
            ReadRunValue,
            WriteRunValue,
            RemoveRunValue,
            ResolveStartupDirectory,
            File.Exists,
            File.Delete)
    {
    }

    internal WindowsRunAtStartupService(
        Func<string?> processPathResolver,
        Func<string?> runValueReader,
        Action<string> runValueWriter,
        Action runValueRemover,
        Func<string> startupDirectoryResolver,
        Func<string, bool> fileExists,
        Action<string> fileDeleter)
    {
        this.processPathResolver = processPathResolver;
        this.runValueReader = runValueReader;
        this.runValueWriter = runValueWriter;
        this.runValueRemover = runValueRemover;
        this.startupDirectoryResolver = startupDirectoryResolver;
        this.fileExists = fileExists;
        this.fileDeleter = fileDeleter;
    }

    public bool IsEnabled() =>
        !string.IsNullOrWhiteSpace(runValueReader()) || GetLegacyShortcutPaths().Any(fileExists);

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            var processPath = processPathResolver();
            if (string.IsNullOrWhiteSpace(processPath) || !fileExists(processPath))
            {
                throw new InvalidOperationException("MultiTool.exe could not be located for startup registration.");
            }

            runValueWriter($"\"{processPath}\" --startup-launch");
            RemoveLegacyShortcuts();
            return;
        }

        runValueRemover();
        RemoveLegacyShortcuts();
    }

    private void RemoveLegacyShortcuts()
    {
        foreach (var shortcutPath in GetLegacyShortcutPaths())
        {
            if (fileExists(shortcutPath))
            {
                fileDeleter(shortcutPath);
            }
        }
    }

    private IEnumerable<string> GetLegacyShortcutPaths()
    {
        var startupDirectory = startupDirectoryResolver();
        yield return Path.Combine(startupDirectory, StartupShortcutFileName);
        yield return Path.Combine(startupDirectory, LegacyStartupShortcutFileName);
    }

    private static string? ReadRunValue()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
        return runKey?.GetValue(RunRegistryValueName) as string;
    }

    private static void WriteRunValue(string value)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunRegistryPath)
            ?? throw new InvalidOperationException("Windows startup registry key could not be opened.");
        runKey.SetValue(RunRegistryValueName, value, RegistryValueKind.String);
    }

    private static void RemoveRunValue()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true);
        runKey?.DeleteValue(RunRegistryValueName, throwOnMissingValue: false);
    }

    private static string ResolveStartupDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "Windows",
            "Start Menu",
            "Programs",
            "Startup");
}
