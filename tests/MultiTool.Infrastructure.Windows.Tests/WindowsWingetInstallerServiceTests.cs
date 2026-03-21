using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;


public sealed partial class WindowsWingetInstallerServiceTests
{
    private static InstallerCommandRunner CreateRunner() =>
        (startInfo, _) =>
        {
            var result = startInfo.Arguments switch
            {
                "--version" => new InstallerCommandResult(0, "v1.28.220", string.Empty),
                "list --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                    0,
                    """
                    Failed in attempting to update the source: winget
                    Name             Id               Version Source
                    ------------------------------------------------
                    Git              Git.Git          2.53.0  winget
                    Mozilla Firefox  Mozilla.Firefox  136.0   winget
                    """,
                    string.Empty),
                "list --upgrade-available --accept-source-agreements --disable-interactivity" => new InstallerCommandResult(
                    0,
                    """
                    Name  Id       Version  Available  Source
                    -----------------------------------------
                    Git   Git.Git  2.53.0   2.53.0.2   winget
                    """,
                    string.Empty),
                "install --exact --id \"Microsoft.VisualStudioCode\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(
                    1,
                    """
                    Found an existing package already installed. Trying to upgrade the installed package...
                    No available upgrade found.
                    No newer package versions are available from the configured sources.
                    """,
                    string.Empty),
                "upgrade --exact --id \"Roblox.Roblox\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent" => new InstallerCommandResult(
                    1,
                    "No installed package found matching input criteria.",
                    string.Empty),
                _ => throw new InvalidOperationException($"Unexpected command: {startInfo.FileName} {startInfo.Arguments}"),
            };

            return Task.FromResult(result);
        };

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "MultiTool.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CreateZipArchiveWithFiles(string destinationPath, params (string RelativePath, string Contents)[] files)
    {
        var tempSourceDirectory = CreateTemporaryDirectory();

        try
        {
            foreach (var (relativePath, contents) in files)
            {
                var fullPath = Path.Combine(tempSourceDirectory, relativePath);
                var parentDirectory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                File.WriteAllText(fullPath, contents);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(tempSourceDirectory, destinationPath);
        }
        finally
        {
            if (Directory.Exists(tempSourceDirectory))
            {
                Directory.Delete(tempSourceDirectory, true);
            }
        }
    }
}
