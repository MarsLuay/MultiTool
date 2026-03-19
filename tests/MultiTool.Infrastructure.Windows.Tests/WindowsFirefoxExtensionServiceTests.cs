using System.Text.Json.Nodes;
using MultiTool.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class WindowsFirefoxExtensionServiceTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "MultiTool.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void GetCatalog_ShouldExposeUBlockOriginAndPrivacyBadger()
    {
        var service = new WindowsFirefoxExtensionService(() => null);

        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                OptionId = "ublock-origin",
                DisplayName = "uBlock Origin",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                OptionId = "privacy-badger",
                DisplayName = "Privacy Badger",
            });
    }

    [Fact]
    public async Task SyncExtensionSelectionsAsync_ShouldWritePoliciesForSelectedExtensions()
    {
        var firefoxDirectory = CreateFirefoxInstallDirectory();
        var service = new WindowsFirefoxExtensionService(() => firefoxDirectory);

        var results = await service.SyncExtensionSelectionsAsync(["ublock-origin", "privacy-badger"]);

        var policiesPath = Path.Combine(firefoxDirectory, "distribution", "policies.json");
        File.Exists(policiesPath).Should().BeTrue();

        var root = JsonNode.Parse(await File.ReadAllTextAsync(policiesPath))!.AsObject();
        var extensionSettings = root["policies"]!["ExtensionSettings"]!.AsObject();

        extensionSettings["uBlock0@raymondhill.net"]!["installation_mode"]!.GetValue<string>().Should().Be("normal_installed");
        extensionSettings["uBlock0@raymondhill.net"]!["install_url"]!.GetValue<string>().Should().Be("https://addons.mozilla.org/firefox/downloads/latest/ublock-origin/latest.xpi");
        extensionSettings["jid1-MnnxcxisBPnSXQ@jetpack"]!["installation_mode"]!.GetValue<string>().Should().Be("normal_installed");
        extensionSettings["jid1-MnnxcxisBPnSXQ@jetpack"]!["install_url"]!.GetValue<string>().Should().Be("https://addons.mozilla.org/firefox/downloads/latest/privacy-badger17/latest.xpi");

        results.Should().HaveCount(2);
        results.Should().OnlyContain(result => result.Succeeded);
        results.Should().OnlyContain(result => result.Changed);
    }

    [Fact]
    public async Task SyncExtensionSelectionsAsync_ShouldPreserveUnrelatedPoliciesAndRemoveDeselectedManagedExtensions()
    {
        var firefoxDirectory = CreateFirefoxInstallDirectory();
        var policiesDirectory = Path.Combine(firefoxDirectory, "distribution");
        Directory.CreateDirectory(policiesDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(policiesDirectory, "policies.json"),
            """
            {
              "policies": {
                "DisableAppUpdate": true,
                "ExtensionSettings": {
                  "uBlock0@raymondhill.net": {
                    "installation_mode": "normal_installed",
                    "install_url": "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin/latest.xpi"
                  },
                  "jid1-MnnxcxisBPnSXQ@jetpack": {
                    "installation_mode": "normal_installed",
                    "install_url": "https://addons.mozilla.org/firefox/downloads/latest/privacy-badger17/latest.xpi"
                  },
                  "other@example.com": {
                    "installation_mode": "allowed"
                  }
                }
              }
            }
            """);

        var service = new WindowsFirefoxExtensionService(() => firefoxDirectory);

        var results = await service.SyncExtensionSelectionsAsync(["ublock-origin"]);

        var root = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(policiesDirectory, "policies.json")))!.AsObject();
        root["policies"]!["DisableAppUpdate"]!.GetValue<bool>().Should().BeTrue();

        var extensionSettings = root["policies"]!["ExtensionSettings"]!.AsObject();
        extensionSettings.ContainsKey("uBlock0@raymondhill.net").Should().BeTrue();
        extensionSettings.ContainsKey("jid1-MnnxcxisBPnSXQ@jetpack").Should().BeFalse();
        extensionSettings.ContainsKey("other@example.com").Should().BeTrue();

        results.Should().ContainSingle();
        results[0].DisplayName.Should().Be("Firefox: uBlock Origin");
        results[0].Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SyncExtensionSelectionsAsync_ShouldReturnFailureWhenFirefoxCannotBeFound()
    {
        var service = new WindowsFirefoxExtensionService(() => null);

        var results = await service.SyncExtensionSelectionsAsync(["ublock-origin"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeFalse();
        results[0].Message.Should().Be("Firefox could not be located. Install Firefox first, then try again.");
    }

    [Fact]
    public async Task SyncExtensionSelectionsAsync_ShouldRestartFirefoxWhenPoliciesChangeAndFirefoxIsRunning()
    {
        var firefoxDirectory = CreateFirefoxInstallDirectory();
        var restartedFirefox = false;
        var service = new WindowsFirefoxExtensionService(
            () => firefoxDirectory,
            firefoxRunningDetector: () => true,
            firefoxRestarter: (_, _) =>
            {
                restartedFirefox = true;
                return Task.FromResult(true);
            });

        var results = await service.SyncExtensionSelectionsAsync(["ublock-origin"]);

        restartedFirefox.Should().BeTrue();
        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
        results[0].Message.Should().Be("Configured for automatic install and restarted Firefox.");
    }

    [Fact]
    public async Task SyncExtensionSelectionsAsync_ShouldUseElevatedWriterWhenDirectPolicyWriteNeedsPermission()
    {
        var firefoxDirectory = CreateFirefoxInstallDirectory();
        var elevatedWriterUsed = false;
        var service = new WindowsFirefoxExtensionService(
            () => firefoxDirectory,
            policyWriter: (_, _, _) => throw new UnauthorizedAccessException("Denied."),
            elevatedPolicyWriter: (path, json, cancellationToken) =>
            {
                elevatedWriterUsed = true;
                return Task.Run(
                    async () =>
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                        await File.WriteAllTextAsync(path, json, cancellationToken);
                        return true;
                    },
                    cancellationToken);
            });

        var results = await service.SyncExtensionSelectionsAsync(["ublock-origin"]);

        elevatedWriterUsed.Should().BeTrue();
        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
        results[0].Message.Should().Be("Configured for automatic install.");
        File.Exists(Path.Combine(firefoxDirectory, "distribution", "policies.json")).Should().BeTrue();
    }

    [Fact]
    public async Task SyncExtensionSelectionsAsync_ShouldReturnFailureWhenElevatedWriteIsNotApproved()
    {
        var firefoxDirectory = CreateFirefoxInstallDirectory();
        var service = new WindowsFirefoxExtensionService(
            () => firefoxDirectory,
            policyWriter: (_, _, _) => throw new UnauthorizedAccessException("Denied."),
            elevatedPolicyWriter: (_, _, _) => Task.FromResult(false));

        var results = await service.SyncExtensionSelectionsAsync(["ublock-origin"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeFalse();
        results[0].Message.Should().Be("Firefox add-ons need administrator permission to update Firefox's policies. Approve the Windows prompt and try again.");
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, true);
        }
    }

    private string CreateFirefoxInstallDirectory()
    {
        var firefoxDirectory = Path.Combine(workingDirectory, "Mozilla Firefox");
        Directory.CreateDirectory(firefoxDirectory);
        File.WriteAllText(Path.Combine(firefoxDirectory, "firefox.exe"), string.Empty);
        return firefoxDirectory;
    }
}
