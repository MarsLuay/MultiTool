using System.IO;
using MultiTool.App.Models;

namespace MultiTool.App.Services;

public sealed class MacroLibraryService : IMacroLibraryService
{
    public string DefaultDirectory { get; } = ResolveDefaultDirectory();

    public IReadOnlyList<SavedMacroEntry> GetSavedMacros()
    {
        Directory.CreateDirectory(DefaultDirectory);

        return Directory
            .EnumerateFiles(DefaultDirectory, "*.acmacro.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(
                filePath => new SavedMacroEntry
                {
                    DisplayName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(filePath)),
                    FileName = Path.GetFileName(filePath),
                    FilePath = filePath,
            })
            .ToArray();
    }

    public string GetSavePath(string macroName)
    {
        Directory.CreateDirectory(DefaultDirectory);
        return Path.Combine(DefaultDirectory, $"{SanitizeFileName(macroName)}.acmacro.json");
    }

    private static string ResolveDefaultDirectory()
    {
        var macrosPath = Path.Combine(AppContext.BaseDirectory, "Macros");
        Directory.CreateDirectory(macrosPath);
        return macrosPath;
    }

    private static string SanitizeFileName(string value)
    {
        var fallback = string.IsNullOrWhiteSpace(value) ? "New Macro" : value.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var characters = fallback.Select(character => Array.IndexOf(invalid, character) >= 0 ? '_' : character).ToArray();
        return new string(characters);
    }
}
