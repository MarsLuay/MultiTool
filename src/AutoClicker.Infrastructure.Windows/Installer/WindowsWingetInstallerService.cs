using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Installer;

public delegate Task<InstallerCommandResult> InstallerCommandRunner(ProcessStartInfo startInfo, CancellationToken cancellationToken);
public delegate Task GuidedInstallerLauncher(string target, CancellationToken cancellationToken);
public delegate Task InstallerFileDownloader(string url, string destinationPath, CancellationToken cancellationToken);
public delegate Task<InstallerReleaseAsset?> InstallerReleaseAssetResolver(CancellationToken cancellationToken);

public sealed record InstallerCommandResult(int ExitCode, string StandardOutput, string StandardError);
public sealed record InstallerReleaseAsset(string FileName, string DownloadUrl);

public sealed class WindowsWingetInstallerService : IInstallerService
{
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();

    private const string Automatic1111PackageId = "AUTOMATIC1111.StableDiffusionWebUI";
    private const string Automatic1111RepositoryUrl = "https://github.com/AUTOMATIC1111/stable-diffusion-webui.git";
    private const string Automatic1111StableDiffusionRepositoryUrl = "https://github.com/w-e-w/stablediffusion.git";
    private const string Automatic1111LauncherFileName = "launch-webui-multitool.bat";
    private const string OpenWebUiPackageId = "OpenWebUI.OpenWebUI";
    private const string OpenWebUiLauncherFileName = "launch-open-webui-multitool.bat";
    private const string RyubingRyujinxPackageId = "Ryubing.Ryujinx";
    private const string RyubingRyujinxLauncherFileName = "launch-ryujinx-ryubing-multitool.bat";
    private const string RyubingRyujinxLatestReleaseApiUrl = "https://git.ryujinx.app/api/v4/projects/ryubing%2Fryujinx/releases?per_page=1";
    private const string AzaharPackageId = "AzaharEmu.Azahar";
    private const string AzaharLauncherFileName = "launch-azahar-multitool.bat";
    private const string AzaharLatestReleaseApiUrl = "https://api.github.com/repos/azahar-emu/azahar/releases/latest";
    private const string MacriumReflectPackageId = "Macrium.Reflect";
    private const string MacriumReflectLauncherFileName = "launch-macrium-reflect-multitool.bat";
    private const string MacriumReflectInstallerResolverUrl = "https://updates.macrium.com/Reflect/v10/getmsi.asp?arch=1&edition=0&type=0";
    private const string Rpcs3PackageId = "RPCS3.RPCS3";
    private const string Rpcs3LauncherFileName = "launch-rpcs3-multitool.bat";
    private const string Rpcs3LatestReleaseApiUrl = "https://api.github.com/repos/RPCS3/rpcs3-binaries-win/releases/latest";

    private static readonly InstallerCatalogItem Automatic1111PythonPackage =
        new("Python.Python.3.10", "Python 3.10", "Developer", "Python 3.10 for older Torch-based apps.");
    private static readonly InstallerCatalogItem OpenWebUiPythonPackage =
        new("Python.Python.3.11", "Python 3.11", "Developer", "Python 3.11 for Open WebUI.");
    private static readonly InstallerCatalogItem Rpcs3VisualCppPackage =
        new("Microsoft.VCRedist.2015+.x64", "Visual C++ Redistributable (x64)", "Utilities", "VC++ runtime required by RPCS3.");
    private static readonly InstallerCatalogItem SevenZipPackage =
        new("7zip.7zip", "7-Zip", "Utilities", "Archive extraction tool for custom installs.");

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
            RyubingRyujinxPackageId,
            "Ryujinx (Ryubing)",
            "Emulation",
            "Downloads the latest Windows build and launches it.",
            false,
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
    private readonly Func<string> automatic1111InstallDirectoryResolver;
    private readonly Func<string?> gitExecutableResolver;
    private readonly Func<string?> python310ExecutableResolver;
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

    public WindowsWingetInstallerService()
        : this(
            RunProcessAsync,
            LaunchGuidedInstallerAsync,
            ResolveAutomatic1111InstallDirectory,
            ResolveGitExecutablePath,
            ResolvePython310ExecutablePath,
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
            DownloadFileAsync)
    {
    }

