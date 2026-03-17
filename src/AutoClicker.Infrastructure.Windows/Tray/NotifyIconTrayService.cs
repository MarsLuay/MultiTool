using System.Drawing;
using System.IO;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Tray;

public sealed class NotifyIconTrayService : ITrayIconService
{
    private readonly string iconDirectoryPath;

    private System.Windows.Forms.NotifyIcon? notifyIcon;
    private System.Windows.Forms.ContextMenuStrip? menu;
    private Icon? idleIcon;
    private Icon? runningIcon;
    private bool initialized;
    private bool disposed;

    public NotifyIconTrayService()
        : this(Path.Combine(AppContext.BaseDirectory, "Resources", "Icons"))
    {
    }

    public NotifyIconTrayService(string iconDirectoryPath)
    {
        this.iconDirectoryPath = iconDirectoryPath;
    }

    public event EventHandler? ShowRequested;

    public event EventHandler? HideRequested;

    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (initialized)
        {
            return;
        }

        idleIcon = LoadIcon("icon.ico");
        runningIcon = LoadIcon("icon_running.ico");
        menu = new System.Windows.Forms.ContextMenuStrip();

        var showItem = new System.Windows.Forms.ToolStripMenuItem("Show MultiTool");
        var hideItem = new System.Windows.Forms.ToolStripMenuItem("Hide MultiTool");
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");

        showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
        hideItem.Click += (_, _) => HideRequested?.Invoke(this, EventArgs.Empty);
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        menu.Items.Add(showItem);
        menu.Items.Add(hideItem);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = idleIcon,
            Text = "MultiTool",
            Visible = true,
            ContextMenuStrip = menu,
        };

        notifyIcon.MouseClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ShowRequested?.Invoke(this, EventArgs.Empty);
            }
        };
        initialized = true;
    }

    public void SetRunningState(bool isRunning)
    {
        if (notifyIcon is null)
        {
            return;
        }

        notifyIcon.Icon = isRunning ? runningIcon ?? idleIcon : idleIcon;
        notifyIcon.Text = isRunning ? "MultiTool - Running..." : "MultiTool";
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (notifyIcon is not null)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            notifyIcon = null;
        }

        menu?.Dispose();
        idleIcon?.Dispose();
        runningIcon?.Dispose();
    }

    private Icon LoadIcon(string fileName)
    {
        var path = Path.Combine(iconDirectoryPath, fileName);
        if (!File.Exists(path))
        {
            return (Icon)SystemIcons.Application.Clone();
        }

        try
        {
            return new Icon(path);
        }
        catch (Exception)
        {
            return (Icon)SystemIcons.Application.Clone();
        }
    }
}
