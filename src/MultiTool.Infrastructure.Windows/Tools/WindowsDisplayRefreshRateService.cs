using System.Runtime.InteropServices;
using MultiTool.Core.Models;
using MultiTool.Core.Services;

namespace MultiTool.Infrastructure.Windows.Tools;

public delegate IReadOnlyList<DisplayRefreshRecommendation> DisplayRefreshRecommendationReader();

public delegate IReadOnlyList<DisplayRefreshApplyResult> DisplayRefreshApplier();

public sealed class WindowsDisplayRefreshRateService : IDisplayRefreshRateService
{
    private const int EnumCurrentSettings = -1;
    private const int AttachedToDesktopFlag = 0x00000001;
    private const int MirroringDriverFlag = 0x00000008;
    private const int CdsUpdateregistry = 0x00000001;
    private const int CdsNoReset = 0x10000000;
    private const int DispChangeSuccessful = 0;

    private readonly DisplayRefreshRecommendationReader recommendationReader;
    private readonly DisplayRefreshApplier applier;

    public WindowsDisplayRefreshRateService()
        : this(GetRecommendationsInternal, ApplyRecommendedInternal)
    {
    }

    public WindowsDisplayRefreshRateService(
        DisplayRefreshRecommendationReader recommendationReader,
        DisplayRefreshApplier applier)
    {
        this.recommendationReader = recommendationReader;
        this.applier = applier;
    }

