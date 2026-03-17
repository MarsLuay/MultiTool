using AutoClicker.Core.Models;
using AutoClicker.App.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoClicker.App.Models;

public partial class DriverUpdateItem : ObservableObject
{
    public DriverUpdateItem(DriverUpdateCandidate update)
    {
        Update = update;
        isSelected = !update.IsOptional;
    }

    public DriverUpdateCandidate Update { get; }

    public string UpdateId => Update.UpdateId;

    public string Title => Update.Title;

    public string DriverModel => Update.DriverModel;

    public string DriverManufacturer => Update.DriverManufacturer;

    public string DriverClass => Update.DriverClass;

    public string DriverDate => Update.DriverDate;

    public string Description => Update.Description;

    public bool IsOptional => Update.IsOptional;

    public bool RequiresUserInput => Update.RequiresUserInput;

    public string ClassificationText => IsOptional
        ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.DriverClassificationOptional)
        : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.DriverClassificationRecommended);

    public string InstallFlowText => RequiresUserInput
        ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.DriverInstallFlowNeedsInteractive)
        : AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.DriverInstallFlowCanInstallDirectly);

    [ObservableProperty]
    private bool isSelected;
}
