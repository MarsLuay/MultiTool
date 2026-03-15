using AutoClicker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoClicker.App.Models;

public partial class InstallerPackageOptionItem : ObservableObject
{
    public InstallerPackageOptionItem(InstallerOptionDefinition option)
    {
        Option = option;
    }

    public InstallerOptionDefinition Option { get; }

    public string OptionId => Option.OptionId;

    public string DisplayName => Option.DisplayName;

    public string Description => Option.Description;

    [ObservableProperty]
    private bool isSelected;
}
