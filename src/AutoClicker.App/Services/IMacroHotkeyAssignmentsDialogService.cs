using AutoClicker.App.Models;
using AutoClicker.Core.Models;

namespace AutoClicker.App.Services;

public interface IMacroHotkeyAssignmentsDialogService
{
    IReadOnlyList<MacroHotkeyAssignment>? Edit(
        IReadOnlyList<SavedMacroEntry> savedMacros,
        IReadOnlyList<MacroHotkeyAssignment> currentAssignments);
}
