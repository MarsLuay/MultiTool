using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public interface IMacroEditorDialogService
{
    RecordedMacro? Edit(RecordedMacro macro);
}
