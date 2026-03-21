using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using MultiTool.Core.Results;
using MultiTool.Core.Services;
using MultiTool.Infrastructure.Windows.Interop;

namespace MultiTool.Infrastructure.Windows.Hotkeys;

public sealed class WindowsHotkeyService : IHotkeyService
{
    private readonly Dictionary<int, RegisteredHotkeyAction> idToAction = new();
    private readonly Dictionary<KeyboardHotkeyTrigger, RegisteredHotkeyAction> lowLevelKeyboardActions = new();
    private readonly Dictionary<ClickMouseButton, RegisteredHotkeyAction> mouseButtonActions = new();
    private readonly HashSet<int> activeLowLevelKeyboardVirtualKeys = [];
    private readonly User32.HookProc keyboardHookCallback;
    private readonly User32.HookProc mouseHookCallback;
    private readonly int currentProcessId = Environment.ProcessId;

    private HwndSource? source;
    private nint handle;
    private nint keyboardHookHandle;
    private nint mouseHookHandle;
    private bool disposed;
    private int nextHotkeyId = 9000;
    private readonly StringBuilder classNameBuffer = new(128);

    public WindowsHotkeyService()
    {
        keyboardHookCallback = KeyboardHookProc;
        mouseHookCallback = MouseHookProc;
    }

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public bool IsAttached => handle != nint.Zero;

    public Func<bool>? LowLevelHotkeySuppressionEvaluator { get; set; }

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

