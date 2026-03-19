using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MultiTool.App.Helpers;
using MultiTool.App.Localization;
using MultiTool.App.Models;
using MultiTool.App.Services;
using MultiTool.Core.Defaults;
using MultiTool.Core.Enums;
using MultiTool.Core.Models;
using MultiTool.Core.Services;
using MultiTool.Core.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MultiTool.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private async Task ToggleAsync()
    {
        if (!autoClickerController.IsRunning)
        {
            var validation = settingsValidator.ValidateClickSettings(BuildClickSettings());
            if (!validation.IsValid)
            {
                StatusMessage = validation.Summary;
                return;
            }
        }

        await autoClickerController.ToggleAsync(BuildClickSettings());
        UpdateRunningState(autoClickerController.IsRunning);
        StatusMessage = autoClickerController.IsRunning
            ? L(AppLanguageKeys.MainStatusClicking)
            : L(AppLanguageKeys.MainStatusAutomationStopped);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveSettingsAsync(L(AppLanguageKeys.MainStatusSettingsSaved), updateStatusOnSuccess: true);
    }

    [RelayCommand]
    private void CaptureCoordinates()
    {
        var point = coordinateCaptureDialogService.Capture();
        if (point is null)
        {
            return;
        }

        FixedX = point.Value.X;
        FixedY = point.Value.Y;
        SelectedLocationMode = ClickLocationMode.FixedPoint;
        StatusMessage = F(AppLanguageKeys.MainStatusCapturedCoordinatesFormat, FixedX, FixedY);
    }

    [RelayCommand]
    private void OpenAbout()
    {
        aboutWindowService.Show();
    }

    public void CaptureCustomKey(Key key)
    {
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(key);
        var displayName = HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey);

        CustomInputKind = MultiTool.Core.Enums.CustomInputKind.Keyboard;
        CustomKeyVirtualKey = virtualKey;
        CustomMouseButton = ClickMouseButton.Left;
        CustomKeyDisplayText = displayName;
        StatusMessage = F(AppLanguageKeys.MainStatusCustomKeySetFormat, displayName);
    }


    public void CaptureClickerHotkey(Key key)
    {
        var capturedKey = key == Key.System ? Key.None : key;
        var virtualKey = HotkeyDisplayNameFormatter.ToVirtualKey(capturedKey);
        if (virtualKey <= 0)
        {
            return;
        }

        hotkeySettings.Toggle = new HotkeyBinding(
            virtualKey,
            HotkeyDisplayNameFormatter.FormatVirtualKey(virtualKey));
        OnPropertyChanged(nameof(ClickerHotkeyDisplay));
        HotkeysChanged?.Invoke(this, EventArgs.Empty);
        ScheduleSettingsAutoSave();
        StatusMessage = F(AppLanguageKeys.MainStatusClickerHotkeySetFormat, ClickerHotkeyDisplay);
    }


    public void CaptureCustomMouseButton(ClickMouseButton mouseButton)
    {
        CustomInputKind = MultiTool.Core.Enums.CustomInputKind.MouseButton;
        CustomKeyVirtualKey = 0;
        CustomMouseButton = mouseButton;
        CustomKeyDisplayText = FormatMouseButtonDisplay(mouseButton);
        StatusMessage = F(AppLanguageKeys.MainStatusCustomInputSetFormat, CustomKeyDisplayText);
    }

    public void UpdateRunningState(bool running)
    {
        IsRunning = running;
        RefreshWindowTitle();
        RefreshHotkeyLabels();
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    private string GetCustomKeyDisplayName() =>
        !string.IsNullOrWhiteSpace(CustomKeyDisplayText) && CustomInputKind != MultiTool.Core.Enums.CustomInputKind.None
            ? CustomKeyDisplayText
            : string.Empty;

    private static string GetCustomKeyDisplayText(ClickSettings settings) =>
        !string.IsNullOrWhiteSpace(settings.CustomKeyDisplayName)
            ? settings.CustomKeyDisplayName
            : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainCustomKeyOrMousePrompt);

    private static string FormatMouseButtonDisplay(ClickMouseButton mouseButton) =>
        mouseButton switch
        {
            ClickMouseButton.Left => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButtonLeft),
            ClickMouseButton.Right => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButtonRight),
            ClickMouseButton.Middle => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButtonMiddle),
            ClickMouseButton.XButton1 => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButton4),
            ClickMouseButton.XButton2 => AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MainMouseButton5),
            _ => mouseButton.ToString(),
        };


    private async Task ForceStopAsync()
    {
        if (!autoClickerController.IsRunning)
        {
            return;
        }

        await autoClickerController.StopAsync();
        UpdateRunningState(autoClickerController.IsRunning);
        StatusMessage = L(AppLanguageKeys.MainStatusAutomationStopped);
    }


}
