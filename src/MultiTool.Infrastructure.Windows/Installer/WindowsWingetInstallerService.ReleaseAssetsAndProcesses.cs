using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using Microsoft.Win32;

namespace MultiTool.Infrastructure.Windows.Installer;

public sealed partial class WindowsWingetInstallerService : IInstallerService
{
    private static readonly Regex WingetPercentRegex = new(@"(?<!\d)(?<percent>\d{1,3})\s*%", RegexOptions.Compiled);
    private static readonly Regex WingetSizeProgressRegex = new(
        @"(?<current>\d+(?:[.,]\d+)?)\s*(?<currentUnit>KB|MB|GB)\s*/\s*(?<total>\d+(?:[.,]\d+)?)\s*(?<totalUnit>KB|MB|GB)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

    private async Task DownloadFileWithProgressAsync(
        string url,
        string destinationPath,
        InstallerCatalogItem package,
        InstallerPackageAction action,
        CancellationToken cancellationToken)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        using var response = await SharedHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81_920];
        long totalRead = 0;
        int? lastReportedPercent = null;

        while (true)
        {
            var bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            totalRead += bytesRead;

            if (totalBytes is null or <= 0)
            {
                continue;
            }

            var percent = (int)Math.Round(totalRead * 100d / totalBytes.Value, MidpointRounding.AwayFromZero);
            percent = Math.Clamp(percent, 0, 100);
            if (lastReportedPercent == percent)
            {
                continue;
            }

            lastReportedPercent = percent;
            ReportOperationProgress(package, action, $"Downloading {percent}%...", percent);
        }

        ReportOperationProgress(package, action, "Downloading 100%...", 100);
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

    private async Task<InstallerCommandResult> RunWingetWithProgressAsync(
        ProcessStartInfo startInfo,
        InstallerCatalogItem package,
        InstallerPackageAction action,
        CancellationToken cancellationToken)
    {
        var progressParser = new WingetProgressParser(statusText => ReportOperationProgress(package, action, statusText), percent => ReportOperationProgress(package, action, $"Downloading {percent}%...", percent));
        return await RunProcessAsync(startInfo, cancellationToken, progressParser.HandleSegment, progressParser.HandleSegment).ConfigureAwait(false);
    }

    private static Task<InstallerCommandResult> RunProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken) =>
        RunProcessAsync(startInfo, cancellationToken, null, null);

    private static async Task<InstallerCommandResult> RunProcessAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken,
        Action<string>? outputSegmentHandler,
        Action<string>? errorSegmentHandler)
    {
        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        process.Start();
        var outputTask = ReadProcessStreamAsync(process.StandardOutput, standardOutput, outputSegmentHandler, cancellationToken);
        var errorTask = ReadProcessStreamAsync(process.StandardError, standardError, errorSegmentHandler, cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
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

    private static async Task ReadProcessStreamAsync(
        StreamReader reader,
        StringBuilder destination,
        Action<string>? segmentHandler,
        CancellationToken cancellationToken)
    {
        var buffer = new char[256];
        var currentSegment = new StringBuilder();

        while (true)
        {
            var readCount = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (readCount == 0)
            {
                break;
            }

            for (var index = 0; index < readCount; index++)
            {
                var character = buffer[index];
                destination.Append(character);

                if (character is '\r' or '\n')
                {
                    FlushCurrentSegment(currentSegment, segmentHandler);
                    continue;
                }

                currentSegment.Append(character);
            }
        }

        FlushCurrentSegment(currentSegment, segmentHandler);
    }

    private static void FlushCurrentSegment(StringBuilder currentSegment, Action<string>? segmentHandler)
    {
        if (currentSegment.Length == 0)
        {
            return;
        }

        segmentHandler?.Invoke(currentSegment.ToString());
        currentSegment.Clear();
    }

    private sealed class WingetProgressParser
    {
        private readonly Action<string> statusReporter;
        private readonly Action<int> percentReporter;
        private int? lastPercent;
        private string? lastStatusText;

        public WingetProgressParser(Action<string> statusReporter, Action<int> percentReporter)
        {
            this.statusReporter = statusReporter;
            this.percentReporter = percentReporter;
        }

        public void HandleSegment(string segment)
        {
            var line = segment.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (TryParseWingetProgressPercent(line, out var percent))
            {
                if (lastPercent == percent)
                {
                    return;
                }

                lastPercent = percent;
                percentReporter(percent);
                return;
            }

            string? statusText = null;
            if (line.Contains("Downloading", StringComparison.OrdinalIgnoreCase))
            {
                statusText = "Downloading...";
            }
            else if (line.Contains("Successfully verified installer hash", StringComparison.OrdinalIgnoreCase))
            {
                statusText = "Verified installer hash.";
            }
            else if (line.Contains("Starting package install", StringComparison.OrdinalIgnoreCase)
                     || line.Contains("Installing package", StringComparison.OrdinalIgnoreCase)
                     || line.Contains("Starting package upgrade", StringComparison.OrdinalIgnoreCase)
                     || line.Contains("Starting package uninstall", StringComparison.OrdinalIgnoreCase))
            {
                statusText = "Running installer...";
            }

            if (string.IsNullOrWhiteSpace(statusText) || string.Equals(lastStatusText, statusText, StringComparison.Ordinal))
            {
                return;
            }

            lastStatusText = statusText;
            statusReporter(statusText);
        }

        private static bool TryParseWingetProgressPercent(string line, out int percent)
        {
            var percentMatch = WingetPercentRegex.Match(line);
            if (percentMatch.Success
                && int.TryParse(percentMatch.Groups["percent"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out percent))
            {
                percent = Math.Clamp(percent, 0, 100);
                return true;
            }

            var sizeMatch = WingetSizeProgressRegex.Match(line);
            if (sizeMatch.Success
                && TryParseSize(sizeMatch.Groups["current"].Value, sizeMatch.Groups["currentUnit"].Value, out var currentBytes)
                && TryParseSize(sizeMatch.Groups["total"].Value, sizeMatch.Groups["totalUnit"].Value, out var totalBytes)
                && totalBytes > 0)
            {
                percent = Math.Clamp((int)Math.Round(currentBytes * 100d / totalBytes, MidpointRounding.AwayFromZero), 0, 100);
                return true;
            }

            percent = 0;
            return false;
        }

        private static bool TryParseSize(string numberText, string unitText, out double bytes)
        {
            if (!double.TryParse(numberText.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                bytes = 0;
                return false;
            }

            var multiplier = unitText.ToUpperInvariant() switch
            {
                "KB" => 1024d,
                "MB" => 1024d * 1024d,
                "GB" => 1024d * 1024d * 1024d,
                _ => 1d,
            };

            bytes = value * multiplier;
            return true;
        }
    }
}
