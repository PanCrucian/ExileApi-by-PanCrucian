using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SharpDX.Windows;

namespace Loader
{
    public class AppForm : RenderForm
    {
        private readonly ContextMenuStrip trayMenu;
        public Action FixImguiCapture;
        private readonly NotifyIcon notifyIcon;

        public AppForm()
        {
            SuspendLayout();
            trayMenu = new ContextMenuStrip();
            var bringToFrontMenuItem = new ToolStripMenuItem("Bring to Front");
            var exitMenuItem = new ToolStripMenuItem("E&xit");
            trayMenu.Items.AddRange(new ToolStripItem[] { bringToFrontMenuItem, exitMenuItem });

            notifyIcon = new NotifyIcon();
            notifyIcon.ContextMenuStrip = trayMenu;
            exitMenuItem.Click += (sender, args) => { Close(); };

            bringToFrontMenuItem.Click += (sender, args) =>
            {
                BringToFront();
                FixImguiCapture?.Invoke();
            };

            Icon = Icon.ExtractAssociatedIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures\\icon.ico"));
            notifyIcon.Icon = Icon;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(0, 0);

            Text = "ExileApi by PanCrucian";
            notifyIcon.Text = "ExileApi by PanCrucian";
            notifyIcon.Visible = true;
            Size = new Size(1600, 900); //Screen.PrimaryScreen.Bounds.Size;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = true;
            BackColor = Color.Black;

            ResumeLayout(false);
            BringToFront();
        }

        protected override void Dispose(bool disposing)
        {
            if (notifyIcon != null)
                notifyIcon.Icon = null;

            notifyIcon?.Dispose();
            trayMenu?.Dispose();
            base.Dispose(disposing);
        }
    }
}
