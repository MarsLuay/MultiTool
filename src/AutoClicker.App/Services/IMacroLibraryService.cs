using AutoClicker.App.Models;

namespace AutoClicker.App.Services;

public interface IMacroLibraryService
{
    string DefaultDirectory { get; }

    IReadOnlyList<SavedMacroEntry> GetSavedMacros();

    string GetSavePath(string macroName);
}
