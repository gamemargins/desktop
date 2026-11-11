// The tray icon's menu (a right click): "Update to x.y.z" in bold on top while a newer version waits for the player
// (only with "Keep the program up to date" off), then Upload now, Open gamemargins.com, the three ticks (Start with
// Windows, Keep the addon up to date, Keep the program up to date) and Quit. Built from its state alone, so MainForm
// rebuilds it whenever that changes.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    internal sealed class TrayMenuState
    {
        /// <summary>The linked site's host, for "Open gamemargins.com".</summary>
        public string Host;
        public bool StartWithWindows;
        public bool KeepAddonUpdated;
        public bool KeepProgramUpdated;
        /// <summary>A newer version the player updates to with a click (the tick off); null when there is none.</summary>
        public string UpdateTo;
    }

    internal sealed class TrayMenuActions
    {
        public Action Update;
        public Action UploadNow;
        public Action OpenSite;
        public Action Quit;
        public Action<bool> StartWithWindows;
        public Action<bool> KeepAddonUpdated;
        public Action<bool> KeepProgramUpdated;
    }

    internal static class TrayMenu
    {
        public static ContextMenuStrip Build(TrayMenuState state, TrayMenuActions actions)
        {
            var menu = new ContextMenuStrip();
            if (!state.KeepProgramUpdated && !string.IsNullOrEmpty(state.UpdateTo))
            {
                var update = new ToolStripMenuItem(Texts.T("Update to {0}", state.UpdateTo)) { Name = "update" };
                update.Font = new Font(update.Font, FontStyle.Bold);
                update.Click += delegate { actions.Update(); };
                menu.Items.Add(update);
                menu.Items.Add(new ToolStripSeparator());
            }
            menu.Items.Add(Texts.T("Upload now"), null, delegate { actions.UploadNow(); });
            menu.Items.Add(Texts.T("Open {0}", state.Host), null, delegate { actions.OpenSite(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Tick("autostart", Texts.T("Start with Windows"), state.StartWithWindows, actions.StartWithWindows));
            menu.Items.Add(Tick("keep-addon", Texts.T("Keep the addon up to date"), state.KeepAddonUpdated, actions.KeepAddonUpdated));
            menu.Items.Add(Tick("keep-program", Texts.T("Keep the program up to date"), state.KeepProgramUpdated, actions.KeepProgramUpdated));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Texts.T("Quit"), null, delegate { actions.Quit(); });
            return menu;
        }

        private static ToolStripMenuItem Tick(string name, string text, bool on, Action<bool> changed)
        {
            var item = new ToolStripMenuItem(text) { Name = name, CheckOnClick = true, Checked = on };
            item.CheckedChanged += delegate { changed(item.Checked); };
            return item;
        }
    }
}
