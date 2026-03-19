using MultiTool.Infrastructure.Windows.Startup;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsRunAtStartupServiceTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "MultiTool.Tests", "startup", Guid.NewGuid().ToString("N"));

    [Fact]
    public void IsEnabled_ShouldReturnTrue_WhenRunRegistryValueExists()
    {
        var startupDirectory = Path.Combine(workingDirectory, "Startup");
        Directory.CreateDirectory(startupDirectory);

        var service = new WindowsRunAtStartupService(
            processPathResolver: () => @"C:\Apps\MultiTool.exe",
            runValueReader: () => "\"C:\\Apps\\MultiTool.exe\" --startup-launch",
            runValueWriter: _ => { },
            runValueRemover: () => { },
            startupDirectoryResolver: () => startupDirectory,
            fileExists: File.Exists,
            fileDeleter: File.Delete);

        service.IsEnabled().Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_ShouldReturnTrue_WhenLegacyStartupShortcutExists()
    {
        var startupDirectory = Path.Combine(workingDirectory, "Startup");
        Directory.CreateDirectory(startupDirectory);
        File.WriteAllText(Path.Combine(startupDirectory, WindowsRunAtStartupService.StartupShortcutFileName), string.Empty);

        var service = new WindowsRunAtStartupService(
            processPathResolver: () => @"C:\Apps\MultiTool.exe",
            runValueReader: () => null,
            runValueWriter: _ => { },
            runValueRemover: () => { },
            startupDirectoryResolver: () => startupDirectory,
            fileExists: File.Exists,
            fileDeleter: File.Delete);

        service.IsEnabled().Should().BeTrue();
    }

    [Fact]
    public void SetEnabled_ShouldWriteRegistryCommandAndRemoveLegacyShortcuts_WhenEnabled()
    {
        var startupDirectory = Path.Combine(workingDirectory, "Startup");
        Directory.CreateDirectory(startupDirectory);
        var appPath = Path.Combine(workingDirectory, "MultiTool.exe");
        var startupShortcutPath = Path.Combine(startupDirectory, WindowsRunAtStartupService.StartupShortcutFileName);
        var legacyShortcutPath = Path.Combine(startupDirectory, WindowsRunAtStartupService.LegacyStartupShortcutFileName);
        File.WriteAllText(appPath, string.Empty);
        File.WriteAllText(startupShortcutPath, string.Empty);
        File.WriteAllText(legacyShortcutPath, string.Empty);

        string? writtenValue = null;
        var deletedPaths = new List<string>();

        var service = new WindowsRunAtStartupService(
            processPathResolver: () => appPath,
            runValueReader: () => null,
            runValueWriter: value => writtenValue = value,
            runValueRemover: () => { },
            startupDirectoryResolver: () => startupDirectory,
            fileExists: File.Exists,
            fileDeleter: path =>
            {
                deletedPaths.Add(path);
                File.Delete(path);
            });

        service.SetEnabled(true);

        writtenValue.Should().Be($"\"{appPath}\" --startup-launch");
        deletedPaths.Should().BeEquivalentTo([startupShortcutPath, legacyShortcutPath]);
        File.Exists(startupShortcutPath).Should().BeFalse();
        File.Exists(legacyShortcutPath).Should().BeFalse();
    }

    [Fact]
    public void SetEnabled_ShouldRemoveRegistryValueAndLegacyShortcuts_WhenDisabled()
    {
        var startupDirectory = Path.Combine(workingDirectory, "Startup");
        Directory.CreateDirectory(startupDirectory);
        var startupShortcutPath = Path.Combine(startupDirectory, WindowsRunAtStartupService.StartupShortcutFileName);
        File.WriteAllText(startupShortcutPath, string.Empty);
        var registryValueRemoved = false;

        var service = new WindowsRunAtStartupService(
            processPathResolver: () => @"C:\Apps\MultiTool.exe",
            runValueReader: () => "\"C:\\Apps\\MultiTool.exe\" --startup-launch",
            runValueWriter: _ => { },
            runValueRemover: () => registryValueRemoved = true,
            startupDirectoryResolver: () => startupDirectory,
            fileExists: File.Exists,
            fileDeleter: File.Delete);

        service.SetEnabled(false);

        registryValueRemoved.Should().BeTrue();
        File.Exists(startupShortcutPath).Should().BeFalse();
    }

    [Fact]
    public void SetEnabled_ShouldThrow_WhenProcessPathCannotBeResolved()
    {
        var startupDirectory = Path.Combine(workingDirectory, "Startup");
        Directory.CreateDirectory(startupDirectory);

        var service = new WindowsRunAtStartupService(
            processPathResolver: () => null,
            runValueReader: () => null,
            runValueWriter: _ => { },
            runValueRemover: () => { },
            startupDirectoryResolver: () => startupDirectory,
            fileExists: File.Exists,
            fileDeleter: File.Delete);

        Action act = () => service.SetEnabled(true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("MultiTool.exe could not be located for startup registration.");
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }
}
