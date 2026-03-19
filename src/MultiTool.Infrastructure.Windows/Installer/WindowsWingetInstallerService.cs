using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using Microsoft.Win32;

namespace MultiTool.Infrastructure.Windows.Installer;

public delegate Task<InstallerCommandResult> InstallerCommandRunner(ProcessStartInfo startInfo, CancellationToken cancellationToken);
public delegate Task GuidedInstallerLauncher(string target, CancellationToken cancellationToken);
public delegate Task InstallerFileDownloader(string url, string destinationPath, CancellationToken cancellationToken);
public delegate Task<InstallerReleaseAsset?> InstallerReleaseAssetResolver(CancellationToken cancellationToken);
public delegate Task InstallerExecutableLauncher(ProcessStartInfo startInfo, bool preferUnelevated, CancellationToken cancellationToken);
public sealed record InstallerCommandResult(int ExitCode, string StandardOutput, string StandardError);
public sealed record InstallerReleaseAsset(string FileName, string DownloadUrl);

public sealed partial class WindowsWingetInstallerService : IInstallerService
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
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint TokenAssignPrimary = 0x0001;
    private const uint TokenDuplicate = 0x0002;
    private const uint TokenQuery = 0x0008;
    private const uint MaximumAllowed = 0x02000000;
    private const int LogonWithProfile = 0x00000001;

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
            ["Python.Python.3.10"] = ["Python 3.10"],
            ["Python.Python.3.11"] = ["Python 3.11"],
            ["Python.Python.3.14"] = ["Python 3.14"],
            [QbittorrentPackageId] = ["qBittorrent"],
            [EverythingPackageId] = ["Everything"],
            [TorBrowserPackageId] = ["Tor Browser"],
        };

    private static readonly IReadOnlyList<InstallerCatalogItem> Catalog =
    [
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
        new("Libretro.RetroArch", "RetroArch", "Emulation", "All-in-one app that runs games from many older consoles.", false),
        new(
            RyubingRyujinxPackageId,
            "Ryujinx (Ryubing)",
            "Emulation",
            "Nintendo Switch emulator.",
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
            "PlayStation 3 emulator.",
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
            "Nintendo 3DS emulator.",
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
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/AUTOMATIC1111/stable-diffusion-webui/wiki/Install-and-Run-on-NVidia-GPUs",
            UpdateUrl: "https://github.com/AUTOMATIC1111/stable-diffusion-webui",
            UsesCustomInstallFlow: true),
        new("Comfy.ComfyUI-Desktop", "ComfyUI", "AI", "Node-based local image workflow app.", false, true),
        new("Neovim.Neovim", "Neovim", "Developer", "Vim-based text editor.", true, true),
        new("Ollama.Ollama", "Ollama", "AI", "Run local language models.", false, true),
        new("Jan.Jan", "Jan", "AI", "Offline desktop AI assistant for local models.", true, true),
        new("ElementLabs.LMStudio", "LM Studio", "AI", "Desktop app for local models.", false, false),
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
        new("Adobe.CreativeCloud", "Adobe Creative Cloud", "Creative", "Adobe apps hub for Photoshop, Illustrator, and more.", true),
        new("Flow-Launcher.Flow-Launcher", "Flow Launcher", "Productivity", "App launcher and command palette.", true),
        new(
            MacriumReflectPackageId,
            "Macrium Reflect",
            "Utilities",
            "Gets the latest Macrium Reflect installer and opens setup for you.",
            false,
            false,
            TrackStatusWithWinget: false,
            InstallUrl: "https://www.macrium.com/products/home",
            UpdateUrl: "https://www.macrium.com/products/home",
            UsesCustomInstallFlow: true),
        new("Obsidian.Obsidian", "Obsidian", "Productivity", "Notetaking app and database that uses Markdown.", true, true),
        new("9NRX63209R7B", "Outlook", "Productivity", "Outlook from Microsoft Store.", true, false, Source: "msstore"),
        new("AutoHotkey.AutoHotkey", "AutoHotkey", "Utilities", "Automation and hotkey scripting.", true, true),
        new("CrystalDewWorld.CrystalDiskMark", "CrystalDiskMark", "Utilities", "Storage benchmark tool."),
        new("Guru3D.Afterburner", "MSI Afterburner", "Utilities", "GPU tuning and monitoring tool.", true, false),
        new("TechPowerUp.NVCleanstall", "NVCleanstall", "Utilities", "Custom NVIDIA driver installer and debloater."),
        new("OpenRGB.OpenRGB", "OpenRGB", "Utilities", "Open-source RGB control app."),
        new("voidtools.Everything", "Everything", "Utilities", "Instant file search.", true),
        new("Mozilla.Firefox", "Firefox", "Browsers", "Mozilla web browser.", true),
        new(
            "Guided.SKiDL",
            "SKiDL",
            "CAD",
            "Python EDA scripting for netlists; AI-friendly for code-assisted design.",
            false,
            true,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/devbisme/skidl",
            UpdateUrl: "https://github.com/devbisme/skidl"),
        new(
            "Guided.PCBFlow",
            "PCBFlow",
            "CAD",
            "Parametric PCB generation in Python; AI-friendly for script-driven layouts.",
            false,
            true,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/michaelgale/pcbflow",
            UpdateUrl: "https://github.com/michaelgale/pcbflow"),
        new(
            "Guided.build123d",
            "build123d",
            "CAD",
            "Python CAD modeling toolkit; AI-friendly for prompt-to-script workflows.",
            false,
            true,
            TrackStatusWithWinget: false,
            InstallUrl: "https://github.com/gumyr/build123d",
            UpdateUrl: "https://github.com/gumyr/build123d"),
        new("Git.Git", "Git", "Developer", "Version control CLI.", true, true),
        new("Microsoft.DotNet.SDK.10", "Microsoft .NET SDK", "Developer", "Latest stable .NET SDK and CLI tools.", false, true),
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
        new("Microsoft.Edge", "Microsoft Edge", "Windows Extras", "Removable only where Windows allows it.", true),
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

    public InstallerPackageCapabilities GetPackageCapabilities(string packageId)
    {
        if (!catalogById.TryGetValue(packageId, out var package))
        {
            return new InstallerPackageCapabilities();
        }

        return BuildPackageCapabilities(package);
    }

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

    public async Task<IReadOnlyList<InstallerOperationResult>> RunPackageOperationAsync(
        string packageId,
        InstallerPackageAction action,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            return [];
        }

        if (!catalogById.TryGetValue(packageId.Trim(), out var package))
        {
            return
            [
                new InstallerOperationResult(
                    packageId.Trim(),
                    packageId.Trim(),
                    false,
                    false,
                    "This package is not in the current installer catalog.",
                    string.Empty),
            ];
        }

        return action switch
        {
            InstallerPackageAction.Install => await InstallPackagesAsync([package.PackageId], cancellationToken).ConfigureAwait(false),
            InstallerPackageAction.Update => await UpgradePackagesAsync([package.PackageId], cancellationToken).ConfigureAwait(false),
            InstallerPackageAction.Uninstall => await UninstallPackagesAsync([package.PackageId], cancellationToken).ConfigureAwait(false),
            InstallerPackageAction.InstallInteractive => await RunInteractiveInstallAsync(package, cancellationToken).ConfigureAwait(false),
            InstallerPackageAction.UpdateInteractive => await RunInteractiveUpgradeAsync(package, cancellationToken).ConfigureAwait(false),
            InstallerPackageAction.Reinstall => await RunReinstallAsync(package, cancellationToken).ConfigureAwait(false),
            _ =>
            [
                new InstallerOperationResult(
                    package.PackageId,
                    package.DisplayName,
                    false,
                    false,
                    "That installer action is not supported yet.",
                    string.Empty),
            ],
        };
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
                results.Add(ApplyGuidance(package, InstallerPackageAction.Install, CreateSkippedInstallResult(package, status)));
                continue;
            }

            try
            {
                results.Add(ApplyGuidance(package, InstallerPackageAction.Install, await InstallPackageAsync(package, cancellationToken).ConfigureAwait(false)));
            }
            catch (Exception ex)
            {
                results.Add(
                    ApplyGuidance(
                        package,
                        InstallerPackageAction.Install,
                        new InstallerOperationResult(
                        package.PackageId,
                        package.DisplayName,
                        false,
                        false,
                        ex.Message,
                        ex.ToString())));
            }
        }

        return results;
    }

    public Task<IReadOnlyList<InstallerOperationResult>> UpgradePackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
        RunBatchAsync(
            NormalizePackageIds(packageIds),
            UpgradePackageAsync,
            InstallerPackageAction.Update,
            cancellationToken);

    public Task<IReadOnlyList<InstallerOperationResult>> UninstallPackagesAsync(IEnumerable<string> packageIds, CancellationToken cancellationToken = default) =>
        RunBatchAsync(
            NormalizePackageIds(packageIds),
            UninstallPackageAsync,
            InstallerPackageAction.Uninstall,
            cancellationToken);

}
