using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using Microsoft.Win32;

namespace AutoClicker.Infrastructure.Windows.Installer;

public delegate Task<InstallerCommandResult> InstallerCommandRunner(ProcessStartInfo startInfo, CancellationToken cancellationToken);
public delegate Task GuidedInstallerLauncher(string target, CancellationToken cancellationToken);
public delegate Task InstallerFileDownloader(string url, string destinationPath, CancellationToken cancellationToken);
public delegate Task<InstallerReleaseAsset?> InstallerReleaseAssetResolver(CancellationToken cancellationToken);
public delegate Task InstallerExecutableLauncher(ProcessStartInfo startInfo, bool preferUnelevated, CancellationToken cancellationToken);

public sealed record InstallerCommandResult(int ExitCode, string StandardOutput, string StandardError);
public sealed record InstallerReleaseAsset(string FileName, string DownloadUrl);

public sealed class WindowsWingetInstallerService : IInstallerService
{
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();

    private const string Automatic1111PackageId = "AUTOMATIC1111.StableDiffusionWebUI";
    private const string Automatic1111RepositoryUrl = "https://github.com/AUTOMATIC1111/stable-diffusion-webui.git";
    private const string Automatic1111StableDiffusionRepositoryUrl = "https://github.com/w-e-w/stablediffusion.git";
    private const string Automatic1111LauncherFileName = "launch-webui-multitool.bat";
    private const string SpotifyPackageId = "Spotify.Spotify";
    private const string SpotifyFallbackInstallerUrl = "https://download.scdn.co/SpotifySetup.exe";
    private const string SpotifyFallbackInstallerFileName = "SpotifySetup.exe";
    private const string SpotifyFallbackInstallerLogFileName = "spotify-installer.log";
    private const string SpotifyFallbackInstallerArgumentsTemplate = "/silent /skip-app-launch /log-file {0}";
    private const int SpotifyFallbackStatusPollAttempts = 20;
    private const string OpenWebUiPackageId = "OpenWebUI.OpenWebUI";
    private const string FirefoxPackageId = "Mozilla.Firefox";
    private const string DiscordPackageId = "Discord.Discord";
    private const string VencordPackageId = "Vendicated.Vencord";
    private const string GitPackageId = "Git.Git";
    private const string PowerShellPackageId = "Microsoft.PowerShell";
    private const string QbittorrentPackageId = "qBittorrent.qBittorrent";
    private const string EverythingPackageId = "voidtools.Everything";
    private const string TorBrowserPackageId = "TorProject.TorBrowser";
    private const string OpenWebUiLauncherFileName = "launch-open-webui-multitool.bat";
    private const string RyubingRyujinxPackageId = "Ryubing.Ryujinx";
    private const string RyubingRyujinxLauncherFileName = "launch-ryujinx-ryubing-multitool.bat";
    private const string RyubingRyujinxLatestReleaseApiUrl = "https://git.ryujinx.app/api/v4/projects/ryubing%2Fryujinx/releases?per_page=1";
    private const string AzaharPackageId = "AzaharEmu.Azahar";
    private const string AzaharLauncherFileName = "launch-azahar-multitool.bat";
    private const string AzaharLatestReleaseApiUrl = "https://api.github.com/repos/azahar-emu/azahar/releases/latest";
    private const string MacriumReflectPackageId = "Macrium.Reflect";
    private const string MacriumReflectLauncherFileName = "launch-macrium-reflect-multitool.bat";
    private const string MacriumReflectUpdateLogFileName = "macrium-reflect-update.log";
    private const string MacriumReflectInstallerResolverUrl = "https://updates.macrium.com/Reflect/v10/getmsi.asp?arch=1&edition=0&type=0";
    private const string Rpcs3PackageId = "RPCS3.RPCS3";
    private const string Rpcs3LauncherFileName = "launch-rpcs3-multitool.bat";
    private const string Rpcs3LatestReleaseApiUrl = "https://api.github.com/repos/RPCS3/rpcs3-binaries-win/releases/latest";
    private const string InstalledReleaseMarkerFileName = ".multitool-installed-release.txt";
    private const int RestartRequiredExitCode = 3010;

