using System.Runtime.InteropServices;

namespace AutoClicker.Infrastructure.Windows.Interop;

internal static class Kernel32
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint GetModuleHandle(string? lpModuleName);
}
