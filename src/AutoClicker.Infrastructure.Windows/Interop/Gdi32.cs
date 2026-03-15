using System.Runtime.InteropServices;

namespace AutoClicker.Infrastructure.Windows.Interop;

internal static class Gdi32
{
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(nint hObject);
}
