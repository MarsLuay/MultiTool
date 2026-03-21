using FluentAssertions;
using MultiTool.App.Services;

namespace MultiTool.App.Tests;

public sealed class FolderPickerServiceTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "MultiTool.Tests", Guid.NewGuid().ToString("N"));

    public FolderPickerServiceTests()
    {
        Directory.CreateDirectory(workingDirectory);
    }

    [Fact]
    public void ResolveInitialDirectory_ShouldReturnCurrentPath_WhenDirectoryExists()
    {
        var existingFolder = Path.Combine(workingDirectory, "Screenshots");
        Directory.CreateDirectory(existingFolder);

        var result = FolderPickerService.ResolveInitialDirectory(existingFolder, workingDirectory);

        result.Should().Be(existingFolder);
    }

    [Fact]
    public void ResolveInitialDirectory_ShouldReturnNearestExistingParent_WhenDirectoryDoesNotExist()
    {
        var downloadsFolder = Path.Combine(workingDirectory, "Downloads");
        Directory.CreateDirectory(downloadsFolder);
        var missingFolder = Path.Combine(downloadsFolder, "Screenshots", "Nested");

        var result = FolderPickerService.ResolveInitialDirectory(missingFolder, workingDirectory);

        result.Should().Be(downloadsFolder);
    }

    [Fact]
    public void ResolveInitialDirectory_ShouldReturnFallback_WhenPathIsBlank()
    {
        var result = FolderPickerService.ResolveInitialDirectory("   ", workingDirectory);

        result.Should().Be(workingDirectory);
    }

    [Fact]
    public void ResolveInitialDirectory_ShouldReturnFallback_WhenPathIsMalformed()
    {
        var malformedPath = "Screenshots" + '\0' + "Folder";

        var result = FolderPickerService.ResolveInitialDirectory(malformedPath, workingDirectory);

        result.Should().Be(workingDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, true);
        }
    }
}
