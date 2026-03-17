using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using Microsoft.Win32;

namespace AutoClicker.Infrastructure.Windows.Tools;

public delegate int MouseSensitivityReader();

public delegate Task MouseSensitivityWriter(int level, CancellationToken cancellationToken);

public delegate int? PrecisionTouchpadSensitivityReader();

public delegate Task PrecisionTouchpadSensitivityWriter(int threshold, CancellationToken cancellationToken);

public sealed class WindowsMouseSensitivityService : IMouseSensitivityService
{
    private const string MouseRegistryPath = @"Control Panel\Mouse";
    private const string MouseSensitivityValueName = "MouseSensitivity";
    private const string PrecisionTouchpadRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad";
    private const string PrecisionTouchpadSensitivityValueName = "AAPThreshold";
    private const int DefaultLevel = 10;
    private const int MinimumLevel = 1;
    private const int MaximumLevel = 20;
    private const int MinimumTouchpadThreshold = 0;
    private const int MaximumTouchpadThreshold = 3;
    private const uint SpiGetMouseSpeed = 0x0070;
    private const uint SpiSetMouseSpeed = 0x0071;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendChange = 0x02;

    private static readonly IReadOnlyList<int> SupportedLevels =
    [
        1, 2, 3, 4, 5,
        6, 7, 8, 9, 10,
        11, 12, 13, 14, 15,
        16, 17, 18, 19, 20,
    ];

    private readonly MouseSensitivityReader levelReader;
    private readonly MouseSensitivityWriter writer;
    private readonly PrecisionTouchpadSensitivityReader touchpadSensitivityReader;
    private readonly PrecisionTouchpadSensitivityWriter touchpadSensitivityWriter;

    public WindowsMouseSensitivityService()
        : this(
            ReadSensitivityLevel,
            WriteSensitivityLevelAsync,
            ReadPrecisionTouchpadThreshold,
            WritePrecisionTouchpadThresholdAsync)
    {
    }

    public WindowsMouseSensitivityService(MouseSensitivityReader levelReader, MouseSensitivityWriter writer)
        : this(levelReader, writer, static () => null, static (_, _) => Task.CompletedTask)
    {
    }

    public WindowsMouseSensitivityService(
        MouseSensitivityReader levelReader,
        MouseSensitivityWriter writer,
        PrecisionTouchpadSensitivityReader touchpadSensitivityReader,
        PrecisionTouchpadSensitivityWriter touchpadSensitivityWriter)
    {
        this.levelReader = levelReader;
        this.writer = writer;
        this.touchpadSensitivityReader = touchpadSensitivityReader;
        this.touchpadSensitivityWriter = touchpadSensitivityWriter;
    }

    public IReadOnlyList<int> GetSupportedLevels() => SupportedLevels;

    public MouseSensitivityStatus GetStatus()
    {
        var level = NormalizeLevel(levelReader());
        var message = $"Current mouse speed: {DescribeLevel(level)}. This changes pointer speed, not vendor-specific onboard mouse DPI.";

        var touchpadThreshold = touchpadSensitivityReader();
        if (touchpadThreshold.HasValue)
        {
            message += $" Precision touchpad sensitivity: {DescribeTouchpadThreshold(touchpadThreshold.Value)}.";
        }

        return new MouseSensitivityStatus(level, message);
    }

    public async Task<MouseSensitivityApplyResult> ApplyAsync(int level, CancellationToken cancellationToken = default)
    {
        if (!SupportedLevels.Contains(level))
        {
            return new MouseSensitivityApplyResult(
                false,
                false,
                level,
                $"Unsupported mouse sensitivity level. Choose one of these presets: {string.Join(", ", SupportedLevels)}.");
        }

        var normalizedLevel = NormalizeLevel(level);
        var before = NormalizeLevel(levelReader());
        var touchpadThresholdBefore = touchpadSensitivityReader();
        var desiredTouchpadThreshold = MapLevelToTouchpadThreshold(normalizedLevel);
        var shouldUpdateTouchpad = touchpadThresholdBefore.HasValue && touchpadThresholdBefore.Value != desiredTouchpadThreshold;

        if (before == normalizedLevel && !shouldUpdateTouchpad)
        {
            return new MouseSensitivityApplyResult(
                true,
                false,
                normalizedLevel,
                $"Mouse speed is already set to {DescribeLevel(normalizedLevel)}.");
        }

        try
        {
            var pointerSpeedChanged = before != normalizedLevel;
            if (pointerSpeedChanged)
            {
                await writer(normalizedLevel, cancellationToken).ConfigureAwait(false);
            }

            if (shouldUpdateTouchpad)
            {
                await touchpadSensitivityWriter(desiredTouchpadThreshold, cancellationToken).ConfigureAwait(false);
            }

            var changed = pointerSpeedChanged || shouldUpdateTouchpad;
            var touchpadSuffix = shouldUpdateTouchpad
                ? $" Precision touchpad sensitivity also set to {DescribeTouchpadThreshold(desiredTouchpadThreshold)}."
                : string.Empty;

            return new MouseSensitivityApplyResult(
                true,
                changed,
                normalizedLevel,
                $"Mouse speed set to {DescribeLevel(normalizedLevel)}. This affects pointer speed immediately, but hardware DPI buttons or vendor mouse software can still override the physical DPI on some mice.{touchpadSuffix}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            return new MouseSensitivityApplyResult(
                false,
                false,
                normalizedLevel,
                $"Unable to update Windows mouse sensitivity: {ex.Message}");
        }
    }

