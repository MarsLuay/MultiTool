using System.Collections.ObjectModel;
using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoClicker.App.ViewModels;

public partial class MacroEditorViewModel : ObservableObject
{
    private readonly DateTimeOffset recordedAt;

    public MacroEditorViewModel(RecordedMacro macro)
    {
        recordedAt = macro.RecordedAt;
        macroName = macro.Name;
        statusMessage = "Pick an event on the left, adjust its details on the right, then press Save.";

        EventKinds = Enum.GetValues<MacroEventKind>();
        MouseButtons =
        [
            ClickMouseButton.Left,
            ClickMouseButton.Right,
            ClickMouseButton.Middle,
            ClickMouseButton.XButton1,
            ClickMouseButton.XButton2,
        ];

        Events = new ObservableCollection<MacroEventEditorItem>(
            macro.Events.Select((macroEvent, index) => MacroEventEditorItem.FromMacroEvent(index + 1, macroEvent)));

        if (Events.Count > 0)
        {
            SelectedEvent = Events[0];
        }
    }

    public ObservableCollection<MacroEventEditorItem> Events { get; }

    public IReadOnlyList<MacroEventKind> EventKinds { get; }

    public IReadOnlyList<ClickMouseButton> MouseButtons { get; }

    public string SummaryText => $"{Events.Count} event(s)";

    public bool HasSelectedEvent => SelectedEvent is not null;

    public string SelectedEventHint =>
        SelectedEvent switch
        {
            null => "Select an event from the list to start editing it.",
            { IsKeyboardKind: true } => "Keyboard events only use the Action, Offset, and Key Code fields.",
            { Kind: MacroEventKind.MouseMove } => "Mouse move events only use the Action, Offset, and X / Y position fields.",
            _ => "Mouse button events use the Action, Offset, Mouse Button, and X / Y position fields.",
        };

    [ObservableProperty]
    private string macroName;

    [ObservableProperty]
    private MacroEventEditorItem? selectedEvent;

    [ObservableProperty]
    private string statusMessage;

    private bool CanRemoveSelectedEvent => SelectedEvent is not null;

    [RelayCommand]
    private void AddEvent()
    {
        var nextOffset = Events.Count > 0 ? Events.Max(item => item.OffsetMilliseconds) : 0;
        var newItem =
            new MacroEventEditorItem
            {
                Index = Events.Count + 1,
                OffsetMilliseconds = nextOffset,
                Kind = MacroEventKind.MouseMove,
                MouseButton = ClickMouseButton.Left,
            };

        Events.Add(newItem);
        SelectedEvent = newItem;

        StatusMessage = "Added a new event. You can edit its details on the right.";
        OnPropertyChanged(nameof(SummaryText));
    }

    [RelayCommand(CanExecute = nameof(CanRemoveSelectedEvent))]
    private void RemoveSelectedEvent()
    {
        if (SelectedEvent is null)
        {
            return;
        }

        Events.Remove(SelectedEvent);
        SelectedEvent = null;
        RenumberEvents();
        StatusMessage = "Removed the selected event.";
    }

    [RelayCommand]
    private void SortByOffset()
    {
        var ordered = Events
            .OrderBy(item => item.OffsetMilliseconds)
            .ToArray();

        Events.Clear();
        foreach (var item in ordered)
        {
            Events.Add(item);
        }

        RenumberEvents();
        StatusMessage = "Sorted events by offset.";
    }

    public bool TryBuildMacro(out RecordedMacro macro, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(MacroName))
        {
            errorMessage = "Enter a macro name before saving.";
            macro = new RecordedMacro(string.Empty, [], TimeSpan.Zero, DateTimeOffset.MinValue);
            return false;
        }

        var normalizedEvents = Events
            .OrderBy(item => item.OffsetMilliseconds)
            .Select(item => item.ToMacroEvent())
            .ToArray();

        var duration = normalizedEvents.Length > 0
            ? normalizedEvents[^1].Offset
            : TimeSpan.Zero;

        macro = new RecordedMacro(MacroName.Trim(), normalizedEvents, duration, recordedAt == default ? DateTimeOffset.Now : recordedAt);
        errorMessage = string.Empty;
        return true;
    }

    partial void OnSelectedEventChanged(MacroEventEditorItem? value)
    {
        OnPropertyChanged(nameof(HasSelectedEvent));
        OnPropertyChanged(nameof(SelectedEventHint));
        RemoveSelectedEventCommand.NotifyCanExecuteChanged();
    }

    private void RenumberEvents()
    {
        for (var index = 0; index < Events.Count; index++)
        {
            Events[index].Index = index + 1;
        }

        OnPropertyChanged(nameof(SummaryText));
        RemoveSelectedEventCommand.NotifyCanExecuteChanged();
    }
}
