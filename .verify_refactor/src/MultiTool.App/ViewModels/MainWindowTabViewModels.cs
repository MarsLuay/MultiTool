namespace MultiTool.App.ViewModels;

public abstract class MainWindowTabViewModelBase
{
    protected MainWindowTabViewModelBase(MainWindowViewModel shell)
    {
        Shell = shell;
    }

    public MainWindowViewModel Shell { get; }
}

public sealed class ClickerTabViewModel(MainWindowViewModel shell) : MainWindowTabViewModelBase(shell);

public sealed class ScreenshotTabViewModel(MainWindowViewModel shell) : MainWindowTabViewModelBase(shell);

public sealed class MacroTabViewModel(MainWindowViewModel shell) : MainWindowTabViewModelBase(shell);

public sealed class InstallerTabViewModel(MainWindowViewModel shell) : MainWindowTabViewModelBase(shell);

public sealed class ToolsTabViewModel(MainWindowViewModel shell) : MainWindowTabViewModelBase(shell);

public sealed class SettingsTabViewModel(MainWindowViewModel shell) : MainWindowTabViewModelBase(shell);
