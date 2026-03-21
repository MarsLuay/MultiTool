using System.Runtime.InteropServices;

namespace MultiTool.Infrastructure.Windows.Interop;

internal static class Kernel32
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint GetModuleHandle(string? lpModuleName);
}
