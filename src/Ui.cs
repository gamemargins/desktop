// The window's parts, drawn by the program itself so every one of them is the same everywhere and in both modes
// (Look.cs): the button (plain, the one gold main button, a red one for unlinking), the card with a thin edge and
// round corners, the switch (no checkboxes anywhere), the status dot, the pill, the list at the left with a bar before
// the page that is open, the bars of the first start's steps. A page is one function that draws with these and says
// what can be clicked (Hit); UiCanvas shows it, at the screen's scale, and handles the mouse and the keyboard (Tab,
// Shift+Tab, Enter, Space). Because a page is only drawn, a picture of it can be made without a window (Shots.cs).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    /// <summary>Something on the page that can be clicked.</summary>
    internal sealed class Hit
    {
        public string Id;
        public RectangleF Box;
        public Action Do;
        public bool Enabled = true;
    }

    internal sealed class Ui
    {
        public const int Plain = 0, Gold = 1, Danger = 2;

        public readonly Graphics G;
        public readonly Palette P;
        public readonly List<Hit> Hits = new List<Hit>();
        /// <summary>The part under the mouse, the part the keyboard is on (shown only after a Tab), and the part held down.</summary>
        public string Hot, Focus, Down;
        /// <summary>Only what lies inside this box can be clicked (a page that scrolls); empty: everything.</summary>
        public RectangleF Clip;

        private static readonly StringFormat OneLine = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces, Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center };
        private static readonly StringFormat OneLineMid = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
        private static readonly StringFormat OneLineFar = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Far };
        private static readonly StringFormat Wrapped = new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.None };

        public Ui(Graphics g, Palette p)
        {
            G = g;
            P = p;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        // The fonts every page uses: one family, five sizes.
        public Font Body { get { return Look.Font(13f, 0); } }
        public Font Strong { get { return Look.Font(13f, 1); } }
        public Font Small { get { return Look.Font(12f, 0); } }
        public Font Tiny { get { return Look.Font(11f, 0); } }
        public Font Title { get { return Look.Font(17f, 2); } }
        public Font Question { get { return Look.Font(15f, 2); } }

        // ------------------------------------------------------------------------------------------------ text

        public float Width(string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (float)Math.Ceiling(G.MeasureString(text, font, 4000, OneLine).Width);
        }

        public float Height(string text, Font font, float width)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (float)Math.Ceiling(G.MeasureString(text, font, new SizeF(width, 4000), Wrapped).Height);
        }

        /// <summary>One line in a box, cut with "…" only when the box is too narrow. `align`: 0 left, 1 middle, 2 right.</summary>
        public void Line(string text, Font font, Color colour, float x, float y, float w, float h, int align)
        {
            if (string.IsNullOrEmpty(text) || w <= 0) return;
            using (var brush = new SolidBrush(colour)) G.DrawString(text, font, brush, new RectangleF(x, y, w, h), align == 1 ? OneLineMid : align == 2 ? OneLineFar : OneLine);
        }

        /// <summary>Text that wraps to `w`; answers how tall it came out.</summary>
        public float Para(string text, Font font, Color colour, float x, float y, float w)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var h = Height(text, font, w);
            using (var brush = new SolidBrush(colour)) G.DrawString(text, font, brush, new RectangleF(x, y, w, h + 2), Wrapped);
            return h;
        }

        // ------------------------------------------------------------------------------------------------ what can be clicked

        public void Add(string id, RectangleF box, Action act, bool enabled)
        {
            if (!Clip.IsEmpty)
            {
                box = RectangleF.Intersect(box, Clip);
                if (box.Width <= 0 || box.Height <= 0) return;
            }
            Hits.Add(new Hit { Id = id, Box = box, Do = act, Enabled = enabled && act != null });
        }

        private void FocusRing(string id, RectangleF r, float radius)
        {
            if (Focus != id) return;
            using (var path = Look.Rounded(RectangleF.Inflate(r, 2.5f, 2.5f), radius + 2.5f))
            using (var pen = new Pen(P.Info, 1.5f))
            {
                G.DrawPath(pen, path);
            }
        }

        public float ButtonWidth(string text)
        {
            return Width(text, Strong) + 24;
        }

        /// <summary>A button, 30 high, as wide as its words. `kind`: Plain, Gold (the page's one main button) or Danger (red words: unlink).</summary>
        public RectangleF Button(string id, float x, float y, string text, int kind, Action act, bool enabled)
        {
            return Button(id, x, y, ButtonWidth(text), text, kind, act, enabled);
        }

        public RectangleF Button(string id, float x, float y, float w, string text, int kind, Action act, bool enabled)
        {
            var r = new RectangleF(x, y, w, Look.ButtonHeight);
            var hot = enabled && Hot == id;
            var down = enabled && Down == id;
            Color face, edge, ink;
            if (kind == Gold)
            {
                face = down ? Look.Mix(P.AccentFill, Color.Black, 0.12) : hot ? Look.Mix(P.AccentFill, Color.White, 0.18) : P.AccentFill;
                edge = face;
                ink = P.OnAccent;
            }
            else
            {
                face = down ? P.Border : hot ? P.SelectedSurface : P.Raised;
                edge = kind == Danger ? P.NegativeEdge : hot ? P.Edge : P.Border;
                ink = kind == Danger ? P.Negative : P.Foreground;
            }
            if (!enabled)
            {
                face = Look.Mix(face, P.Background, 0.55);
                edge = Look.Mix(edge, P.Background, 0.55);
                ink = Look.Mix(ink, face, 0.5);
            }
            using (var path = Look.Rounded(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1), Look.Radius))
            using (var back = new SolidBrush(face))
            using (var pen = new Pen(edge, 1f))
            {
                G.FillPath(back, path);
                G.DrawPath(pen, path);
            }
            Line(text, Strong, ink, r.X, r.Y - 0.5f, r.Width, r.Height, 1);
            FocusRing(id, r, Look.Radius);
            Add(id, r, act, enabled);
            return r;
        }

        /// <summary>Gold words that act as a link.</summary>
        public RectangleF LinkText(string id, float x, float y, string text, Font font, Action act)
        {
            var w = Width(text, font);
            var r = new RectangleF(x, y, w, 18);
            Line(text, font, P.Accent, x, y, w + 2, 18, 0);
            if (Hot == id)
            {
                using (var pen = new Pen(P.Accent, 1f)) G.DrawLine(pen, x, y + 16.5f, x + w, y + 16.5f);
            }
            FocusRing(id, r, 3);
            Add(id, r, act, true);
            return r;
        }

        /// <summary>A switch, 34 by 18, its right edge at `right`, its middle at `cy`. On: the text's own colour ("what is on is white").</summary>
        public void Switch(string id, float right, float cy, bool on, Action act, bool enabled)
        {
            var r = new RectangleF(right - 34, cy - 9, 34, 18);
            var track = on ? P.Selected : P.Raised;
            var edge = on ? P.Selected : P.Edge;
            var knob = on ? P.Background : P.Muted;
            if (!enabled)
            {
                track = Look.Mix(track, P.Surface, 0.6);
                edge = Look.Mix(edge, P.Surface, 0.6);
                knob = Look.Mix(knob, P.Surface, 0.4);
            }
            using (var path = Look.Rounded(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1), 9))
            using (var back = new SolidBrush(track))
            using (var pen = new Pen(edge, 1f))
            using (var dot = new SolidBrush(knob))
            {
                G.FillPath(back, path);
                G.DrawPath(pen, path);
                G.FillEllipse(dot, on ? r.Right - 15 : r.X + 3, r.Y + 3, 12, 12);
            }
            FocusRing(id, r, 9);
            Add(id, r, act, enabled);
        }

        /// <summary>A tick box for "Which games do you play?": the one place a choice of several is made.</summary>
        public void Tick(float x, float y, bool on)
        {
            var r = new RectangleF(x + 0.5f, y + 0.5f, 14, 14);
            using (var path = Look.Rounded(r, 3.5f))
            using (var back = new SolidBrush(on ? P.Selected : P.Surface))
            using (var pen = new Pen(on ? P.Selected : P.Muted, 1.2f))
            {
                G.FillPath(back, path);
                G.DrawPath(pen, path);
            }
            if (!on) return;
            using (var pen = new Pen(P.Background, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                G.DrawLines(pen, new[] { new PointF(x + 3.6f, y + 7.8f), new PointF(x + 6.4f, y + 10.4f), new PointF(x + 11.2f, y + 4.8f) });
            }
        }

        /// <summary>A round mark for a choice of one: the first start's languages.</summary>
        public void Radio(float x, float y, bool on)
        {
            using (var pen = new Pen(on ? P.Selected : P.Muted, 1.3f)) G.DrawEllipse(pen, x + 0.5f, y + 0.5f, 14, 14);
            if (!on) return;
            using (var brush = new SolidBrush(P.Selected)) G.FillEllipse(brush, x + 4f, y + 4f, 7, 7);
        }

        /// <summary>How wide a choice of a few words in one control is.</summary>
        public float SegmentsWidth(string[] labels)
        {
            float w = 6;
            foreach (var label in labels) w += Width(label, Strong) + 22;
            return w;
        }

        /// <summary>
        /// A choice of a few in one control, its right edge at `right`: the picked one stands in a box with the edge in
        /// the text's own colour, the others are quiet words. Answers its left edge.
        /// </summary>
        public float Segments(string id, float right, float y, string[] labels, int picked, Action<int> pick)
        {
            var total = SegmentsWidth(labels);
            var left = right - total;
            using (var path = Look.Rounded(new RectangleF(left + 0.5f, y + 0.5f, total - 1, Look.ButtonHeight - 1), Look.Radius))
            using (var back = new SolidBrush(P.Background))
            using (var pen = new Pen(P.Border, 1f))
            {
                G.FillPath(back, path);
                G.DrawPath(pen, path);
            }
            var x = left + 3;
            for (var i = 0; i < labels.Length; i++)
            {
                var w = Width(labels[i], Strong) + 22;
                var box = new RectangleF(x, y + 3, w, Look.ButtonHeight - 6);
                var name = id + "-" + i;
                if (i == picked)
                {
                    using (var path = Look.Rounded(new RectangleF(box.X + 0.5f, box.Y + 0.5f, box.Width - 1, box.Height - 1), Look.Radius - 1))
                    using (var back = new SolidBrush(P.Raised))
                    using (var pen = new Pen(P.Selected, 1.2f))
                    {
                        G.FillPath(back, path);
                        G.DrawPath(pen, path);
                    }
                }
                Line(labels[i], Strong, i == picked ? P.Foreground : Hot == name ? P.Foreground : P.Muted, box.X, box.Y - 0.5f, box.Width, box.Height, 1);
                FocusRing(name, box, Look.Radius - 1);
                var index = i;
                Add(name, box, delegate { pick(index); }, true);
                x += w;
            }
            return left;
        }

        /// <summary>A quiet line of the list at the left: grey words, not a page ("+ Add a game").</summary>
        public void NavQuiet(string id, float x, float y, float w, string text, Action act)
        {
            var r = new RectangleF(x, y, w, 30);
            if (Hot == id)
            {
                using (var path = Look.Rounded(r, Look.Radius))
                using (var back = new SolidBrush(Look.Mix(P.Raised, P.Background, 0.5)))
                {
                    G.FillPath(back, path);
                }
            }
            Line(text, Body, Hot == id ? P.Foreground : P.Muted, x + 12, y - 0.5f, w - 22, 30, 0);
            FocusRing(id, r, Look.Radius);
            Add(id, r, act, true);
        }

        // ------------------------------------------------------------------------------------------------ shapes

        public void Card(float x, float y, float w, float h)
        {
            Card(x, y, w, h, P.Surface, P.Border);
        }

        public void Card(float x, float y, float w, float h, Color face, Color edge)
        {
            using (var path = Look.Rounded(new RectangleF(x + 0.5f, y + 0.5f, w - 1, h - 1), Look.CardRadius))
            using (var back = new SolidBrush(face))
            using (var pen = new Pen(edge, 1f))
            {
                G.FillPath(back, path);
                G.DrawPath(pen, path);
            }
        }

        public void Fill(Color colour, float x, float y, float w, float h)
        {
            using (var brush = new SolidBrush(colour)) G.FillRectangle(brush, x, y, w, h);
        }

        public void Rule(float x, float y, float w)
        {
            Fill(P.Border, x, y, w, 1);
        }

        /// <summary>The status dot, 7 across, its middle at `cy`.</summary>
        public void Dot(float x, float cy, Color colour)
        {
            using (var brush = new SolidBrush(colour)) G.FillEllipse(brush, x, cy - 3.5f, 7, 7);
        }

        /// <summary>A dot and a line after it: how something is doing. Answers the line's height (it wraps).</summary>
        public float Status(float x, float y, float w, Color dot, string text, Color colour)
        {
            Dot(x, y + 9, dot);
            return Math.Max(18, Para(text, Small, colour, x + 12, y + 1, w - 12) + 2);
        }

        /// <summary>A pill: a state in a word. `tone`: "ok", "warn", "bad" or "" (grey).</summary>
        public float Pill(float x, float y, string text, string tone)
        {
            var w = Width(text, Tiny) + 18 + (tone == "ok" ? 10 : 0);
            var face = tone == "ok" ? P.PositiveSoft : tone == "warn" ? P.WarningSoft : tone == "bad" ? P.NegativeSoft : P.Surface;
            var edge = tone == "ok" ? P.PositiveEdge : tone == "warn" ? P.WarningEdge : tone == "bad" ? P.NegativeEdge : P.Border;
            var ink = tone == "ok" ? P.Positive : tone == "warn" ? P.Warning : tone == "bad" ? P.Negative : P.Foreground;
            using (var path = Look.Rounded(new RectangleF(x + 0.5f, y + 0.5f, w - 1, 19), 10))
            using (var back = new SolidBrush(face))
            using (var pen = new Pen(edge, 1f))
            {
                G.FillPath(back, path);
                G.DrawPath(pen, path);
            }
            var left = x + 9;
            if (tone == "ok")
            {
                using (var brush = new SolidBrush(ink)) G.FillEllipse(brush, left, y + 7.5f, 5, 5);
                left += 10;
            }
            Line(text, Tiny, ink, left, y, w, 20, 0);
            return w;
        }

        /// <summary>The first start's steps: a bar per step (done grey, this one in the text's colour, to come faint) and "Step 2 of 3".</summary>
        public void Steps(float x, float y, int step, int of, string words)
        {
            for (var i = 1; i <= of; i++)
            {
                var colour = i < step ? P.Muted : i == step ? P.Selected : P.Raised;
                using (var path = Look.Rounded(new RectangleF(x + (i - 1) * 28, y + 6, 22, 4), 2))
                using (var brush = new SolidBrush(colour))
                {
                    G.FillPath(brush, path);
                }
            }
            Line(words, Small, P.Muted, x + of * 28 + 4, y, 200, 16, 0);
        }

        /// <summary>A line of the list at the left; the open page stands on a raised row with a bar before it, in the text's colour.</summary>
        public void Nav(string id, float x, float y, float w, string text, bool open, Action act)
        {
            var h = Math.Max(30, Height(text, Strong, w - 22) + 12);
            NavRow(id, x, y, w, h, text, open, act);
        }

        public float NavHeight(string text, float w)
        {
            return Math.Max(30, Height(text, Strong, w - 22) + 12);
        }

        private void NavRow(string id, float x, float y, float w, float h, string text, bool open, Action act)
        {
            var r = new RectangleF(x, y, w, h);
            if (open || Hot == id)
            {
                using (var path = Look.Rounded(r, Look.Radius))
                using (var back = new SolidBrush(open ? P.Raised : Look.Mix(P.Raised, P.Background, 0.5)))
                {
                    G.FillPath(back, path);
                }
            }
            if (open)
            {
                using (var path = Look.Rounded(new RectangleF(x, y + 6, 3, h - 12), 1.5f))
                using (var bar = new SolidBrush(P.Selected))
                {
                    G.FillPath(bar, path);
                }
            }
            Para(text, Strong, P.Foreground, x + 12, y + (h - Height(text, Strong, w - 22)) / 2 - 0.5f, w - 22);
            FocusRing(id, r, Look.Radius);
            Add(id, r, act, true);
        }

        /// <summary>A check or a cross before a line that wraps: what Tempo does (green) and what it never does (red). Answers the height.</summary>
        public float Mark(float x, float y, float w, bool yes, string text)
        {
            var colour = yes ? P.Positive : P.Negative;
            using (var pen = new Pen(colour, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                if (yes) G.DrawLines(pen, new[] { new PointF(x + 1.5f, y + 9.5f), new PointF(x + 4.8f, y + 12.8f), new PointF(x + 11f, y + 5.5f) });
                else
                {
                    G.DrawLine(pen, x + 2.5f, y + 5f, x + 10f, y + 12.5f);
                    G.DrawLine(pen, x + 10f, y + 5f, x + 2.5f, y + 12.5f);
                }
            }
            return Para(text, Body, P.Foreground, x + 20, y, w - 20);
        }
    }

    /// <summary>Shows a page drawn with Ui: the screen's scale, the mouse, the keyboard, and a wheel for a page taller than the window.</summary>
    internal sealed class UiCanvas : Control
    {
        /// <summary>Draws the page into a surface of (width, height) pixels at 96 dpi.</summary>
        public Action<Ui, float, float> Render;
        public float Zoom = 1f;
        /// <summary>How far the page's own part is scrolled, and how far it may be (the page says so while it draws).</summary>
        public float Scroll;
        public float ScrollMost;

        private List<Hit> _hits = new List<Hit>();
        private string _hot, _focus, _down;
        private bool _keys;

        public UiCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Look.Now;
            e.Graphics.Clear(p.Background);
            if (Render == null) return;
            e.Graphics.ScaleTransform(Zoom, Zoom);
            var ui = new Ui(e.Graphics, p) { Hot = _hot, Focus = _keys ? _focus : null, Down = _down };
            if (Scroll > ScrollMost) Scroll = ScrollMost;
            Render(ui, ClientSize.Width / Zoom, ClientSize.Height / Zoom);
            _hits = ui.Hits;
        }

        private Hit Under(Point pixel)
        {
            var p = new PointF(pixel.X / Zoom, pixel.Y / Zoom);
            for (var i = _hits.Count - 1; i >= 0; i--) if (_hits[i].Enabled && _hits[i].Box.Contains(p)) return _hits[i];
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var hit = Under(e.Location);
            var id = hit != null ? hit.Id : null;
            Cursor = hit != null ? Cursors.Hand : Cursors.Default;
            if (id == _hot) return;
            _hot = id;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hot == null && _down == null) return;
            _hot = null;
            _down = null;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            _keys = false;
            var hit = Under(e.Location);
            _down = hit != null ? hit.Id : null;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            var hit = Under(e.Location);
            var was = _down;
            _down = null;
            Invalidate();
            if (hit != null && hit.Id == was) Press(hit);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (ScrollMost <= 0) return;
            var next = Math.Max(0, Math.Min(ScrollMost, Scroll - e.Delta / 120f * 48f));
            if (next == Scroll) return;
            Scroll = next;
            Invalidate();
        }

        private void Press(Hit hit)
        {
            _focus = hit.Id;
            // On the next turn: the action may draw another page, which must not happen inside this event.
            var act = hit.Do;
            if (act == null) return;
            try { BeginInvoke(new Action(delegate { act(); Invalidate(); })); } catch (Exception) { }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            return key == Keys.Tab || key == Keys.Enter || key == Keys.Space || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            var list = new List<Hit>();
            foreach (var h in _hits) if (h.Enabled) list.Add(h);
            if (list.Count == 0) return;
            var at = list.FindIndex(delegate(Hit h) { return h.Id == _focus; });
            if (e.KeyCode == Keys.Tab)
            {
                _keys = true;
                at = at < 0 ? (e.Shift ? list.Count - 1 : 0) : (at + (e.Shift ? list.Count - 1 : 1)) % list.Count;
                _focus = list[at].Id;
                e.Handled = true;
                Invalidate();
            }
            else if ((e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) && at >= 0 && _keys)
            {
                e.Handled = true;
                Press(list[at]);
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            if (!_keys) return;
            _keys = false;
            Invalidate();
        }
    }
}
