using System.Collections.ObjectModel;
using AutoClicker.App.Localization;
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
        statusMessage = L(AppLanguageKeys.MacroEditorStatusInitial);

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

    public string WindowTitleText => L(AppLanguageKeys.MacroEditorTitle);

    public string NameLabelText => L(AppLanguageKeys.MacroEditorNameLabel);

    public string DescriptionText => L(AppLanguageKeys.MacroEditorDescription);

    public string EventsHeaderText => L(AppLanguageKeys.MacroEditorEventsHeader);

    public string PickEventHintText => L(AppLanguageKeys.MacroEditorPickEventHint);

    public string AddEventButtonText => L(AppLanguageKeys.MacroEditorAddEventButton);

    public string RemoveSelectedButtonText => L(AppLanguageKeys.MacroEditorRemoveSelectedButton);

    public string SortByOffsetButtonText => L(AppLanguageKeys.MacroEditorSortByOffsetButton);

    public string ColumnNumberText => L(AppLanguageKeys.MacroEditorColumnNumber);

    public string ColumnOffsetText => L(AppLanguageKeys.MacroEditorColumnOffset);

    public string ColumnActionText => L(AppLanguageKeys.MacroEditorColumnAction);

    public string ColumnDetailsText => L(AppLanguageKeys.MacroEditorColumnDetails);

    public string SelectedEventHeaderText => L(AppLanguageKeys.MacroEditorSelectedEventHeader);

    public string SelectedEventNullText => L(AppLanguageKeys.MacroEditorSelectedEventNull);

    public string ActionLabelText => L(AppLanguageKeys.MacroEditorActionLabel);

    public string OffsetLabelText => L(AppLanguageKeys.MacroEditorOffsetLabel);

    public string KeyCodeLabelText => L(AppLanguageKeys.MacroEditorKeyCodeLabel);

    public string MouseButtonLabelText => L(AppLanguageKeys.MacroEditorMouseButtonLabel);

    public string XPositionLabelText => L(AppLanguageKeys.MacroEditorXPositionLabel);

    public string YPositionLabelText => L(AppLanguageKeys.MacroEditorYPositionLabel);

    public string FieldHintText => L(AppLanguageKeys.MacroEditorFieldHint);

    public string CancelButtonText => L(AppLanguageKeys.MacroEditorCancelButton);

    public string SaveButtonText => L(AppLanguageKeys.MacroEditorSaveButton);

    public string SummaryText => F(AppLanguageKeys.MacroEditorSummaryEventsFormat, Events.Count);

    public bool HasSelectedEvent => SelectedEvent is not null;

    public string SelectedEventHint =>
        SelectedEvent switch
        {
            null => L(AppLanguageKeys.MacroEditorSelectedHintNone),
            { IsKeyboardKind: true } => L(AppLanguageKeys.MacroEditorSelectedHintKeyboard),
            { Kind: MacroEventKind.MouseMove } => L(AppLanguageKeys.MacroEditorSelectedHintMouseMove),
            _ => L(AppLanguageKeys.MacroEditorSelectedHintMouseButton),
        };

    public string SelectedEventDetailsText =>
        string.IsNullOrWhiteSpace(SelectedEvent?.DetailsDisplayName)
            ? SelectedEventNullText
            : SelectedEvent.DetailsDisplayName;

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

        StatusMessage = L(AppLanguageKeys.MacroEditorStatusAdded);
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
        StatusMessage = L(AppLanguageKeys.MacroEditorStatusRemoved);
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
        StatusMessage = L(AppLanguageKeys.MacroEditorStatusSorted);
    }

    public bool TryBuildMacro(out RecordedMacro macro, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(MacroName))
        {
            errorMessage = L(AppLanguageKeys.MacroEditorErrorEnterName);
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
        OnPropertyChanged(nameof(SelectedEventDetailsText));
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

    private static string L(string key) => AppLanguageStrings.GetForCurrentLanguage(key);

    private static string F(string key, params object[] args) => AppLanguageStrings.FormatForCurrentLanguage(key, args);
}
