using AutoClicker.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class WindowsWingetInstallerServiceTests
{
    [Fact]
    public void GetCatalog_ShouldIncludeAnyDesk()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Adobe.Acrobat.Reader.64-bit",
                DisplayName = "Adobe Acrobat Reader",
                Category = "Productivity",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "AnyDesk.AnyDesk",
                DisplayName = "AnyDesk",
                Category = "Remote Access",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Audacity.Audacity",
                DisplayName = "Audacity",
                Category = "Creator",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "BlenderFoundation.Blender",
                DisplayName = "Blender",
                Category = "Creative",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Bitwarden.Bitwarden",
                DisplayName = "Bitwarden",
                Category = "Security",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Cloudflare.Warp",
                DisplayName = "Cloudflare WARP",
                Category = "Security",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "9N0866FS04W8",
                DisplayName = "Dolby Access",
                Category = "Media",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Microsoft.Teams",
                DisplayName = "Microsoft Teams",
                Category = "Communication",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "KiCad.KiCad",
                DisplayName = "KiCad",
                Category = "CAD",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "FreeCAD.FreeCAD",
                DisplayName = "FreeCAD",
                Category = "CAD",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "PCSX2Team.PCSX2",
                DisplayName = "PCSX2",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Vita3K.Vita3K",
                DisplayName = "Vita3K",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "DolphinEmulator.Dolphin",
                DisplayName = "Dolphin",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Cemu.Cemu",
                DisplayName = "Cemu",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Libretro.RetroArch",
                DisplayName = "RetroArch",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Ryubing.Ryujinx",
                DisplayName = "Ryujinx (Ryubing)",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "RPCS3.RPCS3",
                DisplayName = "RPCS3",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "AzaharEmu.Azahar",
                DisplayName = "Lime3DS (Azahar)",
                Category = "Emulation",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Anki.Anki",
                DisplayName = "Anki",
                Category = "Learning",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "AUTOMATIC1111.StableDiffusionWebUI",
                DisplayName = "Stable Diffusion WebUI (AUTOMATIC1111)",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Comfy.ComfyUI-Desktop",
                DisplayName = "ComfyUI",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "OpenJS.NodeJS.LTS",
                DisplayName = "Node.js",
                Category = "Developer",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "9MV0B5HZVK9Z",
                DisplayName = "Xbox",
                Category = "Games",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Neovim.Neovim",
                DisplayName = "Neovim",
                Category = "Developer",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Ollama.Ollama",
                DisplayName = "Ollama",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "ElementLabs.LMStudio",
                DisplayName = "LM Studio",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "OpenWebUI.OpenWebUI",
                DisplayName = "Open WebUI",
                Category = "AI",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Macrium.Reflect",
                DisplayName = "Macrium Reflect",
                Category = "Utilities",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Guru3D.Afterburner",
                DisplayName = "MSI Afterburner",
                Category = "Utilities",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "OpenRGB.OpenRGB",
                DisplayName = "OpenRGB",
                Category = "Utilities",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "TorProject.TorBrowser",
                DisplayName = "Tor Browser",
                Category = "Browsers",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Microsoft.WSL",
                DisplayName = "WSL",
                Category = "Developer",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "WiresharkFoundation.Wireshark",
                DisplayName = "Wireshark",
                Category = "Networking",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Python.Python.3.14",
                DisplayName = "Python",
                Category = "Developer",
            });
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMapInstalledAndUpgradeablePackages()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var statuses = await service.GetPackageStatusesAsync(
            [
                "Git.Git",
                "Mozilla.Firefox",
                "Google.Chrome",
            ]);

        statuses.Should().ContainEquivalentOf(new { PackageId = "Git.Git", IsInstalled = true, HasUpdateAvailable = true, StatusText = "Update available" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "Mozilla.Firefox", IsInstalled = true, HasUpdateAvailable = false, StatusText = "Installed" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "Google.Chrome", IsInstalled = false, HasUpdateAvailable = false, StatusText = "Not installed" });
    }

    [Fact]
    public async Task GetPackageStatusesAsync_ShouldMarkGuidedInstallEntriesAsUntracked()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var statuses = await service.GetPackageStatusesAsync(
            [
                "Macrium.Reflect",
                "RPCS3.RPCS3",
            ]);

        statuses.Should().ContainEquivalentOf(new { PackageId = "Macrium.Reflect", IsInstalled = false, HasUpdateAvailable = false, StatusText = "Guided install" });
        statuses.Should().ContainEquivalentOf(new { PackageId = "RPCS3.RPCS3", IsInstalled = false, HasUpdateAvailable = false, StatusText = "Guided install" });
    }

    [Fact]
    public void CatalogEntries_ShouldExposeShortDescriptions()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        service.GetCatalog().Should().OnlyContain(
            item => !string.IsNullOrWhiteSpace(item.Description)
                && item.Description.Length <= 80);
        service.GetCleanupCatalog().Should().OnlyContain(
            item => !string.IsNullOrWhiteSpace(item.Description)
                && item.Description.Length <= 80);
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldTreatExistingInstallAsSuccessfulNoOp()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var results = await service.InstallPackagesAsync(["Microsoft.VisualStudioCode"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeFalse();
        results[0].Message.Should().Be("Already installed and up to date.");
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldReportMissingInstall()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var results = await service.UpgradePackagesAsync(["Roblox.Roblox"]);

        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeFalse();
        results[0].Changed.Should().BeFalse();
        results[0].Message.Should().Be("This app is not installed yet.");
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldInstallDiscordBeforeVencord()
    {
        var commands = new List<string>();
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                commands.Add(startInfo.Arguments);
                var packageId = startInfo.Arguments.Contains("Vendicated.Vencord", StringComparison.Ordinal)
                    ? "Vendicated.Vencord"
                    : "Discord.Discord";
                return Task.FromResult(
                    new InstallerCommandResult(
                        0,
                        $"Successfully installed {packageId}",
                        string.Empty));
            });

        var results = await service.InstallPackagesAsync(["Vendicated.Vencord"]);

        commands.Should().ContainInOrder(
            "install --exact --id \"Discord.Discord\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent",
            "install --exact --id \"Vendicated.Vencord\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
        results.Select(result => result.PackageId).Should().ContainInOrder("Discord.Discord", "Vendicated.Vencord");
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldIncludeMsStoreSourceForStorePackages()
    {
        string? capturedArguments = null;
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                capturedArguments = startInfo.Arguments;
                return Task.FromResult(new InstallerCommandResult(0, "Successfully installed", string.Empty));
            });

        await service.InstallPackagesAsync(["9NRX63209R7B"]);

        capturedArguments.Should().Be("install --exact --id \"9NRX63209R7B\" --source \"msstore\" --accept-package-agreements --accept-source-agreements --disable-interactivity --silent");
    }

    [Fact]
    public async Task UninstallPackagesAsync_ShouldUseMsStoreSourceForCleanupPackages()
    {
        string? capturedArguments = null;
        var service = new WindowsWingetInstallerService(
            (startInfo, _) =>
            {
                capturedArguments = startInfo.Arguments;
                return Task.FromResult(new InstallerCommandResult(0, "Successfully uninstalled", string.Empty));
            });

        var results = await service.UninstallPackagesAsync(["XP8BT8DW290MPQ"]);

        capturedArguments.Should().Be("uninstall --exact --id \"XP8BT8DW290MPQ\" --source \"msstore\" --accept-source-agreements --disable-interactivity --silent");
        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
    }

    [Fact]
    public async Task InstallPackagesAsync_ShouldOpenGuidedInstallPageForExternalEntries()
    {
        var openedTargets = new List<string>();
        var service = new WindowsWingetInstallerService(
            CreateRunner(),
            (target, _) =>
            {
                openedTargets.Add(target);
                return Task.CompletedTask;
            });

        var results = await service.InstallPackagesAsync(["Macrium.Reflect"]);

        openedTargets.Should().ContainSingle()
            .Which.Should().Be("https://www.macrium.com/products/home");
        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
        results[0].Message.Should().Be("Opened the official install page.");
    }

    [Fact]
    public async Task UpgradePackagesAsync_ShouldOpenGuidedUpdatePageForExternalEntries()
    {
        var openedTargets = new List<string>();
        var service = new WindowsWingetInstallerService(
            CreateRunner(),
            (target, _) =>
            {
                openedTargets.Add(target);
                return Task.CompletedTask;
            });

        var results = await service.UpgradePackagesAsync(["AUTOMATIC1111.StableDiffusionWebUI"]);

        openedTargets.Should().ContainSingle()
            .Which.Should().Be("https://github.com/AUTOMATIC1111/stable-diffusion-webui");
        results.Should().ContainSingle();
        results[0].Succeeded.Should().BeTrue();
        results[0].Changed.Should().BeTrue();
        results[0].Message.Should().Be("Opened the official update page.");
    }

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
}
