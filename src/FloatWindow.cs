// Tempo's float: a small window that stays on top of the game, drawn by this program. Two sizes, switched with the
// button next to the cross: large (the three numbers an hour, the level with its bar, the monsters, the row
// "Picked up here" under them, and the buttons) and small (EXP, kills and mesos an hour, the next level, the round
// button; no monsters, so no row either). Its head is the session's time as a stopwatch with the map beside it. Light
// or dark as Windows is (Look.cs).
// The buttons work like a stopwatch: while Tempo reads there is only Pause; once paused, Continue and Reset (the words
// come from the page).
//
// It keeps to the game as if it were part of it (the owner's words of 24 November 2026: "alleen boven maple altijd
// zichtbaar", "als maple klein word gemaakt dat het ook mee klein word", "zo lijkt het net of het in het spel zit
// geplakt"): on top only while the game is the window in front, else right above the game in the order of windows, so a
// window over the game is over the float too; hidden while the game is minimised and back with it; a drag sticks to the
// game's edges (its window's and its picture's under the title bar, from inside and outside) and the screen's; and a float
// at the game moves with it when the game's window moves or changes size. Without a game it is on top of nothing.
//
// It is a separate window, never a layer inside the game. A click or a drag on it never takes the keyboard away from
// the game (WS_EX_NOACTIVATE) and it is not in Alt-Tab (WS_EX_TOOLWINDOW). It is not in the picture Tempo reads either:
// the capture is of the game's window alone. "Lock the float (click through)" in Tempo's Settings lets clicks pass through
// it. Every word on it comes from the Tempo page, in the page's language; the program only draws.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    internal sealed class FloatRow
    {
        public string Label = "";
        public string Value = "";
    }

    internal sealed class FloatMonster
    {
        /// <summary>What the page calls it: the folds are kept by this.</summary>
        public string Key = "";
        public string Name = "";
        public string Kills = "";
        public string Picture;
        public string DropIcon;
        public string DropText;
        /// <summary>Everything it dropped this session, the most pieces first: a picture and a count each (the large size shows them all under the name).</summary>
        public List<FloatHereItem> Drops = new List<FloatHereItem>();
    }

    internal sealed class FloatHereItem
    {
        /// <summary>The item's name, shown when the pointer rests on its picture.</summary>
        public string Name = "";
        public string Icon;
        public string Count = "";
    }

    /// <summary>
    /// The row under the monsters that never scrolls away: what was
    /// picked up without a certain monster. "Picked up here", "not certain", "205 items", the items with the most
    /// pieces (picture and count) and "+6" for the rest.
    /// </summary>
    internal sealed class FloatHere
    {
        public const int MostItems = 5;
        public string Name = "";
        public string Note = "";
        public string Total = "";
        public List<FloatHereItem> Items = new List<FloatHereItem>();
        public string More = "";
    }

    /// <summary>What the page asks the float to show (the protocol's float "update").</summary>
    internal sealed class FloatValues
    {
        /// <summary>reading, paused, waiting or idle.</summary>
        public string State = "idle";
        /// <summary>Which session this is: one more after every Reset. A new session starts with every monster open again.</summary>
        public double SessionNo;
        /// <summary>The session's time as a stopwatch ("26:14"); "" while there is none.</summary>
        public string Clock = "";
        /// <summary>Beside the clock: the map, "Paused", "Not started".</summary>
        public string Place = "";
        public List<FloatRow> Rows = new List<FloatRow>();
        /// <summary>The next level: "Next level" and "50 h 13 min" for the small size, "Lv. 45" and "in 50 h 13 min" for the large one.</summary>
        public string NextLabel;
        public string NextLevel = "";
        public string NextValue = "";
        public string NextIn = "";
        public double NextPercent;
        public List<FloatMonster> Monsters = new List<FloatMonster>();
        /// <summary>The row "Picked up here"; null when everything picked up has its monster, or nothing was.</summary>
        public FloatHere Here;
        public string OutTitle;
        public string OutNote;
        public string Stop = "Stop";
        public string Start = "Start";
        public string Reset = "Reset";
        public string SmallLabel = "Small";
        public string LargeLabel = "Large";
        public string CloseLabel = "Close";

        public bool HasNext { get { return NextLabel != null; } }
        public bool Out { get { return OutTitle != null; } }

        public static FloatValues Parse(Dictionary<string, object> v)
        {
            var f = new FloatValues();
            if (v == null) return f;
            var state = Json.Str(v, "state");
            if (state == "reading" || state == "paused" || state == "waiting") f.State = state;
            f.SessionNo = Json.Number(v, "sessionNo");
            f.Clock = Cut(Json.Str(v, "clock") ?? "", 12);
            f.Place = Cut(Json.Str(v, "place") ?? "", 80);
            foreach (var r in Json.List(v, "rows"))
            {
                var row = r as Dictionary<string, object>;
                if (row == null || f.Rows.Count >= 3) continue;
                f.Rows.Add(new FloatRow { Label = Cut(Json.Str(row, "label") ?? "", 40), Value = Cut(Json.Str(row, "value") ?? "", 24) });
            }
            var next = Json.Object(v, "next");
            if (next != null)
            {
                f.NextLabel = Cut(Json.Str(next, "label") ?? "", 40);
                f.NextLevel = Cut(Json.Str(next, "level") ?? "", 24);
                f.NextValue = Cut(Json.Str(next, "value") ?? "", 30);
                f.NextIn = Cut(Json.Str(next, "inValue") ?? f.NextValue, 34);
                f.NextPercent = Math.Max(0, Math.Min(100, Json.Number(next, "percent")));
            }
            foreach (var m in Json.List(v, "monsters"))
            {
                var mob = m as Dictionary<string, object>;
                if (mob == null || f.Monsters.Count >= 200) continue;
                var one = new FloatMonster { Key = Cut(Json.Str(mob, "key") ?? "", 40), Name = Cut(Json.Str(mob, "name") ?? "", 80), Kills = Cut(Json.Str(mob, "kills") ?? "", 30), Picture = Json.Str(mob, "picture") };
                var drop = Json.Object(mob, "drop");
                if (drop != null)
                {
                    one.DropIcon = Json.Str(drop, "icon");
                    one.DropText = Cut(Json.Str(drop, "text") ?? "", 80);
                }
                foreach (var d in Json.List(mob, "drops"))
                {
                    var item = d as Dictionary<string, object>;
                    if (item == null || one.Drops.Count >= 60) continue;
                    one.Drops.Add(new FloatHereItem { Icon = Json.Str(item, "icon"), Count = Cut(Json.Str(item, "count") ?? "", 12), Name = Cut(Json.Str(item, "name") ?? "", 80) });
                }
                f.Monsters.Add(one);
            }
            var here = Json.Object(v, "here");
            if (here != null)
            {
                var row = new FloatHere { Name = Cut(Json.Str(here, "name") ?? "", 40), Note = Cut(Json.Str(here, "note") ?? "", 40), Total = Cut(Json.Str(here, "total") ?? "", 30), More = Cut(Json.Str(here, "more") ?? "", 8) };
                foreach (var i in Json.List(here, "items"))
                {
                    var item = i as Dictionary<string, object>;
                    if (item == null || row.Items.Count >= FloatHere.MostItems) continue;
                    row.Items.Add(new FloatHereItem { Icon = Json.Str(item, "icon"), Count = Cut(Json.Str(item, "count") ?? "", 12), Name = Cut(Json.Str(item, "name") ?? "", 80) });
                }
                f.Here = row;
            }
            var gone = Json.Object(v, "out");
            if (gone != null)
            {
                f.OutTitle = Cut(Json.Str(gone, "title") ?? "", 60);
                f.OutNote = Cut(Json.Str(gone, "note") ?? "", 240);
            }
            var buttons = Json.Object(v, "buttons");
            if (buttons != null)
            {
                f.Stop = Cut(Json.Str(buttons, "stop") ?? f.Stop, 24);
                f.Start = Cut(Json.Str(buttons, "start") ?? f.Start, 24);
                f.Reset = Cut(Json.Str(buttons, "reset") ?? f.Reset, 24);
            }
            var sizes = Json.Object(v, "sizeLabels");
            if (sizes != null)
            {
                f.SmallLabel = Cut(Json.Str(sizes, "small") ?? f.SmallLabel, 40);
                f.LargeLabel = Cut(Json.Str(sizes, "large") ?? f.LargeLabel, 40);
                f.CloseLabel = Cut(Json.Str(sizes, "close") ?? f.CloseLabel, 40);
            }
            return f;
        }

        private static string Cut(string s, int max)
        {
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }

    /// <summary>The pictures on the float (a monster, its drop), fetched once from the site and kept in memory.</summary>
    internal sealed class FloatImages
    {
        private const int Most = 400;
        private readonly Dictionary<string, Image> _known = new Dictionary<string, Image>();
        private readonly HashSet<string> _asked = new HashSet<string>();
        private readonly HttpClient _http;
        private readonly string _origin;
        private readonly Control _ui;
        /// <summary>A picture arrived: draw again.</summary>
        public Action Arrived;

        /// <summary>`origin`: only pictures of this site are fetched ("https://gamemargins.com").</summary>
        public FloatImages(string origin, Control ui)
        {
            _origin = origin;
            _ui = ui;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("GameMarginsDesktop/" + Program.Version + " (Windows)");
        }

        /// <summary>A picture that is already here (the pictures made by --shots, read from the site's own files): never fetched.</summary>
        public void Put(string url, Image image)
        {
            _known[url] = image;
        }

        public Image Get(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            Image known;
            if (_known.TryGetValue(url, out known)) return known;
            if (_asked.Count >= Most || !_asked.Add(url)) return null;
            Uri address;
            if (_origin == null || !Uri.TryCreate(url, UriKind.Absolute, out address)) return null;
            if (!string.Equals(address.GetLeftPart(UriPartial.Authority), _origin, StringComparison.OrdinalIgnoreCase)) return null;
            var task = Fetch(url, address);
            GC.KeepAlive(task);
            return null;
        }

        private async System.Threading.Tasks.Task Fetch(string url, Uri address)
        {
            try
            {
                var bytes = await _http.GetByteArrayAsync(address).ConfigureAwait(false);
                if (bytes.Length > 512 * 1024) return;
                // The picture keeps reading from its stream, so the stream stays with it.
                var image = Image.FromStream(new MemoryStream(bytes));
                _ui.BeginInvoke(new Action(delegate
                {
                    _known[url] = image;
                    if (Arrived != null) Arrived();
                }));
            }
            catch (Exception)
            {
                // No picture: the line shows without one.
            }
        }
    }

    /// <summary>Where the float's buttons were drawn, for the mouse.</summary>
    internal sealed class FloatHits
    {
        public RectangleF Strip, Size, Close, Stop, Start, Reset, Monsters, Grip;
        public int MonstersShown;
        /// <summary>The last monster the list may start at: from there on everything fits.</summary>
        public int ScrollMost;
        /// <summary>How much larger than usual the large float's letters and paddings are drawn (1 to FloatPainter.MostZoom): every box here is in the float's own measure, before this and before the screen's scale.</summary>
        public float Zoom = 1f;
        /// <summary>How large a monster's picture and an item's picture are, in pixels at 96 dpi (24 and 14 at the usual width).</summary>
        public float MonsterPx = 24f, ItemPx = 14f;
        /// <summary>What did not fit while drawing (a number cut, a row past the edge): empty in a float that is right. The pictures' check reads it.</summary>
        public List<string> Problems = new List<string>();
        /// <summary>A monster's own line, by its key: a click folds its drops away, or open again.</summary>
        public List<KeyValuePair<string, RectangleF>> Folds = new List<KeyValuePair<string, RectangleF>>();
        /// <summary>The pictures that say something when the pointer rests on them: an item's name and count.</summary>
        public List<KeyValuePair<RectangleF, string>> Tips = new List<KeyValuePair<RectangleF, string>>();
    }

    /// <summary>
    /// The float's drawing, apart from its window so a picture of it can be made without one. Its
    /// head is the session's time as a stopwatch with the map beside it (no product name, no "read
    /// 0.1 s ago"); large has the three numbers an hour, the level with its bar, the monsters, "Picked up here", and
    /// Stop; small has the four numbers in one block with the round button. Under every monster of the large size
    /// stand all its drops, a small picture and a count each, on as many lines as they need. The large size can be
    /// A small arrow before a monster folds its drops away and open again (`folded`: the keys of the folded ones).
    /// The pointer resting on a drop's picture shows the item's name (FloatTip). It can be
    /// dragged larger by its bottom right corner (`user`: the size the player gave it; empty: as tall as three
    /// monsters). Its width sets how large everything is drawn, the pictures most of all (they are what is hard to
    /// see): with t = (width - 280) / 110, from 0 to 1.5, the letters and paddings are drawn 1 + 0.12 t times the usual,
    /// a monster's picture is 24 + 12 t pixels and an item's 14 + 12 t (so 24 and 14 at the usual 280, 36 and 26 at 390,
    /// 42 and 32 from 445 on); wider than 445 only adds room, and height only ever adds room (more monsters). A row
    /// that does not fit whole shows cut off at the list's edge, so the list never ends in an empty band while there
    /// are more monsters. The small size is never resized. A locked float (clicks go
    /// through it) shows a small lock and the word for "locked" in its head and nothing that could be clicked: no
    /// buttons in the head, no arrows, no Stop, no corner; so it is shorter. Not in the game: "Waiting" in the head and
    /// a note that wraps to the float's width, in both sizes, so nothing is ever cut off. Light or dark as Windows is
    /// (Look.cs): the same shapes, the mode's own colours, one thin edge and no glow.
    /// </summary>
    internal static class FloatPainter
    {
        public const int LargeWidth = 280;
        public const int SmallWidth = 208;
        private const int Head = 30;
        private const int Row = 21;
        private const int MonsterRow = 38;
        /// <summary>Where a monster's name and its drops begin: after the arrow (none while locked) and the picture.</summary>
        private static float TextLeft { get { return (_locked ? 10f : 22f) + _monster + 8f; } }
        /// <summary>A line of drops: an item's picture and a little air.</summary>
        private static float DropLine { get { return _item + 4f; } }
        /// <summary>The float being measured or drawn is locked: no arrows (so the rows begin further left) and no buttons. Set at the door (Measure, Paint), on the one thread that draws.</summary>
        private static bool _locked;
        /// <summary>What the float being measured or drawn is drawn at, set at the door: the letters' scale, and in the float's own measure (before that scale) a monster's picture, an item's picture, the letters of an item's count and the air after an item.</summary>
        private static float _zoom = 1f, _monster = 24f, _item = 14f, _count = 11f, _air = 9f;
        /// <summary>The largest the letters and paddings go: 1.18 times the usual, from 445 wide.</summary>
        public const float MostZoom = 1.18f;
        /// <summary>The width the float was drawn for (monster 36, item 26), and how far past it the growing goes on.</summary>
        private const float PickedWidth = 390f, MostGrowth = 1.5f;

        /// <summary>How far the large float has grown at the size the player gave it: 0 at the usual width, 1 at 390, 1.5 (the most) from 445.</summary>
        private static float Growth(Size user)
        {
            if (user.IsEmpty) return 0f;
            return Math.Max(0f, Math.Min(MostGrowth, (user.Width - LargeWidth) / (PickedWidth - LargeWidth)));
        }

        /// <summary>How much larger than usual the large float's letters and paddings are drawn at the size the player gave it: 1 at the usual width, 1.12 at 390, 1.18 from 445.</summary>
        public static float Zoom(Size user)
        {
            return 1f + 0.12f * Growth(user);
        }

        /// <summary>A monster's picture and an item's picture at that size, in pixels at 96 dpi: 24 and 14 at the usual width, 36 and 26 at 390, 42 and 32 from 445.</summary>
        public static float MonsterPx(Size user) { return 24f + 12f * Growth(user); }
        public static float ItemPx(Size user) { return 14f + 12f * Growth(user); }

        /// <summary>The door: every measure below is for this float (its size, locked or not).</summary>
        private static void Door(Size user, bool small, bool locked)
        {
            var size = small ? Size.Empty : user;
            var t = Growth(size);
            _locked = locked;
            _zoom = Zoom(size);
            _monster = MonsterPx(size) / _zoom;
            _item = ItemPx(size) / _zoom;
            _count = (11f + 2.5f * t) / _zoom;
            _air = 9f + 2f * t;
        }

        private const int MonstersMost = 3;
        private const int Button = 28;
        /// <summary>The room under the last row of a locked float, which has no buttons.</summary>
        private const int Foot = 6;

        private static Font F(float px, bool semi)
        {
            return Look.Font(px, semi ? 1 : 0);
        }

        private static readonly StringFormat Near = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces, Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center };
        private static readonly StringFormat Far = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap, Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        private static readonly StringFormat Mid = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap, Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        private static readonly StringFormat Wrap = new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.None };

        private static readonly Bitmap Scrap = new Bitmap(1, 1);
        private static readonly Graphics Ruler = MakeRuler();

        /// <summary>The ruler measures letters the way the float draws them, so what is measured for the size is what is drawn.</summary>
        private static Graphics MakeRuler()
        {
            var g = Graphics.FromImage(Scrap);
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            return g;
        }

        /// <summary>How tall the waiting note is at this width.</summary>
        private static float NoteHeight(FloatValues v, float width)
        {
            if (string.IsNullOrEmpty(v.OutNote)) return 0;
            lock (Ruler) return (float)Math.Ceiling(Ruler.MeasureString(v.OutNote, F(12f, false), new SizeF(width - 20, 2000), Wrap).Height);
        }

        /// <summary>How wide a drop is: its picture, its count and the room after it.</summary>
        private static float DropWidth(Graphics g, FloatHereItem item)
        {
            return _item + 3 + (float)Math.Ceiling(g.MeasureString(item.Count, F(_count, false), 100, Near).Width) + _air;
        }

        /// <summary>On how many lines a monster's drops stand at this width.</summary>
        private static int DropLines(Graphics g, FloatMonster m, float w)
        {
            if (m.Drops.Count == 0) return 0;
            var lines = 1;
            var x = TextLeft;
            foreach (var item in m.Drops)
            {
                var width = DropWidth(g, item);
                if (x > TextLeft && x + width - _air > w - 10)
                {
                    lines++;
                    x = TextLeft;
                }
                x += width;
            }
            return lines;
        }

        /// <summary>A monster's row: 38 with no drops; else its name's line and a line per row of drops; never less than its picture.</summary>
        private static float MonsterHeight(Graphics g, FloatMonster m, float w, HashSet<string> folded)
        {
            var lines = folded != null && folded.Contains(m.Key) ? 0 : DropLines(g, m, w);
            return Math.Max(lines == 0 ? MonsterRow : 25 + lines * DropLine + 3, _monster + 4);
        }

        /// <summary>
        /// "Picked up here"'s items in their places: each item's left edge and line (0, 1, …), then "+6" when there is one;
        /// they wrap at the float's edge. Answers how many lines they take. `place`: called for each (the item, or null
        /// for "+6"; its left edge; its line; how wide its count is).
        /// </summary>
        private static int HereFlow(Graphics g, FloatHere here, float w, Action<FloatHereItem, float, int, float> place)
        {
            var left = TextLeft;
            var x = left;
            var line = 0;
            var font = F(_count, false);
            foreach (var item in here.Items)
            {
                var countWidth = g.MeasureString(item.Count, font, 100, Near).Width + 2;
                if (x > left && x + _item + 3 + countWidth > w - 10)
                {
                    x = left;
                    line++;
                }
                if (place != null) place(item, x, line, countWidth);
                x += _item + 3 + countWidth + 7;
            }
            if (here.More.Length > 0)
            {
                var moreWidth = g.MeasureString(here.More, font, 100, Near).Width + 2;
                if (x > left && x + moreWidth > w - 10)
                {
                    x = left;
                    line++;
                }
                if (place != null) place(null, x, line, moreWidth);
            }
            return line + 1;
        }

        /// <summary>The row "Picked up here": its name's line and a line per row of items (38 at the usual size), never less than its ring.</summary>
        private static float HereHeight(Graphics g, FloatHere here, float w)
        {
            return Math.Max(20 + HereFlow(g, here, w, null) * DropLine, _monster + 10);
        }

        /// <summary>The numbers over the monsters, and the foot under them (the fixed row and the buttons): what the large size always shows.</summary>
        private static float Over(FloatValues v)
        {
            return Head + 8 + 3 * Row + 2 + (v.HasNext ? 31 : 0);
        }

        private static float Under(FloatValues v, float w)
        {
            float here = 0;
            if (v.Here != null) lock (Ruler) here = 9 + HereHeight(Ruler, v.Here, w);
            return here + (_locked ? Foot : 8 + Button + 10);
        }

        /// <summary>The least the large size may be tall at this width (in the float's own measure): room for one monster with a line of drops.</summary>
        private static float Least(FloatValues v, float w)
        {
            return Over(v) + 9 + Math.Max(25 + DropLine + 3, _monster + 4) + Under(v, w);
        }

        /// <summary>The least height the large float may be dragged to at this width, in pixels at 96 dpi: one monster's room, at that width's sizes.</summary>
        public static int LeastHeight(FloatValues v, int width)
        {
            Door(new Size(Math.Max(LargeWidth, width), 1), false, false);
            return (int)Math.Ceiling(Least(v, Math.Max(LargeWidth, width) / _zoom) * _zoom);
        }

        /// <summary>The float's size in pixels at 96 dpi for what it shows. `user`: the size the player dragged the large one to (empty: none).</summary>
        public static Size Measure(FloatValues v, bool small, Size user)
        {
            return Measure(v, small, user, null, false);
        }

        public static Size Measure(FloatValues v, bool small, Size user, HashSet<string> folded)
        {
            return Measure(v, small, user, folded, false);
        }

        /// <summary>`locked`: without the buttons, so shorter by their room.</summary>
        public static Size Measure(FloatValues v, bool small, Size user, HashSet<string> folded, bool locked)
        {
            Door(user, small, locked);
            if (small)
            {
                if (v.Out) return new Size(SmallWidth, (int)(Head + 8 + NoteHeight(v, SmallWidth) + 10));
                return new Size(SmallWidth, Head + 8 + 2 * 34 + 6);
            }
            var buttons = locked ? Foot : 8 + Button + 10;
            if (user.IsEmpty)
            {
                if (v.Out) return new Size(LargeWidth, (int)Math.Ceiling(Head + 8 + NoteHeight(v, LargeWidth) + 4 + buttons));
                float h = Over(v);
                var shown = Math.Min(MonstersMost, v.Monsters.Count);
                if (shown > 0)
                {
                    h += 9;
                    lock (Ruler) for (var i = 0; i < shown; i++) h += MonsterHeight(Ruler, v.Monsters[i], LargeWidth, folded);
                }
                return new Size(LargeWidth, (int)Math.Ceiling(h + Under(v, LargeWidth)));
            }
            // Dragged: the width is the player's and sets the sizes; everything inside is measured in the float's own measure and drawn that much larger.
            var w = Math.Max(LargeWidth, user.Width);
            var z = _zoom;
            // Not in the game: as tall as the note.
            if (v.Out) return new Size(w, (int)Math.Ceiling((Head + 8 + NoteHeight(v, w / z) + 4 + buttons) * z));
            // The height is the player's too (never less than one monster's room); locked, the buttons' room goes off it, so the monsters keep theirs.
            _locked = false;
            var least = Least(v, w / z) * z;
            _locked = locked;
            var tall = Math.Max(least, user.Height) - (locked ? (8 + Button + 10 - Foot) * z : 0);
            return new Size(w, (int)Math.Round(tall));
        }

        /// <summary>
        /// Draws the float at `scale` (1 at 96 dpi) into a surface of Measure × scale. `hot`: the button under the mouse
        /// ("size", "close", "stop", "start", "reset" or null). `scroll`: the first monster shown. `light`: the mode.
        /// `user`: the size the player dragged the large one to. `locked`: clicks go through it, so nothing to click is drawn.
        /// `lockedWord`: "locked" in the program's language.
        /// </summary>
        public static FloatHits Paint(Graphics g, FloatValues v, bool small, float scale, string hot, int scroll, FloatImages images, bool light, Size user, bool locked, HashSet<string> folded, string lockedWord)
        {
            var t = Look.Of(light);
            var hits = new FloatHits();
            var outer = Measure(v, small, user, folded, locked);
            Door(user, small, locked);
            var z = _zoom;
            hits.Zoom = z;
            hits.MonsterPx = _monster * z;
            hits.ItemPx = _item * z;
            // Drawn in the float's own measure and made larger as a whole: at the usual width that is the usual float, to the pixel.
            var size = new SizeF(outer.Width / z, outer.Height / z);
            using (var face = new SolidBrush(t.FloatFace)) g.FillRectangle(face, 0, 0, (float)Math.Ceiling(outer.Width * scale), (float)Math.Ceiling(outer.Height * scale));
            var device = scale;
            scale *= z;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.ScaleTransform(scale, scale);
            float w = size.Width, h = size.Height;
            using (var bg = new SolidBrush(t.FloatFace)) g.FillRectangle(bg, 0, 0, w, h);
            using (var fg = new SolidBrush(t.Foreground))
            using (var muted = new SolidBrush(t.Muted))
            using (var line = new Pen(t.Border, 1f))
            {
                // The head: the dot, the stopwatch, the map; the size button and the cross. The head is where the float is dragged.
                g.DrawLine(line, 0, Head, w, Head);
                var reading = v.State == "reading";
                using (var dot = new SolidBrush(reading ? t.Positive : t.Muted)) g.FillEllipse(dot, 10, (Head - 7) / 2f, 7, 7);
                var headRight = w - 4 - 44;
                if (locked)
                {
                    // Where the two buttons were: a small lock and the word. Nothing in a locked float can be clicked, so nothing looks as if it could.
                    var word = lockedWord ?? "";
                    var wordWidth = word.Length > 0 ? (float)Math.Ceiling(g.MeasureString(word, F(11.5f, false), 300, Near).Width) : 0;
                    // The small size has no room for the word beside a long clock: the lock alone says it.
                    if (small && wordWidth > 70) wordWidth = 0;
                    var lockLeft = w - 10 - wordWidth - (wordWidth > 0 ? 5 : 0) - 10;
                    Lock(g, new RectangleF(lockLeft, (Head - 12) / 2f, 10, 12), t.Muted);
                    if (wordWidth > 0) g.DrawString(word, F(11.5f, false), muted, new RectangleF(lockLeft + 15, 0.5f, wordWidth + 2, Head), Near);
                    headRight = lockLeft - 2;
                }
                else
                {
                    hits.Close = new RectangleF(w - 4 - 22, 4, 22, 22);
                    hits.Size = new RectangleF(w - 4 - 44, 4, 22, 22);
                }
                var first = v.Out ? v.OutTitle : v.Clock.Length > 0 ? v.Clock : v.Place;
                // The map stands beside the clock in the large size; the small one has room for a word ("Paused"), not for a map's name.
                var second = v.Out || v.Clock.Length == 0 || (small && v.State == "reading") ? "" : v.Place;
                var room = headRight - 6 - 24;
                if (g.MeasureString(first, F(13f, true), 600, Near).Width > room + 0.5f) hits.Problems.Add("the head's first words are cut");
                var firstWidth = Math.Min(room, g.MeasureString(first, F(13f, true), 600, Near).Width + 1);
                g.DrawString(first, F(13f, true), fg, new RectangleF(24, 0, firstWidth, Head), Near);
                if (second.Length > 0 && room - firstWidth > 30) g.DrawString(second, F(12f, false), muted, new RectangleF(24 + firstWidth + 7, 0.5f, room - firstWidth - 7, Head), Near);
                hits.Strip = new RectangleF(0, 0, headRight, Head);
                if (!locked)
                {
                    IconButton(g, t, hits.Size, hot == "size", small ? "grow" : "shrink");
                    IconButton(g, t, hits.Close, hot == "close", "close");
                }

                if (small) PaintSmall(g, v, t, hits, hot, w, fg, muted);
                else PaintLarge(g, v, t, hits, hot, scroll, images, w, h, fg, muted, line, folded);

                // The corner the large size is dragged larger by: two short strokes.
                if (!small && !locked && !v.Out)
                {
                    hits.Grip = new RectangleF(w - 16, h - 16, 16, 16);
                    using (var pen = new Pen(hot == "grip" ? t.Foreground : t.Muted, 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(pen, w - 4, h - 11, w - 11, h - 4);
                        g.DrawLine(pen, w - 4, h - 6.5f, w - 6.5f, h - 4);
                    }
                }

            }
            g.ResetTransform();
            // One thin edge all round, so the float stands off whatever is behind it: no shadow, no glow. As thin at every size.
            using (var edge = new Pen(t.FloatEdge, device)) g.DrawRectangle(edge, 0.5f * device, 0.5f * device, (outer.Width - 1) * device, (outer.Height - 1) * device);
            return hits;
        }

        private static void PaintLarge(Graphics g, FloatValues v, Palette t, FloatHits hits, string hot, int scroll, FloatImages images, float w, float h, Brush fg, Brush muted, Pen line, HashSet<string> folded)
        {
            float y = Head + 8;
            var body = F(12.5f, false);
            var tiny = F(11f, false);
            if (v.Out)
            {
                // Not in the game: what Tempo waits for, wrapped to the float's width.
                var noteHeight = NoteHeight(v, w);
                g.DrawString(v.OutNote, F(12f, false), muted, new RectangleF(10, y, w - 20, noteHeight + 2), Wrap);
                y += noteHeight + 4;
            }
            else
            {
                for (var i = 0; i < 3; i++)
                {
                    var row = i < v.Rows.Count ? v.Rows[i] : new FloatRow();
                    if (g.MeasureString(row.Label, body, 600, Near).Width + g.MeasureString(row.Value, F(13.5f, true), 600, Near).Width + 6 > w - 20) hits.Problems.Add("a number's row does not fit: " + row.Label);
                    g.DrawString(row.Label, body, muted, new RectangleF(10, y, w - 20, Row), Near);
                    g.DrawString(row.Value, F(13.5f, true), fg, new RectangleF(10, y, w - 20, Row), Far);
                    y += Row;
                }
                y += 2;
                if (v.HasNext)
                {
                    if (g.MeasureString(v.NextLevel.Length > 0 ? v.NextLevel : v.NextLabel, body, 600, Near).Width + g.MeasureString(v.NextIn, F(12.5f, true), 600, Near).Width + 6 > w - 20) hits.Problems.Add("the next level's row does not fit");
                    g.DrawString(v.NextLevel.Length > 0 ? v.NextLevel : v.NextLabel, body, muted, new RectangleF(10, y, w - 20, 18), Near);
                    g.DrawString(v.NextIn, F(12.5f, true), fg, new RectangleF(10, y, w - 20, 18), Far);
                    Bar(g, t, new RectangleF(10, y + 21, w - 20, 5), v.NextPercent);
                    y += 31;
                }
                // The monsters: as many rows as fit between the numbers and the foot, each as tall as its drops need;
                // the wheel scrolls by rows.
                var area = h - Under(v, w) - y - 9;
                tiny = F(_count, false);
                if (v.Monsters.Count > 0 && area > 0)
                {
                    g.DrawLine(line, 0, y + 4, w, y + 4);
                    y += 9;
                    // The last row the list may start at: from there on the rest fits.
                    var most = v.Monsters.Count - 1;
                    var rest = MonsterHeight(g, v.Monsters[most], w, folded);
                    while (most > 0 && rest + MonsterHeight(g, v.Monsters[most - 1], w, folded) <= area + 0.5f)
                    {
                        most--;
                        rest += MonsterHeight(g, v.Monsters[most], w, folded);
                    }
                    hits.ScrollMost = most;
                    var first = Math.Max(0, Math.Min(scroll, most));
                    hits.Monsters = new RectangleF(0, y, w, area);
                    var state = g.Save();
                    g.SetClip(new RectangleF(0, y, w, area), CombineMode.Intersect);
                    var ry = y;
                    var shown = 0;
                    for (var i = first; i < v.Monsters.Count; i++)
                    {
                        var m = v.Monsters[i];
                        var rowHeight = MonsterHeight(g, m, w, folded);
                        // A row that does not fit whole is still begun, cut off at the list's edge: it says there is more to scroll to.
                        var whole = ry + rowHeight <= y + area + 0.5f;
                        if (shown > 0 && ry > y + area - 6) break;
                        var isFolded = folded != null && folded.Contains(m.Key);
                        var open = m.Drops.Count > 0 && !isFolded;
                        var lineTop = ry + (open ? 5 : 10);
                        // The arrow: down while the drops show, to the right while they are folded away. A click on the monster's line turns it.
                        if (m.Drops.Count > 0 && !_locked)
                        {
                            Icon(g, isFolded ? "right" : "down", new RectangleF(8, lineTop + 4, 9, 9), hot == "fold:" + m.Key ? t.Foreground : t.Muted);
                            hits.Folds.Add(new KeyValuePair<string, RectangleF>(m.Key, RectangleF.Intersect(new RectangleF(0, ry, w, open ? 25 : rowHeight), new RectangleF(0, y, w, area))));
                        }
                        var picture = images != null ? images.Get(m.Picture) : null;
                        var pictureTop = Math.Max(ry + 1, lineTop - 3 - (_monster - 24) / 2);
                        if (picture != null) Fit(g, picture, new RectangleF(TextLeft - 8 - _monster, pictureTop, _monster, _monster));
                        else if (!string.IsNullOrEmpty(m.Picture)) Blank(g, t, new RectangleF(TextLeft - 6 - _monster, pictureTop + 2, _monster - 4, _monster - 4));
                        var killsWidth = string.IsNullOrEmpty(m.Kills) ? 0 : g.MeasureString(m.Kills, body, 200, Near).Width + 8;
                        if (g.MeasureString(m.Name, F(12.5f, true), 600, Near).Width > w - 10 - killsWidth - TextLeft + 0.5f) hits.Problems.Add("a monster's name is cut: " + m.Name);
                        g.DrawString(m.Name, F(12.5f, true), fg, new RectangleF(TextLeft, lineTop, w - 10 - killsWidth - TextLeft, 17), Near);
                        if (killsWidth > 0) g.DrawString(m.Kills, body, muted, new RectangleF(10, lineTop, w - 20, 17), Far);
                        // Every drop: its picture and how many, wrapping to the next line at the float's edge.
                        var dx = TextLeft;
                        var dy = ry + 25;
                        if (open)
                        {
                            foreach (var item in m.Drops)
                            {
                                var width = DropWidth(g, item);
                                if (dx > TextLeft && dx + width - _air > w - 10)
                                {
                                    dx = TextLeft;
                                    dy += DropLine;
                                }
                                if (dx + width - _air > w - 10 + 0.5f) hits.Problems.Add("a drop passes the edge under " + m.Name);
                                if (whole && dy + 1 + _item > ry + rowHeight + 0.5f) hits.Problems.Add("a drop's picture passes its row under " + m.Name);
                                var icon = images != null ? images.Get(item.Icon) : null;
                                if (icon != null) Fit(g, icon, new RectangleF(dx, dy + 1, _item, _item));
                                else Blank(g, t, new RectangleF(dx + 1, dy + 2, _item - 2, _item - 2));
                                g.DrawString(item.Count, tiny, muted, new RectangleF(dx + _item + 3, dy, width - _item - 3, _item + 2), Near);
                                // The name on hover only where the picture shows (a row cut off at the list's edge has less to point at).
                                var tipBox = RectangleF.Intersect(new RectangleF(dx - 2, dy, width - _air + 4, _item + 2), new RectangleF(0, y, w, area));
                                if (item.Name.Length > 0 && tipBox.Height > 6) hits.Tips.Add(new KeyValuePair<RectangleF, string>(tipBox, item.Name + " × " + item.Count));
                                dx += width;
                            }
                        }
                        ry += rowHeight;
                        // The first row counts as shown even when it is cut (a float at its least height with a monster of many drops).
                        if (!whole && shown > 0) break;
                        shown++;
                        if (!whole) break;
                    }
                    g.Restore(state);
                    hits.MonstersShown = shown;
                    // More monsters than fit: a thin mark at the right says where in the list this is.
                    if (most > 0)
                    {
                        var track = area - 6f;
                        var thumb = Math.Max(10f, track * shown / v.Monsters.Count);
                        var at = (track - thumb) * first / most;
                        using (var mark = new SolidBrush(t.Edge)) g.FillRectangle(mark, w - 4, y + 3 + at, 2, thumb);
                    }
                    y += area;
                }
                else if (area > 0) y += 9 + area;
                // "Picked up here": under the monsters, never scrolled away.
                if (v.Here != null)
                {
                    g.DrawLine(line, 0, y + 4, w, y + 4);
                    y += 9;
                    PaintHere(g, t, v.Here, images, y, w, fg, muted, tiny, hits);
                    y += HereHeight(g, v.Here, w);
                }
            }
            // A locked float has no buttons: no click reaches it.
            if (_locked)
            {
                if (y + Foot > h + 1f) hits.Problems.Add("the rows pass the bottom");
                return;
            }
            // The buttons, as on a stopwatch: Stop while Tempo reads; Start and Reset once it is stopped.
            var top = y + 8;
            if (top + Button + 10 > h + 1f) hits.Problems.Add("the buttons pass the bottom");
            if (v.State == "paused")
            {
                var half = (w - 20 - 8) / 2f;
                hits.Start = new RectangleF(10, top, half, Button);
                hits.Reset = new RectangleF(10 + half + 8, top, half, Button);
                TextButton(g, t, hits.Start, hot == "start", v.Start);
                TextButton(g, t, hits.Reset, hot == "reset", v.Reset);
            }
            else if (v.State == "idle")
            {
                hits.Start = new RectangleF(10, top, w - 20, Button);
                TextButton(g, t, hits.Start, hot == "start", v.Start);
            }
            else
            {
                hits.Stop = new RectangleF(10, top, w - 20, Button);
                TextButton(g, t, hits.Stop, hot == "stop", v.Stop);
            }
        }

        /// <summary>The row "Picked up here" at `y`: a grey ring with "?" where a monster's picture stands, the name with "· not certain", the pieces at the right, and under the name the items with the most pieces (on a second line when they do not fit one).</summary>
        private static void PaintHere(Graphics g, Palette t, FloatHere here, FloatImages images, float y, float w, Brush fg, Brush muted, Font tiny, FloatHits hits)
        {
            var left = TextLeft;
            var height = HereHeight(g, here, w);
            var ring = _monster - 4;
            var ringBox = new RectangleF(left - 8 - _monster + 2, y + (height - ring) / 2, ring, ring);
            using (var pen = new Pen(t.Muted, 1f) { DashStyle = DashStyle.Dash }) g.DrawEllipse(pen, ringBox);
            g.DrawString("?", F(11f * ring / 20f, true), muted, ringBox, Mid);
            var note = F(12f, false);
            var totalWidth = here.Total.Length > 0 ? g.MeasureString(here.Total, note, 200, Near).Width + 8 : 0;
            if (totalWidth > 0) g.DrawString(here.Total, note, muted, new RectangleF(10, y + 3, w - 20, 17), Far);
            var room = w - 10 - totalWidth - left;
            var nameWidth = Math.Min(room, g.MeasureString(here.Name, F(12.5f, true), 400, Near).Width + 2);
            if (g.MeasureString(here.Name, F(12.5f, true), 400, Near).Width > room + 0.5f) hits.Problems.Add("\"" + here.Name + "\" is cut");
            g.DrawString(here.Name, F(12.5f, true), fg, new RectangleF(left, y + 3, nameWidth, 17), Near);
            if (here.Note.Length > 0 && room - nameWidth > 24) g.DrawString("· " + here.Note, note, muted, new RectangleF(left + nameWidth + 3, y + 3, room - nameWidth - 3, 17), Near);
            HereFlow(g, here, w, delegate(FloatHereItem item, float x, int line, float countWidth)
            {
                var top = y + 20 + line * DropLine;
                if (x + (item != null ? _item + 3 : 0) + countWidth > w - 10 + 2.5f) hits.Problems.Add("\"" + here.Name + "\": an item passes the edge");
                if (item == null)
                {
                    g.DrawString(here.More, tiny, muted, new RectangleF(x, top - 1, countWidth + 2, _item + 2), Near);
                    return;
                }
                var icon = images != null ? images.Get(item.Icon) : null;
                if (icon != null) Fit(g, icon, new RectangleF(x, top, _item, _item));
                else Blank(g, t, new RectangleF(x + 1, top + 1, _item - 2, _item - 2));
                g.DrawString(item.Count, tiny, muted, new RectangleF(x + _item + 3, top - 1, countWidth, _item + 2), Near);
                if (item.Name.Length > 0) hits.Tips.Add(new KeyValuePair<RectangleF, string>(new RectangleF(x - 2, top - 1, _item + 3 + countWidth + 4, _item + 2), item.Name + " × " + item.Count));
            });
        }

        // ---- the small label that says an item's name (FloatTip shows it; --shots draws it)

        public static Size TipSize(string text)
        {
            lock (Ruler) return new Size((int)Math.Ceiling(Ruler.MeasureString(text, F(12f, false), 600, Near).Width) + 17, 24);
        }

        public static void PaintTip(Graphics g, string text, float scale, bool light)
        {
            var t = Look.Of(light);
            var size = TipSize(text);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.ScaleTransform(scale, scale);
            using (var bg = new SolidBrush(t.Raised)) g.FillRectangle(bg, 0, 0, size.Width, size.Height);
            using (var edge = new Pen(t.Edge, 1f)) g.DrawRectangle(edge, 0.5f, 0.5f, size.Width - 1, size.Height - 1);
            using (var fg = new SolidBrush(t.Foreground)) g.DrawString(text, F(12f, false), fg, new RectangleF(8, 0, size.Width - 8, size.Height - 1), Near);
            g.ResetTransform();
        }

        private static void PaintSmall(Graphics g, FloatValues v, Palette t, FloatHits hits, string hot, float w, Brush fg, Brush muted)
        {
            float top = Head + 8;
            if (v.Out)
            {
                var noteHeight = NoteHeight(v, w);
                // Not in the game: the note over the whole width, as many lines as it needs; the float is as tall as its words.
                g.DrawString(v.OutNote, F(12f, false), muted, new RectangleF(10, top, w - 20, noteHeight + 2), Wrap);
                return;
            }
            const float button = 28;
            var right = _locked ? w - 2 : w - 10 - button;
            var column = (right - 10 - 10 - 8) / 2f;
            // EXP and kills an hour on the first line, mesos an hour and the next level on the second.
            var cells = new FloatRow[4];
            cells[0] = v.Rows.Count > 0 ? v.Rows[0] : new FloatRow();
            cells[1] = v.Rows.Count > 2 ? v.Rows[2] : new FloatRow();
            cells[2] = v.Rows.Count > 1 ? v.Rows[1] : new FloatRow();
            cells[3] = v.HasNext ? new FloatRow { Label = v.NextLabel, Value = v.NextValue } : new FloatRow();
            var number = F(13.5f, true);
            var narrow = F(11.5f, true);
            for (var i = 0; i < 4; i++)
            {
                var x = 10 + (i % 2) * (column + 8);
                var y = top + (i / 2) * 34;
                g.DrawString(cells[i].Label, F(10.5f, false), muted, new RectangleF(x, y, column, 13), Near);
                // A value too wide for its place is written smaller rather than cut.
                var wide = g.MeasureString(cells[i].Value, number, 400, Near).Width > column;
                g.DrawString(cells[i].Value, wide ? narrow : number, fg, new RectangleF(x, y + 12, column, 19), Near);
            }
            // A locked float has no buttons.
            if (_locked) return;
            // The round buttons: pause while Tempo reads; play and reset once it is stopped.
            if (v.State == "paused")
            {
                hits.Start = new RectangleF(right, top + 3, button, button);
                hits.Reset = new RectangleF(right, top + 3 + button + 6, button, button);
                RoundButton(g, t, hits.Start, hot == "start", "play");
                RoundButton(g, t, hits.Reset, hot == "reset", "reset");
            }
            else if (v.State == "idle")
            {
                hits.Start = new RectangleF(right, top + 19, button, button);
                RoundButton(g, t, hits.Start, hot == "start", "play");
            }
            else
            {
                hits.Stop = new RectangleF(right, top + 19, button, button);
                RoundButton(g, t, hits.Stop, hot == "stop", "pause");
            }
        }

        private static void Fit(Graphics g, Image image, RectangleF box)
        {
            try
            {
                var ratio = Math.Min(box.Width / image.Width, box.Height / image.Height);
                var iw = image.Width * ratio;
                var ih = image.Height * ratio;
                g.DrawImage(image, box.X + (box.Width - iw) / 2, box.Y + (box.Height - ih), iw, ih);
            }
            catch (Exception)
            {
                // A picture that will not draw is left out.
            }
        }

        /// <summary>A small padlock: clicks go through this float.</summary>
        private static void Lock(Graphics g, RectangleF b, Color colour)
        {
            using (var brush = new SolidBrush(colour))
            using (var pen = new Pen(colour, 1.4f))
            using (var body = Look.Rounded(new RectangleF(b.X, b.Y + 5, b.Width, b.Height - 5), 1.5f))
            {
                g.DrawArc(pen, b.X + 2, b.Y + 0.7f, b.Width - 4, 8, 180, 180);
                g.FillPath(brush, body);
            }
        }

        /// <summary>A quiet square where a picture has not arrived (yet).</summary>
        private static void Blank(Graphics g, Palette t, RectangleF box)
        {
            using (var path = Look.Rounded(box, 4))
            using (var brush = new SolidBrush(t.Raised))
            {
                g.FillPath(brush, path);
            }
        }

        private static void Bar(Graphics g, Palette t, RectangleF r, double percent)
        {
            using (var track = Look.Rounded(r, r.Height / 2))
            using (var back = new SolidBrush(t.Raised))
            {
                g.FillPath(back, track);
            }
            var width = (float)(r.Width * percent / 100.0);
            if (width < r.Height) return;
            using (var fill = Look.Rounded(new RectangleF(r.X, r.Y, width, r.Height), r.Height / 2))
            using (var blue = new SolidBrush(t.Measure))
            {
                g.FillPath(blue, fill);
            }
        }

        private static void IconButton(Graphics g, Palette t, RectangleF r, bool hot, string icon)
        {
            if (hot)
            {
                using (var path = Look.Rounded(r, 4))
                using (var back = new SolidBrush(t.Raised))
                {
                    g.FillPath(back, path);
                }
            }
            Icon(g, icon, new RectangleF(r.X + (r.Width - 12) / 2, r.Y + (r.Height - 12) / 2, 12, 12), hot ? t.Foreground : t.Muted);
        }

        private static void TextButton(Graphics g, Palette t, RectangleF r, bool hot, string text)
        {
            using (var path = Look.Rounded(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1), Look.Radius))
            using (var back = new SolidBrush(hot ? t.SelectedSurface : t.Raised))
            using (var edge = new Pen(hot ? t.Edge : t.Border, 1f))
            using (var fg = new SolidBrush(t.Foreground))
            {
                g.FillPath(back, path);
                g.DrawPath(edge, path);
                g.DrawString(text, F(12.5f, true), fg, new RectangleF(r.X, r.Y - 0.5f, r.Width, r.Height), Mid);
            }
        }

        private static void RoundButton(Graphics g, Palette t, RectangleF r, bool hot, string icon)
        {
            using (var back = new SolidBrush(hot ? t.SelectedSurface : t.Raised))
            using (var edge = new Pen(t.Edge, 1f))
            {
                g.FillEllipse(back, r);
                g.DrawEllipse(edge, r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1);
            }
            Icon(g, icon, new RectangleF(r.X + (r.Width - 12) / 2, r.Y + (r.Height - 12) / 2, 12, 12), t.Foreground);
        }

        /// <summary>The float's small drawings, each in a box of 12: pause, play, reset, shrink, grow, close.</summary>
        private static void Icon(Graphics g, string icon, RectangleF b, Color colour)
        {
            Func<float, float, PointF> p = delegate(float x, float y) { return new PointF(b.X + b.Width * x / 24f, b.Y + b.Height * y / 24f); };
            using (var brush = new SolidBrush(colour))
            using (var pen = new Pen(colour, 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                switch (icon)
                {
                    case "pause":
                        g.FillRectangle(brush, p(6, 5).X, p(6, 5).Y, b.Width * 4 / 24f, b.Height * 14 / 24f);
                        g.FillRectangle(brush, p(14, 5).X, p(14, 5).Y, b.Width * 4 / 24f, b.Height * 14 / 24f);
                        break;
                    case "play":
                        g.FillPolygon(brush, new[] { p(7.5f, 4.5f), p(7.5f, 19.5f), p(20f, 12) });
                        break;
                    case "reset":
                        // Most of a circle, turning back to an arrow at its top left.
                        g.DrawArc(pen, p(3, 3).X, p(3, 3).Y, b.Width * 18 / 24f, b.Height * 18 / 24f, 215, 305);
                        g.DrawLines(pen, new[] { p(3, 3), p(3, 8), p(8, 8) });
                        break;
                    case "down":
                        g.FillPolygon(brush, new[] { p(3, 7), p(21, 7), p(12, 19) });
                        break;
                    case "right":
                        g.FillPolygon(brush, new[] { p(7, 3), p(7, 21), p(19, 12) });
                        break;
                    case "shrink":
                        g.DrawLines(pen, new[] { p(4, 14), p(10, 14), p(10, 20) });
                        g.DrawLine(pen, p(3, 21), p(10, 14));
                        g.DrawLines(pen, new[] { p(20, 10), p(14, 10), p(14, 4) });
                        g.DrawLine(pen, p(21, 3), p(14, 10));
                        break;
                    case "grow":
                        g.DrawLines(pen, new[] { p(15, 3), p(21, 3), p(21, 9) });
                        g.DrawLine(pen, p(21, 3), p(14, 10));
                        g.DrawLines(pen, new[] { p(9, 21), p(3, 21), p(3, 15) });
                        g.DrawLine(pen, p(3, 21), p(10, 14));
                        break;
                    default:
                        g.DrawLine(pen, p(18, 6), p(6, 18));
                        g.DrawLine(pen, p(6, 6), p(18, 18));
                        break;
                }
            }
        }
    }

    /// <summary>
    /// The small label that says an item's name when the pointer rests on its picture in the float. A window of its
    /// own that can never take the keyboard or a click: not activated, not in Alt-Tab, and the mouse goes through it.
    /// </summary>
    internal sealed class FloatTip : Form
    {
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int how);

        private string _text = "";
        private float _scale = 1f;

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Topmost, a tool window, never activated, and transparent to the mouse.
                cp.ExStyle |= 0x8 | 0x80 | 0x08000000 | 0x20 | 0x80000;
                return cp;
            }
        }

        [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr h, uint key, byte alpha, uint flags);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // A layered window that lets the mouse through must be told it is fully there.
            try { SetLayeredWindowAttributes(Handle, 0, 255, 2); } catch (Exception) { }
        }

        public FloatTip()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        /// <summary>Shows `text` with its top left corner at `at` (kept on the screen), without taking anything from the game.</summary>
        public void ShowAt(string text, Point at, float scale)
        {
            _text = text;
            _scale = scale;
            var size = FloatPainter.TipSize(text);
            var w = (int)Math.Round(size.Width * scale);
            var h = (int)Math.Round(size.Height * scale);
            var area = Screen.FromPoint(at).WorkingArea;
            var x = Math.Max(area.Left, Math.Min(at.X, area.Right - w));
            var y = at.Y + h > area.Bottom ? at.Y - h - (int)(24 * scale) : at.Y;
            // Placed and shown by Windows directly, with "do not activate": the form's own Show could take the keyboard.
            SetWindowPos(Handle, new IntPtr(-1), x, y, w, h, 0x0010);
            ShowWindow(Handle, 4);
            Invalidate();
        }

        public void HideNow()
        {
            if (IsHandleCreated) ShowWindow(Handle, 0);
        }

        protected override void WndProc(ref Message m)
        {
            // Never activated by a click (none reaches it anyway).
            if (m.Msg == 0x21)
            {
                m.Result = new IntPtr(3);
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            FloatPainter.PaintTip(e.Graphics, _text, _scale, Look.IsLight);
        }
    }

    internal sealed class FloatWindow : Form
    {
        private const int WS_EX_TOPMOST = 0x8, WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_LAYERED = 0x80000, WS_EX_NOACTIVATE = 0x08000000;
        private const int WM_MOUSEACTIVATE = 0x21, WM_DPICHANGED = 0x02E0, MA_NOACTIVATE = 3;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr h, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr h, int index, IntPtr value);
        [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr h, uint key, byte alpha, uint flags);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);
        [DllImport("dwmapi.dll", PreserveSig = true)] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("dwmapi.dll", PreserveSig = true)] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out NativeRect value, int size);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr h, uint command);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out NativeRect r);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr h, out NativeRect r);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr h, ref NativePoint p);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint process);
        [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)] private static extern bool PlaceWindow(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);
        private delegate void WinEventProc(IntPtr hook, uint e, IntPtr window, int obj, int child, uint thread, uint time);
        [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEventProc proc, uint process, uint thread, uint flags);
        [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint param, ref bool value, uint ini);
        private const uint SPI_GETCLIENTAREAANIMATION = 0x1042;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B, EVENT_SYSTEM_MINIMIZEEND = 0x0017, WINEVENT_OUTOFCONTEXT = 0, WINEVENT_SKIPOWNPROCESS = 2;
        /// <summary>
        /// The float's way in when the game comes back from the taskbar, timed on the owner's PC (24 November 2026): Windows
        /// unfolds the game in 160 to 190 ms, most of it from 100 ms on; the float stays clear for FadeDelayMs, then comes in
        /// over FadeMs, so it is whole as the game lands (a fade from the start stood there at full size beside a game still
        /// small: "dan ineens schiet tempo in het scherm").
        /// </summary>
        private const int FadeDelayMs = 80, FadeMs = 110;
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int L, T, R, B; }
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1), HWND_NOTOPMOST = new IntPtr(-2);
        private const uint GW_HWNDPREV = 3, SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10, SWP_NOOWNERZORDER = 0x200;
        /// <summary>How near an edge a drag sticks to it, in pixels at 96 dpi.</summary>
        private const int SnapReach = 12;

        private readonly Settings _settings;
        private readonly FloatImages _images;
        private readonly FloatTip _tip = new FloatTip();
        private readonly Timer _tipDelay = new Timer { Interval = 400 };
        private RectangleF _tipBox;
        /// <summary>The monsters whose drops are folded away, by key, for this session; a new session (the page counts them) starts with all open.</summary>
        private readonly HashSet<string> _folded = new HashSet<string>();
        private double _sessionNo = -1;
        private FloatValues _values = new FloatValues();
        private FloatHits _hits = new FloatHits();
        private bool _small;
        private bool _locked;
        private float _scale = 1f;
        private string _hot;
        private string _tipFor;
        private int _scroll;
        private bool _dragging;
        private Point _dragFrom;
        /// <summary>The size the player dragged the large float to, in pixels at 96 dpi; empty until they do.</summary>
        private Size _user;
        private bool _resizing;
        private bool _placed;

        /// <summary>The game's window (IntPtr.Zero while there is none), asked twenty times a second.</summary>
        public Func<IntPtr> GameWindowOf;
        private readonly Timer _withGame = new Timer { Interval = 50 };
        /// <summary>Whether the float is in the topmost band now (its style starts it there).</summary>
        private bool _onTop = true;
        /// <summary>Hidden because the game is minimised: open all the same, and shown again with the game.</summary>
        private bool _hiddenWithGame;
        /// <summary>The game window's visible frame at the last look, for moving with it.</summary>
        private Rectangle _gameWas;
        /// <summary>The float moved with the game since its place was last kept.</summary>
        private bool _followed;
        /// <summary>The game's process, told by Windows the moment its window moves (WinEventProc kept here, or it is collected).</summary>
        private IntPtr _moveHook;
        private uint _hookedProcess;
        private WinEventProc _onMove;
        private IntPtr _minimiseHook;
        private WinEventProc _onMinimise;
        /// <summary>The fade in, step by step (about 60 a second), from the moment the game starts to unfold.</summary>
        private readonly Timer _fade = new Timer { Interval = 15 };
        private DateTime _fadeFrom;
        private bool _fading;
        /// <summary>A placement Windows refused is said once in the log, not twenty times a second.</summary>
        private bool _refusalLogged;

        /// <summary>Open: shown, or hidden only while the game is minimised.</summary>
        public bool Open { get { return Visible || _hiddenWithGame; } }

        /// <summary>A button was pressed: "start", "stop" or "reset". The page does the rest.</summary>
        public Action<string> Command;
        /// <summary>The cross was pressed.</summary>
        public Action CloseAsked;
        /// <summary>The size was switched to "small" or "large".</summary>
        public Action<string> SizeSwitched;

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        /// <summary>`origin`: the site whose pictures the float may fetch.</summary>
        public FloatWindow(Settings settings, string origin)
        {
            _settings = settings;
            _small = settings.FloatSize == "small";
            _user = ParseSize(settings.FloatLarge);
            _images = new FloatImages(origin, this);
            _images.Arrived = delegate { Invalidate(); };
            _withGame.Tick += delegate { WithGame(); };
            _fade.Tick += delegate { FadeStep(); };
            _tipDelay.Tick += delegate
            {
                _tipDelay.Stop();
                if (_tipFor != null && Visible && !_dragging && !_resizing) _tip.ShowAt(_tipFor, new Point(Left + (int)(_tipBox.Left * _scale * _hits.Zoom), Top + (int)((_tipBox.Bottom + 4) * _scale * _hits.Zoom)), _scale * _hits.Zoom);
            };
            Text = "Tempo";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            // On top through the window's style alone (CreateParams): the form's own TopMost would take the keyboard when the float shows.
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public string SizeName { get { return _small ? "small" : "large"; } }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { _scale = Math.Max(1f, GetDpiForWindow(Handle) / 96f); } catch (Exception) { _scale = 1f; }
            try
            {
                // Windows 11 rounds the corners; older Windows keeps them square.
                var round = 2;
                DwmSetWindowAttribute(Handle, 33, ref round, sizeof(int));
            }
            catch (Exception) { }
            ApplyLock();
            _withGame.Start();
        }

        /// <summary>Shows the float where it last stood on this arrangement of screens; the first time beside the game's window, else at the screen's top right.</summary>
        public void ShowAt(Rectangle gameBounds)
        {
            if (!_placed)
            {
                _placed = true;
                var size = PixelSize();
                Point topRight;
                string kept;
                if (_settings.FloatPlaces.TryGetValue(LayoutKey(), out kept) && TryPoint(kept, out topRight)) { }
                else
                {
                    var area = Screen.PrimaryScreen.WorkingArea;
                    topRight = new Point(area.Right - 16, area.Top + 16);
                    if (!gameBounds.IsEmpty)
                    {
                        var screen = Screen.FromRectangle(gameBounds).WorkingArea;
                        if (gameBounds.Right + 12 + size.Width <= screen.Right) topRight = new Point(gameBounds.Right + 12 + size.Width, Math.Max(screen.Top + 8, gameBounds.Top));
                        else topRight = new Point(screen.Right - 16, screen.Top + 16);
                    }
                }
                Bounds = Clamp(new Rectangle(topRight.X - size.Width, topRight.Y, size.Width, size.Height));
            }
            if (!Visible) Show();
            Fit(false);
        }

        public void SetValues(FloatValues values)
        {
            var before = FloatPainter.Measure(_values, _small, _user, _folded, _locked);
            if (values.SessionNo != _sessionNo)
            {
                _sessionNo = values.SessionNo;
                _folded.Clear();
            }
            _values = values;
            if (_scroll > Math.Max(0, values.Monsters.Count - 1)) _scroll = 0;
            if (!_resizing && FloatPainter.Measure(values, _small, _user, _folded, _locked) != before) Fit(false);
            Invalidate();
        }

        public void SetLocked(bool locked)
        {
            if (_locked == locked) return;
            _locked = locked;
            if (IsHandleCreated) ApplyLock();
            // Locked, the float has no buttons and is shorter; unlocked, they are back.
            NoTip();
            if (Visible) Fit(false);
            Invalidate();
        }

        /// <summary>Locked: clicks pass through the float to whatever is under it.</summary>
        private void ApplyLock()
        {
            try
            {
                var style = GetWindowLongPtr(Handle, -20).ToInt64();
                var wanted = _locked ? style | WS_EX_TRANSPARENT | WS_EX_LAYERED : style & ~(long)(WS_EX_TRANSPARENT | WS_EX_LAYERED);
                if (wanted == style) return;
                SetWindowLongPtr(Handle, -20, new IntPtr(wanted));
                if (_locked) SetLayeredWindowAttributes(Handle, 0, 255, 2);
                Invalidate();
            }
            catch (Exception) { }
        }

        /// <summary>"320,460" as a size; empty when it is none.</summary>
        private static Size ParseSize(string text)
        {
            var parts = (text ?? "").Split(',');
            int w, h;
            if (parts.Length != 2 || !int.TryParse(parts[0], out w) || !int.TryParse(parts[1], out h) || w < FloatPainter.LargeWidth || h < 100 || w > 4000 || h > 4000) return Size.Empty;
            return new Size(w, h);
        }

        /// <summary>The corner is being dragged: the large float takes the size the mouse asks for, between the least that shows a monster and the screen's work area; its top left corner stays.</summary>
        private void ResizeTo(Point screen)
        {
            var area = Screen.FromControl(this).WorkingArea;
            // The width first: it sets how large everything is drawn, and with that the least height (one monster's room at that scale).
            var w = Math.Max(FloatPainter.LargeWidth, Math.Min((int)Math.Round((screen.X - Left) / _scale), (int)((area.Right - Left) / _scale)));
            var h = Math.Max(FloatPainter.LeastHeight(_values, w), Math.Min((int)Math.Round((screen.Y - Top) / _scale), (int)((area.Bottom - Top) / _scale)));
            if (_user.Width == w && _user.Height == h) return;
            _user = new Size(w, h);
            var size = PixelSize();
            Bounds = new Rectangle(Left, Top, size.Width, size.Height);
            Invalidate();
        }

        private Size PixelSize()
        {
            var logical = FloatPainter.Measure(_values, _small, _user, _folded, _locked);
            return new Size((int)Math.Round(logical.Width * _scale), (int)Math.Round(logical.Height * _scale));
        }

        /// <summary>The window takes the size of what it shows; its top right corner stays where it is.</summary>
        private void Fit(bool remember)
        {
            var size = PixelSize();
            if (Size != size)
            {
                // The side that sticks to an edge (the game's or the screen's) stays on it, so a float against the game's
                // right side stays against it when it turns small (the owner's test of 24 November 2026), and one on the
                // game's bottom grows upward; otherwise its top right corner stays, as before.
                var b = Bounds;
                var xs = new List<int>();
                var ys = new List<int>();
                Lines(b, Screen.FromRectangle(b).WorkingArea, 1, xs, ys);
                var left = On(b.Left, xs) && !On(b.Right, xs);
                var bottom = On(b.Bottom, ys) && !On(b.Top, ys);
                Bounds = Clamp(new Rectangle(left ? b.Left : b.Right - size.Width, bottom ? b.Bottom - size.Height : b.Top, size.Width, size.Height));
            }
            if (remember) Remember();
        }

        private static bool On(int value, List<int> lines)
        {
            foreach (var line in lines) if (Math.Abs(value - line) <= 1) return true;
            return false;
        }

        private static Rectangle Clamp(Rectangle r)
        {
            var area = Screen.FromRectangle(r).WorkingArea;
            var x = Math.Max(area.Left, Math.Min(r.X, area.Right - r.Width));
            var y = Math.Max(area.Top, Math.Min(r.Y, area.Bottom - r.Height));
            return new Rectangle(x, y, r.Width, r.Height);
        }

        private static string LayoutKey()
        {
            var parts = new List<string>();
            foreach (var s in Screen.AllScreens) parts.Add(s.Bounds.X + "," + s.Bounds.Y + "," + s.Bounds.Width + "," + s.Bounds.Height);
            parts.Sort(StringComparer.Ordinal);
            return string.Join(";", parts.ToArray());
        }

        private static bool TryPoint(string text, out Point point)
        {
            point = Point.Empty;
            var parts = (text ?? "").Split(',');
            int x, y;
            if (parts.Length != 2 || !int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y)) return false;
            point = new Point(x, y);
            return true;
        }

        // ------------------------------------------------------------------------------------------------ with the game

        /// <summary>The game's window, or IntPtr.Zero when there is none (or it is gone).</summary>
        private IntPtr Game()
        {
            var game = IntPtr.Zero;
            try { if (GameWindowOf != null) game = GameWindowOf(); } catch (Exception) { }
            return game != IntPtr.Zero && IsWindow(game) ? game : IntPtr.Zero;
        }

        /// <summary>The window's frame as the screen shows it (without Windows' invisible borders), else its rectangle.</summary>
        private static Rectangle FrameOf(IntPtr window)
        {
            NativeRect r;
            try
            {
                if (DwmGetWindowAttribute(window, 9, out r, Marshal.SizeOf(typeof(NativeRect))) == 0 && r.R > r.L) return Rectangle.FromLTRB(r.L, r.T, r.R, r.B);
            }
            catch (Exception) { }
            return GetWindowRect(window, out r) ? Rectangle.FromLTRB(r.L, r.T, r.R, r.B) : Rectangle.Empty;
        }

        /// <summary>The game's picture: the window's inside, under its title bar.</summary>
        private static Rectangle PictureOf(IntPtr window)
        {
            NativeRect r;
            if (!GetClientRect(window, out r) || r.R <= 0 || r.B <= 0) return Rectangle.Empty;
            var p = new NativePoint();
            if (!ClientToScreen(window, ref p)) return Rectangle.Empty;
            return new Rectangle(p.X, p.Y, r.R, r.B);
        }

        private static bool IsTopmost(IntPtr window)
        {
            return (GetWindowLongPtr(window, -20).ToInt64() & WS_EX_TOPMOST) != 0;
        }

        /// <summary>
        /// Twenty times a second: hidden while the game is minimised and back with it; moved with the game's window when it
        /// is at the game; on top while the game (or the float itself) is in front, else right above the game; on top of
        /// nothing without a game.
        /// </summary>
        private void WithGame()
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                var game = Game();
                if (game == IntPtr.Zero)
                {
                    if (_hiddenWithGame)
                    {
                        _hiddenWithGame = false;
                        Show();
                    }
                    _gameWas = Rectangle.Empty;
                    if (_onTop && Visible)
                    {
                        SetWindowPos(Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
                        _onTop = false;
                    }
                    return;
                }
                if (IsIconic(game))
                {
                    HideWithGame();
                    return;
                }
                // Back from the taskbar, and the hook did not say so first: in now.
                if (_hiddenWithGame) FadeIn();
                if (!Visible) return;
                Hook(game);
                var frame = FrameOf(game);
                if (!frame.IsEmpty)
                {
                    if (!_gameWas.IsEmpty && frame != _gameWas && !_dragging && !_resizing && AtGame(Bounds, _gameWas)) Follow(_gameWas, frame);
                    else if (_followed && frame == _gameWas)
                    {
                        // The game stands still again: the float's new place is kept.
                        _followed = false;
                        Remember();
                    }
                    _gameWas = frame;
                }
                var front = GetForegroundWindow();
                if (front == game || front == Handle)
                {
                    if (!_onTop)
                    {
                        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
                        _onTop = true;
                    }
                    return;
                }
                BelowFront(game);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Another window is in front: the float goes right above the game and its own small windows (the game's input
        /// method windows lie right above it), behind the nearest window of another program over the game; placed after a
        /// window that is not topmost, it is no longer topmost either. Never behind one of the game's own windows: Windows
        /// refuses that (access denied) when the game runs as administrator, and the float stayed over everything (the
        /// owner's test of 24 November 2026). With only topmost windows over the game: at the top of the others, which is
        /// right above the game too. A window that refuses (another program run as administrator) is passed for the next.
        /// </summary>
        private void BelowFront(IntPtr game)
        {
            uint gameProcess;
            GetWindowThreadProcessId(game, out gameProcess);
            var window = game;
            for (var i = 0; i < 400; i++)
            {
                window = GetWindow(window, GW_HWNDPREV);
                if (window == Handle && !_onTop) return;
                if (window == IntPtr.Zero || IsTopmost(window)) break;
                uint process;
                GetWindowThreadProcessId(window, out process);
                if (process == gameProcess) continue;
                if (PlaceWindow(Handle, window, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER))
                {
                    _onTop = false;
                    return;
                }
                LogRefusal(Marshal.GetLastWin32Error());
            }
            if (PlaceWindow(Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER)) _onTop = false;
            else LogRefusal(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Whether Windows animates a window back from the taskbar: "Animation effects" (Settings, Accessibility) or the older
        /// "Animate windows when minimizing and maximizing". Either may be on alone: on the owner's PC the older one said no
        /// while the game unfolded for some 200 ms (pictures of 24 November 2026), so both are asked.
        /// </summary>
        private static bool Animates()
        {
            try
            {
                var on = false;
                if (SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref on, 0) && on) return true;
            }
            catch (Exception) { }
            return SystemInformation.IsMinimizeRestoreAnimationEnabled;
        }

        private void LogRefusal(int error)
        {
            if (_refusalLogged) return;
            _refusalLogged = true;
            Tempo.Log("the float could not be placed in the order of windows: error " + error);
        }

        /// <summary>
        /// Told by Windows the moment the game's window moves or changes size, so the float moves with it at once rather
        /// than at the next of the twenty looks a second (the owner's test: "rent een beetje achter het maplestory scherm aan").
        /// Out of context: nothing goes into the game's process. The looks stay, for whatever the hook misses.
        /// </summary>
        private void Hook(IntPtr game)
        {
            uint process;
            GetWindowThreadProcessId(game, out process);
            if (process == 0 || process == _hookedProcess) return;
            Unhook();
            _onMove = OnGameMoved;
            _moveHook = SetWinEventHook(EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, _onMove, process, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            _onMinimise = OnGameMinimised;
            _minimiseHook = SetWinEventHook(EVENT_SYSTEM_MINIMIZEEND, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, _onMinimise, process, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            _hookedProcess = process;
        }

        private void Unhook()
        {
            if (_moveHook != IntPtr.Zero)
            {
                try { UnhookWinEvent(_moveHook); } catch (Exception) { }
            }
            if (_minimiseHook != IntPtr.Zero)
            {
                try { UnhookWinEvent(_minimiseHook); } catch (Exception) { }
            }
            _moveHook = IntPtr.Zero;
            _minimiseHook = IntPtr.Zero;
            _hookedProcess = 0;
        }

        /// <summary>
        /// The game starts to unfold from the taskbar: the float fades in over about the time Windows takes, so the two open
        /// together (the owner's words of 24 November 2026: "kan je het niet gewoon echt samen laten open gaan, nu gaat het
        /// gewoon apart"). Going to the taskbar stays as it was, the look's ("minimaliseren loopt wel synchroon").
        /// </summary>
        private void OnGameMinimised(IntPtr hook, uint e, IntPtr window, int obj, int child, uint thread, uint time)
        {
            if (obj != 0 || child != 0 || IsDisposed) return;
            try
            {
                if (window != Game()) return;
                if (e == EVENT_SYSTEM_MINIMIZEEND && _hiddenWithGame) FadeIn();
            }
            catch (Exception) { }
        }

        private void HideWithGame()
        {
            if (!Visible) return;
            NoTip();
            EndFade();
            _hiddenWithGame = true;
            Hide();
        }

        /// <summary>Shows the float again with the game: fading in when Windows animates, at once when it does not.</summary>
        private void FadeIn()
        {
            _hiddenWithGame = false;
            _gameWas = Rectangle.Empty;
            if (!Animates())
            {
                Show();
                return;
            }
            var style = GetWindowLongPtr(Handle, -20).ToInt64();
            SetWindowLongPtr(Handle, -20, new IntPtr(style | WS_EX_LAYERED));
            SetLayeredWindowAttributes(Handle, 0, 0, 2);
            Show();
            _fadeFrom = DateTime.UtcNow;
            _fading = true;
            _fade.Start();
        }

        private void FadeStep()
        {
            if (!_fading || IsDisposed) return;
            var t = ((DateTime.UtcNow - _fadeFrom).TotalMilliseconds - FadeDelayMs) / FadeMs;
            if (t >= 1)
            {
                EndFade();
                return;
            }
            if (t <= 0) return;
            // Slow, then quicker, then slow: it settles as the game does.
            var eased = t * t * (3 - 2 * t);
            SetLayeredWindowAttributes(Handle, 0, (byte)Math.Round(255 * eased), 2);
        }

        /// <summary>The fade is over (or cut short): fully there, and no longer a layered window unless it is locked.</summary>
        private void EndFade()
        {
            if (!_fading) return;
            _fade.Stop();
            _fading = false;
            try
            {
                SetLayeredWindowAttributes(Handle, 0, 255, 2);
                if (!_locked)
                {
                    var style = GetWindowLongPtr(Handle, -20).ToInt64();
                    SetWindowLongPtr(Handle, -20, new IntPtr(style & ~(long)WS_EX_LAYERED));
                }
                Invalidate();
            }
            catch (Exception) { }
        }

        private void OnGameMoved(IntPtr hook, uint e, IntPtr window, int obj, int child, uint thread, uint time)
        {
            // The window itself (OBJID_WINDOW, no child), and only the game's: its cursor and caret move too.
            if (obj != 0 || child != 0 || IsDisposed || !Visible || _dragging || _resizing) return;
            try
            {
                var game = Game();
                if (game == IntPtr.Zero || window != game || IsIconic(game)) return;
                var frame = FrameOf(game);
                if (frame.IsEmpty || _gameWas.IsEmpty || frame == _gameWas) return;
                if (AtGame(Bounds, _gameWas)) Follow(_gameWas, frame);
                _gameWas = frame;
            }
            catch (Exception) { }
        }

        /// <summary>Whether the float touches the game's window (or is within a drag's reach of it).</summary>
        private bool AtGame(Rectangle me, Rectangle game)
        {
            var reach = (int)Math.Round(SnapReach * _scale);
            var near = game;
            near.Inflate(reach, reach);
            return near.IntersectsWith(me);
        }

        /// <summary>The game's window moved or changed size: the float keeps its place to the game's nearer sides.</summary>
        private void Follow(Rectangle was, Rectangle now)
        {
            var b = Bounds;
            var dx = b.Left + b.Width / 2 > was.Left + was.Width / 2 ? now.Right - was.Right : now.Left - was.Left;
            var dy = b.Top + b.Height / 2 > was.Top + was.Height / 2 ? now.Bottom - was.Bottom : now.Top - was.Top;
            if (dx == 0 && dy == 0) return;
            Location = new Point(b.X + dx, b.Y + dy);
            _followed = true;
        }

        /// <summary>
        /// Where a drag to `want` lands: within a few pixels of an edge it sticks to that edge, with its own side on it or the
        /// other: the game window's frame and its picture under the title bar (from inside and from outside, where the float
        /// is beside that side) and the screen's work area.
        /// </summary>
        private Point Snap(Point want)
        {
            var size = Size;
            var reach = (int)Math.Round(SnapReach * _scale);
            var xs = new List<int>();
            var ys = new List<int>();
            Lines(new Rectangle(want, size), Screen.FromPoint(Cursor.Position).WorkingArea, reach, xs, ys);
            return new Point(Nearest(want.X, size.Width, xs, reach), Nearest(want.Y, size.Height, ys, reach));
        }

        /// <summary>The lines the float sticks to: the screen's work area, and the game's frame and picture where `me` is beside them (within `reach`).</summary>
        private void Lines(Rectangle me, Rectangle area, int reach, List<int> xs, List<int> ys)
        {
            xs.Add(area.Left);
            xs.Add(area.Right);
            ys.Add(area.Top);
            ys.Add(area.Bottom);
            var game = Game();
            if (game == IntPtr.Zero || IsIconic(game)) return;
            foreach (var r in new[] { FrameOf(game), PictureOf(game) })
            {
                if (r.IsEmpty) continue;
                if (me.Bottom >= r.Top - reach && me.Top <= r.Bottom + reach)
                {
                    xs.Add(r.Left);
                    xs.Add(r.Right);
                }
                if (me.Right >= r.Left - reach && me.Left <= r.Right + reach)
                {
                    ys.Add(r.Top);
                    ys.Add(r.Bottom);
                }
            }
        }

        /// <summary>`start` moved so that its start or its end is on the nearest line within `reach`; else as it is.</summary>
        private static int Nearest(int start, int length, List<int> lines, int reach)
        {
            var best = start;
            var gap = reach + 1;
            foreach (var line in lines)
            {
                var a = Math.Abs(start - line);
                if (a < gap)
                {
                    gap = a;
                    best = line;
                }
                var b = Math.Abs(start + length - line);
                if (b < gap)
                {
                    gap = b;
                    best = line - length;
                }
            }
            return best;
        }

        private void Remember()
        {
            _settings.FloatPlaces[LayoutKey()] = Right + "," + Top;
            _settings.FloatSize = SizeName;
            _settings.Save();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            _hits = FloatPainter.Paint(e.Graphics, _values, _small, _scale, _hot, _scroll, _images, Look.IsLight, _user, _locked, _folded, Texts.T("locked"));
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        private string Under(Point pixel)
        {
            var p = new PointF(pixel.X / (_scale * _hits.Zoom), pixel.Y / (_scale * _hits.Zoom));
            if (!_small && !_hits.Grip.IsEmpty && _hits.Grip.Contains(p)) return "grip";
            if (_hits.Close.Contains(p)) return "close";
            if (_hits.Size.Contains(p)) return "size";
            if (!_hits.Stop.IsEmpty && _hits.Stop.Contains(p)) return "stop";
            if (!_hits.Start.IsEmpty && _hits.Start.Contains(p)) return "start";
            if (!_hits.Reset.IsEmpty && _hits.Reset.Contains(p)) return "reset";
            if (_hits.Strip.Contains(p)) return "strip";
            foreach (var fold in _hits.Folds) if (fold.Value.Contains(p)) return "fold:" + fold.Key;
            // Anywhere else the float is held and dragged (the owner's test of 24 November 2026: the small one could not be
            // moved by its numbers, which is nearly all of it).
            return "strip";
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (Under(e.Location) == "grip")
            {
                _resizing = true;
                Capture = true;
                return;
            }
            if (Under(e.Location) != "strip") return;
            _dragging = true;
            _dragFrom = e.Location;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_resizing)
            {
                ResizeTo(Cursor.Position);
                return;
            }
            if (_dragging)
            {
                var at = Cursor.Position;
                Location = Snap(new Point(at.X - _dragFrom.X, at.Y - _dragFrom.Y));
                return;
            }
            var under = Under(e.Location);
            Cursor = under == "grip" ? Cursors.SizeNWSE : Cursors.Default;
            var hot = under == "strip" ? null : under;
            if (hot != _hot)
            {
                _hot = hot;
                Invalidate();
            }
            // What the pointer rests on says its name after a moment: an item's picture, or a button without words.
            var p = new PointF(e.X / (_scale * _hits.Zoom), e.Y / (_scale * _hits.Zoom));
            string tip = hot == "close" ? _values.CloseLabel : hot == "size" ? (_small ? _values.LargeLabel : _values.SmallLabel) : hot == "stop" && _small ? _values.Stop : hot == "start" && _small ? _values.Start : hot == "reset" && _small ? _values.Reset : null;
            var box = hot == "close" ? _hits.Close : hot == "size" ? _hits.Size : hot == "stop" ? _hits.Stop : hot == "start" ? _hits.Start : hot == "reset" ? _hits.Reset : RectangleF.Empty;
            if (tip == null && !_small)
            {
                foreach (var one in _hits.Tips)
                {
                    if (!one.Key.Contains(p)) continue;
                    tip = one.Value;
                    box = one.Key;
                    break;
                }
            }
            if (tip != _tipFor || box != _tipBox)
            {
                _tipFor = tip;
                _tipBox = box;
                _tip.HideNow();
                _tipDelay.Stop();
                if (tip != null) _tipDelay.Start();
            }
        }

        private void NoTip()
        {
            _tipFor = null;
            _tipDelay.Stop();
            _tip.HideNow();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) NoTip();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            NoTip();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            NoTip();
            if (_hot == null) return;
            _hot = null;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_resizing)
            {
                // The size is kept, and with it the corner the float is placed by.
                _resizing = false;
                Capture = false;
                _settings.FloatLarge = _user.Width + "," + _user.Height;
                Bounds = Clamp(Bounds);
                Remember();
                return;
            }
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
                Bounds = Clamp(Bounds);
                Remember();
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            var under = Under(e.Location) ?? "";
            if (under.StartsWith("fold:", StringComparison.Ordinal))
            {
                var key = under.Substring(5);
                if (!_folded.Remove(key)) _folded.Add(key);
                NoTip();
                Fit(false);
                Invalidate();
                return;
            }
            switch (under)
            {
                case "close":
                    // On the next turn: the float goes away, which must not happen inside its own mouse event.
                    if (CloseAsked != null) BeginInvoke(new Action(delegate { if (CloseAsked != null) CloseAsked(); }));
                    break;
                case "size":
                    _small = !_small;
                    _hot = null;
                    Fit(true);
                    Invalidate();
                    if (SizeSwitched != null) SizeSwitched(SizeName);
                    break;
                case "stop":
                    if (Command != null) Command("stop");
                    break;
                case "start":
                    if (Command != null) Command("start");
                    break;
                case "reset":
                    if (Command != null) Command("reset");
                    break;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_small || _hits.MonstersShown == 0) return;
            var most = _hits.ScrollMost;
            var next = Math.Max(0, Math.Min(most, _scroll + (e.Delta < 0 ? 1 : -1)));
            if (next == _scroll) return;
            _scroll = next;
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                // A click works, and the keyboard stays where it was.
                m.Result = new IntPtr(MA_NOACTIVATE);
                return;
            }
            if (m.Msg == WM_DPICHANGED)
            {
                _scale = Math.Max(1f, (m.WParam.ToInt64() & 0xFFFF) / 96f);
                var size = PixelSize();
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, size.Width, size.Height, 0x0002 | 0x0004 | 0x0010);
                Invalidate();
                return;
            }
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _withGame.Dispose();
                _fade.Dispose();
                Unhook();
                _tipDelay.Dispose();
                _tip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
