using MultiTool.App.Models;
using MultiTool.Core.Models;

namespace MultiTool.App.Services;

public interface IMacroHotkeyAssignmentsDialogService
{
    IReadOnlyList<MacroHotkeyAssignment>? Edit(
        IReadOnlyList<SavedMacroEntry> savedMacros,
        IReadOnlyList<MacroHotkeyAssignment> currentAssignments);
}
