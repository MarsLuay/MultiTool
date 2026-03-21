using System.Runtime.InteropServices;

namespace MultiTool.Infrastructure.Windows.Interop;

internal static class Kernel32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileTime
    {
        public uint DwLowDateTime;
        public uint DwHighDateTime;

        public ulong ToUInt64() => ((ulong)DwHighDateTime << 32) | DwLowDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct MemoryStatusEx
    {
        public uint DwLength;
        public uint DwMemoryLoad;
        public ulong UllTotalPhys;
        public ulong UllAvailPhys;
        public ulong UllTotalPageFile;
        public ulong UllAvailPageFile;
        public ulong UllTotalVirtual;
        public ulong UllAvailVirtual;
        public ulong UllAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