    private static int ReadSensitivityLevel()
    {
        var level = 0;

        try
        {
            if (SystemParametersInfoGet(SpiGetMouseSpeed, 0, ref level, 0) && level > 0)
            {
                return NormalizeLevel(level);
            }
        }
        catch (EntryPointNotFoundException)
        {
        }

        using var mouseKey = Registry.CurrentUser.OpenSubKey(MouseRegistryPath, writable: false);
        if (mouseKey is null)
        {
            return DefaultLevel;
        }

        return NormalizeLevel(ReadRegistryInt(mouseKey.GetValue(MouseSensitivityValueName)));
    }

    private static async Task WriteSensitivityLevelAsync(int level, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Run(
            () =>
            {
                var normalizedLevel = NormalizeLevel(level);

                using var mouseKey = Registry.CurrentUser.CreateSubKey(MouseRegistryPath, writable: true)
                    ?? throw new IOException("The current-user Mouse registry key could not be opened.");

                mouseKey.SetValue(
                    MouseSensitivityValueName,
                    normalizedLevel.ToString(CultureInfo.InvariantCulture),
                    RegistryValueKind.String);

                if (!SystemParametersInfoSet(
                        SpiSetMouseSpeed,
                        0,
                        (IntPtr)normalizedLevel,
                        SpifUpdateIniFile | SpifSendChange))
                {
                    throw new IOException(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static int? ReadPrecisionTouchpadThreshold()
    {
        using var touchpadKey = Registry.CurrentUser.OpenSubKey(PrecisionTouchpadRegistryPath, writable: false);
        if (touchpadKey is null)
        {
            return null;
        }

        var rawValue = touchpadKey.GetValue(PrecisionTouchpadSensitivityValueName);
        var parsed = ReadRegistryInt(rawValue);
        if (parsed < MinimumTouchpadThreshold || parsed > MaximumTouchpadThreshold)
        {
            return null;
        }

        return parsed;
    }

    private static async Task WritePrecisionTouchpadThresholdAsync(int threshold, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Run(
            () =>
            {
                var normalizedThreshold = NormalizeTouchpadThreshold(threshold);
                using var touchpadKey = Registry.CurrentUser.OpenSubKey(PrecisionTouchpadRegistryPath, writable: true);
                if (touchpadKey is null)
                {
                    return;
                }

                touchpadKey.SetValue(
                    PrecisionTouchpadSensitivityValueName,
                    normalizedThreshold,
                    RegistryValueKind.DWord);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static int NormalizeLevel(int level)
    {
        if (level < MinimumLevel)
        {
            return MinimumLevel;
        }

        if (level > MaximumLevel)
        {
            return MaximumLevel;
        }

        return level;
    }

    private static string DescribeLevel(int level)
    {
        var normalizedLevel = NormalizeLevel(level);
        var feelLabel = normalizedLevel switch
        {
            <= 4 => "Very Slow",
            <= 8 => "Slow",
            <= 12 => "Balanced",
            <= 16 => "Fast",
            _ => "Very Fast",
        };

        return normalizedLevel == DefaultLevel
            ? $"{feelLabel} ({normalizedLevel}/20, Windows middle)"
            : $"{feelLabel} ({normalizedLevel}/20)";
    }

    private static int MapLevelToTouchpadThreshold(int level)
    {
        var normalized = NormalizeLevel(level);
        return normalized switch
        {
            <= 5 => 3,
            <= 10 => 2,
            <= 15 => 1,
            _ => 0,
        };
    }

    private static int NormalizeTouchpadThreshold(int threshold)
    {
        if (threshold < MinimumTouchpadThreshold)
        {
            return MinimumTouchpadThreshold;
        }

        if (threshold > MaximumTouchpadThreshold)
        {
            return MaximumTouchpadThreshold;
        }

        return threshold;
    }

    private static string DescribeTouchpadThreshold(int threshold)
    {
        return NormalizeTouchpadThreshold(threshold) switch
        {
            0 => "Most sensitive",
            1 => "High sensitivity",
            2 => "Medium sensitivity",
            _ => "Low sensitivity",
        };
    }

    private static int ReadRegistryInt(object? value) =>
        value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            string stringValue when int.TryParse(stringValue, out var parsedValue) => parsedValue,
            _ => DefaultLevel,
        };

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    private static extern bool SystemParametersInfoGet(uint action, uint param, ref int value, uint winIni);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    private static extern bool SystemParametersInfoSet(uint action, uint param, IntPtr value, uint winIni);
}
