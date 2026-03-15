using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Installer;

public delegate Task<InstallerCommandResult> InstallerCommandRunner(ProcessStartInfo startInfo, CancellationToken cancellationToken);
public delegate Task GuidedInstallerLauncher(string target, CancellationToken cancellationToken);

public sealed record InstallerCommandResult(int ExitCode, string StandardOutput, string StandardError);

public sealed class WindowsWingetInstallerService : IInstallerService
{
    private static readonly IReadOnlyList<InstallerCatalogItem> Catalog =
    [
        new("Adobe.CreativeCloud", "Adobe Creative Cloud", "Creative", "Adobe Creative Cloud launcher."),
        new("Adobe.Acrobat.Reader.64-bit", "Adobe Acrobat Reader", "Productivity", "PDF reader from Adobe."),
        new("AnyDesk.AnyDesk", "AnyDesk", "Remote Access", "Remote desktop and support app."),
        new("Audacity.Audacity", "Audacity", "Creator", "Audio recording and editing app."),
        new("Bitwarden.Bitwarden", "Bitwarden", "Security", "Password manager desktop app."),
        new("BlenderFoundation.Blender", "Blender", "Creative", "Open-source 3D creation suite."),
        new("Cloudflare.Warp", "Cloudflare WARP", "Security", "Cloudflare WARP VPN client."),
        new("9N0866FS04W8", "Dolby Access", "Media", "Dolby audio setup from Microsoft Store.", false, false, Source: "msstore"),
        new("Discord.Discord", "Discord", "Communication", "Voice, chat, and community app.", true),
        new("Microsoft.Teams", "Microsoft Teams", "Communication", "Microsoft Teams desktop client."),
        new("Vendicated.Vencord", "Vencord", "Communication", "Discord mod installer for Discord.", false, false, Dependencies: ["Discord.Discord"]),
        new("FreeCAD.FreeCAD", "FreeCAD", "CAD", "Open-source parametric CAD app."),
        new("KiCad.KiCad", "KiCad", "CAD", "Open-source PCB design suite."),
        new("PCSX2Team.PCSX2", "PCSX2", "Emulation", "PlayStation 2 emulator.", false),
        new("Vita3K.Vita3K", "Vita3K", "Emulation", "PlayStation Vita emulator.", false),
        new("DolphinEmulator.Dolphin", "Dolphin", "Emulation", "GameCube and Wii emulator.", false),
        new("Cemu.Cemu", "Cemu", "Emulation", "Wii U emulator.", false),
        new("Libretro.RetroArch", "RetroArch", "Emulation", "Multi-system emulator frontend.", false),
        new(
            "Ryubing.Ryujinx",
            "Ryujinx (Ryubing)",
            "Emulation",
            "Community continuation of Ryujinx.",
            false,
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/Ryubing/Stable-Releases/releases/latest",
            UpdateUrl: "https://github.com/Ryubing/Stable-Releases/releases/latest"),
        new(
            "RPCS3.RPCS3",
            "RPCS3",
            "Emulation",
            "PlayStation 3 emulator.",
            false,
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://rpcs3.net/download",
            UpdateUrl: "https://rpcs3.net/download"),
        new(
            "AzaharEmu.Azahar",
            "Lime3DS (Azahar)",
            "Emulation",
            "Azahar is the Lime3DS successor.",
            false,
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/azahar-emu/azahar/releases/latest",
            UpdateUrl: "https://github.com/azahar-emu/azahar/releases/latest"),
        new("Anki.Anki", "Anki", "Learning", "Spaced-repetition flashcard app."),
        new(
            "AUTOMATIC1111.StableDiffusionWebUI",
            "Stable Diffusion WebUI (AUTOMATIC1111)",
            "AI",
            "Guided setup from the official project.",
            false,
            true,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/AUTOMATIC1111/stable-diffusion-webui/wiki/Install-and-Run-on-NVidia-GPUs",
            UpdateUrl: "https://github.com/AUTOMATIC1111/stable-diffusion-webui"),
        new("Comfy.ComfyUI-Desktop", "ComfyUI", "AI", "Node-based local image workflow app.", false, true),
        new("Neovim.Neovim", "Neovim", "Developer", "Vim-based text editor.", true, true),
        new("Ollama.Ollama", "Ollama", "AI", "Run local language models.", false, true),
        new("ElementLabs.LMStudio", "LM Studio", "AI", "Desktop app for local models.", false, true),
        new(
            "OpenWebUI.OpenWebUI",
            "Open WebUI",
            "AI",
            "Guided setup from the official docs.",
            false,
            true,
            TrackStatusWithWinget: false,
            InstallUrl: "https://docs.openwebui.com/getting-started/quick-start/",
            UpdateUrl: "https://docs.openwebui.com/getting-started/quick-start/"),
        new("Spotify.Spotify", "Spotify", "Media", "Spotify desktop player.", true),
        new("VideoLAN.VLC", "VLC", "Media", "Media player for most files.", true),
        new("Flow-Launcher.Flow-Launcher", "Flow Launcher", "Productivity", "App launcher and command palette.", true),
        new(
            "Macrium.Reflect",
            "Macrium Reflect",
            "Utilities",
            "Backup and recovery suite.",
            false,
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://www.macrium.com/products/home",
            UpdateUrl: "https://www.macrium.com/products/home"),
        new("Obsidian.Obsidian", "Obsidian", "Productivity", "Markdown notes and knowledge base."),
        new("9NRX63209R7B", "Outlook", "Productivity", "Outlook from Microsoft Store.", false, false, Source: "msstore"),
        new("AutoHotkey.AutoHotkey", "AutoHotkey", "Utilities", "Automation and hotkey scripting.", true, true),
        new("CrystalDewWorld.CrystalDiskMark", "CrystalDiskMark", "Utilities", "Storage benchmark tool."),
        new("Guru3D.Afterburner", "MSI Afterburner", "Utilities", "GPU tuning and monitoring tool.", true, true),
        new("OpenRGB.OpenRGB", "OpenRGB", "Utilities", "Open-source RGB control app."),
        new("voidtools.Everything", "Everything", "Utilities", "Instant file search.", true),
        new("Mozilla.Firefox", "Firefox", "Browsers", "Mozilla web browser.", true),
        new("Git.Git", "Git", "Developer", "Version control CLI.", true, true),
        new("OpenJS.NodeJS.LTS", "Node.js", "Developer", "Node.js LTS runtime.", true, true),
        new("Mojang.MinecraftLauncher", "Minecraft Launcher", "Games", "Official Minecraft launcher."),
        new("EpicGames.EpicGamesLauncher", "Epic Games Launcher", "Games", "Epic Games launcher and store."),
        new("9MV0B5HZVK9Z", "Xbox", "Games", "Xbox app from Microsoft Store.", false, false, Source: "msstore"),
        new("Microsoft.PowerShell", "PowerShell", "Developer", "Latest PowerShell release.", true, true),
        new("Python.Python.3.14", "Python", "Developer", "Latest stable Python release.", true, true),
        new("qBittorrent.qBittorrent", "qBittorrent", "Downloads", "Open-source BitTorrent client."),
        new("Roblox.Roblox", "Roblox", "Games", "Official Roblox client."),
        new("Valve.Steam", "Steam", "Games", "PC game launcher and store.", true),
        new("Microsoft.WSL", "WSL", "Developer", "Windows Subsystem for Linux.", true, true),
        new("Microsoft.VisualStudioCode", "Visual Studio Code", "Developer", "Code editor from Microsoft.", true, true),
        new("WiresharkFoundation.Wireshark", "Wireshark", "Networking", "Packet capture and analysis tool.", true, true),
        new("OBSProject.OBSStudio", "OBS Studio", "Creator", "Streaming and recording studio.", true),
        new("TorProject.TorBrowser", "Tor Browser", "Browsers", "Privacy browser on Tor."),
    ];

