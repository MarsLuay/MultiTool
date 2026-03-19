using System.Windows;
using System.Windows.Input;
using MultiTool.App.Localization;

namespace MultiTool.App.Views;

public partial class MacroNamePromptWindow : Window
{
    private readonly string saveDirectory;

    public MacroNamePromptWindow(string suggestedName, string saveDirectory)
    {
        InitializeComponent();
        this.saveDirectory = saveDirectory;

        Title = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptTitle);
        PromptHeadingTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptHeading);
        PromptDescriptionTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptDescription);
        PromptNameLabelTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptNameLabel);
        PromptSaveToLabelTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptSaveToLabel);
        ErrorTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptErrorEnterName);
        PromptOverwriteHintTextBlock.Text = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptOverwriteHint);
        PromptCancelButton.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptCancel);
        PromptSaveButton.Content = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptSave);
        NameTextBox.ToolTip = AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptNameTooltip);

        SaveFolderTextBlock.Text = saveDirectory;
        NameTextBox.Text = string.IsNullOrWhiteSpace(suggestedName)
            ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptDefaultName)
            : suggestedName.Trim();
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
        SaveFilePreviewTextBlock.Text = AppLanguageStrings.FormatForCurrentLanguage(AppLanguageKeys.MacroNamePromptSavePreviewFormat, previewName);
    }

    private static string SanitizeFileName(string value)
    {
        var fallback = string.IsNullOrWhiteSpace(value)
            ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.MacroNamePromptDefaultName)
            : value.Trim();
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var characters = fallback.Select(character => Array.IndexOf(invalid, character) >= 0 ? '_' : character).ToArray();
        return new string(characters);
    }
}
