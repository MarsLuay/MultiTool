using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;
using AutoClicker.Infrastructure.Windows.Interop;

namespace AutoClicker.Infrastructure.Windows.Macro;

public sealed class WindowsMacroService : IMacroService
{
    private static readonly TimeSpan MouseMoveCaptureInterval = TimeSpan.FromMilliseconds(20);

    private readonly IMouseInputService mouseInputService;
    private readonly ICursorService cursorService;
    private readonly User32.HookProc keyboardHookCallback;
    private readonly User32.HookProc mouseHookCallback;
    private readonly Stopwatch stopwatch = new();
    private readonly object syncRoot = new();
    private readonly int currentProcessId = Environment.ProcessId;

    private readonly List<MacroEvent> recordingEvents = [];

    private nint keyboardHookHandle;
    private nint mouseHookHandle;
    private bool disposed;
    private bool isRecording;
    private bool isPlaying;
    private string recordingName = "New Macro";
    private RecordedMacro? currentMacro;
    private ScreenPoint? lastMousePosition;
    private TimeSpan lastMouseMoveOffset;
    private bool recordMouseMovement = true;

    public WindowsMacroService(IMouseInputService mouseInputService, ICursorService cursorService)
    {
        this.mouseInputService = mouseInputService;
        this.cursorService = cursorService;
        keyboardHookCallback = KeyboardHookProc;
        mouseHookCallback = MouseHookProc;
    }

    public bool IsRecording
    {
        get
        {
            lock (syncRoot)
            {
                return isRecording;
            }
        }
    }

    public bool IsPlaying
    {
        get
        {
            lock (syncRoot)
            {
                return isPlaying;
            }
        }
    }

    public RecordedMacro? CurrentMacro
    {
        get
        {
            lock (syncRoot)
            {
                return currentMacro;
            }
        }
    }

    public void StartRecording(string? name = null, bool recordMouseMovement = true)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        lock (syncRoot)
        {
            if (isRecording)
            {
                throw new InvalidOperationException("A macro is already being recorded.");
            }

            if (isPlaying)
            {
                throw new InvalidOperationException("Cannot start recording while a macro is playing.");
            }
        }

        var moduleName = Process.GetCurrentProcess().MainModule?.ModuleName;
        var moduleHandle = Kernel32.GetModuleHandle(moduleName);
        var keyboardHook = User32.SetWindowsHookEx(User32.WhKeyboardLl, keyboardHookCallback, moduleHandle, 0);
        if (keyboardHook == nint.Zero)
        {
            throw new InvalidOperationException("Unable to install the keyboard hook for macro recording.");
        }

        var mouseHook = User32.SetWindowsHookEx(User32.WhMouseLl, mouseHookCallback, moduleHandle, 0);
        if (mouseHook == nint.Zero)
        {
            User32.UnhookWindowsHookEx(keyboardHook);
            throw new InvalidOperationException("Unable to install the mouse hook for macro recording.");
        }

