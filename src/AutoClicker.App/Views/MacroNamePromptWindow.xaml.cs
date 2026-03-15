using System.Windows;
using System.Windows.Input;

namespace AutoClicker.App.Views;

public partial class MacroNamePromptWindow : Window
{
    private readonly string saveDirectory;

    public MacroNamePromptWindow(string suggestedName, string saveDirectory)
    {
        InitializeComponent();
        this.saveDirectory = saveDirectory;
        SaveFolderTextBlock.Text = saveDirectory;
        NameTextBox.Text = string.IsNullOrWhiteSpace(suggestedName) ? "New Macro" : suggestedName.Trim();
        UpdateSavePreview();
        Loaded += MacroNamePromptWindow_Loaded;
    }

    public string MacroName => NameTextBox.Text.Trim();

    private void MacroNamePromptWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Keyboard.Focus(NameTextBox);
        NameTextBox.SelectAll();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ErrorTextBlock.Visibility = Visibility.Visible;
            Keyboard.Focus(NameTextBox);
            NameTextBox.SelectAll();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void NameTextBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ErrorTextBlock.Visibility = Visibility.Collapsed;
        UpdateSavePreview();
    }

    private void UpdateSavePreview()
    {
        var previewName = SanitizeFileName(NameTextBox.Text);
        SaveFilePreviewTextBlock.Text = $"{previewName}.acmacro.json";
    }

    private static string SanitizeFileName(string value)
    {
        var fallback = string.IsNullOrWhiteSpace(value) ? "New Macro" : value.Trim();
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var characters = fallback.Select(character => Array.IndexOf(invalid, character) >= 0 ? '_' : character).ToArray();
        return new string(characters);
    }
}
