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

public sealed partial class WindowsWingetInstallerService : IInstallerService
{
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
            LaunchInstallerViaShellProcessToken(startInfo);
            return Task.CompletedTask;
        }

        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    private static void LaunchInstallerViaShellProcessToken(ProcessStartInfo startInfo)
    {
        var shellWindow = GetShellWindow();
        if (shellWindow == IntPtr.Zero)
        {
            throw new InvalidOperationException("Windows Explorer is not available to launch the installer without admin rights.");
        }

        _ = GetWindowThreadProcessId(shellWindow, out var shellProcessId);
        if (shellProcessId == 0)
        {
            throw new InvalidOperationException("Windows Explorer could not be resolved for an unelevated installer launch.");
        }

        IntPtr shellProcessHandle = IntPtr.Zero;
        IntPtr shellTokenHandle = IntPtr.Zero;
        IntPtr primaryTokenHandle = IntPtr.Zero;

        try
        {
            shellProcessHandle = OpenProcess(ProcessQueryLimitedInformation, false, shellProcessId);
            if (shellProcessHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows Explorer could not be opened for an unelevated installer launch.");
            }

            if (!OpenProcessToken(shellProcessHandle, TokenAssignPrimary | TokenDuplicate | TokenQuery, out shellTokenHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows Explorer's user token could not be opened for an unelevated installer launch.");
            }

            if (!DuplicateTokenEx(
                    shellTokenHandle,
                    MaximumAllowed,
                    IntPtr.Zero,
                    SecurityImpersonationLevel.SecurityImpersonation,
                    TokenType.TokenPrimary,
                    out primaryTokenHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows Explorer's user token could not be duplicated for an unelevated installer launch.");
            }

            var startupInfo = new StartupInfo
            {
                cb = Marshal.SizeOf<StartupInfo>(),
            };
            var commandLine = new StringBuilder(BuildCreateProcessCommandLine(startInfo));
            var currentDirectory = string.IsNullOrWhiteSpace(startInfo.WorkingDirectory)
                ? null
                : startInfo.WorkingDirectory;

            if (!CreateProcessWithTokenW(
                    primaryTokenHandle,
                    LogonWithProfile,
                    startInfo.FileName,
                    commandLine,
                    0,
                    IntPtr.Zero,
                    currentDirectory,
                    ref startupInfo,
                    out var processInformation))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The installer could not be launched from Windows Explorer's unelevated user context.");
            }

            try
            {
            }
            finally
            {
                if (processInformation.hThread != IntPtr.Zero)
                {
                    _ = CloseHandle(processInformation.hThread);
                }

                if (processInformation.hProcess != IntPtr.Zero)
                {
                    _ = CloseHandle(processInformation.hProcess);
                }
            }
        }
        finally
        {
            if (primaryTokenHandle != IntPtr.Zero)
            {
                _ = CloseHandle(primaryTokenHandle);
            }

            if (shellTokenHandle != IntPtr.Zero)
            {
                _ = CloseHandle(shellTokenHandle);
            }

            if (shellProcessHandle != IntPtr.Zero)
            {
                _ = CloseHandle(shellProcessHandle);
            }
        }
    }

    private static string BuildCreateProcessCommandLine(ProcessStartInfo startInfo)
    {
        var fileName = QuoteArgument(startInfo.FileName);
        if (string.IsNullOrWhiteSpace(startInfo.Arguments))
        {
            return fileName;
        }

        return $"{fileName} {startInfo.Arguments}";
    }

    private static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    private enum SecurityImpersonationLevel
    {
        Anonymous = 0,
        Identification = 1,
        SecurityImpersonation = 2,
        Delegation = 3,
    }

    private enum TokenType
    {
        TokenPrimary = 1,
        TokenImpersonation = 2,
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateTokenEx(
        IntPtr existingTokenHandle,
        uint desiredAccess,
        IntPtr tokenAttributes,
        SecurityImpersonationLevel impersonationLevel,
        TokenType tokenType,
        out IntPtr duplicatedTokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessWithTokenW(
        IntPtr tokenHandle,
        int logonFlags,
        string applicationName,
        StringBuilder commandLine,
        int creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation);

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