    private static readonly IReadOnlyList<InstallerCatalogItem> CleanupCatalog =
    [
        new("9P1J8S7CCWWT", "Clipchamp", "Windows Extras", "Bundled video editor.", true, false, Source: "msstore"),
        new("Microsoft.Edge", "Microsoft Edge", "Windows Extras", "Removable only where Windows allows it."),
        new("XP8BT8DW290MPQ", "Microsoft Teams", "Windows Extras", "Inbox Teams Store app.", true, false, Source: "msstore"),
        new("9WZDNCRFJ3Q2", "MSN Weather", "Windows Extras", "Bundled weather app.", true, false, Source: "msstore"),
        new("9NRX63209R7B", "Outlook for Windows", "Windows Extras", "Consumer Outlook Store app.", true, false, Source: "msstore"),
    ];

    private readonly InstallerCommandRunner commandRunner;
    private readonly GuidedInstallerLauncher guidedInstallerLauncher;
    private readonly IReadOnlyDictionary<string, InstallerCatalogItem> catalogById;

    public WindowsWingetInstallerService()
        : this(RunProcessAsync, LaunchGuidedInstallerAsync)
    {
    }

    public WindowsWingetInstallerService(
        InstallerCommandRunner commandRunner,
        GuidedInstallerLauncher? guidedInstallerLauncher = null)
    {
        this.commandRunner = commandRunner;
        this.guidedInstallerLauncher = guidedInstallerLauncher ?? LaunchGuidedInstallerAsync;
        catalogById = Catalog
            .Concat(CleanupCatalog)
            .GroupBy(item => item.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<InstallerCatalogItem> GetCatalog() => Catalog;

    public IReadOnlyList<InstallerCatalogItem> GetCleanupCatalog() => CleanupCatalog;

    public async Task<InstallerEnvironmentInfo> GetEnvironmentInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await RunWingetAsync("--version", cancellationToken).ConfigureAwait(false);
            var output = NormalizeOutput(result);
            var version = string.IsNullOrWhiteSpace(output) ? "unknown" : output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)[0];
            return new InstallerEnvironmentInfo(true, version, $"winget {version} is ready for silent installs and updates.");
        }
        catch (Win32Exception)
        {
            return new InstallerEnvironmentInfo(
                false,
                string.Empty,
                "winget is unavailable. Guided download entries still work, but silent installs need winget.");
        }
        catch (Exception ex)
        {
            return new InstallerEnvironmentInfo(
                false,
                string.Empty,
                $"Installer setup failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<InstallerPackageStatus>> GetPackageStatusesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default)
    {
        var normalizedIds = NormalizePackageIds(packageIds);
        if (normalizedIds.Count == 0)
        {
            return [];
        }

        var statusesById = new Dictionary<string, InstallerPackageStatus>(StringComparer.OrdinalIgnoreCase);
        var wingetTrackedIds = new List<string>(normalizedIds.Count);

        foreach (var packageId in normalizedIds)
        {
            if (catalogById.TryGetValue(packageId, out var package) && !package.TrackStatusWithWinget)
            {
                statusesById[packageId] = new InstallerPackageStatus(packageId, false, false, "Guided install");
                continue;
            }

            wingetTrackedIds.Add(packageId);
        }

        if (wingetTrackedIds.Count > 0)
        {
            var installedResult = await RunWingetAsync("list --accept-source-agreements --disable-interactivity", cancellationToken).ConfigureAwait(false);
            var upgradesResult = await RunWingetAsync("list --upgrade-available --accept-source-agreements --disable-interactivity", cancellationToken).ConfigureAwait(false);

            var installedOutput = NormalizeOutput(installedResult);
            var upgradesOutput = NormalizeOutput(upgradesResult);
            var installedIds = FindPackageIdsInOutput(installedOutput, wingetTrackedIds);
            var upgradeIds = FindPackageIdsInOutput(upgradesOutput, wingetTrackedIds);

            foreach (var packageId in wingetTrackedIds)
            {
                var hasUpgrade = upgradeIds.Contains(packageId);
                var isInstalled = hasUpgrade || installedIds.Contains(packageId);
                var statusText = hasUpgrade
                    ? "Update available"
                    : isInstalled
                        ? "Installed"
                        : "Not installed";
                statusesById[packageId] = new InstallerPackageStatus(packageId, isInstalled, hasUpgrade, statusText);
            }
        }

        return
        [
            .. normalizedIds.Select(
                packageId => statusesById.TryGetValue(packageId, out var status)
                    ? status
                    : new InstallerPackageStatus(packageId, false, false, "Not installed")),
        ];
    }

    public Task<IReadOnlyList<InstallerOperationResult>> InstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
        RunBatchAsync(
            ExpandPackageIdsForInstall(packageIds),
            InstallPackageAsync,
            cancellationToken);

    public Task<IReadOnlyList<InstallerOperationResult>> UpgradePackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
        RunBatchAsync(
            NormalizePackageIds(packageIds),
            UpgradePackageAsync,
            cancellationToken);

    public Task<IReadOnlyList<InstallerOperationResult>> UninstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
        RunBatchAsync(
            NormalizePackageIds(packageIds),
            UninstallPackageAsync,
            cancellationToken);

    private async Task<IReadOnlyList<InstallerOperationResult>> RunBatchAsync(
        IReadOnlyList<string> packageIds,
        Func<InstallerCatalogItem, CancellationToken, Task<InstallerOperationResult>> executePackageAsync,
        CancellationToken cancellationToken)
    {
        if (packageIds.Count == 0)
        {
            return [];
        }

        var results = new List<InstallerOperationResult>(packageIds.Count);

        foreach (var packageId in packageIds)
        {
            if (!catalogById.TryGetValue(packageId, out var package))
            {
                results.Add(
                    new InstallerOperationResult(
                        packageId,
                        packageId,
                        false,
                        false,
                        "This package is not in the current installer catalog.",
                        string.Empty));
                continue;
            }

            try
            {
                results.Add(await executePackageAsync(package, cancellationToken).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                results.Add(
                    new InstallerOperationResult(
                        package.PackageId,
                        package.DisplayName,
                        false,
                        false,
                        ex.Message,
                        ex.ToString()));
            }
        }

        return results;
    }

    private async Task<InstallerOperationResult> InstallPackageAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        if (UsesGuidedInstall(package))
        {
            return await LaunchGuidedPackageAsync(
                package,
                package.InstallUrl,
                "Opened the official install page.",
                cancellationToken).ConfigureAwait(false);
        }

        var result = await RunWingetAsync(BuildPackageCommand("install", package), cancellationToken).ConfigureAwait(false);
        return InterpretInstallResult(package, result);
    }

    private async Task<InstallerOperationResult> UpgradePackageAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        if (UsesGuidedUpdate(package))
        {
            return await LaunchGuidedPackageAsync(
                package,
                package.UpdateUrl ?? package.InstallUrl,
                "Opened the official update page.",
                cancellationToken).ConfigureAwait(false);
        }

        var result = await RunWingetAsync(BuildPackageCommand("upgrade", package), cancellationToken).ConfigureAwait(false);
        return InterpretUpgradeResult(package, result);
    }

    private async Task<InstallerOperationResult> UninstallPackageAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var result = await RunWingetAsync(BuildPackageCommand("uninstall", package), cancellationToken).ConfigureAwait(false);
        return InterpretUninstallResult(package, result);
    }