    public Task<IReadOnlyList<DisplayRefreshRecommendation>> GetRecommendationsAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => recommendationReader(), cancellationToken);

    public Task<IReadOnlyList<DisplayRefreshApplyResult>> ApplyRecommendedAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => applier(), cancellationToken);

    private static IReadOnlyList<DisplayRefreshRecommendation> GetRecommendationsInternal() =>
        [.. EnumerateDisplayTargets().Select(BuildRecommendation)];

    private static IReadOnlyList<DisplayRefreshApplyResult> ApplyRecommendedInternal()
    {
        var targets = EnumerateDisplayTargets();
        var results = new List<DisplayRefreshApplyResult>(targets.Count);
        var stagedChanges = new List<DisplayTarget>();

        foreach (var target in targets)
        {
            if (!target.NeedsChange || target.RecommendedMode is null)
            {
                results.Add(
                    new DisplayRefreshApplyResult(
                        target.DeviceName,
                        target.DisplayName,
                        true,
                        false,
                        "Already at the top refresh rate for the current resolution."));
                continue;
            }

            var recommendedMode = target.RecommendedMode.Value;
            var stageResult = ChangeDisplaySettingsEx(
                target.DeviceName,
                ref recommendedMode,
                IntPtr.Zero,
                CdsUpdateregistry | CdsNoReset,
                IntPtr.Zero);

            if (stageResult == DispChangeSuccessful)
            {
                stagedChanges.Add(target);
                continue;
            }

            results.Add(
                new DisplayRefreshApplyResult(
                    target.DeviceName,
                    target.DisplayName,
                    false,
                    false,
                    $"Unable to stage {target.TargetFrequency} Hz: {DescribeDisplayChangeResult(stageResult)}"));
        }

        if (stagedChanges.Count > 0)
        {
            var applyResult = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
            var applySucceeded = applyResult == DispChangeSuccessful;
            foreach (var target in stagedChanges)
            {
                results.Add(
                    new DisplayRefreshApplyResult(
                        target.DeviceName,
                        target.DisplayName,
                        applySucceeded,
                        applySucceeded,
                        applySucceeded
                            ? $"Switched to {target.TargetFrequency} Hz."
                            : $"Unable to apply staged change: {DescribeDisplayChangeResult(applyResult)}"));
            }
        }

        return results
            .OrderBy(static result => result.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static List<DisplayTarget> EnumerateDisplayTargets()
    {
        var targets = new List<DisplayTarget>();

        for (var deviceIndex = 0; ; deviceIndex++)
        {
            var adapter = CreateDisplayDevice();
            if (!EnumDisplayDevices(null, deviceIndex, ref adapter, 0))
            {
                break;
            }

            if ((adapter.StateFlags & AttachedToDesktopFlag) == 0 || (adapter.StateFlags & MirroringDriverFlag) != 0)
            {
                continue;
            }

            var currentMode = CreateDevMode();
            if (!EnumDisplaySettingsEx(adapter.DeviceName, EnumCurrentSettings, ref currentMode, 0))
            {
                continue;
            }

            var monitorName = adapter.DeviceString;
            var monitor = CreateDisplayDevice();
            if (EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0) && !string.IsNullOrWhiteSpace(monitor.DeviceString))
            {
                monitorName = monitor.DeviceString;
            }

            var currentFrequency = NormalizeFrequency(currentMode.dmDisplayFrequency);
            var bestMode = default(DEVMODE?);
            var bestFrequency = currentFrequency;

            for (var modeIndex = 0; ; modeIndex++)
            {
                var candidateMode = CreateDevMode();
                if (!EnumDisplaySettingsEx(adapter.DeviceName, modeIndex, ref candidateMode, 0))
                {
                    break;
                }

                if (!MatchesCurrentMode(candidateMode, currentMode))
                {
                    continue;
                }

                var candidateFrequency = NormalizeFrequency(candidateMode.dmDisplayFrequency);
                if (candidateFrequency > bestFrequency)
                {
                    bestFrequency = candidateFrequency;
                    bestMode = candidateMode;
                }
            }

            var resolution = $"{currentMode.dmPelsWidth} x {currentMode.dmPelsHeight}";
            targets.Add(
                new DisplayTarget(
                    adapter.DeviceName,
                    $"{monitorName} ({adapter.DeviceName})",
                    resolution,
                    currentFrequency,
                    bestFrequency,
                    bestMode));
        }

        return targets
            .OrderBy(static target => target.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DisplayRefreshRecommendation BuildRecommendation(DisplayTarget target)
    {
        var message = target.NeedsChange
            ? $"Ready to switch from {FormatFrequency(target.CurrentFrequency)} to {FormatFrequency(target.TargetFrequency)}."
            : "Already at the top refresh rate for the current resolution.";

        return new DisplayRefreshRecommendation(
            target.DeviceName,
            target.DisplayName,
            target.Resolution,
            target.CurrentFrequency,
            target.TargetFrequency,
            target.NeedsChange,
            message);
    }

    private static bool MatchesCurrentMode(DEVMODE candidateMode, DEVMODE currentMode) =>
        candidateMode.dmPelsWidth == currentMode.dmPelsWidth &&
        candidateMode.dmPelsHeight == currentMode.dmPelsHeight &&
        candidateMode.dmBitsPerPel == currentMode.dmBitsPerPel &&
        candidateMode.dmDisplayFlags == currentMode.dmDisplayFlags &&
        candidateMode.dmDisplayOrientation == currentMode.dmDisplayOrientation;

    private static int NormalizeFrequency(int frequency) =>
        frequency <= 1 ? 0 : frequency;

    private static string FormatFrequency(int frequency) =>
        frequency <= 1 ? "default" : $"{frequency} Hz";

    private static DISPLAY_DEVICE CreateDisplayDevice() =>
        new()
        {
            cb = Marshal.SizeOf<DISPLAY_DEVICE>(),
        };

    private static DEVMODE CreateDevMode()
    {
        var mode = new DEVMODE();
        mode.dmDeviceName = new string('\0', 32);
        mode.dmFormName = new string('\0', 32);
        mode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
        return mode;
    }

    private static string DescribeDisplayChangeResult(int result) =>
        result switch
        {
            -6 => "bad dual-view mode",
            -5 => "bad display mode",
            -4 => "driver failed",
            -3 => "not updated in registry",
            -2 => "bad flags",
            -1 => "bad parameter",
            1 => "restart required",
            _ => $"error {result}",
        };

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, int iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettingsEx(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode, int dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, int dwflags, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
    private static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, int dwflags, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public int StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;

        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;

        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    private sealed record DisplayTarget(
        string DeviceName,
        string DisplayName,
        string Resolution,
        int CurrentFrequency,
        int TargetFrequency,
        DEVMODE? RecommendedMode)
    {
        public bool NeedsChange => TargetFrequency > CurrentFrequency && RecommendedMode is not null;
    }
}
