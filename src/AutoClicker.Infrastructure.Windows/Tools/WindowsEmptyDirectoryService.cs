using System.IO;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Tools;

public sealed class WindowsEmptyDirectoryService : IEmptyDirectoryService
{
    public Task<EmptyDirectoryScanResult> FindEmptyDirectoriesAsync(string rootPath, CancellationToken cancellationToken = default)
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
                ScanDirectory(fullRootPath, fullRootPath, candidates, warnings, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        var allNestedDirectoriesAreEmpty = true;
        foreach (var directory in directories)
        {
            if (!ScanDirectory(directory, rootPath, candidates, warnings, cancellationToken))
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
}
