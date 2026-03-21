using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MultiTool.Infrastructure.Windows.Interop;

internal static class StorageIoctl
{
    internal const uint GenericRead = 0x80000000;
    internal const uint GenericWrite = 0x40000000;
    internal const uint FileShareRead = 0x00000001;
    internal const uint FileShareWrite = 0x00000002;
    internal const uint OpenExisting = 3;

    internal const uint IoctlStorageQueryProperty = (FileDeviceMassStorage << 16) | (FileAnyAccess << 14) | (0x0500 << 2) | MethodBuffered;
    internal const uint IoctlAtaPassThrough = (FileDeviceController << 16) | ((FileReadAccess | FileWriteAccess) << 14) | (0x040b << 2) | MethodBuffered;
    internal const uint IoctlScsiPassThrough = (FileDeviceController << 16) | ((FileReadAccess | FileWriteAccess) << 14) | (0x0401 << 2) | MethodBuffered;

    internal const uint PropertyStandardQuery = 0;
    internal const uint StorageDeviceProtocolSpecificProperty = 50;

    internal const uint ProtocolTypeAta = 2;
    internal const uint ProtocolTypeNvme = 3;

    internal const uint NvmeDataTypeLogPage = 2;
    internal const uint NvmeHealthInfoLogPageId = 0x02;

    internal const ushort AtaFlagsDrdyRequired = 1 << 0;
    internal const ushort AtaFlagsDataIn = 1 << 1;

    internal const byte ScsiIoctlDataIn = 1;

    private const uint FileDeviceMassStorage = 0x0000002d;
    private const uint FileDeviceController = 0x00000004;
    private const uint MethodBuffered = 0;
    private const uint FileAnyAccess = 0;
    private const uint FileReadAccess = 0x0001;
    private const uint FileWriteAccess = 0x0002;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        nint lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        nint hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        [In] byte[] lpInBuffer,
        int nInBufferSize,
        [Out] byte[] lpOutBuffer,
        int nOutBufferSize,
        out int lpBytesReturned,
        nint lpOverlapped);

    [StructLayout(LayoutKind.Sequential)]
    internal struct StorageProtocolSpecificData
    {
        public uint ProtocolType;
        public uint DataType;
        public uint ProtocolDataRequestValue;
        public uint ProtocolDataRequestSubValue;
        public uint ProtocolDataOffset;
        public uint ProtocolDataLength;
        public uint FixedProtocolReturnData;
        public uint ProtocolDataRequestSubValue2;
        public uint ProtocolDataRequestSubValue3;
        public uint ProtocolDataRequestSubValue4;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StoragePropertyQueryWithProtocolData
    {
        public uint PropertyId;
        public uint QueryType;
        public StorageProtocolSpecificData ProtocolSpecificData;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StorageProtocolDataDescriptor
    {
        public uint Version;
        public uint Size;
        public StorageProtocolSpecificData ProtocolSpecificData;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AtaPassThroughEx
    {
        public ushort Length;
        public ushort AtaFlags;
        public byte PathId;
        public byte TargetId;
        public byte Lun;
        public byte ReservedAsUchar;
        public uint DataTransferLength;
        public uint TimeOutValue;
        public uint ReservedAsUlong;
        public nuint DataBufferOffset;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] PreviousTaskFile;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] CurrentTaskFile;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ScsiPassThrough
    {
        public ushort Length;
        public byte ScsiStatus;
        public byte PathId;
        public byte TargetId;
        public byte Lun;
        public byte CdbLength;
        public byte SenseInfoLength;
        public byte DataIn;
        public uint DataTransferLength;
        public uint TimeOutValue;
        public nuint DataBufferOffset;
        public uint SenseInfoOffset;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Cdb;
    }
}