        lock (syncRoot)
        {
            keyboardHookHandle = keyboardHook;
            mouseHookHandle = mouseHook;
            recordingName = string.IsNullOrWhiteSpace(name) ? "New Macro" : name.Trim();
            recordingEvents.Clear();
            currentMacro = null;
            lastMousePosition = null;
            lastMouseMoveOffset = TimeSpan.Zero;
            this.recordMouseMovement = recordMouseMovement;
            stopwatch.Restart();
            isRecording = true;
        }
    }

    public RecordedMacro StopRecording()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        lock (syncRoot)
        {
            if (!isRecording)
            {
                throw new InvalidOperationException("There is no active macro recording to stop.");
            }

            isRecording = false;
            stopwatch.Stop();
            UninstallHooksNoLock();

            var snapshot = recordingEvents.ToArray();
            var duration = snapshot.Length > 0 ? snapshot[^1].Offset : stopwatch.Elapsed;

            currentMacro = new RecordedMacro(recordingName, snapshot, duration, DateTimeOffset.Now);
            lastMousePosition = null;
            lastMouseMoveOffset = TimeSpan.Zero;
            recordMouseMovement = true;
            return currentMacro;
        }
    }

    public void SetCurrentMacro(RecordedMacro macro)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        lock (syncRoot)
        {
            if (isRecording)
            {
                throw new InvalidOperationException("Cannot replace the current macro while recording.");
            }

            if (isPlaying)
            {
                throw new InvalidOperationException("Cannot replace the current macro while playing one.");
            }

            currentMacro = macro;
            recordingName = macro.Name;
        }
    }

    public async Task PlayAsync(int repeatCount, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        RecordedMacro macro;

        lock (syncRoot)
        {
            if (isRecording)
            {
                throw new InvalidOperationException("Cannot play a macro while recording one.");
            }

            if (isPlaying)
            {
                throw new InvalidOperationException("A macro is already playing.");
            }

            macro = currentMacro ?? throw new InvalidOperationException("There is no recorded macro to play.");
            if (macro.Events.Count == 0)
            {
                throw new InvalidOperationException("The recorded macro does not contain any input events.");
            }

            isPlaying = true;
        }

        try
        {
            var loopCount = Math.Max(1, repeatCount);

            for (var loop = 0; loop < loopCount; loop++)
            {
                var previousOffset = TimeSpan.Zero;

                foreach (var macroEvent in macro.Events)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var delay = macroEvent.Offset - previousOffset;
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }

                    ExecuteMacroEvent(macroEvent);
                    previousOffset = macroEvent.Offset;
                }
            }
        }
        finally
        {
            lock (syncRoot)
            {
                isPlaying = false;
            }
        }
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        lock (syncRoot)
        {
            currentMacro = null;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (syncRoot)
        {
            disposed = true;
            isRecording = false;
            isPlaying = false;
            stopwatch.Stop();
            UninstallHooksNoLock();
            recordingEvents.Clear();
        }
    }

    private void ExecuteMacroEvent(MacroEvent macroEvent)
    {
        switch (macroEvent.Kind)
        {
            case MacroEventKind.MouseMove:
                cursorService.SetCursorPosition(macroEvent.Position);
                break;
            case MacroEventKind.MouseButtonDown:
                cursorService.SetCursorPosition(macroEvent.Position);
                mouseInputService.Press(macroEvent.MouseButton);
                break;
            case MacroEventKind.MouseButtonUp:
                cursorService.SetCursorPosition(macroEvent.Position);
                mouseInputService.Release(macroEvent.MouseButton);
                break;
            case MacroEventKind.KeyDown:
                mouseInputService.PressKey(macroEvent.VirtualKey);
                break;
            case MacroEventKind.KeyUp:
                mouseInputService.ReleaseKey(macroEvent.VirtualKey);
                break;
            default:
                throw new NotSupportedException($"Macro event kind {macroEvent.Kind} is not supported.");
        }
    }

    private nint KeyboardHookProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && !IsCurrentProcessForegroundWindow())
        {
            var message = wParam.ToInt32();
            var kind = message switch
            {
                User32.WmKeyDown or User32.WmSysKeyDown => MacroEventKind.KeyDown,
                User32.WmKeyUp or User32.WmSysKeyUp => MacroEventKind.KeyUp,
                _ => (MacroEventKind?)null,
            };

            if (kind is not null)
            {
                var hookStruct = Marshal.PtrToStructure<User32.KBDLLHOOKSTRUCT>(lParam);
                TryRecordEvent(new MacroEvent(GetCurrentOffset(), kind.Value, VirtualKey: (int)hookStruct.vkCode));
            }
        }

        return User32.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
    }

    private nint MouseHookProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && !IsCurrentProcessForegroundWindow())
        {
            var hookStruct = Marshal.PtrToStructure<User32.MSLLHOOKSTRUCT>(lParam);
            var position = new ScreenPoint(hookStruct.pt.x, hookStruct.pt.y);
            var message = wParam.ToInt32();

            switch (message)
            {
                case User32.WmMouseMove:
                    TryRecordMouseMove(position);
                    break;
                case User32.WmLButtonDown:
                    TryRecordEvent(new MacroEvent(GetCurrentOffset(), MacroEventKind.MouseButtonDown, MouseButton: ClickMouseButton.Left, Position: position));
                    break;
                case User32.WmLButtonUp:
                    TryRecordEvent(new MacroEvent(GetCurrentOffset(), MacroEventKind.MouseButtonUp, MouseButton: ClickMouseButton.Left, Position: position));
                    break;
                case User32.WmRButtonDown:
                    TryRecordEvent(new MacroEvent(GetCurrentOffset(), MacroEventKind.MouseButtonDown, MouseButton: ClickMouseButton.Right, Position: position));
                    break;
                case User32.WmRButtonUp:
                    TryRecordEvent(new MacroEvent(GetCurrentOffset(), MacroEventKind.MouseButtonUp, MouseButton: ClickMouseButton.Right, Position: position));
                    break;
                case User32.WmMButtonDown:
                    TryRecordEvent(new MacroEvent(GetCurrentOffset(), MacroEventKind.MouseButtonDown, MouseButton: ClickMouseButton.Middle, Position: position));
                    break;
                case User32.WmMButtonUp:
                    TryRecordEvent(new MacroEvent(GetCurrentOffset(), MacroEventKind.MouseButtonUp, MouseButton: ClickMouseButton.Middle, Position: position));
                    break;
                case User32.WmXButtonDown:
                    {
                        var mouseButton = TranslateXButton(hookStruct.mouseData);
                        if (mouseButton is not null)
                        {
                            TryRecordEvent(new MacroEvent(GetCurrentOffset(), MacroEventKind.MouseButtonDown, MouseButton: mouseButton.Value, Position: position));
                        }

                        break;
                    }
                case User32.WmXButtonUp:
                    {
                        var mouseButton = TranslateXButton(hookStruct.mouseData);
                        if (mouseButton is not null)
                        {
                            TryRecordEvent(new MacroEvent(GetCurrentOffset(), MacroEventKind.MouseButtonUp, MouseButton: mouseButton.Value, Position: position));
                        }

                        break;
                    }
            }
        }

        return User32.CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
    }

    private TimeSpan GetCurrentOffset()
    {
        lock (syncRoot)
        {
            return stopwatch.Elapsed;
        }
    }

    private void TryRecordMouseMove(ScreenPoint position)
    {
        lock (syncRoot)
        {
            if (!isRecording)
            {
                return;
            }

            if (!recordMouseMovement)
            {
                return;
            }

            var offset = stopwatch.Elapsed;
            if (lastMousePosition is { } lastPosition && position == lastPosition)
            {
                return;
            }

            if (lastMousePosition is not null && offset - lastMouseMoveOffset < MouseMoveCaptureInterval)
            {
                return;
            }

            recordingEvents.Add(new MacroEvent(offset, MacroEventKind.MouseMove, Position: position));
            lastMousePosition = position;
            lastMouseMoveOffset = offset;
        }
    }

    private void TryRecordEvent(MacroEvent macroEvent)
    {
        lock (syncRoot)
        {
            if (!isRecording)
            {
                return;
            }

            recordingEvents.Add(macroEvent);
            if (macroEvent.Kind is MacroEventKind.MouseButtonDown or MacroEventKind.MouseButtonUp)
            {
                lastMousePosition = macroEvent.Position;
                lastMouseMoveOffset = macroEvent.Offset;
            }
        }
    }

    private bool IsCurrentProcessForegroundWindow()
    {
        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow == nint.Zero)
        {
            return false;
        }

        User32.GetWindowThreadProcessId(foregroundWindow, out var processId);
        return processId == currentProcessId;
    }

    private static ClickMouseButton? TranslateXButton(uint mouseData)
    {
        var xButton = (mouseData >> 16) & 0xFFFF;
        return xButton switch
        {
            User32.XButton1 => ClickMouseButton.XButton1,
            User32.XButton2 => ClickMouseButton.XButton2,
            _ => null,
        };
    }

    private void UninstallHooksNoLock()
    {
        if (keyboardHookHandle != nint.Zero)
        {
            User32.UnhookWindowsHookEx(keyboardHookHandle);
            keyboardHookHandle = nint.Zero;
        }

        if (mouseHookHandle != nint.Zero)
        {
            User32.UnhookWindowsHookEx(mouseHookHandle);
            mouseHookHandle = nint.Zero;
        }
    }
}
