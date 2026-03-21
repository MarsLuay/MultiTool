using MultiTool.Core.Models;
using MultiTool.Infrastructure.Windows.Installer;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;


public sealed partial class WindowsWingetInstallerServiceTests
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
                PackageId = "Microsoft.DotNet.SDK.10",
                DisplayName = "Microsoft .NET SDK",
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
                PackageId = "TechPowerUp.NVCleanstall",
                DisplayName = "NVCleanstall",
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
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Guided.SKiDL",
                DisplayName = "SKiDL",
                Category = "CAD",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Guided.PCBFlow",
                DisplayName = "PCBFlow",
                Category = "CAD",
            });
        service.GetCatalog().Should().ContainEquivalentOf(
            new
            {
                PackageId = "Guided.build123d",
                DisplayName = "build123d",
                Category = "CAD",
            });
    }

    [Fact]
    public void GetPackageCapabilities_ShouldExposeInteractiveAndReinstallForWingetPackages()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var capabilities = service.GetPackageCapabilities("Microsoft.VisualStudioCode");

        capabilities.SupportsInstall.Should().BeTrue();
        capabilities.SupportsUpdate.Should().BeTrue();
        capabilities.SupportsInteractiveInstall.Should().BeTrue();
        capabilities.SupportsInteractiveUpdate.Should().BeTrue();
        capabilities.SupportsReinstall.Should().BeTrue();
        capabilities.UsesWinget.Should().BeTrue();
        capabilities.UsesCustomFlow.Should().BeFalse();
    }

    [Fact]
    public void GetPackageCapabilities_ShouldExposeOfficialPageForCustomPackages()
    {
        var service = new WindowsWingetInstallerService(CreateRunner());

        var capabilities = service.GetPackageCapabilities("AUTOMATIC1111.StableDiffusionWebUI");

        capabilities.SupportsInteractiveInstall.Should().BeFalse();
        capabilities.SupportsReinstall.Should().BeFalse();
        capabilities.SupportsOpenInstallPage.Should().BeTrue();
        capabilities.SupportsOpenUpdatePage.Should().BeTrue();
        capabilities.UsesCustomFlow.Should().BeTrue();
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
}
