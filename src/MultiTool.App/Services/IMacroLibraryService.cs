using MultiTool.App.Models;

namespace MultiTool.App.Services;

public interface IMacroLibraryService
{
    string DefaultDirectory { get; }

    IReadOnlyList<SavedMacroEntry> GetSavedMacros();

    string GetSavePath(string macroName);
}
