using System.Diagnostics;
using AutoClicker.Infrastructure.Windows.Tools;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class WindowsBrowserLauncherServiceTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "AutoClicker.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void OpenUrl_ShouldUseDefaultBrowserForStandardWebLinks()
    {
        ProcessStartInfo? capturedStartInfo = null;
        var service = new WindowsBrowserLauncherService(
            startInfo => capturedStartInfo = startInfo,
            () => null);

        var result = service.OpenUrl("https://fmhy.net/");

        result.BrowserDisplayName.Should().Be("default browser");
        capturedStartInfo.Should().NotBeNull();
        capturedStartInfo!.FileName.Should().Be("https://fmhy.net/");
        capturedStartInfo.UseShellExecute.Should().BeTrue();
        capturedStartInfo.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void OpenUrl_ShouldUseTorBrowserForOnionLinks()
    {
        ProcessStartInfo? capturedStartInfo = null;
        var torBrowserPath = CreateTorBrowserExecutable();
        var service = new WindowsBrowserLauncherService(
            startInfo => capturedStartInfo = startInfo,
            () => torBrowserPath);

        var result = service.OpenUrl("http://libraryexample.onion/");

        result.BrowserDisplayName.Should().Be("Tor Browser");
        capturedStartInfo.Should().NotBeNull();
        capturedStartInfo!.FileName.Should().Be(torBrowserPath);
        capturedStartInfo.Arguments.Should().Be("http://libraryexample.onion/");
        capturedStartInfo.UseShellExecute.Should().BeTrue();
        capturedStartInfo.WorkingDirectory.Should().Be(Path.GetDirectoryName(torBrowserPath));
    }

    [Fact]
    public void OpenUrl_ShouldThrowWhenTorBrowserIsRequiredButMissing()
    {
        var service = new WindowsBrowserLauncherService(
            _ => throw new InvalidOperationException("Process starter should not be called."),
            () => null);

        var action = () => service.OpenUrl("http://libraryexample.onion/");

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Tor Browser could not be located. Install it first, then try again.");
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, true);
        }
    }

    private string CreateTorBrowserExecutable()
    {
        var torBrowserDirectory = Path.Combine(workingDirectory, "Tor Browser", "Browser");
        Directory.CreateDirectory(torBrowserDirectory);
        var executablePath = Path.Combine(torBrowserDirectory, "firefox.exe");
        File.WriteAllText(executablePath, string.Empty);
        return executablePath;
    }
}
