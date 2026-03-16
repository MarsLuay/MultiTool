using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AutoClicker.Infrastructure.Windows.Tools;

internal static class NtfsFolderCountProvider
{
    private const uint GenericRead = 0x80000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint FileShareDelete = 0x00000004;
    private const uint OpenExisting = 3;
    private const uint FileAttributeDirectory = 0x00000010;
    private const uint FsctlEnumUsnData = 0x000900B3;
    private const uint FsctlQueryUsnJournal = 0x000900F4;
    private const int ErrorHandleEof = 38;
    private const int MinimumUsnRecordV2Length = 60;
    private const int DefaultBufferSize = 64 * 1024;

    public static int? TryGetExactFolderCount(string rootPath)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(rootPath))
        {
            return null;
        }

        string fullRootPath;
        try
        {
            fullRootPath = Path.GetFullPath(rootPath);
        }
        catch
        {
            return null;
        }

        var volumeRoot = Path.GetPathRoot(fullRootPath);
        if (string.IsNullOrWhiteSpace(volumeRoot))
        {
            return null;
        }

        // The NTFS MFT/USN shortcut only applies to whole local volumes.
        if (!string.Equals(
                Path.TrimEndingDirectorySeparator(fullRootPath),
                Path.TrimEndingDirectorySeparator(volumeRoot),
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        DriveInfo drive;
        try
        {
            drive = new DriveInfo(volumeRoot);
        }
        catch
        {
            return null;
        }

        if (!drive.IsReady
            || drive.DriveType != DriveType.Fixed
            || !string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TryCountVolumeDirectories(volumeRoot);
    }

    private static int? TryCountVolumeDirectories(string volumeRoot)
    {
        try
        {
            var volumePath = $@"\\.\\{volumeRoot.TrimEnd('\\')}";
            using var volumeHandle = CreateFile(
                volumePath,
                GenericRead,
                FileShareRead | FileShareWrite | FileShareDelete,
                nint.Zero,
                OpenExisting,
                0,
                nint.Zero);

            if (volumeHandle.IsInvalid)
            {
                return null;
            }

            if (!DeviceIoControl(
                    volumeHandle,
                    FsctlQueryUsnJournal,
                    nint.Zero,
                    0,
                    out UsnJournalDataV0 journalData,
                    Marshal.SizeOf<UsnJournalDataV0>(),
                    out _,
                    nint.Zero))
            {
                return null;
            }

            var enumData = new MftEnumDataV0
            {
                StartFileReferenceNumber = 0,
                LowUsn = 0,
                HighUsn = journalData.NextUsn,
            };

            var buffer = new byte[DefaultBufferSize];
            long folderCount = 1; // Include the volume root itself.

            while (true)
            {
                if (!DeviceIoControl(
                        volumeHandle,
                        FsctlEnumUsnData,
                        ref enumData,
                        Marshal.SizeOf<MftEnumDataV0>(),
                        buffer,
                        buffer.Length,
                        out var bytesReturned,
                        nint.Zero))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == ErrorHandleEof)
                    {
                        break;
                    }

                    return null;
                }

                if (bytesReturned <= sizeof(long))
                {
                    break;
                }

                enumData.StartFileReferenceNumber = BinaryPrimitives.ReadInt64LittleEndian(buffer.AsSpan(0, sizeof(long)));
                var offset = sizeof(long);
                while (offset < bytesReturned)
                {
                    var remainingBytes = bytesReturned - offset;
                    if (remainingBytes < MinimumUsnRecordV2Length)
                    {
                        return null;
                    }

                    var record = buffer.AsSpan(offset, remainingBytes);
                    var recordLength = BinaryPrimitives.ReadUInt32LittleEndian(record);
                    if (recordLength < MinimumUsnRecordV2Length || offset + recordLength > bytesReturned)
                    {
                        return null;
                    }

                    var fileAttributes = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(52, sizeof(uint)));
                    if ((fileAttributes & FileAttributeDirectory) != 0)
                    {
                        folderCount++;
                    }

                    offset += (int)recordLength;
                }
            }

            return folderCount >= int.MaxValue
                ? int.MaxValue
                : (int)folderCount;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        nint lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        nint hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        nint lpInBuffer,
        int nInBufferSize,
        out UsnJournalDataV0 lpOutBuffer,
        int nOutBufferSize,
        out int lpBytesReturned,
        nint lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        ref MftEnumDataV0 lpInBuffer,
        int nInBufferSize,
        [Out] byte[] lpOutBuffer,
        int nOutBufferSize,
        out int lpBytesReturned,
        nint lpOverlapped);

    [StructLayout(LayoutKind.Sequential)]
    private struct UsnJournalDataV0
    {
        public ulong UsnJournalID;
        public long FirstUsn;
        public long NextUsn;
        public long LowestValidUsn;
        public long MaxUsn;
        public ulong MaximumSize;
        public ulong AllocationDelta;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MftEnumDataV0
    {
        public long StartFileReferenceNumber;
        public long LowUsn;
        public long HighUsn;
    }
}
