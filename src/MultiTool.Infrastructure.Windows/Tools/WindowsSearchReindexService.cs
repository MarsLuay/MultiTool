using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

public sealed class WindowsSearchReindexService : IWindowsSearchReindexService
{
    private const string WindowsSearchServiceName = "WSearch";
    private static readonly string WindowsSearchDataRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Search", "Data");

    public WindowsSearchReindexStatus GetStatus()
    {
        var searchServiceAvailable = IsWindowsSearchServiceAvailable();
        if (!searchServiceAvailable)
        {
            return new WindowsSearchReindexStatus(
                false,
                false,
                "Windows Search service is not available on this PC.");
        }

        var requiresPrompt = !IsCurrentProcessElevated();
        return new WindowsSearchReindexStatus(
            true,
            requiresPrompt,
            requiresPrompt
                ? "Windows Search service detected. Re-indexing will request administrator permission."
                : "Windows Search service detected. You can re-index now.");
    }

    public async Task<WindowsSearchReindexResult> ReindexAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsWindowsSearchServiceAvailable())
        {
            return new WindowsSearchReindexResult(false, false, "Windows Search service is not available on this PC.");
        }

        try
        {
            if (IsCurrentProcessElevated())
            {
                await RequestReindexDirectAsync(cancellationToken).ConfigureAwait(false);
                return new WindowsSearchReindexResult(true, true, "Windows Search re-index was requested. Index rebuild can take several minutes.");
            }

            var elevatedResult = await RequestReindexElevatedAsync(cancellationToken).ConfigureAwait(false);
            return elevatedResult
                ? new WindowsSearchReindexResult(true, true, "Windows Search re-index was requested with administrator permission. Index rebuild can take several minutes.")
                : new WindowsSearchReindexResult(false, false, "Windows Search re-index helper did not complete successfully.");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new WindowsSearchReindexResult(false, false, "Administrator permission was canceled.");
        }
        catch (Exception ex)
        {
            return new WindowsSearchReindexResult(false, false, $"Windows Search re-index failed: {ex.Message}");
        }
    }

    private static async Task RequestReindexDirectAsync(CancellationToken cancellationToken)
    {
        await Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryRequestReindexViaCom())
                {
                    TryRestartWindowsSearchService();
                    return;
                }

                ResetWindowsSearchIndexData();
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> RequestReindexElevatedAsync(CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "MultiTool", "search-reindex", Guid.NewGuid().ToString("N"));
        var scriptPath = Path.Combine(tempDirectory, "reindex-search.ps1");

        Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(scriptPath, BuildReindexScript(), cancellationToken).ConfigureAwait(false);

            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                });

            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
            catch
            {
            }
        }
    }

    private static string BuildReindexScript()
    {
        var sb = new StringBuilder();
        sb.AppendLine("$ErrorActionPreference = 'Stop'");
        sb.AppendLine("$usedCom = $false");
        sb.AppendLine("try {");
        sb.AppendLine("  $manager = New-Object -ComObject Microsoft.Windows.Search.Manager");
        sb.AppendLine("  $catalog = $manager.GetCatalog('SystemIndex')");
        sb.AppendLine("  $catalog.Reindex()");
        sb.AppendLine("  $usedCom = $true");
        sb.AppendLine("} catch { }");
        sb.AppendLine("if (-not $usedCom) {");
        sb.AppendLine("  try { Stop-Service -Name WSearch -Force -ErrorAction SilentlyContinue } catch { }");
        sb.AppendLine($"  $dataRoot = '{WindowsSearchDataRoot.Replace("'", "''")}'");
        sb.AppendLine("  if (Test-Path -LiteralPath $dataRoot) {");
        sb.AppendLine("    Get-ChildItem -LiteralPath $dataRoot -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine("try { Start-Service -Name WSearch -ErrorAction SilentlyContinue } catch { }");
        return sb.ToString();
    }

    private static bool TryRequestReindexViaCom()
    {
        object? manager = null;
        object? catalog = null;
        try
        {
            var managerType = Type.GetTypeFromProgID("Microsoft.Windows.Search.Manager", throwOnError: false);
            if (managerType is null)
            {
                return false;
            }

            manager = Activator.CreateInstance(managerType);
            if (manager is null)
            {
                return false;
            }

            catalog = managerType.InvokeMember(
                "GetCatalog",
                System.Reflection.BindingFlags.InvokeMethod,
                binder: null,
                target: manager,
                args: ["SystemIndex"]);

            if (catalog is null)
            {
                return false;
            }

            catalog.GetType().InvokeMember(
                "Reindex",
                System.Reflection.BindingFlags.InvokeMethod,
                binder: null,
                target: catalog,
                args: null);

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (catalog is not null && Marshal.IsComObject(catalog))
            {
                Marshal.ReleaseComObject(catalog);
            }

            if (manager is not null && Marshal.IsComObject(manager))
            {
                Marshal.ReleaseComObject(manager);
            }
        }
    }

    private static void ResetWindowsSearchIndexData()
    {
        TryStopWindowsSearchService();

        try
        {
            if (Directory.Exists(WindowsSearchDataRoot))
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(WindowsSearchDataRoot))
                {
                    try
                    {
                        if (Directory.Exists(entry))
                        {
                            Directory.Delete(entry, true);
                        }
                        else
                        {
                            File.Delete(entry);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
        }

        TryStartWindowsSearchService();
    }

    private static bool IsWindowsSearchServiceAvailable()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name FROM Win32_Service WHERE Name='WSearch'");
            return searcher
                .Get()
                .Cast<ManagementObject>()
                .Any();
        }
        catch
        {
            return false;
        }
    }

    private static void TryRestartWindowsSearchService()
    {
        try
        {
            RunScCommand($"stop {WindowsSearchServiceName}");
            RunScCommand($"start {WindowsSearchServiceName}");
        }
        catch
        {
        }
    }

    private static void RunScCommand(string arguments)
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            });

        process?.WaitForExit(10_000);
    }

    private static void TryStopWindowsSearchService() => RunScCommand($"stop {WindowsSearchServiceName}");

    private static void TryStartWindowsSearchService() => RunScCommand($"start {WindowsSearchServiceName}");

    private static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}