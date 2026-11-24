// The menu of the icon by the clock (a right click). It is short: it starts with
// two lines that say how things are, a dot before each ("Tempo is reading · 26 min", "WoW: uploaded 3 min ago"; green
// = works, orange = watch out, red = can't, grey = off), then only what can be done: Open Tempo (the main one, bold),
// Stop watching or Start watching, Upload now; then Open GameMargins Desktop, Settings and Quit. No ticks: every
// setting has one place, the window's Settings (and Tempo's own, in Tempo). A game the program is not used for has
// no line; where Tempo can't be used its status line stays and there is no Open Tempo. While "Keep the program up to date" is off and a newer version waits, "Update to x.y.z" is the first thing
// to do. The menu is drawn by the program (TrayPaint), light or dark as Windows is, so it is one with the window.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    internal sealed class TrayMenuState
    {
        /// <summary>Which games the program is used for.</summary>
        public bool Wow = true;
        public bool Maple;
        /// <summary>How Tempo is doing, and its dot: "ok", "warn", "bad" or "" (grey).</summary>
        public string TempoLine;
        public string TempoTone = "";
        public string WowLine;
        public string WowTone = "";
        /// <summary>Tempo can be opened (linked, and the question answered with yes).</summary>
        public bool TempoReady;
        /// <summary>Tempo can't be used for now (the site refused to open it, or this Windows can't look): its status line stays, "Open Tempo" goes.</summary>
        public bool TempoShut;
        /// <summary>Tempo reads right now: "Stop watching"; else "Start watching".</summary>
        public bool Watching;
        /// <summary>WoW is linked: "Upload now" works.</summary>
        public bool WowReady;
        /// <summary>A newer version the player updates to with a click ("Keep the program up to date" off); null when there is none.</summary>
        public string UpdateTo;
    }

    internal sealed class TrayMenuActions
    {
        public Action Update;
        public Action UploadNow;
        public Action Quit;
        public Action OpenTempo;
        public Action StopWatching;
        public Action StartWatching;
        public Action OpenWindow;
        public Action OpenSettings;
    }

    /// <summary>One line of the menu: "status" (a dot and words, not to click), "main" (bold), "action", or "sep".</summary>
    internal sealed class TrayLine
    {
        public string Kind = "action";
        public string Name;
        public string Text = "";
        public string Tone = "";
        public Action Do;
        public bool Enabled = true;
    }

    internal static class TrayMenu
    {
        /// <summary>The menu's lines for `state`, top to bottom.</summary>
        public static List<TrayLine> Lines(TrayMenuState state, TrayMenuActions actions)
        {
            var lines = new List<TrayLine>();
            if (state.Maple && state.TempoLine != null) lines.Add(new TrayLine { Kind = "status", Name = "tempo-status", Text = state.TempoLine, Tone = state.TempoTone });
            if (state.Wow && state.WowLine != null) lines.Add(new TrayLine { Kind = "status", Name = "wow-status", Text = state.WowLine, Tone = state.WowTone });
            if (lines.Count > 0) lines.Add(new TrayLine { Kind = "sep" });
            var before = lines.Count;
            if (!string.IsNullOrEmpty(state.UpdateTo)) lines.Add(new TrayLine { Kind = "main", Name = "update", Text = Texts.T("Update to {0}", state.UpdateTo), Do = actions.Update });
            if (state.Maple && !state.TempoShut)
            {
                lines.Add(new TrayLine { Kind = string.IsNullOrEmpty(state.UpdateTo) ? "main" : "action", Name = "open-tempo", Text = Texts.T("Open Tempo"), Do = actions.OpenTempo });
                if (state.TempoReady)
                {
                    if (state.Watching) lines.Add(new TrayLine { Name = "stop-watching", Text = Texts.T("Stop watching"), Do = actions.StopWatching });
                    else lines.Add(new TrayLine { Name = "start-watching", Text = Texts.T("Start watching"), Do = actions.StartWatching });
                }
            }
            if (state.Wow) lines.Add(new TrayLine { Name = "upload", Text = Texts.T("Upload now"), Do = actions.UploadNow, Enabled = state.WowReady });
            if (lines.Count > before) lines.Add(new TrayLine { Kind = "sep" });
            lines.Add(new TrayLine { Name = "open-window", Text = Texts.T("Open GameMargins Desktop"), Do = actions.OpenWindow });
            lines.Add(new TrayLine { Name = "settings", Text = Texts.T("Settings"), Do = actions.OpenSettings });
            lines.Add(new TrayLine { Name = "quit", Text = Texts.T("Quit"), Do = actions.Quit });
            return lines;
        }

        /// <summary>An empty menu in the program's look; `fill` puts its lines in each time it opens, so the minutes are right.</summary>
        public static ContextMenuStrip Make(Func<List<TrayLine>> fill)
        {
            var menu = new ContextMenuStrip { ShowImageMargin = false, ShowCheckMargin = false, Renderer = new TrayRenderer(), AutoSize = false, DropShadowEnabled = true };
            // A menu needs a line to open at all; the real ones come in Opening.
            menu.Items.Add(new ToolStripMenuItem(" "));
            menu.Opening += delegate { Fill(menu, fill()); };
            menu.Opened += delegate { TrayPaint.RoundCorners(menu.Handle); };
            return menu;
        }

        public static void Fill(ContextMenuStrip menu, List<TrayLine> lines)
        {
            Fill(menu, lines, TrayPaint.ScreenScale());
        }

        /// <summary>The lines into the menu at a screen's scale (the pictures' check fills one at every scale, never shown).</summary>
        public static void Fill(ContextMenuStrip menu, List<TrayLine> lines, float scale)
        {
            menu.SuspendLayout();
            var old = new List<ToolStripItem>();
            foreach (ToolStripItem item in menu.Items) old.Add(item);
            menu.Items.Clear();
            foreach (var item in old) item.Dispose();
            menu.Padding = new Padding((int)Math.Round(TrayPaint.Pad * scale));
            menu.BackColor = Look.Now.MenuFace;
            var width = (int)Math.Round(TrayPaint.Width(lines, scale) * scale);
            foreach (var line in lines)
            {
                ToolStripItem item;
                if (line.Kind == "sep") item = new ToolStripSeparator { AutoSize = false, Size = new Size(width, (int)Math.Round(TrayPaint.SepHeight * scale)), Margin = Padding.Empty };
                else
                {
                    var entry = new ToolStripMenuItem(line.Text) { Name = line.Name ?? "", AutoSize = false, Size = new Size(width, (int)Math.Round(TrayPaint.LineHeight * scale)), Margin = Padding.Empty, Padding = Padding.Empty, Tag = line };
                    // A status line is read, not clicked; it stays "enabled" so Windows doesn't grey its words.
                    var act = line.Do;
                    if (line.Kind != "status" && line.Enabled && act != null) entry.Click += delegate { act(); };
                    item = entry;
                }
                menu.Items.Add(item);
            }
            // The menu's own size, said outright: its lines' width and their heights, with its padding round them. Left to
            // itself Windows' menu sizes its window from what it thinks the lines' words need in its own font and not from
            // the lines' boxes, so a long line's box reached past the window's edge and its last letter was cut there
            // (seen with a menu 262 wide round lines 295 wide).
            var pad = (int)Math.Round(TrayPaint.Pad * scale);
            var height = 0;
            foreach (ToolStripItem item in menu.Items) height += item.Height;
            menu.AutoSize = false;
            menu.Size = new Size(width + 2 * pad, height + 2 * pad);
            menu.ResumeLayout(true);
        }
    }

    /// <summary>The menu's drawing, apart from Windows' menu so a picture of it can be made without one.</summary>
    internal static class TrayPaint
    {
        public const float Pad = 4f, LineHeight = 28f, SepHeight = 9f, MinWidth = 232f;

        [DllImport("dwmapi.dll", PreserveSig = true)] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private static readonly StringFormat Near = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces, Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center };
        private static readonly Bitmap Scrap = new Bitmap(1, 1);
        private static readonly Graphics Ruler = Graphics.FromImage(Scrap);

        /// <summary>The main screen's scale (the icon by the clock lives there).</summary>
        public static float ScreenScale()
        {
            try { using (var g = Graphics.FromHwnd(IntPtr.Zero)) return Math.Max(1f, g.DpiX / 96f); } catch (Exception) { return 1f; }
        }

        /// <summary>Windows 11 rounds the menu's corners; older Windows keeps them square.</summary>
        public static void RoundCorners(IntPtr handle)
        {
            try
            {
                var round = 2;
                DwmSetWindowAttribute(handle, 33, ref round, sizeof(int));
            }
            catch (Exception) { }
        }

        private static Font FontOf(TrayLine line)
        {
            return Look.Font(13f, line.Kind == "main" ? 1 : 0);
        }

        /// <summary>What stands right of a line's words: the room Line leaves inside its box, and the menu's own air.</summary>
        public const float RightInset = 8f, RightAir = 14f;

        /// <summary>The lines' width at 96 dpi: the widest line, and never narrower than MinWidth.</summary>
        public static float Width(List<TrayLine> lines)
        {
            return Width(lines, 1f);
        }

        /// <summary>
        /// The lines' width in the menu's own measure (96 dpi) when it is drawn at `scale`: as wide as its longest line
        /// plus the usual air at the right, never narrower than MinWidth. The words are measured as they are drawn (the
        /// same hinting, at the screen's scale: letters fitted to the pixel grid are not the same width at every scale)
        /// with a little over, so a long line never touches the edge (the Dutch "MapleStory draait · Tempo is niet
        /// gestart" once lost its last letter).
        /// </summary>
        public static float Width(List<TrayLine> lines, float scale)
        {
            var most = MinWidth;
            lock (Ruler)
            {
                var state = Ruler.Save();
                Ruler.ResetTransform();
                Ruler.ScaleTransform(scale, scale);
                Ruler.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                Ruler.PixelOffsetMode = PixelOffsetMode.Half;
                foreach (var line in lines)
                {
                    if (line.Kind == "sep") continue;
                    var words = (float)Math.Ceiling(Ruler.MeasureString(line.Text, FontOf(line), 4000, Near).Width * 1.04f) + 2;
                    var w = TextLeft(line) + words + RightInset + RightAir;
                    if (w > most) most = (float)Math.Ceiling(w);
                }
                Ruler.Restore(state);
            }
            return most;
        }

        /// <summary>Where a line's words begin in its box: after the dot of a status line.</summary>
        public static float TextLeft(TrayLine line)
        {
            return line.Kind == "status" ? 28f : 14f;
        }

        public static Color ToneColour(Palette p, string tone)
        {
            return tone == "ok" ? p.Positive : tone == "warn" ? p.Warning : tone == "bad" ? p.Negative : p.Muted;
        }

        /// <summary>One line into `r` (pixels at 96 dpi; the caller scaled the drawing).</summary>
        public static void Line(Graphics g, Palette p, TrayLine line, RectangleF r, bool hot)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            if (line.Kind == "sep")
            {
                using (var brush = new SolidBrush(p.Border)) g.FillRectangle(brush, r.X + 8, r.Y + (float)Math.Floor(r.Height / 2), r.Width - 16, 1);
                return;
            }
            var status = line.Kind == "status";
            if (hot && !status && line.Enabled)
            {
                using (var path = Look.Rounded(r, 5))
                using (var back = new SolidBrush(p.MenuHot))
                {
                    g.FillPath(back, path);
                }
            }
            var left = r.X + 14;
            if (status)
            {
                using (var dot = new SolidBrush(ToneColour(p, line.Tone))) g.FillEllipse(dot, r.X + 12, r.Y + r.Height / 2 - 3.5f, 7, 7);
                left = r.X + 28;
            }
            var ink = !line.Enabled ? Look.Mix(p.Muted, p.MenuFace, 0.35) : p.Foreground;
            using (var brush = new SolidBrush(ink)) g.DrawString(line.Text, FontOf(line), brush, new RectangleF(left, r.Y - 0.5f, r.Right - left - 8, r.Height), Near);
        }

        /// <summary>The whole menu, for a picture: its face, its edge and every line. Answers its size at 96 dpi.</summary>
        public static SizeF Size(List<TrayLine> lines)
        {
            return Size(lines, 1f);
        }

        /// <summary>The menu's size in its own measure (96 dpi) when it is drawn at `scale`.</summary>
        public static SizeF Size(List<TrayLine> lines, float scale)
        {
            var h = 2 * Pad;
            foreach (var line in lines) h += line.Kind == "sep" ? SepHeight : LineHeight;
            return new SizeF(Width(lines, scale) + 2 * Pad, h);
        }

        public static void Menu(Graphics g, Palette p, List<TrayLine> lines, float scale)
        {
            var size = Size(lines, scale);
            g.ScaleTransform(scale, scale);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            using (var path = Look.Rounded(new RectangleF(0.5f, 0.5f, size.Width - 1, size.Height - 1), 8))
            using (var back = new SolidBrush(p.MenuFace))
            using (var edge = new Pen(p.MenuEdge, 1f))
            {
                g.FillPath(back, path);
                g.DrawPath(edge, path);
            }
            var y = Pad;
            foreach (var line in lines)
            {
                var h = line.Kind == "sep" ? SepHeight : LineHeight;
                Line(g, p, line, new RectangleF(Pad, y, size.Width - 2 * Pad, h), false);
                y += h;
            }
            g.ResetTransform();
        }
    }

    /// <summary>Windows' menu drawn with TrayPaint: the same face, edge, dots and words as the pictures.</summary>
    internal sealed class TrayRenderer : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var back = new SolidBrush(Look.Now.MenuFace)) e.Graphics.FillRectangle(back, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var edge = new Pen(Look.Now.MenuEdge, 1f)) e.Graphics.DrawRectangle(edge, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var line = e.Item.Tag as TrayLine;
            if (line == null) return;
            var scale = e.Item.Height / TrayPaint.LineHeight;
            var state = e.Graphics.Save();
            e.Graphics.ScaleTransform(scale, scale);
            TrayPaint.Line(e.Graphics, Look.Now, line, new RectangleF(0, 0, e.Item.Width / scale, TrayPaint.LineHeight), e.Item.Selected);
            e.Graphics.Restore(state);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // The words are drawn with the line (OnRenderMenuItemBackground), in the program's own font.
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var scale = e.Item.Height / TrayPaint.SepHeight;
            var state = e.Graphics.Save();
            e.Graphics.ScaleTransform(scale, scale);
            TrayPaint.Line(e.Graphics, Look.Now, new TrayLine { Kind = "sep" }, new RectangleF(0, 0, e.Item.Width / scale, TrayPaint.SepHeight), false);
            e.Graphics.Restore(state);
        }
    }
}