    public WindowsWingetInstallerService(
        InstallerCommandRunner commandRunner,
        GuidedInstallerLauncher? guidedInstallerLauncher = null,
        Func<string>? automatic1111InstallDirectoryResolver = null,
        Func<string?>? gitExecutableResolver = null,
        Func<string?>? python310ExecutableResolver = null,
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
        InstallerFileDownloader? fileDownloader = null)
    {
        this.commandRunner = commandRunner;
        this.guidedInstallerLauncher = guidedInstallerLauncher ?? LaunchGuidedInstallerAsync;
        this.automatic1111InstallDirectoryResolver = automatic1111InstallDirectoryResolver ?? ResolveAutomatic1111InstallDirectory;
        this.gitExecutableResolver = gitExecutableResolver ?? ResolveGitExecutablePath;
        this.python310ExecutableResolver = python310ExecutableResolver ?? ResolvePython310ExecutablePath;
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
            if (TryGetCustomPackageStatus(packageId, out var customStatus))
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

    private bool TryGetCustomPackageStatus(string packageId, out InstallerPackageStatus status)
    {
        if (string.Equals(packageId, Automatic1111PackageId, StringComparison.OrdinalIgnoreCase))
        {
            var installDirectory = automatic1111InstallDirectoryResolver();
            var isInstalled =
                !string.IsNullOrWhiteSpace(installDirectory) &&
                File.Exists(Path.Combine(installDirectory, "webui-user.bat")) &&
                File.Exists(Path.Combine(installDirectory, "webui.bat"));
            status = new InstallerPackageStatus(
                packageId,
                isInstalled,
                false,
                isInstalled ? "Installed (custom)" : "Not installed");
            return true;
        }

        if (string.Equals(packageId, OpenWebUiPackageId, StringComparison.OrdinalIgnoreCase))
        {
            var installDirectory = openWebUiInstallDirectoryResolver();
            var isInstalled =
                !string.IsNullOrWhiteSpace(installDirectory) &&
                File.Exists(Path.Combine(installDirectory, "venv", "Scripts", "python.exe")) &&
                File.Exists(Path.Combine(installDirectory, "venv", "Scripts", "open-webui.exe")) &&
                File.Exists(Path.Combine(installDirectory, OpenWebUiLauncherFileName));
            status = new InstallerPackageStatus(
                packageId,
                isInstalled,
                false,
                isInstalled ? "Installed (custom)" : "Not installed");
            return true;
        }

        if (string.Equals(packageId, RyubingRyujinxPackageId, StringComparison.OrdinalIgnoreCase))
        {
            var installDirectory = ryubingRyujinxInstallDirectoryResolver();
            var isInstalled = !string.IsNullOrWhiteSpace(installDirectory)
                && !string.IsNullOrWhiteSpace(ResolveRyubingRyujinxExecutablePath(installDirectory));
            status = new InstallerPackageStatus(
                packageId,
                isInstalled,
                false,
                isInstalled ? "Installed (custom)" : "Not installed");
            return true;
        }

        if (string.Equals(packageId, AzaharPackageId, StringComparison.OrdinalIgnoreCase))
        {
            var installDirectory = azaharInstallDirectoryResolver();
            var isInstalled = !string.IsNullOrWhiteSpace(installDirectory)
                && !string.IsNullOrWhiteSpace(ResolveAzaharExecutablePath(installDirectory));
            status = new InstallerPackageStatus(
                packageId,
                isInstalled,
                false,
                isInstalled ? "Installed (custom)" : "Not installed");
            return true;
        }

        if (string.Equals(packageId, MacriumReflectPackageId, StringComparison.OrdinalIgnoreCase))
        {
            var isInstalled = !string.IsNullOrWhiteSpace(macriumReflectExecutableResolver());
            status = new InstallerPackageStatus(
                packageId,
                isInstalled,
                false,
                isInstalled ? "Installed (custom)" : "Not installed");
            return true;
        }

        if (string.Equals(packageId, Rpcs3PackageId, StringComparison.OrdinalIgnoreCase))
        {
            var installDirectory = rpcs3InstallDirectoryResolver();
            var isInstalled = !string.IsNullOrWhiteSpace(installDirectory)
                && !string.IsNullOrWhiteSpace(ResolveRpcs3ExecutablePath(installDirectory));
            status = new InstallerPackageStatus(
                packageId,
                isInstalled,
                false,
                isInstalled ? "Installed (custom)" : "Not installed");
            return true;
        }

        status = new InstallerPackageStatus(packageId, false, false, "Status unavailable");
        return false;
    }

    private async Task<(bool Succeeded, string Message, string Output)> EnsurePackageInstalledAsync(
        InstallerCatalogItem package,
        string dependentDisplayName,
        CancellationToken cancellationToken)
    {
        try
        {
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

    private static string ResolveAutomatic1111InstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "stable-diffusion-webui");

    private static string ResolveOpenWebUiInstallDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MultiTool",
            "Apps",
            "open-webui");

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
