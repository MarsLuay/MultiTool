using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Tools;

public delegate FnCtrlSwapStatus FnCtrlSwapProbe();

public sealed class WindowsFnCtrlSwapService : IFnCtrlSwapService
{
    private readonly FnCtrlSwapProbe probe;

    public WindowsFnCtrlSwapService()
        : this(ProbeStatus)
    {
    }

    public WindowsFnCtrlSwapService(FnCtrlSwapProbe probe)
    {
        this.probe = probe;
    }

    public FnCtrlSwapStatus GetStatus() => probe();

    public async Task<FnCtrlSwapResult> ToggleAsync(CancellationToken cancellationToken = default)
    {
        var before = probe();

        if (!before.IsSupported)
        {
            return new FnCtrlSwapResult(false, false, before.Message);
        }

        var newValue = before.IsSwapped ? "Disable" : "Enable";
        var newLabel = before.IsSwapped ? "back to normal" : "swapped";

        try
        {
            return await ToggleElevatedAsync(newValue, newLabel, cancellationToken).ConfigureAwait(false);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new FnCtrlSwapResult(false, false, "Administrator permission was canceled.");
        }
    }

    private static async Task<FnCtrlSwapResult> ToggleElevatedAsync(
        string newValue,
        string newLabel,
        CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "MultiTool", "fn-ctrl-swap", Guid.NewGuid().ToString("N"));
        var tempScriptPath = Path.Combine(tempDirectory, "fn-ctrl-swap.ps1");

        Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(
                tempScriptPath,
                BuildToggleScript(newValue),
                cancellationToken).ConfigureAwait(false);

            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                });

            if (process is null)
            {
                return new FnCtrlSwapResult(false, false, "Windows could not start the elevated Fn/Ctrl swap helper.");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                return new FnCtrlSwapResult(false, false, $"The Fn/Ctrl swap helper exited with code {process.ExitCode}.");
            }

            return new FnCtrlSwapResult(true, true, $"Fn and Ctrl keys are now {newLabel}. The change takes effect immediately on most Lenovo models.");
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

    internal static string BuildToggleScript(string newValue)
    {
        var sb = new StringBuilder();
        sb.AppendLine("$ErrorActionPreference = 'Stop'");
        sb.AppendLine();
        sb.AppendLine($"$setter = Get-WmiObject -Namespace root\\wmi -Class Lenovo_SetBiosSetting");
        sb.AppendLine($"$setter.SetBiosSetting('FnCtrlKeySwap,{newValue}') | Out-Null");
        sb.AppendLine();
        sb.AppendLine("$saver = Get-WmiObject -Namespace root\\wmi -Class Lenovo_SaveBiosSettings");
        sb.AppendLine("$saver.SaveBiosSettings() | Out-Null");
        return sb.ToString();
    }

    private static FnCtrlSwapStatus ProbeStatus()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" +
                        "(Get-WmiObject -Namespace root\\wmi -Class Lenovo_BiosSetting -ErrorAction Stop " +
                        "| Where-Object { $_.CurrentSetting -like 'FnCtrlKeySwap,*' }).CurrentSetting\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(10_000);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return new FnCtrlSwapStatus(false, false, "This feature is only available on Lenovo laptops with WMI BIOS support. No compatible Lenovo BIOS was detected.");
            }

            // Output is like "FnCtrlKeySwap,Enable" or "FnCtrlKeySwap,Disable"
            var isSwapped = output.Contains("Enable", StringComparison.OrdinalIgnoreCase);

            return isSwapped
                ? new FnCtrlSwapStatus(true, true, "Lenovo BIOS detected. Fn and Ctrl keys are currently swapped (Ctrl is on the far left).")
                : new FnCtrlSwapStatus(true, false, "Lenovo BIOS detected. Fn and Ctrl keys are in their default positions (Fn is on the far left).");
        }
        catch
        {
            return new FnCtrlSwapStatus(false, false, "This feature is only available on Lenovo laptops with WMI BIOS support. No compatible Lenovo BIOS was detected.");
        }
    }
}