    private async Task<InstallerCommandResult> RunWingetAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "winget",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        return await commandRunner(startInfo, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InstallerOperationResult> LaunchGuidedPackageAsync(
        InstallerCatalogItem package,
        string? target,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "No official install link is configured for this app.",
                string.Empty);
        }

        await guidedInstallerLauncher(target, cancellationToken).ConfigureAwait(false);
        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            successMessage,
            target);
    }

    private static InstallerOperationResult InterpretInstallResult(InstallerCatalogItem package, InstallerCommandResult result)
    {
        var output = NormalizeOutput(result);

        if (Contains(output, "Successfully installed"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Installed successfully.", output);
        }

        if (Contains(output, "Found an existing package already installed"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, false, "Already installed and up to date.", output);
        }

        if (Contains(output, "No package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, "winget could not find this package.", output);
        }

        if (result.ExitCode == 0)
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Install completed.", output);
        }

        return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, SummarizeFailure(output, "Install failed."), output);
    }

    private static InstallerOperationResult InterpretUpgradeResult(InstallerCatalogItem package, InstallerCommandResult result)
    {
        var output = NormalizeOutput(result);

        if (Contains(output, "Successfully installed") || Contains(output, "Successfully upgraded"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Updated successfully.", output);
        }

        if (Contains(output, "No available upgrade found") || Contains(output, "No newer package versions are available"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, false, "Already up to date.", output);
        }

        if (Contains(output, "No installed package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, "This app is not installed yet.", output);
        }

        if (Contains(output, "No package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, "winget could not find this package.", output);
        }

        if (result.ExitCode == 0)
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Update completed.", output);
        }

        return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, SummarizeFailure(output, "Update failed."), output);
    }

    private static InstallerOperationResult InterpretUninstallResult(InstallerCatalogItem package, InstallerCommandResult result)
    {
        var output = NormalizeOutput(result);

        if (Contains(output, "Successfully uninstalled") || Contains(output, "Successfully removed"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Removed successfully.", output);
        }

        if (Contains(output, "No installed package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, false, "Already missing on this PC.", output);
        }

        if (Contains(output, "No package found matching input criteria"))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, "Windows does not expose this app as removable on this PC.", output);
        }

        if (result.ExitCode == 0)
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Removal completed.", output);
        }

        return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, SummarizeFailure(output, "Removal failed."), output);
    }

    private static HashSet<string> FindPackageIdsInOutput(string output, IReadOnlyList<string> packageIds)
    {
        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(output))
        {
            return matches;
        }

        foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var packageId in packageIds)
            {
                if (line.Contains(packageId, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(packageId);
                }
            }
        }

        return matches;
    }

    private static IReadOnlyList<string> NormalizePackageIds(IEnumerable<string> packageIds)
    {
        var normalizedIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageId in packageIds)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                continue;
            }

            var normalized = packageId.Trim();
            if (seen.Add(normalized))
            {
                normalizedIds.Add(normalized);
            }
        }

        return normalizedIds;
    }

    private IReadOnlyList<string> ExpandPackageIdsForInstall(IEnumerable<string> packageIds)
    {
        var expandedIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageId in NormalizePackageIds(packageIds))
        {
            ExpandPackageWithDependencies(packageId, expandedIds, seen);
        }

        return expandedIds;
    }

    private void ExpandPackageWithDependencies(string packageId, List<string> expandedIds, HashSet<string> seen)
    {
        if (!catalogById.TryGetValue(packageId, out var package))
        {
            if (seen.Add(packageId))
            {
                expandedIds.Add(packageId);
            }

            return;
        }

        if (package.Dependencies is not null)
        {
            foreach (var dependencyId in package.Dependencies)
            {
                ExpandPackageWithDependencies(dependencyId, expandedIds, seen);
            }
        }

        if (seen.Add(packageId))
        {
            expandedIds.Add(packageId);
        }
    }

    private static string BuildPackageCommand(string verb, InstallerCatalogItem package)
    {
        var sourceSegment = string.IsNullOrWhiteSpace(package.Source)
            ? string.Empty
            : $" --source {QuoteArgument(package.Source)}";

        var packageAgreementSegment = verb.Equals("uninstall", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : " --accept-package-agreements";

        return $"{verb} --exact --id {QuoteArgument(package.PackageId)}{sourceSegment}{packageAgreementSegment} --accept-source-agreements --disable-interactivity --silent";
    }

    private static string NormalizeOutput(InstallerCommandResult result)
    {
        var combined = $"{result.StandardOutput}{Environment.NewLine}{result.StandardError}";
        var lines = combined
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !IsProgressLine(line))
            .Where(line => !string.Equals(line, "Failed in attempting to update the source: winget", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return string.Join(Environment.NewLine, lines);
    }

    private static bool IsProgressLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return true;
        }

        if (line.Contains("KB /", StringComparison.OrdinalIgnoreCase)
            || line.Contains("MB /", StringComparison.OrdinalIgnoreCase)
            || line.Contains("GB /", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (line.EndsWith("%", StringComparison.Ordinal) && line.Any(char.IsDigit))
        {
            return true;
        }

        foreach (var character in line)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            if (character is '-' or '\\' or '|' or '/' or '█' or '▒' or '▓' or '░')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool Contains(string output, string value) =>
        output.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool UsesGuidedInstall(InstallerCatalogItem package) =>
        !string.IsNullOrWhiteSpace(package.InstallUrl) && !package.TrackStatusWithWinget;

    private static bool UsesGuidedUpdate(InstallerCatalogItem package) =>
        (!string.IsNullOrWhiteSpace(package.UpdateUrl) || UsesGuidedInstall(package))
        && !package.TrackStatusWithWinget;

    private static string QuoteArgument(string value) => $"\"{value}\"";

    private static string SummarizeFailure(string output, string fallbackMessage)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return fallbackMessage;
        }

        var interestingLine = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(
                line => !line.StartsWith("Found ", StringComparison.OrdinalIgnoreCase)
                     && !line.StartsWith("This application is licensed", StringComparison.OrdinalIgnoreCase)
                     && !line.StartsWith("Microsoft is not responsible", StringComparison.OrdinalIgnoreCase)
                     && !line.StartsWith("Starting package install", StringComparison.OrdinalIgnoreCase));

        return interestingLine ?? fallbackMessage;
    }

    private static Task LaunchGuidedInstallerAsync(string target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Process.Start(
            new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true,
            });
        return Task.CompletedTask;
    }

    private static async Task<InstallerCommandResult> RunProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        var outputCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                outputCompletion.TrySetResult();
                return;
            }

            standardOutput.AppendLine(args.Data);
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                errorCompletion.TrySetResult();
                return;
            }

            standardError.AppendLine(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(outputCompletion.Task, errorCompletion.Task).ConfigureAwait(false);
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw;
        }

        return new InstallerCommandResult(process.ExitCode, standardOutput.ToString(), standardError.ToString());
    }
}