        var results = new List<HotkeyRegistrationResult>();
        var registeredKeyboardTriggers = new HashSet<KeyboardHotkeyTrigger>();
        if (settings.Toggle.InputKind == HotkeyInputKind.MouseButton)
        {
            results.Add(RegisterMouseBinding(HotkeyAction.Toggle, settings.Toggle));
        }
        else
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.Toggle, settings.Toggle, GetToggleHotkeyModifiers(settings), settings, registeredKeyboardTriggers));
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.ForceStop, settings.Toggle, GetForceStopHotkeyModifiers(settings), settings, registeredKeyboardTriggers, actionLabel: "Force stop clicker"));
        }

        if (HasConfiguredKeyboardBinding(settings.PinWindow))
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.WindowPinToggle, settings.PinWindow, [settings.PinWindow.Modifiers], settings, registeredKeyboardTriggers, actionLabel: "Pin window"));
        }

        if (screenshotSettings.CaptureHotkey.InputKind == HotkeyInputKind.Keyboard)
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.ScreenshotCapture, screenshotSettings.CaptureHotkey, [screenshotSettings.CaptureHotkey.Modifiers], settings, registeredKeyboardTriggers));
        }

        if (macroSettings.PlayHotkey.InputKind == HotkeyInputKind.Keyboard)
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.MacroPlay, macroSettings.PlayHotkey, [macroSettings.PlayHotkey.Modifiers], settings, registeredKeyboardTriggers, actionLabel: "Macro play"));
        }

        if (macroSettings.RecordHotkey.InputKind == HotkeyInputKind.Keyboard)
        {
            results.AddRange(RegisterKeyboardBinding(HotkeyAction.MacroRecordToggle, macroSettings.RecordHotkey, [macroSettings.RecordHotkey.Modifiers], settings, registeredKeyboardTriggers, actionLabel: "Macro record"));
        }

        foreach (var assignment in macroSettings.AssignedHotkeys.Where(assignment => assignment.IsEnabled && assignment.Hotkey.InputKind == HotkeyInputKind.Keyboard))
        {
            results.AddRange(
                RegisterKeyboardBinding(
                    HotkeyAction.MacroAssigned,
                    assignment.Hotkey,
                    [assignment.Hotkey.Modifiers],
                    settings,
                    registeredKeyboardTriggers,
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
        lowLevelKeyboardActions.Clear();
        mouseButtonActions.Clear();
        activeLowLevelKeyboardVirtualKeys.Clear();
        nextHotkeyId = 9000;
        UninstallKeyboardHook();
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
        HotkeySettings settings,
        HashSet<KeyboardHotkeyTrigger> registeredKeyboardTriggers,
        string? payload = null,
        string? actionLabel = null)
    {
        foreach (var modifier in modifiers.Distinct())
        {
            var trigger = new KeyboardHotkeyTrigger(binding.VirtualKey, modifier);
            if (!registeredKeyboardTriggers.Add(trigger))
            {
                yield return new HotkeyRegistrationResult(action, binding.Clone(), modifier, false, actionLabel);
                continue;
            }

            var registeredAction = new RegisteredHotkeyAction(action, payload);
            var succeeded = ShouldUseLowLevelOverride(settings, binding, modifier)
                ? RegisterLowLevelKeyboardBinding(trigger, registeredAction)
                : RegisterWindowsHotKeyBinding(trigger, registeredAction);

            if (!succeeded)
            {
                registeredKeyboardTriggers.Remove(trigger);
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

    private bool RegisterWindowsHotKeyBinding(KeyboardHotkeyTrigger trigger, RegisteredHotkeyAction action)
    {
        var hotkeyId = nextHotkeyId++;
        var registrationModifiers = (uint)trigger.Modifiers | User32.ModNoRepeat;
        var succeeded = User32.RegisterHotKey(handle, hotkeyId, registrationModifiers, (uint)trigger.VirtualKey);
        if (succeeded)
        {
            idToAction[hotkeyId] = action;
        }

        return succeeded;
    }

    private bool RegisterLowLevelKeyboardBinding(KeyboardHotkeyTrigger trigger, RegisteredHotkeyAction action)
    {
        if (!InstallKeyboardHook())
        {
            return false;
        }

        lowLevelKeyboardActions[trigger] = action;
        return true;
    }

    private bool InstallKeyboardHook()
    {
        if (keyboardHookHandle != nint.Zero)
        {
            return true;
        }

        var moduleName = Process.GetCurrentProcess().MainModule?.ModuleName;
        var moduleHandle = Kernel32.GetModuleHandle(moduleName);
        keyboardHookHandle = User32.SetWindowsHookEx(User32.WhKeyboardLl, keyboardHookCallback, moduleHandle, 0);
        return keyboardHookHandle != nint.Zero;
    }

    private void UninstallKeyboardHook()
    {
        if (keyboardHookHandle == nint.Zero)
        {
            return;
        }

        User32.UnhookWindowsHookEx(keyboardHookHandle);
        keyboardHookHandle = nint.Zero;
    }

    private nint KeyboardHookProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0 || lowLevelKeyboardActions.Count == 0)
        {
            return User32.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        var message = wParam.ToInt32();
        var isKeyDown = message is User32.WmKeyDown or User32.WmSysKeyDown;
        var isKeyUp = message is User32.WmKeyUp or User32.WmSysKeyUp;
        if (!isKeyDown && !isKeyUp)
        {
            return User32.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        var hookStruct = Marshal.PtrToStructure<User32.KBDLLHOOKSTRUCT>(lParam);
        if ((hookStruct.flags & User32.LlkhfInjected) != 0)
        {
            return User32.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        var virtualKey = (int)hookStruct.vkCode;

        if (isKeyUp)
        {
            return activeLowLevelKeyboardVirtualKeys.Remove(virtualKey)
                ? 1
                : User32.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        if (activeLowLevelKeyboardVirtualKeys.Contains(virtualKey))
        {
            return 1;
        }

        var trigger = new KeyboardHotkeyTrigger(virtualKey, GetActiveKeyboardModifiers());
        if (!lowLevelKeyboardActions.TryGetValue(trigger, out var action))
        {
            return User32.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        if (ShouldSuppressLowLevelHotkeyExecution())
        {
            return User32.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        activeLowLevelKeyboardVirtualKeys.Add(virtualKey);
        HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(action.Action, action.Payload));
        return 1;
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

    internal static IReadOnlyList<HotkeyModifiers> GetToggleHotkeyModifiers(HotkeySettings settings) =>
        settings.Toggle.Modifiers != HotkeyModifiers.None
            ? [settings.Toggle.Modifiers]
            : settings.AllowModifierVariants
            ? [HotkeyModifiers.None, HotkeyModifiers.Alt, HotkeyModifiers.Control]
            : [HotkeyModifiers.None];

    internal static IReadOnlyList<HotkeyModifiers> GetForceStopHotkeyModifiers(HotkeySettings settings) =>
        GetToggleHotkeyModifiers(settings)
            .Select(static modifier => modifier | HotkeyModifiers.Shift)
            .Where(modifier => modifier != settings.Toggle.Modifiers)
            .Distinct()
            .ToArray();

    internal static bool ShouldUseLowLevelOverride(HotkeySettings settings, HotkeyBinding binding, HotkeyModifiers modifier) =>
        settings.OverrideApplicationShortcuts
        && binding.InputKind == HotkeyInputKind.Keyboard
        && binding.Modifiers != HotkeyModifiers.None
        && modifier != HotkeyModifiers.None;

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

    private bool ShouldSuppressLowLevelHotkeyExecution()
    {
        if (LowLevelHotkeySuppressionEvaluator?.Invoke() == true)
        {
            return true;
        }

        return ShouldSuppressForCurrentProcessTextInput();
    }

    private bool ShouldSuppressForCurrentProcessTextInput()
    {
        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow == nint.Zero)
        {
            return false;
        }

        User32.GetWindowThreadProcessId(foregroundWindow, out var processId);
        if (processId != currentProcessId)
        {
            return false;
        }

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

        return guiThreadInfo.hwndCaret != nint.Zero && (guiThreadInfo.flags & User32.GuiCaretBlinking) != 0;
    }

    private static HotkeyModifiers GetActiveKeyboardModifiers()
    {
        var modifiers = HotkeyModifiers.None;
        if (IsModifierPressed(User32.VkLControl) || IsModifierPressed(User32.VkRControl))
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (IsModifierPressed(User32.VkLMenu) || IsModifierPressed(User32.VkRMenu))
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (IsModifierPressed(User32.VkLShift) || IsModifierPressed(User32.VkRShift))
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (IsModifierPressed(User32.VkLWin) || IsModifierPressed(User32.VkRWin))
        {
            modifiers |= HotkeyModifiers.Windows;
        }

        return modifiers;
    }

    private static bool IsModifierPressed(int virtualKey) => (User32.GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private readonly record struct RegisteredHotkeyAction(HotkeyAction Action, string? Payload);

    private readonly record struct KeyboardHotkeyTrigger(int VirtualKey, HotkeyModifiers Modifiers);
}
