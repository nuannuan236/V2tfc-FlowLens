using System.Drawing;
using Forms = System.Windows.Forms;

namespace V2rayN.FlowLens.App;

public sealed class TrayIconController : IDisposable
{
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly Forms.ToolStripMenuItem pauseMenuItem;
    private bool disposed;

    public TrayIconController(
        Action show,
        Action refreshNow,
        Action togglePause,
        Action openHistoryFolder,
        Action copyDiagnostics,
        Action exit)
    {
        pauseMenuItem = new Forms.ToolStripMenuItem(UiText.Get("Action.Pause", "Pause Refresh"), null, (_, _) => togglePause());

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(new Forms.ToolStripMenuItem("Show FlowLens", null, (_, _) => show()));
        menu.Items.Add(new Forms.ToolStripMenuItem(UiText.Get("Action.Refresh", "Refresh Now"), null, (_, _) => refreshNow()));
        menu.Items.Add(pauseMenuItem);
        menu.Items.Add(new Forms.ToolStripMenuItem(UiText.Get("Action.OpenHistoryFolder", "Open History Folder"), null, (_, _) => openHistoryFolder()));
        menu.Items.Add(new Forms.ToolStripMenuItem(UiText.Get("Action.CopyDiagnostics", "Copy Diagnostics"), null, (_, _) => copyDiagnostics()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Exit", null, (_, _) => exit()));

        notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = SystemIcons.Application,
            Text = "v2rayN FlowLens",
            Visible = true
        };

        notifyIcon.DoubleClick += (_, _) => show();
    }

    public void UpdatePaused(bool isPaused)
    {
        pauseMenuItem.Text = isPaused
            ? UiText.Get("Action.Resume", "Resume Refresh")
            : UiText.Get("Action.Pause", "Pause Refresh");
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        notifyIcon.Visible = false;
        notifyIcon.ContextMenuStrip?.Dispose();
        notifyIcon.Dispose();
    }
}
