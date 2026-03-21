namespace MultiTool.Core.Services;

public interface IRunAtStartupService
{
    bool IsEnabled();

    void SetEnabled(bool enabled);
}
