using AutoClicker.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class WindowsEmptyDirectoryServiceTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "AutoClicker.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task FindEmptyDirectoriesAsync_ShouldReturnLeafAndParentDirectoriesThatBecomeEmpty()
    {
        var rootPath = CreateDirectory("ScanRoot");
        CreateDirectory(Path.Combine(rootPath, "AlreadyEmpty"));
        CreateDirectory(Path.Combine(rootPath, "Nested", "Child"));
        CreateDirectory(Path.Combine(rootPath, "HasFiles"));
        await File.WriteAllTextAsync(Path.Combine(rootPath, "HasFiles", "keep.txt"), "keep");

        var service = new WindowsEmptyDirectoryService();

        var result = await service.FindEmptyDirectoriesAsync(rootPath);

        result.Warnings.Should().BeEmpty();
        result.Candidates.Select(candidate => candidate.FullPath).Should().BeEquivalentTo(
            Path.Combine(rootPath, "AlreadyEmpty"),
            Path.Combine(rootPath, "Nested"),
            Path.Combine(rootPath, "Nested", "Child"));
        result.Candidates.Should().ContainEquivalentOf(
            new
            {
                FullPath = Path.Combine(rootPath, "Nested"),
                ContainsNestedEmptyDirectories = true,
            });
        result.Candidates.Should().ContainEquivalentOf(
            new
            {
                FullPath = Path.Combine(rootPath, "AlreadyEmpty"),
                ContainsNestedEmptyDirectories = false,
            });
    }

    [Fact]
    public async Task DeleteDirectoriesAsync_ShouldDeleteChildrenBeforeParents()
    {
        var rootPath = CreateDirectory("DeleteRoot");
        var parentPath = CreateDirectory(Path.Combine(rootPath, "Parent"));
        var childPath = CreateDirectory(Path.Combine(parentPath, "Child"));
        var service = new WindowsEmptyDirectoryService();

        var results = await service.DeleteDirectoriesAsync([parentPath, childPath]);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(result => result.Succeeded);
        results.Should().OnlyContain(result => result.Deleted);
        Directory.Exists(childPath).Should().BeFalse();
        Directory.Exists(parentPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDirectoriesAsync_ShouldReportDirectoriesThatAreNoLongerEmpty()
    {
        var rootPath = CreateDirectory("NotEmptyRoot");
        var targetPath = CreateDirectory(Path.Combine(rootPath, "Target"));
        await File.WriteAllTextAsync(Path.Combine(targetPath, "file.txt"), "keep");
        var service = new WindowsEmptyDirectoryService();

        var results = await service.DeleteDirectoriesAsync([targetPath]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeFalse();
        results[0].Deleted.Should().BeFalse();
        results[0].Message.Should().Be("Directory is no longer empty.");
        Directory.Exists(targetPath).Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, true);
        }
    }

    private string CreateDirectory(string relativePath)
    {
        var path = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(workingDirectory, relativePath);
        Directory.CreateDirectory(path);
        return path;
    }
}
