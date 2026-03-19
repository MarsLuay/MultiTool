using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using Microsoft.Win32;

namespace MultiTool.Infrastructure.Windows.Tools;

public delegate EdgeEnvironmentStatus EdgeEnvironmentProbe();

public delegate Task<EdgeCommandResult> EdgeCommandRunner(ProcessStartInfo startInfo, CancellationToken cancellationToken);

public sealed record EdgeCommandResult(int ExitCode, string StandardOutput, string StandardError);

public sealed record EdgeEnvironmentStatus(bool IsInstalled, string? UninstallString, string Message);

public sealed class WindowsEdgeRemovalService : IEdgeRemovalService
{
    private const string EdgeUninstallRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge";
    private const string EdgeUpdateClientStatePath = @"SOFTWARE\Microsoft\EdgeUpdate\ClientState\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}";
    private const string EdgeUpdateDevPath = @"SOFTWARE\Microsoft\EdgeUpdateDev";
    private const string EdgeUwpRelativePath = @"SystemApps\Microsoft.MicrosoftEdge_8wekyb3d8bbwe";

    private readonly EdgeEnvironmentProbe environmentProbe;
    private readonly EdgeCommandRunner commandRunner;

    public WindowsEdgeRemovalService()
        : this(ProbeEnvironment, RunProcessAsync)
    {
    }

    public WindowsEdgeRemovalService(
        EdgeEnvironmentProbe environmentProbe,
        EdgeCommandRunner commandRunner)
    {
        this.environmentProbe = environmentProbe;
        this.commandRunner = commandRunner;
    }

    public EdgeRemovalStatus GetStatus()
    {
        var environment = environmentProbe();
        return new EdgeRemovalStatus(environment.IsInstalled, environment.Message);
    }

    public async Task<EdgeRemovalResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        var before = environmentProbe();

        if (!before.IsInstalled)
        {
            return new EdgeRemovalResult(true, false, "Microsoft Edge is not installed or has already been removed.");
        }

        if (string.IsNullOrWhiteSpace(before.UninstallString))
        {
            return new EdgeRemovalResult(false, false, "Microsoft Edge appears to be installed, but no uninstall command was found in the registry.");
        }