    private static readonly InstallerCatalogItem Automatic1111PythonPackage =
        new("Python.Python.3.10", "Python 3.10", "Developer", "Python 3.10 for older Torch-based apps.");
    private static readonly InstallerCatalogItem OpenWebUiPythonPackage =
        new("Python.Python.3.11", "Python 3.11", "Developer", "Python 3.11 for Open WebUI.");
    private static readonly InstallerCatalogItem Rpcs3VisualCppPackage =
        new("Microsoft.VCRedist.2015+.x64", "Visual C++ Redistributable (x64)", "Utilities", "VC++ runtime required by RPCS3.");
    private static readonly InstallerCatalogItem SevenZipPackage =
        new("7zip.7zip", "7-Zip", "Utilities", "Archive extraction tool for custom installs.");
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> WingetOutputAliases =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [FirefoxPackageId] = ["Mozilla Firefox"],
            [PowerShellPackageId] = ["PowerShell 7", "PowerShell"],
            [QbittorrentPackageId] = ["qBittorrent"],
            [EverythingPackageId] = ["Everything"],
            [TorBrowserPackageId] = ["Tor Browser"],
        };

    private static readonly IReadOnlyList<InstallerCatalogItem> Catalog =
    [
        new("Adobe.CreativeCloud", "Adobe Creative Cloud", "Creative", "Adobe Creative Cloud launcher."),
        new("Adobe.Acrobat.Reader.64-bit", "Adobe Acrobat Reader", "Productivity", "PDF reader from Adobe."),
        new("AnyDesk.AnyDesk", "AnyDesk", "Remote Access", "Remote desktop and support app."),
        new("Audacity.Audacity", "Audacity", "Creator", "Audio recording and editing app."),
        new("Bitwarden.Bitwarden", "Bitwarden", "Security", "Password manager desktop app."),
        new("BlenderFoundation.Blender", "Blender", "Creative", "Open-source 3D creation suite."),
        new("Cloudflare.Warp", "Cloudflare WARP", "Security", "Cloudflare WARP VPN client.", true),
        new("9N0866FS04W8", "Dolby Access", "Media", "Dolby audio setup from Microsoft Store.", true, false, Source: "msstore"),
        new("Discord.Discord", "Discord", "Communication", "Voice, chat, and community app.", true),
        new("Microsoft.Teams", "Microsoft Teams", "Communication", "Microsoft Teams desktop client."),
        new("Vendicated.Vencord", "Vencord", "Communication", "Discord mod installer for Discord.", true, false, Dependencies: ["Discord.Discord"]),
        new("FreeCAD.FreeCAD", "FreeCAD", "CAD", "Open-source parametric CAD app."),
        new("KiCad.KiCad", "KiCad", "CAD", "Open-source PCB design suite."),
        new("PCSX2Team.PCSX2", "PCSX2", "Emulation", "PlayStation 2 emulator.", false),
        new("Vita3K.Vita3K", "Vita3K", "Emulation", "PlayStation Vita emulator.", false),
        new("DolphinEmulator.Dolphin", "Dolphin", "Emulation", "GameCube and Wii emulator.", false),
        new("Cemu.Cemu", "Cemu", "Emulation", "Wii U emulator.", false),
        new("Libretro.RetroArch", "RetroArch", "Emulation", "Multi-system emulator frontend.", false),
        new(
            RyubingRyujinxPackageId,
            "Ryujinx (Ryubing)",
            "Emulation",
            "Downloads the latest Windows build and launches it.",
            true,
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/Ryubing/Stable-Releases/releases/latest",
            UpdateUrl: "https://github.com/Ryubing/Stable-Releases/releases/latest",
            UsesCustomInstallFlow: true),
        new(
            Rpcs3PackageId,
            "RPCS3",
            "Emulation",
            "Downloads the latest Windows build and launches it.",
            false,
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://rpcs3.net/download",
            UpdateUrl: "https://rpcs3.net/download",
            UsesCustomInstallFlow: true),
        new(
            AzaharPackageId,
            "Lime3DS (Azahar)",
            "Emulation",
            "Downloads the latest Windows MSYS2 build and launches it.",
            false,
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/azahar-emu/azahar/releases/latest",
            UpdateUrl: "https://github.com/azahar-emu/azahar/releases/latest",
            UsesCustomInstallFlow: true),
        new("Anki.Anki", "Anki", "Learning", "Spaced-repetition flashcard app."),
        new(
            Automatic1111PackageId,
            "Stable Diffusion WebUI (AUTOMATIC1111)",
            "AI",
            "Clones the official repo and launches first-run setup.",
            false,
            true,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/AUTOMATIC1111/stable-diffusion-webui/wiki/Install-and-Run-on-NVidia-GPUs",
            UpdateUrl: "https://github.com/AUTOMATIC1111/stable-diffusion-webui",
            UsesCustomInstallFlow: true),
        new("Comfy.ComfyUI-Desktop", "ComfyUI", "AI", "Node-based local image workflow app.", false, true),
        new("Neovim.Neovim", "Neovim", "Developer", "Vim-based text editor.", true, true),
        new("Ollama.Ollama", "Ollama", "AI", "Run local language models.", false, true),
        new("ElementLabs.LMStudio", "LM Studio", "AI", "Desktop app for local models.", false, true),
        new(
            OpenWebUiPackageId,
            "Open WebUI",
            "AI",
            "Creates a Python 3.11 venv and launches the local server.",
            false,
            true,
            TrackStatusWithWinget: false,
            InstallUrl: "https://docs.openwebui.com/getting-started/quick-start/",
            UpdateUrl: "https://docs.openwebui.com/getting-started/quick-start/",
            UsesCustomInstallFlow: true),
        new("Spotify.Spotify", "Spotify", "Media", "Spotify desktop player.", true),
        new("VideoLAN.VLC", "VLC", "Media", "Media player for most files.", true),
        new("Flow-Launcher.Flow-Launcher", "Flow Launcher", "Productivity", "App launcher and command palette.", true),
        new(
            MacriumReflectPackageId,
            "Macrium Reflect",
            "Utilities",
            "Downloads the latest Home installer and launches it.",
            false,
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://www.macrium.com/products/home",
            UpdateUrl: "https://www.macrium.com/products/home",
            UsesCustomInstallFlow: true),
        new("Obsidian.Obsidian", "Obsidian", "Productivity", "Markdown notes and knowledge base.", true),
        new("9NRX63209R7B", "Outlook", "Productivity", "Outlook from Microsoft Store.", true, false, Source: "msstore"),
        new("AutoHotkey.AutoHotkey", "AutoHotkey", "Utilities", "Automation and hotkey scripting.", true, true),
        new("CrystalDewWorld.CrystalDiskMark", "CrystalDiskMark", "Utilities", "Storage benchmark tool."),
        new("Guru3D.Afterburner", "MSI Afterburner", "Utilities", "GPU tuning and monitoring tool.", true, true),
        new("OpenRGB.OpenRGB", "OpenRGB", "Utilities", "Open-source RGB control app."),
        new("voidtools.Everything", "Everything", "Utilities", "Instant file search.", true),
        new("Mozilla.Firefox", "Firefox", "Browsers", "Mozilla web browser.", true),
        new("Git.Git", "Git", "Developer", "Version control CLI.", true, true),
        new("OpenJS.NodeJS.LTS", "Node.js", "Developer", "Node.js LTS runtime.", true, true),
        new("Mojang.MinecraftLauncher", "Minecraft Launcher", "Games", "Official Minecraft launcher.", true),
        new("EpicGames.EpicGamesLauncher", "Epic Games Launcher", "Games", "Epic Games launcher and store.", true),
        new("9MV0B5HZVK9Z", "Xbox", "Games", "Xbox app from Microsoft Store.", false, false, Source: "msstore"),
        new("Microsoft.PowerShell", "PowerShell", "Developer", "Latest PowerShell release.", true, true),
        new("Python.Python.3.14", "Python", "Developer", "Latest stable Python release.", true, true),
        new("qBittorrent.qBittorrent", "qBittorrent", "Downloads", "Open-source BitTorrent client.", true),
        new("Roblox.Roblox", "Roblox", "Games", "Official Roblox client.", true),
        new("Valve.Steam", "Steam", "Games", "PC game launcher and store.", true),
        new("Microsoft.WSL", "WSL", "Developer", "Windows Subsystem for Linux.", true, true),
        new("Microsoft.VisualStudioCode", "Visual Studio Code", "Developer", "Code editor from Microsoft.", true, true),
        new("WiresharkFoundation.Wireshark", "Wireshark", "Networking", "Packet capture and analysis tool.", false, true),
        new("OBSProject.OBSStudio", "OBS Studio", "Creator", "Streaming and recording studio.", false),
        new("TorProject.TorBrowser", "Tor Browser", "Browsers", "Privacy browser on Tor.", true),
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
    private readonly Func<string> automatic1111InstallDirectoryResolver;
    private readonly Func<string?> gitExecutableResolver;
    private readonly Func<string?> python310ExecutableResolver;
    private readonly Func<string?> firefoxInstallDirectoryResolver;
    private readonly Func<string> openWebUiInstallDirectoryResolver;
    private readonly Func<string?> python311ExecutableResolver;
    private readonly Func<string> ryubingRyujinxInstallDirectoryResolver;
    private readonly InstallerReleaseAssetResolver ryubingRyujinxReleaseResolver;
    private readonly Func<string> azaharInstallDirectoryResolver;
    private readonly InstallerReleaseAssetResolver azaharReleaseResolver;
    private readonly Func<string> macriumReflectWorkingDirectoryResolver;
    private readonly Func<string?> macriumReflectExecutableResolver;
    private readonly InstallerReleaseAssetResolver macriumReflectReleaseResolver;
    private readonly Func<string> rpcs3InstallDirectoryResolver;
    private readonly Func<string?> sevenZipExecutableResolver;
    private readonly InstallerReleaseAssetResolver rpcs3ReleaseResolver;
    private readonly InstallerFileDownloader fileDownloader;
    private readonly InstallerExecutableLauncher installerExecutableLauncher;
    private readonly Func<TimeSpan, CancellationToken, Task> delayAsync;
    private readonly Func<string, InstallerPackageStatus?> localPackageStatusResolver;

    public WindowsWingetInstallerService()
        : this(
            RunProcessAsync,
            LaunchGuidedInstallerAsync,
            ResolveAutomatic1111InstallDirectory,
            ResolveGitExecutablePath,
            ResolvePython310ExecutablePath,
            ResolveFirefoxInstallDirectory,
            ResolveOpenWebUiInstallDirectory,
            ResolvePython311ExecutablePath,
            ResolveRyubingRyujinxInstallDirectory,
            ResolveLatestRyubingRyujinxReleaseAssetAsync,
            ResolveAzaharInstallDirectory,
            ResolveLatestAzaharReleaseAssetAsync,
            ResolveMacriumReflectWorkingDirectory,
            ResolveMacriumReflectExecutablePath,
            ResolveLatestMacriumReflectInstallerAsync,
            ResolveRpcs3InstallDirectory,
            ResolveSevenZipExecutablePath,
            ResolveLatestRpcs3ReleaseAssetAsync,
            DownloadFileAsync,
            LaunchInstallerExecutableAsync,
            DelayAsync)
    {
    }

    public WindowsWingetInstallerService(
        InstallerCommandRunner commandRunner,
        GuidedInstallerLauncher? guidedInstallerLauncher = null,
        Func<string>? automatic1111InstallDirectoryResolver = null,
        Func<string?>? gitExecutableResolver = null,
        Func<string?>? python310ExecutableResolver = null,
        Func<string?>? firefoxInstallDirectoryResolver = null,
        Func<string>? openWebUiInstallDirectoryResolver = null,
        Func<string?>? python311ExecutableResolver = null,
        Func<string>? ryubingRyujinxInstallDirectoryResolver = null,
        InstallerReleaseAssetResolver? ryubingRyujinxReleaseResolver = null,
        Func<string>? azaharInstallDirectoryResolver = null,
        InstallerReleaseAssetResolver? azaharReleaseResolver = null,
        Func<string>? macriumReflectWorkingDirectoryResolver = null,
        Func<string?>? macriumReflectExecutableResolver = null,
        InstallerReleaseAssetResolver? macriumReflectReleaseResolver = null,
        Func<string>? rpcs3InstallDirectoryResolver = null,
        Func<string?>? sevenZipExecutableResolver = null,
        InstallerReleaseAssetResolver? rpcs3ReleaseResolver = null,
        InstallerFileDownloader? fileDownloader = null,
        InstallerExecutableLauncher? installerExecutableLauncher = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Func<string, InstallerPackageStatus?>? localPackageStatusResolver = null)
    {
        this.commandRunner = commandRunner;
        this.guidedInstallerLauncher = guidedInstallerLauncher ?? LaunchGuidedInstallerAsync;
        this.automatic1111InstallDirectoryResolver = automatic1111InstallDirectoryResolver ?? ResolveAutomatic1111InstallDirectory;
        this.gitExecutableResolver = gitExecutableResolver ?? ResolveGitExecutablePath;
        this.python310ExecutableResolver = python310ExecutableResolver ?? ResolvePython310ExecutablePath;
        this.firefoxInstallDirectoryResolver = firefoxInstallDirectoryResolver ?? ResolveFirefoxInstallDirectory;
        this.openWebUiInstallDirectoryResolver = openWebUiInstallDirectoryResolver ?? ResolveOpenWebUiInstallDirectory;
        this.python311ExecutableResolver = python311ExecutableResolver ?? ResolvePython311ExecutablePath;
        this.ryubingRyujinxInstallDirectoryResolver = ryubingRyujinxInstallDirectoryResolver ?? ResolveRyubingRyujinxInstallDirectory;
        this.ryubingRyujinxReleaseResolver = ryubingRyujinxReleaseResolver ?? ResolveLatestRyubingRyujinxReleaseAssetAsync;
        this.azaharInstallDirectoryResolver = azaharInstallDirectoryResolver ?? ResolveAzaharInstallDirectory;
        this.azaharReleaseResolver = azaharReleaseResolver ?? ResolveLatestAzaharReleaseAssetAsync;
        this.macriumReflectWorkingDirectoryResolver = macriumReflectWorkingDirectoryResolver ?? ResolveMacriumReflectWorkingDirectory;
        this.macriumReflectExecutableResolver = macriumReflectExecutableResolver ?? ResolveMacriumReflectExecutablePath;
        this.macriumReflectReleaseResolver = macriumReflectReleaseResolver ?? ResolveLatestMacriumReflectInstallerAsync;
        this.rpcs3InstallDirectoryResolver = rpcs3InstallDirectoryResolver ?? ResolveRpcs3InstallDirectory;
        this.sevenZipExecutableResolver = sevenZipExecutableResolver ?? ResolveSevenZipExecutablePath;
        this.rpcs3ReleaseResolver = rpcs3ReleaseResolver ?? ResolveLatestRpcs3ReleaseAssetAsync;
        this.fileDownloader = fileDownloader ?? DownloadFileAsync;
        this.installerExecutableLauncher = installerExecutableLauncher ?? LaunchInstallerExecutableAsync;
        this.delayAsync = delayAsync ?? DelayAsync;
        this.localPackageStatusResolver = localPackageStatusResolver ?? TryGetLocalPackageStatus;
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
            var customStatus = await GetCustomPackageStatusAsync(packageId, cancellationToken).ConfigureAwait(false);
            if (customStatus is not null)
            {
                statusesById[packageId] = customStatus;
                continue;
            }

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

        ApplyLocalFallbackStatuses(statusesById, wingetTrackedIds);

        return
        [
            .. normalizedIds.Select(
                packageId => statusesById.TryGetValue(packageId, out var status)
                    ? status
                    : new InstallerPackageStatus(packageId, false, false, "Not installed")),
        ];
    }

    public async Task<IReadOnlyList<InstallerOperationResult>> InstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default)
    {
        var expandedPackageIds = await ExpandPackageIdsForInstallAsync(packageIds, cancellationToken).ConfigureAwait(false);
        if (expandedPackageIds.Count == 0)
        {
            return [];
        }

        var statusesById = (await GetPackageStatusesAsync(expandedPackageIds, cancellationToken).ConfigureAwait(false))
            .GroupBy(static status => status.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.Last(), StringComparer.OrdinalIgnoreCase);
        var results = new List<InstallerOperationResult>(expandedPackageIds.Count);

        foreach (var packageId in expandedPackageIds)
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

            if (statusesById.TryGetValue(packageId, out var status) && status.IsInstalled)
            {
                results.Add(CreateSkippedInstallResult(package, status));
                continue;
            }

            try
            {
                results.Add(await InstallPackageAsync(package, cancellationToken).ConfigureAwait(false));
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
        if (IsSpotifyPackage(package))
        {
            return await InstallSpotifyAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsAutomatic1111Package(package))
        {
            return await InstallAutomatic1111Async(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsOpenWebUiPackage(package))
        {
            return await InstallOpenWebUiAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsRyubingRyujinxPackage(package))
        {
            return await InstallRyubingRyujinxAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsAzaharPackage(package))
        {
            return await InstallAzaharAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsMacriumReflectPackage(package))
        {
            return await InstallMacriumReflectAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsRpcs3Package(package))
        {
            return await InstallRpcs3Async(package, cancellationToken).ConfigureAwait(false);
        }

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

    private async Task<InstallerOperationResult> InstallSpotifyAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var wingetResult = await RunWingetAsync(BuildPackageCommand("install", package), cancellationToken).ConfigureAwait(false);
        var interpretedResult = InterpretInstallResult(package, wingetResult);
        if (interpretedResult.Succeeded)
        {
            return interpretedResult;
        }

        return await RunSpotifyInstallerFallbackAsync(package, interpretedResult, isUpgrade: false, cancellationToken).ConfigureAwait(false);
    }

    private static InstallerOperationResult CreateSkippedInstallResult(InstallerCatalogItem package, InstallerPackageStatus status)
    {
        var message = status.HasUpdateAvailable
            ? "Already installed. Use Update to upgrade."
            : "Already installed. Skipped.";

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            false,
            message,
            status.StatusText);
    }

    private async Task<InstallerOperationResult> UpgradePackageAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        if (IsSpotifyPackage(package))
        {
            return await UpgradeSpotifyAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsAutomatic1111Package(package))
        {
            return await UpgradeAutomatic1111Async(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsOpenWebUiPackage(package))
        {
            return await UpgradeOpenWebUiAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsRyubingRyujinxPackage(package))
        {
            return await UpgradeRyubingRyujinxAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsAzaharPackage(package))
        {
            return await UpgradeAzaharAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsMacriumReflectPackage(package))
        {
            return await UpgradeMacriumReflectAsync(package, cancellationToken).ConfigureAwait(false);
        }

        if (IsRpcs3Package(package))
        {
            return await UpgradeRpcs3Async(package, cancellationToken).ConfigureAwait(false);
        }

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

    private async Task<InstallerOperationResult> UpgradeSpotifyAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var wingetResult = await RunWingetAsync(BuildPackageCommand("upgrade", package), cancellationToken).ConfigureAwait(false);
        var interpretedResult = InterpretUpgradeResult(package, wingetResult);
        if (interpretedResult.Succeeded)
        {
            return interpretedResult;
        }

        return await RunSpotifyInstallerFallbackAsync(package, interpretedResult, isUpgrade: true, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InstallerOperationResult> UninstallPackageAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var result = await RunWingetAsync(BuildPackageCommand("uninstall", package), cancellationToken).ConfigureAwait(false);
        return InterpretUninstallResult(package, result);
    }

    private async Task<InstallerOperationResult> RunSpotifyInstallerFallbackAsync(
        InstallerCatalogItem package,
        InstallerOperationResult wingetFailure,
        bool isUpgrade,
        CancellationToken cancellationToken)
    {
        var workingDirectory = ResolveSpotifyWorkingDirectory();
        Directory.CreateDirectory(workingDirectory);

        var installerPath = Path.Combine(workingDirectory, SpotifyFallbackInstallerFileName);
        var logPath = Path.Combine(workingDirectory, SpotifyFallbackInstallerLogFileName);

        try
        {
            await fileDownloader(SpotifyFallbackInstallerUrl, installerPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Spotify fallback download failed after winget failed: {wingetFailure.Message} Direct installer error: {ex.Message}",
                wingetFailure.Output);
        }

        var installerArguments = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            SpotifyFallbackInstallerArgumentsTemplate,
            QuoteArgument(logPath));

        try
        {
            await installerExecutableLauncher(
                new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = installerArguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true,
                },
                preferUnelevated: true,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Spotify fallback launch failed after winget failed: {wingetFailure.Message} Installer launch error: {ex.Message}",
                wingetFailure.Output);
        }

        var fallbackSucceeded = await WaitForSpotifyFallbackStatusAsync(isUpgrade, cancellationToken).ConfigureAwait(false);
        if (fallbackSucceeded)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                true,
                isUpgrade
                    ? "Updated successfully through Spotify's official installer fallback."
                    : "Installed successfully through Spotify's official installer fallback.",
                $"winget failure: {wingetFailure.Message}{Environment.NewLine}fallback installer: {installerPath}");
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Started Spotify's official installer fallback after winget failed ({wingetFailure.Message}). Spotify may still be finishing the update in the background. If it still shows update ready after a minute, check {logPath}.",
            $"winget failure: {wingetFailure.Message}{Environment.NewLine}fallback installer: {installerPath}{Environment.NewLine}log: {logPath}");
    }

    private async Task<bool> WaitForSpotifyFallbackStatusAsync(bool requireNoUpdateAvailable, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < SpotifyFallbackStatusPollAttempts; attempt++)
        {
            if (attempt > 0)
            {
                await delayAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }

            var status = (await GetPackageStatusesAsync([SpotifyPackageId], cancellationToken).ConfigureAwait(false))
                .FirstOrDefault();

            if (status is null || !status.IsInstalled)
            {
                continue;
            }

            if (!requireNoUpdateAvailable || !status.HasUpdateAvailable)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<InstallerOperationResult> InstallAutomatic1111Async(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = automatic1111InstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Stable Diffusion WebUI is invalid.",
                string.Empty);
        }

        var webUiBatchPath = Path.Combine(installDirectory, "webui-user.bat");
        var webUiCoreBatchPath = Path.Combine(installDirectory, "webui.bat");
        var launcherPath = Path.Combine(installDirectory, Automatic1111LauncherFileName);

        var gitReady = await EnsureExecutableAsync(
            catalogById["Git.Git"],
            package.DisplayName,
            gitExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!gitReady.Succeeded || string.IsNullOrWhiteSpace(gitReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                gitReady.Message,
                gitReady.Output);
        }

        var pythonReady = await EnsureExecutableAsync(
            Automatic1111PythonPackage,
            package.DisplayName,
            python310ExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!pythonReady.Succeeded || string.IsNullOrWhiteSpace(pythonReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                pythonReady.Message,
                pythonReady.Output);
        }

        if (File.Exists(webUiBatchPath) && File.Exists(webUiCoreBatchPath))
        {
            await WriteAutomatic1111LauncherAsync(
                launcherPath,
                installDirectory,
                pythonReady.ExecutablePath,
                gitReady.ExecutablePath,
                cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchAutomatic1111Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        if (Directory.Exists(installDirectory))
        {
            if (Directory.EnumerateFileSystemEntries(installDirectory).Any())
            {
                return new InstallerOperationResult(
                    package.PackageId,
                    package.DisplayName,
                    false,
                    false,
                    $"The install folder already exists and is not empty: {installDirectory}",
                    installDirectory);
            }

            Directory.Delete(installDirectory, recursive: false);
        }

        var parentDirectory = Path.GetDirectoryName(installDirectory);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Stable Diffusion WebUI is invalid.",
                installDirectory);
        }

        Directory.CreateDirectory(parentDirectory);

        var cloneResult = await commandRunner(
            CreateCommandProcessStartInfo(
                gitReady.ExecutablePath,
                $"clone {QuoteArgument(Automatic1111RepositoryUrl)} {QuoteArgument(installDirectory)}"),
            cancellationToken).ConfigureAwait(false);
        var cloneOutput = NormalizeOutput(cloneResult);
        if (cloneResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(cloneOutput, "Git clone failed."),
                cloneOutput);
        }

        await WriteAutomatic1111LauncherAsync(
            launcherPath,
            installDirectory,
            pythonReady.ExecutablePath,
            gitReady.ExecutablePath,
            cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchAutomatic1111Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = $"{cloneOutput}{Environment.NewLine}{launchSetupResult.Output}".Trim(),
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Cloned the official repo to {installDirectory} and launched first-run setup.",
            cloneOutput);
    }

    private async Task<InstallerOperationResult> InstallOpenWebUiAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = openWebUiInstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Open WebUI is invalid.",
                string.Empty);
        }

        var venvDirectory = Path.Combine(installDirectory, "venv");
        var venvPythonPath = Path.Combine(venvDirectory, "Scripts", "python.exe");
        var openWebUiExecutablePath = Path.Combine(venvDirectory, "Scripts", "open-webui.exe");
        var dataDirectory = Path.Combine(installDirectory, "data");
        var launcherPath = Path.Combine(installDirectory, OpenWebUiLauncherFileName);

        var pythonReady = await EnsureExecutableAsync(
            OpenWebUiPythonPackage,
            package.DisplayName,
            python311ExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!pythonReady.Succeeded || string.IsNullOrWhiteSpace(pythonReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                pythonReady.Message,
                pythonReady.Output);
        }

        if (File.Exists(venvPythonPath) && File.Exists(openWebUiExecutablePath))
        {
            Directory.CreateDirectory(dataDirectory);
            await WriteOpenWebUiLauncherAsync(
                launcherPath,
                installDirectory,
                dataDirectory,
                openWebUiExecutablePath,
                cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchOpenWebUiAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        if (Directory.Exists(installDirectory) && Directory.EnumerateFileSystemEntries(installDirectory).Any())
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"The install folder already exists and is not empty: {installDirectory}",
                installDirectory);
        }

        Directory.CreateDirectory(installDirectory);

        var createVenvResult = await commandRunner(
            CreateCommandProcessStartInfo(
                pythonReady.ExecutablePath,
                $"-m venv {QuoteArgument(venvDirectory)}"),
            cancellationToken).ConfigureAwait(false);
        var createVenvOutput = NormalizeOutput(createVenvResult);
        if (createVenvResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(createVenvOutput, "Python could not create the Open WebUI virtual environment."),
                createVenvOutput);
        }

        if (!File.Exists(venvPythonPath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Python created the environment, but the virtual environment executable is missing.",
                venvDirectory);
        }

        var installResult = await commandRunner(
            CreateCommandProcessStartInfo(
                venvPythonPath,
                "-m pip install open-webui"),
            cancellationToken).ConfigureAwait(false);
        var installOutput = NormalizeOutput(installResult);
        if (installResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(installOutput, "Open WebUI could not be installed into the virtual environment."),
                installOutput);
        }

        if (!File.Exists(openWebUiExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Open WebUI installed, but its launcher executable could not be located automatically.",
                venvDirectory);
        }

        Directory.CreateDirectory(dataDirectory);
        await WriteOpenWebUiLauncherAsync(
            launcherPath,
            installDirectory,
            dataDirectory,
            openWebUiExecutablePath,
            cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchOpenWebUiAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = $"{createVenvOutput}{Environment.NewLine}{installOutput}{Environment.NewLine}{launchSetupResult.Output}".Trim(),
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Installed Open WebUI into {installDirectory} and launched the local server.",
            $"{createVenvOutput}{Environment.NewLine}{installOutput}".Trim());
    }

    private async Task<InstallerOperationResult> InstallRyubingRyujinxAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = ryubingRyujinxInstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Ryujinx (Ryubing) is invalid.",
                string.Empty);
        }

        var launcherPath = Path.Combine(installDirectory, RyubingRyujinxLauncherFileName);
        var existingExecutablePath = ResolveRyubingRyujinxExecutablePath(installDirectory);
        if (!string.IsNullOrWhiteSpace(existingExecutablePath))
        {
            await WriteRyubingRyujinxLauncherAsync(launcherPath, existingExecutablePath, cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchRyubingRyujinxAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        if (Directory.Exists(installDirectory) && Directory.EnumerateFileSystemEntries(installDirectory).Any())
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"The install folder already exists and is not empty: {installDirectory}",
                installDirectory);
        }

        Directory.CreateDirectory(installDirectory);

        InstallerReleaseAsset? releaseAsset;
        try
        {
            releaseAsset = await ryubingRyujinxReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Ryujinx (Ryubing) release lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest Ryujinx (Ryubing) Windows build could not be resolved automatically.",
                string.Empty);
        }

        var downloadsDirectory = Path.Combine(installDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var archivePath = Path.Combine(downloadsDirectory, releaseAsset.FileName);

        try
        {
            await fileDownloader(releaseAsset.DownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Ryujinx (Ryubing) download failed: {ex.Message}",
                ex.ToString());
        }

        try
        {
            ZipFile.ExtractToDirectory(archivePath, installDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Ryujinx (Ryubing) could not be extracted: {ex.Message}",
                ex.ToString());
        }

        var executablePath = ResolveRyubingRyujinxExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Ryujinx (Ryubing) was extracted, but Ryujinx.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);
        await WriteRyubingRyujinxLauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchRyubingRyujinxAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = archivePath,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Installed Ryujinx (Ryubing) into {installDirectory} and launched it.",
            archivePath);
    }

    private async Task<InstallerOperationResult> InstallAzaharAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = azaharInstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Lime3DS (Azahar) is invalid.",
                string.Empty);
        }

        var launcherPath = Path.Combine(installDirectory, AzaharLauncherFileName);
        var existingExecutablePath = ResolveAzaharExecutablePath(installDirectory);
        if (!string.IsNullOrWhiteSpace(existingExecutablePath))
        {
            await WriteAzaharLauncherAsync(launcherPath, existingExecutablePath, cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchAzaharAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        if (Directory.Exists(installDirectory) && Directory.EnumerateFileSystemEntries(installDirectory).Any())
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"The install folder already exists and is not empty: {installDirectory}",
                installDirectory);
        }

        Directory.CreateDirectory(installDirectory);

        InstallerReleaseAsset? releaseAsset;
        try
        {
            releaseAsset = await azaharReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Lime3DS (Azahar) release lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest Lime3DS (Azahar) Windows build could not be resolved automatically.",
                string.Empty);
        }

        var downloadsDirectory = Path.Combine(installDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var archivePath = Path.Combine(downloadsDirectory, releaseAsset.FileName);

        try
        {
            await fileDownloader(releaseAsset.DownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Lime3DS (Azahar) download failed: {ex.Message}",
                ex.ToString());
        }

        try
        {
            ZipFile.ExtractToDirectory(archivePath, installDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Lime3DS (Azahar) could not be extracted: {ex.Message}",
                ex.ToString());
        }

        var executablePath = ResolveAzaharExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Lime3DS (Azahar) was extracted, but azahar.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);
        await WriteAzaharLauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchAzaharAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = archivePath,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Installed Lime3DS (Azahar) into {installDirectory} and launched it.",
            archivePath);
    }

    private async Task<InstallerOperationResult> InstallMacriumReflectAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var workingDirectory = macriumReflectWorkingDirectoryResolver();
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The working directory for Macrium Reflect is invalid.",
                string.Empty);
        }

        var launcherPath = Path.Combine(workingDirectory, MacriumReflectLauncherFileName);
        var installedExecutablePath = macriumReflectExecutableResolver();
        if (!string.IsNullOrWhiteSpace(installedExecutablePath))
        {
            await WriteMacriumReflectLauncherAsync(launcherPath, installedExecutablePath, cancellationToken).ConfigureAwait(false);
            var existingLaunchResult = await LaunchMacriumReflectAsync(launcherPath, workingDirectory, cancellationToken).ConfigureAwait(false);
            return existingLaunchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        Directory.CreateDirectory(workingDirectory);

        InstallerReleaseAsset? installerAsset;
        try
        {
            installerAsset = await macriumReflectReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Macrium Reflect installer lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (installerAsset is null || string.IsNullOrWhiteSpace(installerAsset.FileName) || string.IsNullOrWhiteSpace(installerAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest Macrium Reflect installer could not be resolved automatically.",
                string.Empty);
        }

        var downloadsDirectory = Path.Combine(workingDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var installerPath = Path.Combine(downloadsDirectory, installerAsset.FileName);

        try
        {
            await fileDownloader(installerAsset.DownloadUrl, installerPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Macrium Reflect download failed: {ex.Message}",
                ex.ToString());
        }

        await WriteMacriumReflectLauncherAsync(launcherPath, installerPath, cancellationToken).ConfigureAwait(false);

        var launchResult = await LaunchMacriumReflectAsync(launcherPath, workingDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = installerPath,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Downloaded the latest Macrium Reflect installer to {installerPath} and launched it.",
            installerPath);
    }

    private async Task<InstallerOperationResult> InstallRpcs3Async(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = rpcs3InstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for RPCS3 is invalid.",
                string.Empty);
        }

        var launcherPath = Path.Combine(installDirectory, Rpcs3LauncherFileName);
        var existingExecutablePath = ResolveRpcs3ExecutablePath(installDirectory);
        if (!string.IsNullOrWhiteSpace(existingExecutablePath))
        {
            await WriteRpcs3LauncherAsync(launcherPath, existingExecutablePath, cancellationToken).ConfigureAwait(false);
            var launchResult = await LaunchRpcs3Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = false,
            };
        }

        var visualCppReady = await EnsurePackageInstalledAsync(
            Rpcs3VisualCppPackage,
            package.DisplayName,
            cancellationToken).ConfigureAwait(false);
        if (!visualCppReady.Succeeded)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                visualCppReady.Message,
                visualCppReady.Output);
        }

        var sevenZipReady = await EnsureExecutableAsync(
            SevenZipPackage,
            package.DisplayName,
            sevenZipExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!sevenZipReady.Succeeded || string.IsNullOrWhiteSpace(sevenZipReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                sevenZipReady.Message,
                sevenZipReady.Output);
        }

        if (Directory.Exists(installDirectory) && Directory.EnumerateFileSystemEntries(installDirectory).Any())
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"The install folder already exists and is not empty: {installDirectory}",
                installDirectory);
        }

        Directory.CreateDirectory(installDirectory);

        InstallerReleaseAsset? releaseAsset;
        try
        {
            releaseAsset = await rpcs3ReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"RPCS3 release lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest RPCS3 Windows build could not be resolved automatically.",
                string.Empty);
        }

        var downloadsDirectory = Path.Combine(installDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var archivePath = Path.Combine(downloadsDirectory, releaseAsset.FileName);

        try
        {
            await fileDownloader(releaseAsset.DownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"RPCS3 download failed: {ex.Message}",
                ex.ToString());
        }

        var extractResult = await commandRunner(
            CreateCommandProcessStartInfo(
                sevenZipReady.ExecutablePath,
                $"x -y {QuoteArgument(archivePath)} -o{QuoteArgument(installDirectory)}"),
            cancellationToken).ConfigureAwait(false);
        var extractOutput = NormalizeOutput(extractResult);
        if (extractResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(extractOutput, "RPCS3 could not be extracted."),
                extractOutput);
        }

        var executablePath = ResolveRpcs3ExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "RPCS3 was extracted, but rpcs3.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);
        await WriteRpcs3LauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);

        var launchSetupResult = await LaunchRpcs3Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchSetupResult.Succeeded)
        {
            return launchSetupResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = extractOutput,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Installed RPCS3 into {installDirectory} and launched it.",
            extractOutput);
    }

    private async Task<InstallerOperationResult> UpgradeAutomatic1111Async(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = automatic1111InstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Stable Diffusion WebUI is invalid.",
                string.Empty);
        }

        var webUiBatchPath = Path.Combine(installDirectory, "webui-user.bat");
        var webUiCoreBatchPath = Path.Combine(installDirectory, "webui.bat");
        if (!File.Exists(webUiBatchPath) || !File.Exists(webUiCoreBatchPath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
        }

        var gitReady = await EnsureExecutableAsync(
            catalogById["Git.Git"],
            package.DisplayName,
            gitExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!gitReady.Succeeded || string.IsNullOrWhiteSpace(gitReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                gitReady.Message,
                gitReady.Output);
        }

        var pythonReady = await EnsureExecutableAsync(
            Automatic1111PythonPackage,
            package.DisplayName,
            python310ExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!pythonReady.Succeeded || string.IsNullOrWhiteSpace(pythonReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                pythonReady.Message,
                pythonReady.Output);
        }

        var launcherPath = Path.Combine(installDirectory, Automatic1111LauncherFileName);
        await WriteAutomatic1111LauncherAsync(
            launcherPath,
            installDirectory,
            pythonReady.ExecutablePath,
            gitReady.ExecutablePath,
            cancellationToken).ConfigureAwait(false);

        var pullResult = await commandRunner(
            CreateCommandProcessStartInfo(
                gitReady.ExecutablePath,
                $"-C {QuoteArgument(installDirectory)} pull"),
            cancellationToken).ConfigureAwait(false);
        var pullOutput = NormalizeOutput(pullResult);
        if (pullResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(pullOutput, "Stable Diffusion WebUI could not be updated."),
                pullOutput);
        }

        if (Contains(pullOutput, "Already up to date.") || Contains(pullOutput, "Already up to date"))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                pullOutput);
        }

        var launchResult = await LaunchAutomatic1111Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = $"{pullOutput}{Environment.NewLine}{launchResult.Output}".Trim(),
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Pulled the latest Stable Diffusion WebUI changes in {installDirectory} and launched it.",
            $"{pullOutput}{Environment.NewLine}{launchResult.Output}".Trim());
    }

    private async Task<InstallerOperationResult> UpgradeOpenWebUiAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = openWebUiInstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Open WebUI is invalid.",
                string.Empty);
        }

        var venvDirectory = Path.Combine(installDirectory, "venv");
        var venvPythonPath = Path.Combine(venvDirectory, "Scripts", "python.exe");
        var openWebUiExecutablePath = Path.Combine(venvDirectory, "Scripts", "open-webui.exe");
        if (!File.Exists(venvPythonPath) || !File.Exists(openWebUiExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
        }

        var installResult = await commandRunner(
            CreateCommandProcessStartInfo(
                venvPythonPath,
                "-m pip install -U open-webui"),
            cancellationToken).ConfigureAwait(false);
        var installOutput = NormalizeOutput(installResult);
        if (installResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(installOutput, "Open WebUI could not be updated."),
                installOutput);
        }

        if (!File.Exists(openWebUiExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Open WebUI updated, but its launcher executable could not be located automatically.",
                venvDirectory);
        }

        var dataDirectory = Path.Combine(installDirectory, "data");
        Directory.CreateDirectory(dataDirectory);
        var launcherPath = Path.Combine(installDirectory, OpenWebUiLauncherFileName);
        await WriteOpenWebUiLauncherAsync(
            launcherPath,
            installDirectory,
            dataDirectory,
            openWebUiExecutablePath,
            cancellationToken).ConfigureAwait(false);

        if (Contains(installOutput, "Requirement already satisfied"))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                installOutput);
        }

        var launchResult = await LaunchOpenWebUiAsync(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = $"{installOutput}{Environment.NewLine}{launchResult.Output}".Trim(),
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Updated Open WebUI in {installDirectory} and launched the local server.",
            $"{installOutput}{Environment.NewLine}{launchResult.Output}".Trim());
    }

    private async Task<InstallerOperationResult> UpgradeRyubingRyujinxAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = ryubingRyujinxInstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Ryujinx (Ryubing) is invalid.",
                string.Empty);
        }

        if (string.IsNullOrWhiteSpace(ResolveRyubingRyujinxExecutablePath(installDirectory)))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
        }

        InstallerReleaseAsset? releaseAsset;
        try
        {
            releaseAsset = await ryubingRyujinxReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Ryujinx (Ryubing) release lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest Ryujinx (Ryubing) Windows build could not be resolved automatically.",
                string.Empty);
        }

        var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
        if (string.Equals(ReadInstalledReleaseMarker(installDirectory), releaseMarker, StringComparison.OrdinalIgnoreCase))
        {
            var launcherPath = Path.Combine(installDirectory, RyubingRyujinxLauncherFileName);
            var executablePath = ResolveRyubingRyujinxExecutablePath(installDirectory);
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                await WriteRyubingRyujinxLauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);
            }

            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                releaseMarker);
        }

        var downloadsDirectory = Path.Combine(installDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var archivePath = Path.Combine(downloadsDirectory, releaseAsset.FileName);

        try
        {
            await fileDownloader(releaseAsset.DownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Ryujinx (Ryubing) download failed: {ex.Message}",
                ex.ToString());
        }

        try
        {
            ZipFile.ExtractToDirectory(archivePath, installDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Ryujinx (Ryubing) could not be extracted: {ex.Message}",
                ex.ToString());
        }

        var updatedExecutablePath = ResolveRyubingRyujinxExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(updatedExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Ryujinx (Ryubing) was updated, but Ryujinx.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);

        var updatedLauncherPath = Path.Combine(installDirectory, RyubingRyujinxLauncherFileName);
        await WriteRyubingRyujinxLauncherAsync(updatedLauncherPath, updatedExecutablePath, cancellationToken).ConfigureAwait(false);

        var launchResult = await LaunchRyubingRyujinxAsync(updatedLauncherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = archivePath,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Updated Ryujinx (Ryubing) in {installDirectory} and launched it.",
            archivePath);
    }

    private async Task<InstallerOperationResult> UpgradeAzaharAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = azaharInstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for Lime3DS (Azahar) is invalid.",
                string.Empty);
        }

        if (string.IsNullOrWhiteSpace(ResolveAzaharExecutablePath(installDirectory)))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
        }

        InstallerReleaseAsset? releaseAsset;
        try
        {
            releaseAsset = await azaharReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Lime3DS (Azahar) release lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest Lime3DS (Azahar) Windows build could not be resolved automatically.",
                string.Empty);
        }

        var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
        if (string.Equals(ReadInstalledReleaseMarker(installDirectory), releaseMarker, StringComparison.OrdinalIgnoreCase))
        {
            var launcherPath = Path.Combine(installDirectory, AzaharLauncherFileName);
            var executablePath = ResolveAzaharExecutablePath(installDirectory);
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                await WriteAzaharLauncherAsync(launcherPath, executablePath, cancellationToken).ConfigureAwait(false);
            }

            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                releaseMarker);
        }

        var downloadsDirectory = Path.Combine(installDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var archivePath = Path.Combine(downloadsDirectory, releaseAsset.FileName);

        try
        {
            await fileDownloader(releaseAsset.DownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Lime3DS (Azahar) download failed: {ex.Message}",
                ex.ToString());
        }

        try
        {
            ZipFile.ExtractToDirectory(archivePath, installDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Lime3DS (Azahar) could not be extracted: {ex.Message}",
                ex.ToString());
        }

        var updatedExecutablePath = ResolveAzaharExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(updatedExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "Lime3DS (Azahar) was updated, but azahar.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);

        var updatedLauncherPath = Path.Combine(installDirectory, AzaharLauncherFileName);
        await WriteAzaharLauncherAsync(updatedLauncherPath, updatedExecutablePath, cancellationToken).ConfigureAwait(false);

        var launchResult = await LaunchAzaharAsync(updatedLauncherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = archivePath,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Updated Lime3DS (Azahar) in {installDirectory} and launched it.",
            archivePath);
    }

    private async Task<InstallerOperationResult> UpgradeMacriumReflectAsync(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var workingDirectory = macriumReflectWorkingDirectoryResolver();
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The working directory for Macrium Reflect is invalid.",
                string.Empty);
        }

        if (string.IsNullOrWhiteSpace(macriumReflectExecutableResolver()))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                workingDirectory);
        }

        Directory.CreateDirectory(workingDirectory);

        InstallerReleaseAsset? installerAsset;
        try
        {
            installerAsset = await macriumReflectReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Macrium Reflect installer lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (installerAsset is null || string.IsNullOrWhiteSpace(installerAsset.FileName) || string.IsNullOrWhiteSpace(installerAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest Macrium Reflect installer could not be resolved automatically.",
                string.Empty);
        }

        var releaseMarker = BuildInstalledReleaseMarkerValue(installerAsset);
        if (string.Equals(ReadInstalledReleaseMarker(workingDirectory), releaseMarker, StringComparison.OrdinalIgnoreCase))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                releaseMarker);
        }

        var downloadsDirectory = Path.Combine(workingDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var installerPath = Path.Combine(downloadsDirectory, installerAsset.FileName);

        try
        {
            await fileDownloader(installerAsset.DownloadUrl, installerPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"Macrium Reflect download failed: {ex.Message}",
                ex.ToString());
        }

        var installResult = await RunMacriumReflectInstallerSilentlyAsync(installerPath, workingDirectory, cancellationToken).ConfigureAwait(false);
        if (!installResult.Succeeded)
        {
            return installResult;
        }

        await WriteInstalledReleaseMarkerAsync(workingDirectory, installerAsset, cancellationToken).ConfigureAwait(false);
        return installResult;
    }

    private async Task<InstallerOperationResult> UpgradeRpcs3Async(InstallerCatalogItem package, CancellationToken cancellationToken)
    {
        var installDirectory = rpcs3InstallDirectoryResolver();
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The install directory for RPCS3 is invalid.",
                string.Empty);
        }

        if (string.IsNullOrWhiteSpace(ResolveRpcs3ExecutablePath(installDirectory)))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "This app is not installed yet.",
                installDirectory);
        }

        var sevenZipReady = await EnsureExecutableAsync(
            SevenZipPackage,
            package.DisplayName,
            sevenZipExecutableResolver,
            cancellationToken).ConfigureAwait(false);
        if (!sevenZipReady.Succeeded || string.IsNullOrWhiteSpace(sevenZipReady.ExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                sevenZipReady.Message,
                sevenZipReady.Output);
        }

        InstallerReleaseAsset? releaseAsset;
        try
        {
            releaseAsset = await rpcs3ReleaseResolver(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"RPCS3 release lookup failed: {ex.Message}",
                ex.ToString());
        }

        if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "The latest RPCS3 Windows build could not be resolved automatically.",
                string.Empty);
        }

        var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
        if (string.Equals(ReadInstalledReleaseMarker(installDirectory), releaseMarker, StringComparison.OrdinalIgnoreCase))
        {
            var existingLauncherPath = Path.Combine(installDirectory, Rpcs3LauncherFileName);
            var executablePath = ResolveRpcs3ExecutablePath(installDirectory);
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                await WriteRpcs3LauncherAsync(existingLauncherPath, executablePath, cancellationToken).ConfigureAwait(false);
            }

            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                true,
                false,
                "Already up to date.",
                releaseMarker);
        }

        var downloadsDirectory = Path.Combine(installDirectory, "downloads");
        Directory.CreateDirectory(downloadsDirectory);
        var archivePath = Path.Combine(downloadsDirectory, releaseAsset.FileName);

        try
        {
            await fileDownloader(releaseAsset.DownloadUrl, archivePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                $"RPCS3 download failed: {ex.Message}",
                ex.ToString());
        }

        var extractResult = await commandRunner(
            CreateCommandProcessStartInfo(
                sevenZipReady.ExecutablePath,
                $"x -y {QuoteArgument(archivePath)} -o{QuoteArgument(installDirectory)}"),
            cancellationToken).ConfigureAwait(false);
        var extractOutput = NormalizeOutput(extractResult);
        if (extractResult.ExitCode != 0)
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                SummarizeFailure(extractOutput, "RPCS3 could not be extracted."),
                extractOutput);
        }

        var updatedExecutablePath = ResolveRpcs3ExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(updatedExecutablePath))
        {
            return new InstallerOperationResult(
                package.PackageId,
                package.DisplayName,
                false,
                false,
                "RPCS3 was updated, but rpcs3.exe could not be located automatically.",
                installDirectory);
        }

        await WriteInstalledReleaseMarkerAsync(installDirectory, releaseAsset, cancellationToken).ConfigureAwait(false);

        var launcherPath = Path.Combine(installDirectory, Rpcs3LauncherFileName);
        await WriteRpcs3LauncherAsync(launcherPath, updatedExecutablePath, cancellationToken).ConfigureAwait(false);

        var launchResult = await LaunchRpcs3Async(launcherPath, installDirectory, cancellationToken).ConfigureAwait(false);
        if (!launchResult.Succeeded)
        {
            return launchResult with
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                Changed = true,
                Output = extractOutput,
            };
        }

        return new InstallerOperationResult(
            package.PackageId,
            package.DisplayName,
            true,
            true,
            $"Updated RPCS3 in {installDirectory} and launched it.",
            extractOutput);
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

        var failureSummary = TrySummarizeFailure(output);
        if (!string.IsNullOrWhiteSpace(failureSummary))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, failureSummary, output);
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

        var failureSummary = TrySummarizeFailure(output);
        if (!string.IsNullOrWhiteSpace(failureSummary))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, failureSummary, output);
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

        var failureSummary = TrySummarizeFailure(output);
        if (!string.IsNullOrWhiteSpace(failureSummary))
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, failureSummary, output);
        }

        if (result.ExitCode == 0)
        {
            return new InstallerOperationResult(package.PackageId, package.DisplayName, true, true, "Removal completed.", output);
        }

        return new InstallerOperationResult(package.PackageId, package.DisplayName, false, false, SummarizeFailure(output, "Removal failed."), output);
    }

    private HashSet<string> FindPackageIdsInOutput(string output, IReadOnlyList<string> packageIds)
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
                if (MatchesWingetOutputLine(line, packageId))
                {
                    matches.Add(packageId);
                }
            }
        }

        return matches;
    }

    private bool MatchesWingetOutputLine(string line, string packageId)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (line.Contains(packageId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var matchToken in GetWingetOutputMatchTokens(packageId))
        {
            if (StartsWithWingetNameToken(line, matchToken))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<string> GetWingetOutputMatchTokens(string packageId)
    {
        if (catalogById.TryGetValue(packageId, out var package) && !string.IsNullOrWhiteSpace(package.DisplayName))
        {
            yield return package.DisplayName;
        }

        if (WingetOutputAliases.TryGetValue(packageId, out var aliases))
        {
            foreach (var alias in aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    yield return alias;
                }
            }
        }
    }

    private static bool StartsWithWingetNameToken(string line, string token)
    {
        if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var trimmedLine = line.TrimStart();
        if (!trimmedLine.StartsWith(token, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trimmedLine.Length == token.Length)
        {
            return true;
        }

        var nextCharacter = trimmedLine[token.Length];
        return char.IsWhiteSpace(nextCharacter)
            || nextCharacter is '(' or '[' or '{' or '-' or '_' or '.';
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

    private async Task<IReadOnlyList<string>> ExpandPackageIdsForInstallAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken)
    {
        var explicitPackageIds = NormalizePackageIds(packageIds);
        var expandedIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageId in explicitPackageIds)
        {
            ExpandPackageWithDependencies(packageId, expandedIds, seen);
        }

        var explicitPackageSet = explicitPackageIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var autoIncludedDependencyIds = expandedIds
            .Where(packageId => !explicitPackageSet.Contains(packageId))
            .ToArray();
        if (autoIncludedDependencyIds.Length == 0)
        {
            return expandedIds;
        }

        var installedDependencyIds = (await GetPackageStatusesAsync(autoIncludedDependencyIds, cancellationToken).ConfigureAwait(false))
            .Where(static status => status.IsInstalled)
            .Select(static status => status.PackageId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return
        [
            .. expandedIds.Where(packageId => explicitPackageSet.Contains(packageId) || !installedDependencyIds.Contains(packageId)),
        ];
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

    private async Task<InstallerPackageStatus?> GetCustomPackageStatusAsync(string packageId, CancellationToken cancellationToken)
    {
        if (string.Equals(packageId, Automatic1111PackageId, StringComparison.OrdinalIgnoreCase))
        {
            return await GetAutomatic1111PackageStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(packageId, OpenWebUiPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return await GetOpenWebUiPackageStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(packageId, RyubingRyujinxPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return await GetRyubingRyujinxPackageStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(packageId, AzaharPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return await GetAzaharPackageStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(packageId, MacriumReflectPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return await GetMacriumReflectPackageStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(packageId, Rpcs3PackageId, StringComparison.OrdinalIgnoreCase))
        {
            return await GetRpcs3PackageStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        return TryGetStaticCustomPackageStatus(packageId, out var status)
            ? status
            : null;
    }

    private async Task<InstallerPackageStatus> GetAutomatic1111PackageStatusAsync(CancellationToken cancellationToken)
    {
        var installDirectory = automatic1111InstallDirectoryResolver();
        var isInstalled =
            !string.IsNullOrWhiteSpace(installDirectory) &&
            File.Exists(Path.Combine(installDirectory, "webui-user.bat")) &&
            File.Exists(Path.Combine(installDirectory, "webui.bat"));

        if (!isInstalled)
        {
            return new InstallerPackageStatus(Automatic1111PackageId, false, false, "Not installed");
        }

        var gitExecutablePath = gitExecutableResolver();
        var gitDirectory = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, ".git");
        if (string.IsNullOrWhiteSpace(gitExecutablePath) || string.IsNullOrWhiteSpace(gitDirectory) || !Directory.Exists(gitDirectory))
        {
            return new InstallerPackageStatus(Automatic1111PackageId, true, false, "Installed (custom)");
        }

        try
        {
            var fetchResult = await commandRunner(
                CreateCommandProcessStartInfo(
                    gitExecutablePath,
                    $"-C {QuoteArgument(installDirectory)} fetch origin --quiet"),
                cancellationToken).ConfigureAwait(false);
            if (fetchResult.ExitCode != 0)
            {
                return new InstallerPackageStatus(Automatic1111PackageId, true, false, "Installed (custom)");
            }

            var behindCountResult = await commandRunner(
                CreateCommandProcessStartInfo(
                    gitExecutablePath,
                    $"-C {QuoteArgument(installDirectory)} rev-list --count HEAD..@{{upstream}}"),
                cancellationToken).ConfigureAwait(false);
            if (behindCountResult.ExitCode != 0)
            {
                return new InstallerPackageStatus(Automatic1111PackageId, true, false, "Installed (custom)");
            }

            var behindCountOutput = NormalizeOutput(behindCountResult);
            var firstLine = behindCountOutput
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            var hasUpdateAvailable = int.TryParse(firstLine, out var behindCount) && behindCount > 0;

            return new InstallerPackageStatus(
                Automatic1111PackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : "Installed (custom)");
        }
        catch
        {
            return new InstallerPackageStatus(Automatic1111PackageId, true, false, "Installed (custom)");
        }
    }

    private async Task<InstallerPackageStatus> GetOpenWebUiPackageStatusAsync(CancellationToken cancellationToken)
    {
        var installDirectory = openWebUiInstallDirectoryResolver();
        var venvPythonPath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, "venv", "Scripts", "python.exe");
        var openWebUiExecutablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, "venv", "Scripts", "open-webui.exe");
        var launcherPath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, OpenWebUiLauncherFileName);
        var isInstalled =
            !string.IsNullOrWhiteSpace(installDirectory) &&
            !string.IsNullOrWhiteSpace(venvPythonPath) &&
            !string.IsNullOrWhiteSpace(openWebUiExecutablePath) &&
            !string.IsNullOrWhiteSpace(launcherPath) &&
            File.Exists(venvPythonPath) &&
            File.Exists(openWebUiExecutablePath) &&
            File.Exists(launcherPath);

        if (!isInstalled)
        {
            return new InstallerPackageStatus(OpenWebUiPackageId, false, false, "Not installed");
        }

        var pythonExecutablePath = venvPythonPath!;

        try
        {
            var outdatedResult = await commandRunner(
                CreateCommandProcessStartInfo(
                    pythonExecutablePath,
                    "-m pip list --outdated --format=json"),
                cancellationToken).ConfigureAwait(false);
            if (outdatedResult.ExitCode != 0)
            {
                return new InstallerPackageStatus(OpenWebUiPackageId, true, false, "Installed (custom)");
            }

            var outdatedOutput = NormalizeOutput(outdatedResult);
            var hasUpdateAvailable = PipOutdatedOutputIncludesPackage(outdatedOutput, "open-webui");
            return new InstallerPackageStatus(
                OpenWebUiPackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : "Installed (custom)");
        }
        catch
        {
            return new InstallerPackageStatus(OpenWebUiPackageId, true, false, "Installed (custom)");
        }
    }

    private async Task<InstallerPackageStatus> GetRyubingRyujinxPackageStatusAsync(CancellationToken cancellationToken)
    {
        var installDirectory = ryubingRyujinxInstallDirectoryResolver();
        var executablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : ResolveRyubingRyujinxExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerPackageStatus(RyubingRyujinxPackageId, false, false, "Not installed");
        }

        try
        {
            var releaseAsset = await ryubingRyujinxReleaseResolver(cancellationToken).ConfigureAwait(false);
            if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
            {
                return new InstallerPackageStatus(RyubingRyujinxPackageId, true, false, "Installed (custom)");
            }

            var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
            var installedReleaseMarker = ReadInstalledReleaseMarker(installDirectory);
            var hasUpdateAvailable =
                !string.IsNullOrWhiteSpace(installedReleaseMarker) &&
                !string.Equals(installedReleaseMarker, releaseMarker, StringComparison.OrdinalIgnoreCase);

            return new InstallerPackageStatus(
                RyubingRyujinxPackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : "Installed (custom)");
        }
        catch
        {
            return new InstallerPackageStatus(RyubingRyujinxPackageId, true, false, "Installed (custom)");
        }
    }

    private async Task<InstallerPackageStatus> GetAzaharPackageStatusAsync(CancellationToken cancellationToken)
    {
        var installDirectory = azaharInstallDirectoryResolver();
        var executablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : ResolveAzaharExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerPackageStatus(AzaharPackageId, false, false, "Not installed");
        }

        try
        {
            var releaseAsset = await azaharReleaseResolver(cancellationToken).ConfigureAwait(false);
            if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
            {
                return new InstallerPackageStatus(AzaharPackageId, true, false, "Installed (custom)");
            }

            var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
            var installedReleaseMarker = ReadInstalledReleaseMarker(installDirectory);
            var hasUpdateAvailable =
                !string.IsNullOrWhiteSpace(installedReleaseMarker) &&
                !string.Equals(installedReleaseMarker, releaseMarker, StringComparison.OrdinalIgnoreCase);

            return new InstallerPackageStatus(
                AzaharPackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : "Installed (custom)");
        }
        catch
        {
            return new InstallerPackageStatus(AzaharPackageId, true, false, "Installed (custom)");
        }
    }

    private async Task<InstallerPackageStatus> GetMacriumReflectPackageStatusAsync(CancellationToken cancellationToken)
    {
        var executablePath = macriumReflectExecutableResolver();
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerPackageStatus(MacriumReflectPackageId, false, false, "Not installed");
        }

        var workingDirectory = macriumReflectWorkingDirectoryResolver();
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return new InstallerPackageStatus(MacriumReflectPackageId, true, false, "Installed (custom)");
        }

        try
        {
            var releaseAsset = await macriumReflectReleaseResolver(cancellationToken).ConfigureAwait(false);
            if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
            {
                return new InstallerPackageStatus(MacriumReflectPackageId, true, false, "Installed (custom)");
            }

            var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
            var installedReleaseMarker = ReadInstalledReleaseMarker(workingDirectory);
            var hasUpdateAvailable =
                !string.IsNullOrWhiteSpace(installedReleaseMarker) &&
                !string.Equals(installedReleaseMarker, releaseMarker, StringComparison.OrdinalIgnoreCase);

            return new InstallerPackageStatus(
                MacriumReflectPackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : "Installed (custom)");
        }
        catch
        {
            return new InstallerPackageStatus(MacriumReflectPackageId, true, false, "Installed (custom)");
        }
    }

    private async Task<InstallerPackageStatus> GetRpcs3PackageStatusAsync(CancellationToken cancellationToken)
    {
        var installDirectory = rpcs3InstallDirectoryResolver();
        var executablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : ResolveRpcs3ExecutablePath(installDirectory);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return new InstallerPackageStatus(Rpcs3PackageId, false, false, "Not installed");
        }

        try
        {
            var releaseAsset = await rpcs3ReleaseResolver(cancellationToken).ConfigureAwait(false);
            if (releaseAsset is null || string.IsNullOrWhiteSpace(releaseAsset.FileName) || string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl))
            {
                return new InstallerPackageStatus(Rpcs3PackageId, true, false, "Installed (custom)");
            }

            var releaseMarker = BuildInstalledReleaseMarkerValue(releaseAsset);
            var installedReleaseMarker = ReadInstalledReleaseMarker(installDirectory);
            var hasUpdateAvailable =
                !string.IsNullOrWhiteSpace(installedReleaseMarker) &&
                !string.Equals(installedReleaseMarker, releaseMarker, StringComparison.OrdinalIgnoreCase);

            return new InstallerPackageStatus(
                Rpcs3PackageId,
                true,
                hasUpdateAvailable,
                hasUpdateAvailable ? "Update available" : "Installed (custom)");
        }
        catch
        {
            return new InstallerPackageStatus(Rpcs3PackageId, true, false, "Installed (custom)");
        }
    }

    private bool TryGetStaticCustomPackageStatus(string packageId, out InstallerPackageStatus status)
    {
        status = new InstallerPackageStatus(packageId, false, false, "Status unavailable");
        return false;
    }

    private void ApplyLocalFallbackStatuses(
        IDictionary<string, InstallerPackageStatus> statusesById,
        IEnumerable<string> packageIds)
    {
        foreach (var packageId in packageIds)
        {
            if (statusesById.TryGetValue(packageId, out var status) && status.IsInstalled)
            {
                continue;
            }

            var fallbackStatus = localPackageStatusResolver(packageId);
            if (fallbackStatus is { IsInstalled: true })
            {
                statusesById[packageId] = fallbackStatus;
            }
        }
    }

    private InstallerPackageStatus? TryGetLocalPackageStatus(string packageId)
    {
        if (string.Equals(packageId, FirefoxPackageId, StringComparison.OrdinalIgnoreCase))
        {
            var firefoxStatus = GetFirefoxPackageFallbackStatus();
            return firefoxStatus.IsInstalled ? firefoxStatus : null;
        }

        if (string.Equals(packageId, GitPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(gitExecutableResolver()));
        }

        if (string.Equals(packageId, PowerShellPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolvePowerShellExecutablePath()));
        }

        if (string.Equals(packageId, DiscordPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveDiscordExecutablePath()));
        }

        if (string.Equals(packageId, VencordPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveVencordInstallDirectory()));
        }

        if (string.Equals(packageId, QbittorrentPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveQbittorrentExecutablePath()));
        }

        if (string.Equals(packageId, EverythingPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveEverythingExecutablePath()));
        }

        if (string.Equals(packageId, TorBrowserPackageId, StringComparison.OrdinalIgnoreCase))
        {
            return CreateLocalDetectedPackageStatus(packageId, !string.IsNullOrWhiteSpace(ResolveTorBrowserExecutablePath()));
        }

        return null;
    }

    private static InstallerPackageStatus? CreateLocalDetectedPackageStatus(string packageId, bool isInstalled) =>
        isInstalled
            ? new InstallerPackageStatus(packageId, true, false, "Installed (detected locally)")
            : null;

    private InstallerPackageStatus GetFirefoxPackageFallbackStatus()
    {
        var installDirectory = firefoxInstallDirectoryResolver();
        var firefoxExecutablePath = string.IsNullOrWhiteSpace(installDirectory)
            ? null
            : Path.Combine(installDirectory, "firefox.exe");
        var isInstalled =
            !string.IsNullOrWhiteSpace(firefoxExecutablePath) &&
            File.Exists(firefoxExecutablePath);

        return isInstalled
            ? new InstallerPackageStatus(FirefoxPackageId, true, false, "Installed (detected locally)")
            : new InstallerPackageStatus(FirefoxPackageId, false, false, "Not installed");
    }

    private static bool PipOutdatedOutputIncludesPackage(string output, string packageName)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(output);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var nameElement))
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.Equals(name, packageName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private async Task<(bool Succeeded, string Message, string Output)> EnsurePackageInstalledAsync(
        InstallerCatalogItem package,
        string dependentDisplayName,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingStatus = (await GetPackageStatusesAsync([package.PackageId], cancellationToken).ConfigureAwait(false))
                .FirstOrDefault();
            if (existingStatus?.IsInstalled == true)
            {
                return (true, $"{package.DisplayName} is already installed.", existingStatus.StatusText);
            }

            var installResult = await RunWingetAsync(BuildPackageCommand("install", package), cancellationToken).ConfigureAwait(false);
            var interpretedResult = InterpretInstallResult(package, installResult);
            return interpretedResult.Succeeded
                ? (true, $"{package.DisplayName} is ready.", interpretedResult.Output)
                : (false, $"{package.DisplayName} is required before {dependentDisplayName} can be installed. {interpretedResult.Message}", interpretedResult.Output);
        }
        catch (Win32Exception)
        {
            return (false, $"{package.DisplayName} is required before {dependentDisplayName} can be installed, and winget is unavailable to install it automatically.", string.Empty);
        }
    }

    private async Task<(bool Succeeded, string? ExecutablePath, string Message, string Output)> EnsureExecutableAsync(
        InstallerCatalogItem package,
        string dependentDisplayName,
        Func<string?> executableResolver,
        CancellationToken cancellationToken)
    {
        var executablePath = executableResolver();
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            return (true, executablePath, $"{package.DisplayName} is ready.", executablePath);
        }

        try
        {
            var installResult = await RunWingetAsync(BuildPackageCommand("install", package), cancellationToken).ConfigureAwait(false);
            var interpretedResult = InterpretInstallResult(package, installResult);
            if (!interpretedResult.Succeeded)
            {
                return (false, null, $"{package.DisplayName} is required before {dependentDisplayName} can be installed. {interpretedResult.Message}", interpretedResult.Output);
            }
        }
        catch (Win32Exception)
        {
            return (false, null, $"{package.DisplayName} is required before {dependentDisplayName} can be installed, and winget is unavailable to install it automatically.", string.Empty);
        }

        executablePath = executableResolver();
        return string.IsNullOrWhiteSpace(executablePath)
            ? (false, null, $"{package.DisplayName} installed, but its executable could not be located automatically.", string.Empty)
            : (true, executablePath, $"{package.DisplayName} is ready.", executablePath);
    }

    private static ProcessStartInfo CreateCommandProcessStartInfo(string fileName, string arguments) =>
        new()
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

    private static string? ReadInstalledReleaseMarker(string installDirectory)
    {
        var markerPath = GetInstalledReleaseMarkerPath(installDirectory);
        return File.Exists(markerPath)
            ? File.ReadAllText(markerPath).Trim()
            : null;
    }

    private static async Task WriteInstalledReleaseMarkerAsync(
        string installDirectory,
        InstallerReleaseAsset releaseAsset,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            GetInstalledReleaseMarkerPath(installDirectory),
            BuildInstalledReleaseMarkerValue(releaseAsset),
            cancellationToken).ConfigureAwait(false);
    }

    private static string GetInstalledReleaseMarkerPath(string installDirectory) =>
        Path.Combine(installDirectory, InstalledReleaseMarkerFileName);

    private static string BuildInstalledReleaseMarkerValue(InstallerReleaseAsset releaseAsset) =>
        !string.IsNullOrWhiteSpace(releaseAsset.DownloadUrl)
            ? releaseAsset.DownloadUrl.Trim()
            : releaseAsset.FileName.Trim();

    private static async Task WriteAutomatic1111LauncherAsync(
        string launcherPath,
        string installDirectory,
        string pythonExecutablePath,
        string gitExecutablePath,
        CancellationToken cancellationToken)
    {
        var launcherContents = $@"@echo off
setlocal
set ""PYTHON={pythonExecutablePath}""
set ""GIT={gitExecutablePath}""
set ""STABLE_DIFFUSION_REPO={Automatic1111StableDiffusionRepositoryUrl}""
set ""VENV_DIR=""
set ""COMMANDLINE_ARGS=""
pushd ""{installDirectory}""
call webui-user.bat
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteOpenWebUiLauncherAsync(
        string launcherPath,
        string installDirectory,
        string dataDirectory,
        string openWebUiExecutablePath,
        CancellationToken cancellationToken)
    {
        var launcherContents = $@"@echo off
setlocal
set ""DATA_DIR={dataDirectory}""
if not exist ""%DATA_DIR%"" mkdir ""%DATA_DIR%""
pushd ""{installDirectory}""
""{openWebUiExecutablePath}"" serve
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteRyubingRyujinxLauncherAsync(
        string launcherPath,
        string executablePath,
        CancellationToken cancellationToken)
    {
        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            throw new InvalidOperationException("Ryujinx (Ryubing) executable path is invalid.");
        }

        var launcherContents = $@"@echo off
setlocal
pushd ""{executableDirectory}""
start """" ""{executablePath}""
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteAzaharLauncherAsync(
        string launcherPath,
        string executablePath,
        CancellationToken cancellationToken)
    {
        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            throw new InvalidOperationException("Lime3DS (Azahar) executable path is invalid.");
        }

        var launcherContents = $@"@echo off
setlocal
pushd ""{executableDirectory}""
start """" ""{executablePath}""
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteMacriumReflectLauncherAsync(
        string launcherPath,
        string targetExecutablePath,
        CancellationToken cancellationToken)
    {
        var targetDirectory = Path.GetDirectoryName(targetExecutablePath);
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new InvalidOperationException("Macrium Reflect target path is invalid.");
        }

        var launcherContents = $@"@echo off
setlocal
pushd ""{targetDirectory}""
start """" ""{targetExecutablePath}""
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteRpcs3LauncherAsync(
        string launcherPath,
        string executablePath,
        CancellationToken cancellationToken)
    {
        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            throw new InvalidOperationException("RPCS3 executable path is invalid.");
        }

        var launcherContents = $@"@echo off
setlocal
pushd ""{executableDirectory}""
start """" ""{executablePath}""
popd
";

        await File.WriteAllTextAsync(launcherPath, launcherContents, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InstallerOperationResult> LaunchAutomatic1111Async(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                Automatic1111PackageId,
                "Stable Diffusion WebUI (AUTOMATIC1111)",
                false,
                false,
                "The launch script for Stable Diffusion WebUI was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                Automatic1111PackageId,
                "Stable Diffusion WebUI (AUTOMATIC1111)",
                true,
                true,
                "Launched the first-run setup window.",
                output)
            : new InstallerOperationResult(
                Automatic1111PackageId,
                "Stable Diffusion WebUI (AUTOMATIC1111)",
                false,
                false,
                SummarizeFailure(output, "The first-run setup could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> LaunchOpenWebUiAsync(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                OpenWebUiPackageId,
                "Open WebUI",
                false,
                false,
                "The launch script for Open WebUI was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                OpenWebUiPackageId,
                "Open WebUI",
                true,
                true,
                "Launched the Open WebUI server window.",
                output)
            : new InstallerOperationResult(
                OpenWebUiPackageId,
                "Open WebUI",
                false,
                false,
                SummarizeFailure(output, "The Open WebUI server could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> LaunchRyubingRyujinxAsync(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                RyubingRyujinxPackageId,
                "Ryujinx (Ryubing)",
                false,
                false,
                "The launch script for Ryujinx (Ryubing) was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                RyubingRyujinxPackageId,
                "Ryujinx (Ryubing)",
                true,
                true,
                "Launched Ryujinx (Ryubing).",
                output)
            : new InstallerOperationResult(
                RyubingRyujinxPackageId,
                "Ryujinx (Ryubing)",
                false,
                false,
                SummarizeFailure(output, "Ryujinx (Ryubing) could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> LaunchAzaharAsync(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                AzaharPackageId,
                "Lime3DS (Azahar)",
                false,
                false,
                "The launch script for Lime3DS (Azahar) was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                AzaharPackageId,
                "Lime3DS (Azahar)",
                true,
                true,
                "Launched Lime3DS (Azahar).",
                output)
            : new InstallerOperationResult(
                AzaharPackageId,
                "Lime3DS (Azahar)",
                false,
                false,
                SummarizeFailure(output, "Lime3DS (Azahar) could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> LaunchMacriumReflectAsync(
        string launcherPath,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                false,
                false,
                "The launch script for Macrium Reflect was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(workingDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                true,
                true,
                "Launched Macrium Reflect.",
                output)
            : new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                false,
                false,
                SummarizeFailure(output, "Macrium Reflect could not be launched."),
                output);
    }

    private async Task<InstallerOperationResult> RunMacriumReflectInstallerSilentlyAsync(
        string installerPath,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(installerPath))
        {
            return new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                false,
                false,
                "The Macrium Reflect installer could not be found after download.",
                installerPath);
        }

        var logsDirectory = Path.Combine(workingDirectory, "Logs");
        Directory.CreateDirectory(logsDirectory);
        var logPath = Path.Combine(logsDirectory, MacriumReflectUpdateLogFileName);
        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }

        var installResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /wait {QuoteArgument(installerPath)} /qn /norestart /l {QuoteArgument(logPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(installResult);
        var details = string.Join(
            Environment.NewLine,
            new[]
            {
                output,
                $"Installer: {installerPath}",
                $"Log: {logPath}",
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        if (installResult.ExitCode == 0)
        {
            return new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                true,
                true,
                "Updated Macrium Reflect silently with the latest installer.",
                details);
        }

        if (installResult.ExitCode == RestartRequiredExitCode)
        {
            return new InstallerOperationResult(
                MacriumReflectPackageId,
                "Macrium Reflect",
                true,
                true,
                "Updated Macrium Reflect silently. Restart Windows to finish the upgrade.",
                details);
        }

        return new InstallerOperationResult(
            MacriumReflectPackageId,
            "Macrium Reflect",
            false,
            false,
            $"Macrium Reflect silent update failed. Review the installer log at {logPath}.",
            details);
    }

    private async Task<InstallerOperationResult> LaunchRpcs3Async(
        string launcherPath,
        string installDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(launcherPath))
        {
            return new InstallerOperationResult(
                Rpcs3PackageId,
                "RPCS3",
                false,
                false,
                "The launch script for RPCS3 was not created correctly.",
                launcherPath);
        }

        var launchResult = await commandRunner(
            CreateCommandProcessStartInfo(
                "cmd.exe",
                $"/c start \"\" /D {QuoteArgument(installDirectory)} {QuoteArgument(launcherPath)}"),
            cancellationToken).ConfigureAwait(false);
        var output = NormalizeOutput(launchResult);

        return launchResult.ExitCode == 0
            ? new InstallerOperationResult(
                Rpcs3PackageId,
                "RPCS3",
                true,
                true,
                "Launched RPCS3.",
                output)
            : new InstallerOperationResult(
                Rpcs3PackageId,
                "RPCS3",
                false,
                false,
                SummarizeFailure(output, "RPCS3 could not be launched."),
                output);
    }

    private static bool UsesGuidedInstall(InstallerCatalogItem package) =>
        !string.IsNullOrWhiteSpace(package.InstallUrl) && !package.TrackStatusWithWinget;

    private static bool UsesGuidedUpdate(InstallerCatalogItem package) =>
        (!string.IsNullOrWhiteSpace(package.UpdateUrl) || UsesGuidedInstall(package))
        && !package.TrackStatusWithWinget;

    private static string QuoteArgument(string value) => $"\"{value}\"";

    private static bool IsAutomatic1111Package(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, Automatic1111PackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsOpenWebUiPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, OpenWebUiPackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsRyubingRyujinxPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, RyubingRyujinxPackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsAzaharPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, AzaharPackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsMacriumReflectPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, MacriumReflectPackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsRpcs3Package(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, Rpcs3PackageId, StringComparison.OrdinalIgnoreCase);

    private static bool IsSpotifyPackage(InstallerCatalogItem package) =>
        string.Equals(package.PackageId, SpotifyPackageId, StringComparison.OrdinalIgnoreCase);

    private static string SummarizeFailure(string output, string fallbackMessage)
    {
        var summarizedFailure = TrySummarizeFailure(output);
        if (!string.IsNullOrWhiteSpace(summarizedFailure))
        {
            return summarizedFailure;
        }

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

    private static string? TrySummarizeFailure(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var lines = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => line.Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!IsFailureSummaryLine(line))
            {
                continue;
            }

            if (IsGenericFailureLeadIn(line))
            {
                var detailLines = lines
                    .Skip(index + 1)
                    .Where(IsFailureSummaryLine)
                    .Take(2)
                    .ToArray();

                if (detailLines.Length > 0)
                {
                    return string.Join(" ", detailLines);
                }

                continue;
            }

            if (IsFailureCodeLine(line) && index > 0)
            {
                var previousLine = lines[index - 1];
                if (IsFailureSummaryLine(previousLine) && !IsGenericFailureLeadIn(previousLine))
                {
                    return $"{previousLine} {line}";
                }
            }

            return line;
        }

        return null;
    }

    private static bool IsFailureSummaryLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || IsInformationalSummaryLine(line))
        {
            return false;
        }

        return line.Contains("error", StringComparison.OrdinalIgnoreCase)
            || line.Contains("failed", StringComparison.OrdinalIgnoreCase)
            || line.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
            || line.Contains("denied", StringComparison.OrdinalIgnoreCase)
            || line.Contains("not success", StringComparison.OrdinalIgnoreCase)
            || line.Contains("could not", StringComparison.OrdinalIgnoreCase)
            || line.Contains("cannot", StringComparison.OrdinalIgnoreCase)
            || line.Contains("can't", StringComparison.OrdinalIgnoreCase)
            || line.Contains("0x", StringComparison.OrdinalIgnoreCase)
            || line.Contains("hash does not match", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericFailureLeadIn(string line) =>
        line.StartsWith("An unexpected error occurred", StringComparison.OrdinalIgnoreCase);

    private static bool IsFailureCodeLine(string line) =>
        line.Contains("0x", StringComparison.OrdinalIgnoreCase);

    private static bool IsInformationalSummaryLine(string line) =>
        line.StartsWith("Found ", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("This application is licensed", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Microsoft is not responsible", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Starting package install", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Downloading ", StringComparison.OrdinalIgnoreCase);

    private static string ResolveAutomatic1111InstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "stable-diffusion-webui");

    private static string ResolveSpotifyWorkingDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "spotify-installer");

    private static string ResolveOpenWebUiInstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "open-webui");

    private static string? ResolveFirefoxInstallDirectory()
    {
        foreach (var registryView in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            var installDirectory = ResolveFirefoxInstallDirectoryFromRegistry(RegistryHive.LocalMachine, registryView)
                ?? ResolveFirefoxInstallDirectoryFromRegistry(RegistryHive.CurrentUser, registryView);

            if (!string.IsNullOrWhiteSpace(installDirectory))
            {
                return installDirectory;
            }
        }

        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Mozilla Firefox"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mozilla Firefox"),
                 })
        {
            if (File.Exists(Path.Combine(candidate, "firefox.exe")))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveFirefoxInstallDirectoryFromRegistry(RegistryHive hive, RegistryView view)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var appPathsKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe");
        var path = appPathsKey?.GetValue(string.Empty) as string;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.GetDirectoryName(path);
    }

    private static string ResolveRyubingRyujinxInstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "ryujinx-ryubing");

    private static string ResolveAzaharInstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "azahar");

    private static string ResolveMacriumReflectWorkingDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "macrium-reflect");

    private static string ResolveRpcs3InstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "rpcs3");

    private static string? ResolveGitExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "git.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "cmd", "git.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin", "git.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("git.exe");
    }

    private static string? ResolvePowerShellExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7-preview", "pwsh.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PowerShell", "7", "pwsh.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "pwsh.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("pwsh.exe");
    }

    private static string? ResolvePython310ExecutablePath()
    {
        return ResolvePythonExecutablePath("3.10", "Python310", "python3.10.exe");
    }

    private static string? ResolvePython311ExecutablePath()
    {
        return ResolvePythonExecutablePath("3.11", "Python311", "python3.11.exe");
    }

    private static string? ResolveSevenZipExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("7z.exe");
    }

    private static string? ResolvePythonExecutablePath(string version, string folderName, string versionedExecutableName)
    {
        foreach (var registryView in new[] { Microsoft.Win32.RegistryView.Registry64, Microsoft.Win32.RegistryView.Registry32 })
        {
            var installDirectory = ResolvePythonInstallDirectoryFromRegistry(Microsoft.Win32.RegistryHive.CurrentUser, registryView, version)
                ?? ResolvePythonInstallDirectoryFromRegistry(Microsoft.Win32.RegistryHive.LocalMachine, registryView, version);
            if (!string.IsNullOrWhiteSpace(installDirectory))
            {
                var candidate = Path.Combine(installDirectory!, "python.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", folderName, "python.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), folderName, "python.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), folderName, "python.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath(versionedExecutableName);
    }

    private static string? ResolvePythonInstallDirectoryFromRegistry(Microsoft.Win32.RegistryHive hive, Microsoft.Win32.RegistryView view, string version)
    {
        using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(hive, view);
        using var installPathKey = baseKey.OpenSubKey($@"SOFTWARE\Python\PythonCore\{version}\InstallPath");
        return installPathKey?.GetValue(string.Empty) as string;
    }

    private static string? ResolveExecutableFromPath(string executableName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue!.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            try
            {
                var candidate = Path.Combine(directory.Trim(), executableName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static string? ResolveDiscordExecutablePath()
    {
        var installDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord");
        if (!Directory.Exists(installDirectory))
        {
            return ResolveExecutableFromPath("Discord.exe");
        }

        var updateExecutablePath = Path.Combine(installDirectory, "Update.exe");
        if (File.Exists(updateExecutablePath))
        {
            return updateExecutablePath;
        }

        return Directory
            .EnumerateFiles(installDirectory, "Discord.exe", SearchOption.AllDirectories)
            .OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string? ResolveVencordInstallDirectory()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vencord"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VencordDesktop"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vencord"),
                 })
        {
            if (!Directory.Exists(candidate))
            {
                continue;
            }

            if (File.Exists(Path.Combine(candidate, "settings", "settings.json"))
                || File.Exists(Path.Combine(candidate, "dist", "patcher.js"))
                || Directory.EnumerateFileSystemEntries(candidate).Any())
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveQbittorrentExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "qBittorrent", "qbittorrent.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "qBittorrent", "qbittorrent.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "qBittorrent", "qbittorrent.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("qbittorrent.exe");
    }

    private static string? ResolveEverythingExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything", "Everything.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything", "Everything.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Everything", "Everything.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything", "Everything.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return ResolveExecutableFromPath("Everything.exe");
    }

    private static string? ResolveTorBrowserExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Tor Browser", "Browser", "firefox.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Tor Browser", "Browser", "firefox.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveRpcs3ExecutablePath(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
        {
            return null;
        }

        var directPath = Path.Combine(installDirectory, "rpcs3.exe");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        return Directory
            .EnumerateFiles(installDirectory, "rpcs3.exe", SearchOption.AllDirectories)
            .FirstOrDefault(path => !path.Contains($"{Path.DirectorySeparatorChar}downloads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveRyubingRyujinxExecutablePath(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
        {
            return null;
        }

        foreach (var candidateFileName in new[] { "Ryujinx.exe", "Ryujinx.Ava.exe" })
        {
            var directPath = Path.Combine(installDirectory, candidateFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }
        }

        return Directory
            .EnumerateFiles(installDirectory, "Ryujinx*.exe", SearchOption.AllDirectories)
            .FirstOrDefault(
                path =>
                    !path.Contains($"{Path.DirectorySeparatorChar}downloads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileName(path).Contains("Updater", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveAzaharExecutablePath(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
        {
            return null;
        }

        foreach (var candidateFileName in new[] { "azahar.exe", "Azahar.exe" })
        {
            var directPath = Path.Combine(installDirectory, candidateFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }
        }

        return Directory
            .EnumerateFiles(installDirectory, "*azahar*.exe", SearchOption.AllDirectories)
            .FirstOrDefault(
                path =>
                    !path.Contains($"{Path.DirectorySeparatorChar}downloads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileName(path).Contains("updater", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileName(path).Contains("room", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveMacriumReflectExecutablePath()
    {
        foreach (var candidate in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Macrium", "Reflect", "ReflectBin.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Macrium", "Reflect", "Reflect.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Macrium", "Reflect", "ReflectBin.exe"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Macrium", "Reflect", "Reflect.exe"),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MultiTool/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return client;
    }

    private static async Task<InstallerReleaseAsset?> ResolveLatestRyubingRyujinxReleaseAssetAsync(CancellationToken cancellationToken)
    {
        using var response = await SharedHttpClient.GetAsync(RyubingRyujinxLatestReleaseApiUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
        {
            return null;
        }

        var latestRelease = document.RootElement[0];
        if (!latestRelease.TryGetProperty("assets", out var assetsElement)
            || !assetsElement.TryGetProperty("links", out var linksElement)
            || linksElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in linksElement.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            var name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name)
                || !name.EndsWith("-win_x64.zip", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var downloadUrl = asset.TryGetProperty("direct_asset_url", out var directAssetUrlElement)
                ? directAssetUrlElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(downloadUrl) && asset.TryGetProperty("url", out var urlElement))
            {
                downloadUrl = urlElement.GetString();
            }

            if (!string.IsNullOrWhiteSpace(downloadUrl))
            {
                return new InstallerReleaseAsset(name, downloadUrl);
            }
        }

        return null;
    }

    private static async Task<InstallerReleaseAsset?> ResolveLatestMacriumReflectInstallerAsync(CancellationToken cancellationToken)
    {
        using var response = await SharedHttpClient.GetAsync(MacriumReflectInstallerResolverUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        var installElement = document.Root;
        if (installElement is null)
        {
            return null;
        }

        var isValid = string.Equals(installElement.Element("valid")?.Value, "True", StringComparison.OrdinalIgnoreCase);
        var fileName = installElement.Element("filename")?.Value;
        var downloadUrl = installElement.Element("url")?.Value;
        if (!isValid || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(downloadUrl))
        {
            return null;
        }

        return new InstallerReleaseAsset(fileName, downloadUrl);
    }

    private static async Task<InstallerReleaseAsset?> ResolveLatestAzaharReleaseAssetAsync(CancellationToken cancellationToken)
    {
        using var response = await SharedHttpClient.GetAsync(AzaharLatestReleaseApiUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("assets", out var assetsElement) || assetsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in assetsElement.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var nameElement)
                || !asset.TryGetProperty("browser_download_url", out var downloadUrlElement))
            {
                continue;
            }

            var name = nameElement.GetString();
            var downloadUrl = downloadUrlElement.GetString();
            if (string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(downloadUrl)
                || !name.EndsWith("-windows-msys2.zip", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new InstallerReleaseAsset(name, downloadUrl);
        }

        return null;
    }

    private static async Task<InstallerReleaseAsset?> ResolveLatestRpcs3ReleaseAssetAsync(CancellationToken cancellationToken)
    {
        using var response = await SharedHttpClient.GetAsync(Rpcs3LatestReleaseApiUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("assets", out var assetsElement) || assetsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in assetsElement.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var nameElement)
                || !asset.TryGetProperty("browser_download_url", out var downloadUrlElement))
            {
                continue;
            }

            var name = nameElement.GetString();
            var downloadUrl = downloadUrlElement.GetString();
            if (string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(downloadUrl)
                || !name.EndsWith("_win64_msvc.7z", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new InstallerReleaseAsset(name, downloadUrl);
        }

        return null;
    }

    private static async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        using var response = await SharedHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
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

    private static Task LaunchInstallerExecutableAsync(ProcessStartInfo startInfo, bool preferUnelevated, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (preferUnelevated && IsCurrentProcessElevated())
        {
            LaunchInstallerViaShellApplication(startInfo);
            return Task.CompletedTask;
        }

        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    private static void LaunchInstallerViaShellApplication(ProcessStartInfo startInfo)
    {
        var shellType = Type.GetTypeFromProgID("Shell.Application")
            ?? throw new InvalidOperationException("Windows Shell automation is unavailable.");

        object? shell = null;

        try
        {
            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Windows Shell automation could not be created.");
            shellType.InvokeMember(
                "ShellExecute",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args:
                [
                    startInfo.FileName,
                    startInfo.Arguments ?? string.Empty,
                    startInfo.WorkingDirectory ?? string.Empty,
                    "open",
                    1,
                ]);
        }
        finally
        {
            if (shell is not null && Marshal.IsComObject(shell))
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }

    private static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);

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
