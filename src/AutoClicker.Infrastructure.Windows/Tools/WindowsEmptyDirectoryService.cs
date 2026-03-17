using System.IO;
using System.Diagnostics;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Tools;

public sealed class WindowsEmptyDirectoryService : IEmptyDirectoryService
{
    private readonly Func<string, int?> exactDirectoryCountResolver;

    public WindowsEmptyDirectoryService()
        : this(NtfsFolderCountProvider.TryGetExactFolderCount)
    {
    }

    public WindowsEmptyDirectoryService(Func<string, int?> exactDirectoryCountResolver)
    {
        this.exactDirectoryCountResolver = exactDirectoryCountResolver ?? throw new ArgumentNullException(nameof(exactDirectoryCountResolver));
    }

    public Task<EmptyDirectoryScanResult> FindEmptyDirectoriesAsync(
        string rootPath,
        IProgress<EmptyDirectoryScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("A root folder is required.", nameof(rootPath));
        }

        var fullRootPath = Path.GetFullPath(rootPath.Trim());
        if (!Directory.Exists(fullRootPath))
        {
            throw new DirectoryNotFoundException($"The folder '{fullRootPath}' does not exist.");
        }

        return Task.Run(
            () =>
            {
                var candidates = new List<EmptyDirectoryCandidate>();
                var warnings = new List<string>();
                int? exactDirectoryCount;
                try
                {
                    exactDirectoryCount = DetermineTotalDirectoryCount(fullRootPath, cancellationToken);
                }
                catch
                {
                    exactDirectoryCount = null;
                }

                var progressState = new ScanProgressState(progress, fullRootPath, exactDirectoryCount);
                ScanDirectory(fullRootPath, fullRootPath, candidates, warnings, progressState, cancellationToken);
                candidates.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.FullPath, right.FullPath));
                return new EmptyDirectoryScanResult(candidates, warnings);
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<EmptyDirectoryDeleteResult>> DeleteDirectoriesAsync(IEnumerable<string> directoryPaths, CancellationToken cancellationToken = default)
    {
        var normalizedPaths = NormalizePaths(directoryPaths)
            .OrderByDescending(
                static path => path.Count(
                    static character => character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar))
            .ThenByDescending(static path => path.Length)
            .ToArray();

        return Task.Run<IReadOnlyList<EmptyDirectoryDeleteResult>>(
            () =>
            {
                var results = new List<EmptyDirectoryDeleteResult>(normalizedPaths.Length);

                foreach (var directoryPath in normalizedPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!Directory.Exists(directoryPath))
                    {
                        results.Add(new EmptyDirectoryDeleteResult(directoryPath, true, false, "Already missing."));
                        continue;
                    }

                    try
                    {
                        if (Directory.EnumerateFileSystemEntries(directoryPath).Any())
                        {
                            results.Add(new EmptyDirectoryDeleteResult(directoryPath, false, false, "Directory is no longer empty."));
                            continue;
                        }

                        Directory.Delete(directoryPath, recursive: false);
                        results.Add(new EmptyDirectoryDeleteResult(directoryPath, true, true, "Deleted."));
                    }
                    catch (Exception ex)
                    {
                        results.Add(new EmptyDirectoryDeleteResult(directoryPath, false, false, ex.Message));
                    }
                }

                return results;
            },
            cancellationToken);
    }

    private static bool ScanDirectory(
        string currentPath,
        string rootPath,
        ICollection<EmptyDirectoryCandidate> candidates,
        ICollection<string> warnings,
        ScanProgressState progressState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(currentPath);
            }
            catch (Exception ex)
            {
                warnings.Add($"Skipped files in {currentPath}: {ex.Message}");
                return false;
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(currentPath);
            }
            catch (Exception ex)
            {
                warnings.Add($"Skipped folders in {currentPath}: {ex.Message}");
                return false;
            }

            progressState.AddDiscoveredDirectories(directories.Length, currentPath);

            var allNestedDirectoriesAreEmpty = true;
            foreach (var directory in directories)
            {
                if (!ScanDirectory(directory, rootPath, candidates, warnings, progressState, cancellationToken))
                {
                    allNestedDirectoriesAreEmpty = false;
                }
            }

            var isRemovable = files.Length == 0 && allNestedDirectoriesAreEmpty;
            if (isRemovable && !string.Equals(currentPath, rootPath, StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(
                    new EmptyDirectoryCandidate(
                        currentPath,
                        ContainsNestedEmptyDirectories: directories.Length > 0));
            }

            return isRemovable;
        }
        finally
        {
            progressState.MarkDirectoryCompleted(currentPath);
        }
    }

    private static IReadOnlyList<string> NormalizePaths(IEnumerable<string> directoryPaths)
    {
        var normalizedPaths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directoryPath in directoryPaths)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                continue;
            }

            var normalizedPath = Path.GetFullPath(directoryPath.Trim());
            if (seen.Add(normalizedPath))
            {
                normalizedPaths.Add(normalizedPath);
            }
        }

        return normalizedPaths;
    }

    private int? DetermineTotalDirectoryCount(string fullRootPath, CancellationToken cancellationToken)
    {
        var exactCount = exactDirectoryCountResolver(fullRootPath);
        if (exactCount is > 0)
        {
            return exactCount;
        }

        if (OperatingSystem.IsWindows())
        {
            var windowsCount = TryCountDirectoriesWithWindowsCommand(fullRootPath, cancellationToken);
            if (windowsCount is > 0)
            {
                return windowsCount;
            }
        }

        return TryCountDirectoriesManaged(fullRootPath, cancellationToken);
    }

    private static int? TryCountDirectoriesWithWindowsCommand(string rootPath, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/d /c dir /a:d /b /s {QuoteArgument(rootPath)}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

            if (!process.Start())
            {
                return null;
            }

            var directoryCount = 1; // Include root directory.
            while (!process.StandardOutput.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = process.StandardOutput.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    directoryCount++;
                }
            }

            process.WaitForExit();
            return process.ExitCode == 0 && directoryCount > 0
                ? directoryCount
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static int? TryCountDirectoriesManaged(string rootPath, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(rootPath))
        {
            return null;
        }

        try
        {
            var count = 0;
            var pending = new Stack<string>();
            pending.Push(rootPath);

            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Pop();
                count++;

                try
                {
                    foreach (var directory in Directory.EnumerateDirectories(current))
                    {
                        pending.Push(directory);
                    }
                }
                catch
                {
                    // Ignore inaccessible folders during pre-count; scan phase reports warnings.
                }
            }

            return count > 0 ? count : null;
        }
        catch
        {
            return null;
        }
    }

    private static string QuoteArgument(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";

    private sealed class ScanProgressState
    {
        private readonly IProgress<EmptyDirectoryScanProgress>? progress;
        private readonly string rootPath;
        private int completedDirectoryCount;
        private int totalDirectoryCount;
        private readonly bool usesExactDirectoryCount;

        public ScanProgressState(IProgress<EmptyDirectoryScanProgress>? progress, string rootPath, int? exactDirectoryCount)
        {
            this.progress = progress;
            this.rootPath = rootPath;
            usesExactDirectoryCount = exactDirectoryCount.HasValue && exactDirectoryCount.Value > 0;
            totalDirectoryCount = usesExactDirectoryCount ? exactDirectoryCount!.Value : 1;
            Report(rootPath);
        }

        public void AddDiscoveredDirectories(int count, string currentPath)
        {
            if (count <= 0 || usesExactDirectoryCount)
            {
                return;
            }

            totalDirectoryCount += count;
            Report(currentPath);
        }

        public void MarkDirectoryCompleted(string currentPath)
        {
            completedDirectoryCount++;
            Report(currentPath);
        }

        private void Report(string currentPath)
        {
            progress?.Report(
                new EmptyDirectoryScanProgress(
                    completedDirectoryCount,
                    Math.Max(totalDirectoryCount, 1),
                    string.IsNullOrWhiteSpace(currentPath) ? rootPath : currentPath));
        }
    }
}
