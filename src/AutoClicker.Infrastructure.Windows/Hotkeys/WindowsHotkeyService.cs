using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Core.Results;
using AutoClicker.Core.Services;
using AutoClicker.Infrastructure.Windows.Interop;

namespace AutoClicker.Infrastructure.Windows.Hotkeys;

public sealed class WindowsHotkeyService : IHotkeyService
{
    private static readonly HotkeyModifiers[] ModifierVariants =
    [
        HotkeyModifiers.None,
        HotkeyModifiers.Alt,
        HotkeyModifiers.Control,
        HotkeyModifiers.Shift,
    ];

    private readonly Dictionary<int, RegisteredHotkeyAction> idToAction = new();
    private readonly Dictionary<ClickMouseButton, RegisteredHotkeyAction> mouseButtonActions = new();
    private readonly User32.HookProc mouseHookCallback;

    private HwndSource? source;
    private nint handle;
    private nint mouseHookHandle;
    private bool disposed;
    private int nextHotkeyId = 9000;
    private readonly StringBuilder classNameBuffer = new(128);

    public WindowsHotkeyService()
    {
        mouseHookCallback = MouseHookProc;
    }

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public bool IsAttached => handle != nint.Zero;

    public void Attach(nint windowHandle)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (windowHandle == nint.Zero)
        {
            throw new ArgumentException("A valid window handle is required to register hotkeys.", nameof(windowHandle));
        }

        if (handle == windowHandle)
        {
            return;
        }

        if (source is not null)
        {
            source.RemoveHook(WndProc);
        }