        try
        {
            return await RemoveElevatedAsync(before.UninstallString, cancellationToken).ConfigureAwait(false);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new EdgeRemovalResult(false, false, "Administrator permission was canceled.");
        }
    }

    private async Task<EdgeRemovalResult> RemoveElevatedAsync(string uninstallString, CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "MultiTool", "edge-removal", Guid.NewGuid().ToString("N"));
        var tempScriptPath = Path.Combine(tempDirectory, "remove-edge.ps1");

        Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(
                tempScriptPath,
                BuildRemovalScript(uninstallString),
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
                return new EdgeRemovalResult(false, false, "Windows could not start the elevated Edge removal helper.");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                return new EdgeRemovalResult(false, false, $"The elevated Edge removal helper exited with code {process.ExitCode}.");
            }

            var after = environmentProbe();

            return after.IsInstalled
                ? new EdgeRemovalResult(false, true, "The Edge uninstaller ran, but Edge still appears to be installed. A restart may be required.")
                : new EdgeRemovalResult(true, true, "Microsoft Edge has been removed.");
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

    internal static string BuildRemovalScript(string uninstallString)
    {
        var sb = new StringBuilder();
        sb.AppendLine("$ErrorActionPreference = 'Stop'");
        sb.AppendLine();

        // Open the 32-bit registry view for Microsoft keys (same as the ave9858 script).
        sb.AppendLine("$regView = [Microsoft.Win32.RegistryView]::Registry32");
        sb.AppendLine("$microsoft = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, $regView).");
        sb.AppendLine("    OpenSubKey('SOFTWARE\\Microsoft', $true)");
        sb.AppendLine();

        // Remove experiment_control_labels that may block uninstallation.
        sb.AppendLine("$edgeClient = $microsoft.OpenSubKey('EdgeUpdate\\ClientState\\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}', $true)");
        sb.AppendLine("if ($null -ne $edgeClient -and $null -ne $edgeClient.GetValue('experiment_control_labels')) {");
        sb.AppendLine("    $edgeClient.DeleteValue('experiment_control_labels')");
        sb.AppendLine("}");
        sb.AppendLine();

        // Set AllowUninstall developer override.
        sb.AppendLine("$microsoft.CreateSubKey('EdgeUpdateDev').SetValue('AllowUninstall', '')");
        sb.AppendLine();

        // Determine a temp folder for the fake dllhost.exe.
        sb.AppendLine("$tempPath = \"$env:SystemRoot\\SystemTemp\"");
        sb.AppendLine("if (-not (Test-Path -Path $tempPath)) {");
        sb.AppendLine("    $tempPath = New-Item \"$env:SystemRoot\\Temp\\$([Guid]::NewGuid().Guid)\" -ItemType Directory");
        sb.AppendLine("}");
        sb.AppendLine("$fakeDllhostPath = \"$tempPath\\dllhost.exe\"");
        sb.AppendLine();

        // Copy cmd.exe as dllhost.exe to spoof the parent-process check.
        sb.AppendLine("Copy-Item \"$env:SystemRoot\\System32\\cmd.exe\" -Destination $fakeDllhostPath");
        sb.AppendLine();

        // Create the legacy Edge UWP stub so Windows does not re-provision Edge.
        sb.AppendLine("$edgeUWP = \"$env:SystemRoot\\SystemApps\\Microsoft.MicrosoftEdge_8wekyb3d8bbwe\"");
        sb.AppendLine("[void](New-Item $edgeUWP -ItemType Directory -ErrorVariable fail -ErrorAction SilentlyContinue)");
        sb.AppendLine("[void](New-Item \"$edgeUWP\\MicrosoftEdge.exe\" -ErrorAction Continue)");
        sb.AppendLine();

        // Run Edge's own uninstaller through the fake dllhost.exe.
        var escapedUninstallString = uninstallString.Replace("'", "''");
        sb.AppendLine($"Start-Process $fakeDllhostPath '/c {escapedUninstallString} --force-uninstall' -WindowStyle Hidden -Wait");
        sb.AppendLine();

        // Clean up temp artefacts.
        sb.AppendLine("[void](Remove-Item \"$edgeUWP\\MicrosoftEdge.exe\" -ErrorAction Continue)");
        sb.AppendLine("[void](Remove-Item $fakeDllhostPath -ErrorAction Continue)");
        sb.AppendLine("if (-not $fail) {");
        sb.AppendLine("    [void](Remove-Item $edgeUWP -ErrorAction Continue)");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static EdgeEnvironmentStatus ProbeEnvironment()
    {
        try
        {
            var regView = RegistryView.Registry32;
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView);
            using var uninstallKey = baseKey.OpenSubKey(EdgeUninstallRegistryPath);

            if (uninstallKey is null)
            {
                return new EdgeEnvironmentStatus(false, null, "Microsoft Edge is not detected on this PC.");
            }

            var uninstallString = uninstallKey.GetValue("UninstallString") as string;

            return string.IsNullOrWhiteSpace(uninstallString)
                ? new EdgeEnvironmentStatus(true, null, "Microsoft Edge appears to be installed, but no uninstall command was found.")
                : new EdgeEnvironmentStatus(true, uninstallString, "Microsoft Edge is installed.");
        }
        catch (Exception ex)
        {
            return new EdgeEnvironmentStatus(false, null, $"Unable to read Edge installation status: {ex.Message}");
        }
    }

    private static async Task<EdgeCommandResult> RunProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new EdgeCommandResult(
            process.ExitCode,
            await standardOutputTask.ConfigureAwait(false),
            await standardErrorTask.ConfigureAwait(false));
    }
}
