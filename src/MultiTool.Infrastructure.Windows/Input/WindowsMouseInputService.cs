using System.Runtime.InteropServices;
using MultiTool.Core.Enums;
using MultiTool.Core.Services;
using MultiTool.Infrastructure.Windows.Interop;

namespace MultiTool.Infrastructure.Windows.Input;

public sealed class WindowsMouseInputService : IMouseInputService
{
    public void Click(ClickMouseButton mouseButton, int times)
    {
        if (times <= 0)
        {
            return;
        }

        var (down, up, mouseData) = GetFlags(mouseButton);

        var inputs = new List<User32.INPUT>(times * 2);

        for (var i = 0; i < times; i++)
        {
            inputs.Add(CreateMouseInput(down, mouseData));
            inputs.Add(CreateMouseInput(up, mouseData));
        }

        SendInputs(inputs, "mouse input sequence");
    }

    public void Press(ClickMouseButton mouseButton)
    {
        var (down, _, mouseData) = GetFlags(mouseButton);
        SendSingleInput(down, mouseData);
    }

    public void Release(ClickMouseButton mouseButton)
    {
        var (_, up, mouseData) = GetFlags(mouseButton);
        SendSingleInput(up, mouseData);
    }

    public void ClickKey(int virtualKey, int times)
    {
        if (times <= 0)
        {
            return;
        }

        var inputs = new List<User32.INPUT>(times * 2);

        for (var i = 0; i < times; i++)
        {
            inputs.Add(CreateKeyboardInput(virtualKey, keyUp: false));
            inputs.Add(CreateKeyboardInput(virtualKey, keyUp: true));
        }

        SendInputs(inputs, "keyboard input sequence");
    }

    public void PressKey(int virtualKey)
    {
        SendSingleInput(CreateKeyboardInput(virtualKey, keyUp: false), "keyboard input event");
    }

    public void ReleaseKey(int virtualKey)
    {
        SendSingleInput(CreateKeyboardInput(virtualKey, keyUp: true), "keyboard input event");
    }

    private static (uint Down, uint Up, uint MouseData) GetFlags(ClickMouseButton mouseButton) =>
        mouseButton switch
        {
            ClickMouseButton.Left => (User32.MouseEventFLeftDown, User32.MouseEventFLeftUp, 0),
            ClickMouseButton.Right => (User32.MouseEventFRightDown, User32.MouseEventFRightUp, 0),
            ClickMouseButton.Middle => (User32.MouseEventFMiddleDown, User32.MouseEventFMiddleUp, 0),
            ClickMouseButton.XButton1 => (User32.MouseEventFXDown, User32.MouseEventFXUp, User32.XButton1),
            ClickMouseButton.XButton2 => (User32.MouseEventFXDown, User32.MouseEventFXUp, User32.XButton2),
            _ => throw new NotSupportedException($"Mouse button {mouseButton} is not supported."),
        };

    private static void SendSingleInput(uint flags, uint mouseData)
    {
        SendSingleInput(CreateMouseInput(flags, mouseData), "mouse input event");
    }

    private static User32.INPUT CreateMouseInput(uint flags, uint mouseData) =>
        new()
        {
            type = User32.InputMouse,
            U = new User32.InputUnion
            {
                mi = new User32.MOUSEINPUT
                {
                    mouseData = mouseData,
                    dwFlags = flags,
                },
            },
        };

    private static User32.INPUT CreateKeyboardInput(int virtualKey, bool keyUp)
    {
        if (virtualKey <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(virtualKey), "Virtual key must be greater than zero.");
        }

        return new User32.INPUT
        {
            type = User32.InputKeyboard,
            U = new User32.InputUnion
            {
                ki = new User32.KEYBDINPUT
                {
                    wVk = (ushort)virtualKey,
                    dwFlags = keyUp ? User32.KeyEventFKeyUp : 0,
                },
            },
        };
    }

    private static void SendSingleInput(User32.INPUT input, string description) =>
        SendInputs([input], description);

    private static void SendInputs(IReadOnlyCollection<User32.INPUT> inputs, string description)
    {
        var sent = User32.SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<User32.INPUT>());
        if (sent != inputs.Count)
        {
            throw new InvalidOperationException($"Windows did not accept the requested {description}.");
        }
    }
}