        handle = windowHandle;
        source = HwndSource.FromHwnd(windowHandle)
            ?? throw new InvalidOperationException("Could not create an HwndSource for the main window.");
        source.AddHook(WndProc);
    }

    public IReadOnlyCollection<HotkeyRegistrationResult> RegisterHotkeys(
        HotkeySettings settings,
        ScreenshotSettings screenshotSettings,
        MacroSettings macroSettings)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!IsAttached)
        {
            throw new InvalidOperationException("Hotkeys can only be registered after the main window handle is available.");
        }

        UnregisterAll();

        var requestedModifiers = ModifierVariants;

        var results = new List<HotkeyRegistrationResult>();
        if (settings.Toggle.InputKind == HotkeyInputKind.MouseButton)
        {
            results.Add(RegisterMouseBinding(HotkeyAction.Toggle, settings.Toggle));
        }
        else
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.Toggle, settings.Toggle, requestedModifiers));
        }

        if (HasConfiguredKeyboardBinding(settings.PinWindow))
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.WindowPinToggle, settings.PinWindow, [HotkeyModifiers.None], actionLabel: "Pin window"));
        }

        if (screenshotSettings.CaptureHotkey.InputKind == HotkeyInputKind.Keyboard)
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.ScreenshotCapture, screenshotSettings.CaptureHotkey, [HotkeyModifiers.None]));
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.ScreenshotOptions, screenshotSettings.CaptureHotkey, [HotkeyModifiers.Shift]));
        }

        if (macroSettings.PlayHotkey.InputKind == HotkeyInputKind.Keyboard)
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.MacroPlay, macroSettings.PlayHotkey, [HotkeyModifiers.None], actionLabel: "Macro play"));
        }

        if (macroSettings.RecordHotkey.InputKind == HotkeyInputKind.Keyboard)
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.MacroRecordToggle, macroSettings.RecordHotkey, [HotkeyModifiers.None], actionLabel: "Macro record"));
        }

        foreach (var assignment in macroSettings.AssignedHotkeys.Where(assignment => assignment.IsEnabled && assignment.Hotkey.InputKind == HotkeyInputKind.Keyboard))
        {
            results.AddRange(
                RegisterKeyboardBinding(
                    HotkeyAction.MacroAssigned,
                    assignment.Hotkey,
                    [HotkeyModifiers.None],
                    payload: assignment.Id,
                    actionLabel: $"Saved macro '{assignment.MacroDisplayName}'"));
        }

        return results;
    }

    public void UnregisterAll()
    {
        foreach (var hotkeyId in idToAction.Keys.ToArray())
        {
            User32.UnregisterHotKey(handle, hotkeyId);
        }

        idToAction.Clear();
        mouseButtonActions.Clear();
        nextHotkeyId = 9000;
        UninstallMouseHook();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        UnregisterAll();

        if (source is not null)
        {
            source.RemoveHook(WndProc);
            source = null;
        }
    }

    private IEnumerable<HotkeyRegistrationResult> RegisterKeyboardBinding(
        HotkeyAction action,
        HotkeyBinding binding,
        IEnumerable<HotkeyModifiers> modifiers,
        string? payload = null,
        string? actionLabel = null)
    {
        foreach (var modifier in modifiers)
        {
            var hotkeyId = nextHotkeyId++;
            var succeeded = User32.RegisterHotKey(handle, hotkeyId, (uint)modifier, (uint)binding.VirtualKey);
            if (succeeded)
            {
                idToAction[hotkeyId] = new RegisteredHotkeyAction(action, payload);
            }

            yield return new HotkeyRegistrationResult(action, binding.Clone(), modifier, succeeded, actionLabel);
        }
    }

    private HotkeyRegistrationResult RegisterMouseBinding(HotkeyAction action, HotkeyBinding binding)
    {
        var succeeded = InstallMouseHook();
        if (succeeded)
        {
            mouseButtonActions[binding.MouseButton] = new RegisteredHotkeyAction(action, null);
        }

        return new HotkeyRegistrationResult(action, binding.Clone(), HotkeyModifiers.None, succeeded);
    }

    private nint WndProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != User32.WmHotkey)
        {
            return nint.Zero;
        }

        var hotkeyId = wParam.ToInt32();
        if (!idToAction.TryGetValue(hotkeyId, out var action))
        {
            return nint.Zero;
        }

        if (ShouldSuppressForExternalTextInput())
        {
            handled = true;
            return nint.Zero;
        }

        handled = true;
        HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(action.Action, action.Payload));
        return nint.Zero;
    }

    private bool InstallMouseHook()
    {
        if (mouseHookHandle != nint.Zero)
        {
            return true;
        }

        var moduleName = Process.GetCurrentProcess().MainModule?.ModuleName;
        var moduleHandle = Kernel32.GetModuleHandle(moduleName);
        mouseHookHandle = User32.SetWindowsHookEx(User32.WhMouseLl, mouseHookCallback, moduleHandle, 0);
        return mouseHookHandle != nint.Zero;
    }

    private void UninstallMouseHook()
    {
        if (mouseHookHandle == nint.Zero)
        {
            return;
        }

        User32.UnhookWindowsHookEx(mouseHookHandle);
        mouseHookHandle = nint.Zero;
    }

    private nint MouseHookProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var mouseButton = TranslateMouseButton(wParam, lParam);
            if (mouseButton is not null && mouseButtonActions.TryGetValue(mouseButton.Value, out var action))
            {
                if (ShouldSuppressForExternalTextInput())
                {
                    return User32.CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
                }

                HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(action.Action, action.Payload));
                return 1;
            }
        }

        return User32.CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
    }

    private static ClickMouseButton? TranslateMouseButton(nint wParam, nint lParam)
    {
        return wParam.ToInt32() switch
        {
            User32.WmRButtonDown => ClickMouseButton.Right,
            User32.WmMButtonDown => ClickMouseButton.Middle,
            User32.WmXButtonDown => TranslateXButton(lParam),
            _ => null,
        };
    }

    private static ClickMouseButton? TranslateXButton(nint lParam)
    {
        var hookStruct = Marshal.PtrToStructure<User32.MSLLHOOKSTRUCT>(lParam);
        var xButton = (hookStruct.mouseData >> 16) & 0xFFFF;

        return xButton switch
        {
            User32.XButton1 => ClickMouseButton.XButton1,
            User32.XButton2 => ClickMouseButton.XButton2,
            _ => null,
        };
    }

    private static bool HasConfiguredKeyboardBinding(HotkeyBinding binding) =>
        binding.InputKind == HotkeyInputKind.Keyboard && binding.VirtualKey > 0;

    private bool ShouldSuppressForExternalTextInput()
    {
        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow == nint.Zero || foregroundWindow == handle)
        {
            return false;
        }

        _ = User32.GetWindowThreadProcessId(foregroundWindow, out _);
        var foregroundThreadId = User32.GetWindowThreadProcessId(foregroundWindow, out _);
        if (foregroundThreadId == 0)
        {
            return false;
        }

        var guiThreadInfo = new User32.GUITHREADINFO
        {
            cbSize = Marshal.SizeOf<User32.GUITHREADINFO>(),
        };

        if (!User32.GetGUIThreadInfo(foregroundThreadId, ref guiThreadInfo))
        {
            return false;
        }

        if (guiThreadInfo.hwndCaret != nint.Zero && (guiThreadInfo.flags & User32.GuiCaretBlinking) != 0)
        {
            return true;
        }

        var focusedHandle = guiThreadInfo.hwndFocus;
        if (focusedHandle == nint.Zero)
        {
            return false;
        }

        classNameBuffer.Clear();
        _ = User32.GetClassName(focusedHandle, classNameBuffer, classNameBuffer.Capacity);
        var className = classNameBuffer.ToString();

        return className.Equals("Edit", StringComparison.OrdinalIgnoreCase)
               || className.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase)
               || className.Equals("Scintilla", StringComparison.OrdinalIgnoreCase)
               || className.Equals("Windows.UI.Core.CoreWindow", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct RegisteredHotkeyAction(HotkeyAction Action, string? Payload);
}
