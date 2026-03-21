using System.Buffers.Binary;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MultiTool.Infrastructure.Windows.Interop;

namespace MultiTool.Infrastructure.Windows.Tools;

internal sealed record DriveSmartDirectReadResult(
    DriveSmartDataSnapshot? SmartData,
    DriveSmartThresholdSnapshot? SmartThresholds,
    IReadOnlyList<DriveSmartAttributeEvaluation> Attributes,
    string AccessPath);

internal static class WindowsDriveSmartPassthroughReader
{
    private const int SmartSectorSize = 512;
    private const int SenseBufferSize = 32;
    private const uint PassThroughTimeoutSeconds = 10;

    private const byte SmartCommand = 0xB0;
    private const byte SmartReadDataFeature = 0xD0;
    private const byte SmartReadThresholdsFeature = 0xD1;
    private const byte SmartCylinderLow = 0x4F;
    private const byte SmartCylinderHigh = 0xC2;

    private static readonly byte[] CandidateDeviceHeads = [0x00, 0xA0, 0x40];

    internal static DriveSmartDirectReadResult? TryRead(
        DriveSmartDiskSnapshot drive,
        DriveSmartPhysicalDiskSnapshot? physicalDisk,
        ICollection<string> warnings)
    {
        foreach (var protocol in BuildFallbackOrder(drive, physicalDisk))
        {
            DriveSmartDirectReadResult? result = protocol switch
            {
                SmartFallbackProtocol.Nvme => TryReadNvmeHealthLog(drive, warnings),
                SmartFallbackProtocol.Ata => TryReadAtaSmartData(drive, warnings),
                SmartFallbackProtocol.Sat => TryReadSatSmartData(drive, warnings),
                _ => null,
            };

            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    internal static byte[] BuildSatSmartReadCdb(byte feature, byte sectorNumber, byte deviceHead)
    {
        return
        [
            0x85,
            0x08,
            0x0E,
            0x00,
            feature,
            0x00,
            0x01,
            0x00,
            sectorNumber,
            0x00,
            SmartCylinderLow,
            0x00,
            SmartCylinderHigh,
            deviceHead,
            SmartCommand,
            0x00,
        ];
    }

    internal static IReadOnlyList<DriveSmartAttributeEvaluation> BuildNvmeHealthAttributes(byte[] healthLog)
    {
        if (healthLog.Length < SmartSectorSize)
        {
            return [];
        }

        List<DriveSmartAttributeEvaluation> attributes = [];

        var criticalWarning = healthLog[0];
        var temperatureKelvin = BinaryPrimitives.ReadUInt16LittleEndian(healthLog.AsSpan(1, 2));
        var temperatureCelsius = temperatureKelvin > 273 ? temperatureKelvin - 273 : 0;
        var availableSpare = healthLog[3];
        var availableSpareThreshold = healthLog[4];
        var percentageUsed = healthLog[5];

        attributes.Add(BuildNvmeCriticalWarningEvaluation(criticalWarning, temperatureCelsius));
        attributes.Add(BuildNvmeTemperatureEvaluation(temperatureKelvin, temperatureCelsius, (criticalWarning & 0x02) != 0));
        attributes.Add(BuildNvmeAvailableSpareEvaluation(availableSpare, availableSpareThreshold, (criticalWarning & 0x01) != 0));
        attributes.Add(BuildNvmePercentageUsedEvaluation(percentageUsed));
        attributes.Add(BuildNvmeInfoEvaluation("05", "Data Units Read", ReadUnsignedInteger(healthLog.AsSpan(32, 16)), includeByteEstimate: true));
        attributes.Add(BuildNvmeInfoEvaluation("06", "Data Units Written", ReadUnsignedInteger(healthLog.AsSpan(48, 16)), includeByteEstimate: true));
        attributes.Add(BuildNvmeInfoEvaluation("07", "Host Read Commands", ReadUnsignedInteger(healthLog.AsSpan(64, 16))));
        attributes.Add(BuildNvmeInfoEvaluation("08", "Host Write Commands", ReadUnsignedInteger(healthLog.AsSpan(80, 16))));
        attributes.Add(BuildNvmeInfoEvaluation("09", "Power Cycle Count", ReadUnsignedInteger(healthLog.AsSpan(112, 16))));
        attributes.Add(BuildNvmeInfoEvaluation("0A", "Power-On Hours", ReadUnsignedInteger(healthLog.AsSpan(128, 16))));
        attributes.Add(BuildNvmeInfoEvaluation("0B", "Unsafe Shutdown Count", ReadUnsignedInteger(healthLog.AsSpan(144, 16))));
        attributes.Add(BuildNvmeCountEvaluation("0C", "Media Errors", ReadUnsignedInteger(healthLog.AsSpan(160, 16))));
        attributes.Add(BuildNvmeInfoEvaluation("0D", "Error Log Entry Count", ReadUnsignedInteger(healthLog.AsSpan(176, 16))));

        return attributes;
    }

    private static IEnumerable<SmartFallbackProtocol> BuildFallbackOrder(
        DriveSmartDiskSnapshot drive,
        DriveSmartPhysicalDiskSnapshot? physicalDisk)
    {
        var hint = string.Join(
            " | ",
            new[]
            {
                physicalDisk?.BusType,
                drive.InterfaceType,
                drive.PnpDeviceId,
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        List<SmartFallbackProtocol> orderedProtocols = [];
        if (hint.Contains("NVME", StringComparison.OrdinalIgnoreCase))
        {
            orderedProtocols.Add(SmartFallbackProtocol.Nvme);
        }

        if (hint.Contains("USB", StringComparison.OrdinalIgnoreCase) ||
            hint.Contains("USBSTOR", StringComparison.OrdinalIgnoreCase))
        {
            orderedProtocols.Add(SmartFallbackProtocol.Sat);
        }

        if (hint.Contains("ATA", StringComparison.OrdinalIgnoreCase) ||
            hint.Contains("SATA", StringComparison.OrdinalIgnoreCase) ||
            hint.Contains("IDE", StringComparison.OrdinalIgnoreCase))
        {
            orderedProtocols.Add(SmartFallbackProtocol.Ata);
        }

        orderedProtocols.Add(SmartFallbackProtocol.Nvme);
        orderedProtocols.Add(SmartFallbackProtocol.Ata);
        orderedProtocols.Add(SmartFallbackProtocol.Sat);

        return orderedProtocols.Distinct();
    }

    private static DriveSmartDirectReadResult? TryReadNvmeHealthLog(DriveSmartDiskSnapshot drive, ICollection<string> warnings)
    {
        using var handle = OpenPhysicalDriveHandleForQuery(drive.DeviceId, out var errorCode);
        if (handle.IsInvalid)
        {
            TryAddAccessDeniedWarning(warnings, errorCode, "NVMe SMART passthrough");
            return null;
        }

        var query = new StorageIoctl.StoragePropertyQueryWithProtocolData
        {
            PropertyId = StorageIoctl.StorageDeviceProtocolSpecificProperty,
            QueryType = StorageIoctl.PropertyStandardQuery,
            ProtocolSpecificData = new StorageIoctl.StorageProtocolSpecificData
            {
                ProtocolType = StorageIoctl.ProtocolTypeNvme,
                DataType = StorageIoctl.NvmeDataTypeLogPage,
                ProtocolDataRequestValue = StorageIoctl.NvmeHealthInfoLogPageId,
                ProtocolDataRequestSubValue = 0,
                ProtocolDataOffset = (uint)Marshal.SizeOf<StorageIoctl.StorageProtocolSpecificData>(),
                ProtocolDataLength = SmartSectorSize,
                FixedProtocolReturnData = 0,
                ProtocolDataRequestSubValue2 = 0,
                ProtocolDataRequestSubValue3 = 0,
                ProtocolDataRequestSubValue4 = 0,
            },
        };

        var inputSize = Marshal.SizeOf<StorageIoctl.StoragePropertyQueryWithProtocolData>();
        var descriptorSize = Marshal.SizeOf<StorageIoctl.StorageProtocolDataDescriptor>();
        byte[] buffer = new byte[descriptorSize + SmartSectorSize];
        WriteStructure(query, buffer);

        if (!StorageIoctl.DeviceIoControl(
                handle,
                StorageIoctl.IoctlStorageQueryProperty,
                buffer,
                inputSize,
                buffer,
                buffer.Length,
                out var bytesReturned,
                nint.Zero))
        {
            TryAddAccessDeniedWarning(warnings, Marshal.GetLastWin32Error(), "NVMe SMART passthrough");
            return null;
        }

        if (bytesReturned < descriptorSize)
        {
            return null;
        }

        var descriptor = ReadStructure<StorageIoctl.StorageProtocolDataDescriptor>(buffer);
        var protocolDataOffsetBase = Marshal.OffsetOf<StorageIoctl.StorageProtocolDataDescriptor>(nameof(StorageIoctl.StorageProtocolDataDescriptor.ProtocolSpecificData)).ToInt32();
        var dataOffset = protocolDataOffsetBase + (int)descriptor.ProtocolSpecificData.ProtocolDataOffset;
        var availableLength = Math.Min((int)descriptor.ProtocolSpecificData.ProtocolDataLength, buffer.Length - dataOffset);
        if (dataOffset < 0 || availableLength < SmartSectorSize || dataOffset + SmartSectorSize > buffer.Length)
        {
            return null;
        }

        byte[] healthLog = new byte[SmartSectorSize];
        Array.Copy(buffer, dataOffset, healthLog, 0, SmartSectorSize);

        var attributes = BuildNvmeHealthAttributes(healthLog);
        return attributes.Count == 0
            ? null
            : new DriveSmartDirectReadResult(null, null, attributes, "NVMe protocol query");
    }

    private static DriveSmartDirectReadResult? TryReadAtaSmartData(DriveSmartDiskSnapshot drive, ICollection<string> warnings)
    {
        using var handle = OpenPhysicalDriveHandleForPassthrough(drive.DeviceId, out var errorCode);
        if (handle.IsInvalid)
        {
            TryAddAccessDeniedWarning(warnings, errorCode, "ATA SMART passthrough");
            return null;
        }

        foreach (var deviceHead in CandidateDeviceHeads)
        {
            if (!TryReadAtaSmartSector(handle, SmartReadDataFeature, 0, deviceHead, out var smartData))
            {
                continue;
            }

            var resolvedSmartData = smartData;
            if (resolvedSmartData is null)
            {
                continue;
            }

            byte[]? smartThresholds = null;
            TryReadAtaSmartSector(handle, SmartReadThresholdsFeature, 1, deviceHead, out smartThresholds);
            if (!LooksLikeSmartSector(resolvedSmartData))
            {
                continue;
            }

            return new DriveSmartDirectReadResult(
                new DriveSmartDataSnapshot(drive.DeviceId, resolvedSmartData),
                smartThresholds is not null && LooksLikeSmartSector(smartThresholds)
                    ? new DriveSmartThresholdSnapshot(drive.DeviceId, smartThresholds)
                    : null,
                [],
                "ATA pass-through");
        }

        return null;
    }

    private static DriveSmartDirectReadResult? TryReadSatSmartData(DriveSmartDiskSnapshot drive, ICollection<string> warnings)
    {
        using var handle = OpenPhysicalDriveHandleForPassthrough(drive.DeviceId, out var errorCode);
        if (handle.IsInvalid)
        {
            TryAddAccessDeniedWarning(warnings, errorCode, "USB SAT SMART passthrough");
            return null;
        }

        foreach (var deviceHead in CandidateDeviceHeads)
        {
            if (!TryReadSatSmartSector(handle, SmartReadDataFeature, 0, deviceHead, out var smartData))
            {
                continue;
            }

            var resolvedSmartData = smartData;
            if (resolvedSmartData is null)
            {
                continue;
            }

            byte[]? smartThresholds = null;
            TryReadSatSmartSector(handle, SmartReadThresholdsFeature, 1, deviceHead, out smartThresholds);
            if (!LooksLikeSmartSector(resolvedSmartData))
            {
                continue;
            }

            return new DriveSmartDirectReadResult(
                new DriveSmartDataSnapshot(drive.DeviceId, resolvedSmartData),
                smartThresholds is not null && LooksLikeSmartSector(smartThresholds)
                    ? new DriveSmartThresholdSnapshot(drive.DeviceId, smartThresholds)
                    : null,
                [],
                "USB SAT pass-through");
        }

        return null;
    }

    private static bool TryReadAtaSmartSector(
        SafeFileHandle handle,
        byte feature,
        byte sectorNumber,
        byte deviceHead,
        out byte[]? sectorData)
    {
        var passThrough = new StorageIoctl.AtaPassThroughEx
        {
            Length = (ushort)Marshal.SizeOf<StorageIoctl.AtaPassThroughEx>(),
            AtaFlags = StorageIoctl.AtaFlagsDataIn | StorageIoctl.AtaFlagsDrdyRequired,
            PathId = 0,
            TargetId = 0,
            Lun = 0,
            ReservedAsUchar = 0,
            DataTransferLength = SmartSectorSize,
            TimeOutValue = PassThroughTimeoutSeconds,
            ReservedAsUlong = 0,
            DataBufferOffset = (nuint)Marshal.SizeOf<StorageIoctl.AtaPassThroughEx>(),
            PreviousTaskFile = new byte[8],
            CurrentTaskFile =
            [
                feature,
                0x01,
                sectorNumber,
                SmartCylinderLow,
                SmartCylinderHigh,
                deviceHead,
                SmartCommand,
                0x00,
            ],
        };

        var structSize = Marshal.SizeOf<StorageIoctl.AtaPassThroughEx>();
        byte[] buffer = new byte[structSize + SmartSectorSize];
        WriteStructure(passThrough, buffer);

        if (!StorageIoctl.DeviceIoControl(
                handle,
                StorageIoctl.IoctlAtaPassThrough,
                buffer,
                buffer.Length,
                buffer,
                buffer.Length,
                out _,
                nint.Zero))
        {
            sectorData = null;
            return false;
        }

        sectorData = new byte[SmartSectorSize];
        Array.Copy(buffer, structSize, sectorData, 0, SmartSectorSize);
        return true;
    }

    private static bool TryReadSatSmartSector(
        SafeFileHandle handle,
        byte feature,
        byte sectorNumber,
        byte deviceHead,
        out byte[]? sectorData)
    {
        var structSize = Marshal.SizeOf<StorageIoctl.ScsiPassThrough>();
        var dataOffset = structSize + SenseBufferSize;
        var passThrough = new StorageIoctl.ScsiPassThrough
        {
            Length = (ushort)structSize,
            ScsiStatus = 0,
            PathId = 0,
            TargetId = 0,
            Lun = 0,
            CdbLength = 16,
            SenseInfoLength = SenseBufferSize,
            DataIn = StorageIoctl.ScsiIoctlDataIn,
            DataTransferLength = SmartSectorSize,
            TimeOutValue = PassThroughTimeoutSeconds,
            DataBufferOffset = (nuint)dataOffset,
            SenseInfoOffset = (uint)structSize,
            Cdb = BuildSatSmartReadCdb(feature, sectorNumber, deviceHead),
        };

        byte[] buffer = new byte[dataOffset + SmartSectorSize];
        WriteStructure(passThrough, buffer);

        if (!StorageIoctl.DeviceIoControl(
                handle,
                StorageIoctl.IoctlScsiPassThrough,
                buffer,
                buffer.Length,
                buffer,
                buffer.Length,
                out _,
                nint.Zero))
        {
            sectorData = null;
            return false;
        }

        sectorData = new byte[SmartSectorSize];
        Array.Copy(buffer, dataOffset, sectorData, 0, SmartSectorSize);
        return true;
    }

    private static SafeFileHandle OpenPhysicalDriveHandleForQuery(string deviceId, out int errorCode)
    {
        var handle = StorageIoctl.CreateFile(
            deviceId,
            0,
            StorageIoctl.FileShareRead | StorageIoctl.FileShareWrite,
            nint.Zero,
            StorageIoctl.OpenExisting,
            0,
            nint.Zero);

        if (!handle.IsInvalid)
        {
            errorCode = 0;
            return handle;
        }

        errorCode = Marshal.GetLastWin32Error();
        handle.Dispose();
        return OpenPhysicalDriveHandleForPassthrough(deviceId, out errorCode);
    }

    private static SafeFileHandle OpenPhysicalDriveHandleForPassthrough(string deviceId, out int errorCode)
    {
        var handle = StorageIoctl.CreateFile(
            deviceId,
            StorageIoctl.GenericRead | StorageIoctl.GenericWrite,
            StorageIoctl.FileShareRead | StorageIoctl.FileShareWrite,
            nint.Zero,
            StorageIoctl.OpenExisting,
            0,
            nint.Zero);

        errorCode = handle.IsInvalid ? Marshal.GetLastWin32Error() : 0;
        return handle;
    }

    private static bool LooksLikeSmartSector(byte[]? sectorData)
    {
        if (sectorData is null || sectorData.Length < 14 || !sectorData.Any(static value => value != 0))
        {
            return false;
        }

        for (var offset = 2; offset + 11 < Math.Min(sectorData.Length, 362); offset += 12)
        {
            if (sectorData[offset] != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void TryAddAccessDeniedWarning(ICollection<string> warnings, int errorCode, string context)
    {
        if (errorCode != 5)
        {
            return;
        }

        var message = $"{context}: Access denied. Start MultiTool as administrator to try direct SMART passthrough on this PC.";
        if (!warnings.Contains(message))
        {
            warnings.Add(message);
        }
    }

    private static DriveSmartAttributeEvaluation BuildNvmeCriticalWarningEvaluation(byte criticalWarning, int temperatureCelsius)
    {
        if (criticalWarning == 0)
        {
            return new DriveSmartAttributeEvaluation(
                "01",
                "OK",
                "Critical Warning Flags",
                "0x00",
                SmartAttributeSeverity.Ok,
                "NVMe critical warning flags are clear.");
        }

        List<string> activeFlags = [];
        var severity = SmartAttributeSeverity.Warning;
        if ((criticalWarning & 0x01) != 0)
        {
            activeFlags.Add("Available spare low");
        }

        if ((criticalWarning & 0x02) != 0)
        {
            activeFlags.Add(temperatureCelsius > 0 ? $"Temperature threshold reached at {temperatureCelsius} C" : "Temperature threshold reached");
        }

        if ((criticalWarning & 0x04) != 0)
        {
            activeFlags.Add("Reliability degraded");
            severity = SmartAttributeSeverity.Critical;
        }

        if ((criticalWarning & 0x08) != 0)
        {
            activeFlags.Add("Drive entered read-only mode");
            severity = SmartAttributeSeverity.Critical;
        }

        if ((criticalWarning & 0x10) != 0)
        {
            activeFlags.Add("Volatile memory backup failed");
            severity = SmartAttributeSeverity.Critical;
        }

        var status = severity == SmartAttributeSeverity.Critical ? "Critical" : "Warning";
        return new DriveSmartAttributeEvaluation(
            "01",
            status,
            "Critical Warning Flags",
            $"0x{criticalWarning:X2}",
            severity,
            string.Join(". ", activeFlags) + ".");
    }

    private static DriveSmartAttributeEvaluation BuildNvmeTemperatureEvaluation(ushort temperatureKelvin, int temperatureCelsius, bool thresholdTriggered)
    {
        var rawData = temperatureCelsius > 0
            ? $"{temperatureCelsius} C ({temperatureKelvin} K)"
            : $"{temperatureKelvin} K";

        if (temperatureCelsius >= 80)
        {
            return new DriveSmartAttributeEvaluation("02", "Critical", "Temperature", rawData, SmartAttributeSeverity.Critical, $"Drive temperature reached {temperatureCelsius} C.");
        }

        if (temperatureCelsius >= 70 || thresholdTriggered)
        {
            var note = temperatureCelsius > 0
                ? $"Drive temperature is high at {temperatureCelsius} C."
                : "The NVMe controller reported a temperature threshold warning.";
            return new DriveSmartAttributeEvaluation("02", "Warning", "Temperature", rawData, SmartAttributeSeverity.Warning, note);
        }

        if (temperatureCelsius > 0)
        {
            return new DriveSmartAttributeEvaluation("02", "OK", "Temperature", rawData, SmartAttributeSeverity.Ok, $"Drive temperature is {temperatureCelsius} C.");
        }

        return new DriveSmartAttributeEvaluation("02", "Info", "Temperature", rawData, SmartAttributeSeverity.Info, string.Empty);
    }

    private static DriveSmartAttributeEvaluation BuildNvmeAvailableSpareEvaluation(byte availableSpare, byte threshold, bool thresholdTriggered)
    {
        var rawData = $"{availableSpare}% (threshold {threshold}%)";
        if (availableSpare == 0 || availableSpare <= Math.Max(1, threshold / 2))
        {
            return new DriveSmartAttributeEvaluation(
                "03",
                "Critical",
                "Available Spare",
                rawData,
                SmartAttributeSeverity.Critical,
                $"Available reserved space is very low at about {availableSpare}%.");
        }

        if (thresholdTriggered || (threshold > 0 && availableSpare <= threshold))
        {
            return new DriveSmartAttributeEvaluation(
                "03",
                "Warning",
                "Available Spare",
                rawData,
                SmartAttributeSeverity.Warning,
                $"Available reserved space is getting low at about {availableSpare}%.");
        }

        return new DriveSmartAttributeEvaluation(
            "03",
            "OK",
            "Available Spare",
            rawData,
            SmartAttributeSeverity.Ok,
            $"Available reserved space still looks healthy at about {availableSpare}%.");
    }

    private static DriveSmartAttributeEvaluation BuildNvmePercentageUsedEvaluation(byte percentageUsed)
    {
        var remainingLife = Math.Max(0, 100 - percentageUsed);
        var rawData = $"{percentageUsed}% used";
        if (remainingLife <= 10)
        {
            return new DriveSmartAttributeEvaluation(
                "04",
                "Critical",
                "Percentage Used",
                rawData,
                SmartAttributeSeverity.Critical,
                $"Remaining SSD life is very low at about {remainingLife}%.");
        }

        if (remainingLife <= 20)
        {
            return new DriveSmartAttributeEvaluation(
                "04",
                "Warning",
                "Percentage Used",
                rawData,
                SmartAttributeSeverity.Warning,
                $"Remaining SSD life is getting low at about {remainingLife}%.");
        }

        return new DriveSmartAttributeEvaluation(
            "04",
            "OK",
            "Percentage Used",
            rawData,
            SmartAttributeSeverity.Ok,
            $"Remaining SSD life still looks healthy at about {remainingLife}%.");
    }

    private static DriveSmartAttributeEvaluation BuildNvmeInfoEvaluation(
        string id,
        string description,
        BigInteger value,
        bool includeByteEstimate = false)
    {
        var rawData = includeByteEstimate
            ? $"{value} data units (~{FormatApproximateBytes(value * 512000)})"
            : FormatBigIntegerRaw(value);
        return new DriveSmartAttributeEvaluation(id, value == 0 ? "OK" : "Info", description, rawData, value == 0 ? SmartAttributeSeverity.Ok : SmartAttributeSeverity.Info, string.Empty);
    }

    private static DriveSmartAttributeEvaluation BuildNvmeCountEvaluation(string id, string description, BigInteger value)
    {
        if (value == 0)
        {
            return new DriveSmartAttributeEvaluation(id, "OK", description, FormatBigIntegerRaw(value), SmartAttributeSeverity.Ok, $"{description} is clear.");
        }

        if (value < 10)
        {
            return new DriveSmartAttributeEvaluation(id, "Warning", description, FormatBigIntegerRaw(value), SmartAttributeSeverity.Warning, $"{description} is non-zero and worth watching.");
        }

        return new DriveSmartAttributeEvaluation(id, "Critical", description, FormatBigIntegerRaw(value), SmartAttributeSeverity.Critical, $"{description} is elevated and points to real media or I/O trouble.");
    }

    private static BigInteger ReadUnsignedInteger(ReadOnlySpan<byte> bytes) =>
        bytes.Length == 0 ? BigInteger.Zero : new BigInteger(bytes, isUnsigned: true, isBigEndian: false);

    private static string FormatBigIntegerRaw(BigInteger value) =>
        value.IsZero ? "0 (0x0)" : $"{value} (0x{value.ToString("X")})";

    private static string FormatApproximateBytes(BigInteger bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
        var unitIndex = 0;
        var current = bytes;
        while (current >= 1024 && unitIndex < units.Length - 1)
        {
            current /= 1024;
            unitIndex++;
        }

        return $"{current} {units[unitIndex]}";
    }

    private static void WriteStructure<T>(T value, byte[] buffer, int offset = 0)
        where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var pointer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(value, pointer, false);
            Marshal.Copy(pointer, buffer, offset, size);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private static T ReadStructure<T>(byte[] buffer, int offset = 0)
        where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var pointer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(buffer, offset, pointer, size);
            return Marshal.PtrToStructure<T>(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private enum SmartFallbackProtocol
    {
        Nvme,
        Ata,
        Sat,
    }
}
