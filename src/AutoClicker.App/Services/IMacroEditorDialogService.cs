using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public interface IMacroEditorDialogService
{
    RecordedMacro? Edit(RecordedMacro macro);
}
