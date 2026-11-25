// Tempo's float: a small window that stays on top of the game, drawn by this program. Three sizes, gone round with the
// one button next to the cross (Small → Large → Bar → Small; its picture is three small shapes with the size you have
// filled, its name card says where a click goes): large (the three numbers an hour, the level with its bar, the
// monsters, the row "Picked up here" under them, and the buttons), small (EXP, kills and mesos an hour, the next
// level, the round button; no monsters, so no row either) and the bar (from 0.4.1:
// one strip 40 high, as tall as the game's dashboard, with the stopwatch and the map, the four numbers as tiles and
// the round buttons; first at the top of the game in its middle, with a place of its own, and sticking to the game's
// middle as well as to its edges). Its head is the session's time as a stopwatch with the map's own name beside it, or
// under it when it does not fit (never cut). Light or
// dark as Windows is (Look.cs). From a page that sends them, the numbers are the session's totals with their words
// ("21,083 EXP", "+3,640 profit") with the rate small under each, in every size; and the player's own shop stands under
// the numbers (the question "Put your shop on the Market?" with Always and Not now, or one line while it is online).
// The buttons work like a stopwatch: while Tempo reads there is only Pause; once paused, Continue and Reset (the words
// come from the page).
//
// It keeps to the game as if it were part of it (the owner's words of 24 November 2026: "alleen boven maple altijd
// zichtbaar", "als maple klein word gemaakt dat het ook mee klein word", "zo lijkt het net of het in het spel zit
// geplakt"): on top only while the game is the window in front, else right above the game in the order of windows, so a
// window over the game is over the float too; hidden while the game is minimised and back with it; gone when the game
// closes and back when it is there again; a drag sticks to the game's edges (its window's and its picture's under the
// title bar, from inside and outside) and the screen's; and a float at the game moves with it when the game's window
// moves or changes size. Shown by the player without a game, it is on top of nothing.
//
// It is a separate window, never a layer inside the game. A click or a drag on it never takes the keyboard away from
// the game (WS_EX_NOACTIVATE), it brings the game to the front when another window was over it (GameToFront), and it
// is not in Alt-Tab (WS_EX_TOOLWINDOW). It is not in the picture Tempo reads either:
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

    /// <summary>
    /// One of the four numbers as the session's total with its word ("21,083" "EXP"), and under it the rate ("48,212 /
    /// hour"); the next level is "2 h 46 min" without a word and "to next level". The tone ("positive", "negative" or
    /// null) colours the total alone: profit green or red.
    /// </summary>
    internal sealed class FloatNumber
    {
        public string Total = "–";
        public string Unit = "";
        public string Rate = "";
        public string Tone;
    }

    /// <summary>
    /// The player's own shop: "ask" (the first time Tempo sees it: the question with Always and Not now), "online" (on the
    /// Market) or "open" (Tempo sees it, and it is not online); or "wait", a shop that waits for the channel (the page's
    /// `wait`: the own shop, or another player's shop Tempo just saw and could not send; the owner's pick 4 B of 25
    /// November 2026), with an orange dot. `Title` and `Note` for the large size, `Short` for the small one and the bar.
    /// Every word comes from the page.
    /// </summary>
    internal sealed class FloatShop
    {
        public string State = "";
        public string Title = "";
        public string Short = "";
        public string Note = "";
        /// <summary>The two buttons' words; null when the shop asks nothing.</summary>
        public string Always, NotNow;

        public bool Asks { get { return State == "ask" && Always != null && NotNow != null; } }
        /// <summary>One line for the small size and the bar: the short words, else the title.</summary>
        public string Line { get { return Short.Length > 0 ? Short : Title; } }
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
        /// <summary>The bar's name; a site from before the bar sends none, and then it is "Bar".</summary>
        public string BarLabel = "Bar";
        public string CloseLabel = "Close";
        /// <summary>
        /// The four numbers as totals with their words, in the float's order: EXP, profit, mesos, the next level. Null from
        /// a site that sends the numbers an hour only (`rows`): then the float is drawn as before.
        /// </summary>
        public FloatNumber[] Totals;
        /// <summary>The player's own shop; null when there is nothing of it to show.</summary>
        public FloatShop Shop;

        public bool HasNext { get { return NextLabel != null; } }
        public bool Out { get { return OutTitle != null; } }
        public bool HasTotals { get { return Totals != null; } }

        /// <summary>The name of a size ("small", "large", "bar") as the page writes it.</summary>
        public string LabelOf(string size)
        {
            return size == "small" ? SmallLabel : size == "bar" ? BarLabel : LargeLabel;
        }

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
                f.BarLabel = Cut(Json.Str(sizes, "bar") ?? f.BarLabel, 40);
                f.CloseLabel = Cut(Json.Str(sizes, "close") ?? f.CloseLabel, 40);
            }
            var totals = Json.Object(v, "totals");
            if (totals != null) f.Totals = new[] { Number(totals, "exp"), Number(totals, "profit"), Number(totals, "mesos"), Number(totals, "next") };
            var shop = Json.Object(v, "shop");
            var shopState = shop != null ? Json.Str(shop, "state") : null;
            if (shopState == "ask" || shopState == "online" || shopState == "open")
            {
                var s = new FloatShop { State = shopState, Title = Cut(Json.Str(shop, "title") ?? "", 80), Short = Cut(Json.Str(shop, "short") ?? "", 60), Note = Cut(Json.Str(shop, "note") ?? "", 80) };
                var answers = Json.Object(shop, "buttons");
                if (answers != null)
                {
                    s.Always = Cut(Json.Str(answers, "always") ?? "", 24);
                    s.NotNow = Cut(Json.Str(answers, "notNow") ?? "", 24);
                }
                if (s.Title.Length > 0 || s.Short.Length > 0) f.Shop = s;
            }
            // A shop that waits for the channel (pick 4 B): in the shop's block with an orange dot, in place of the own shop's
            // line unless that asks its question. A page before it sends no `wait`: nothing changes.
            var wait = Json.Object(v, "wait");
            if (wait != null && (f.Shop == null || f.Shop.State != "ask"))
            {
                var s = new FloatShop { State = "wait", Title = Cut(Json.Str(wait, "title") ?? "", 80), Short = Cut(Json.Str(wait, "short") ?? "", 60), Note = Cut(Json.Str(wait, "note") ?? "", 160) };
                if (s.Title.Length > 0 || s.Short.Length > 0) f.Shop = s;
            }
            return f;
        }

        /// <summary>One number of `totals` by its key; "–" when the page sent none.</summary>
        private static FloatNumber Number(Dictionary<string, object> totals, string key)
        {
            var n = Json.Object(totals, key);
            if (n == null) return new FloatNumber();
            var tone = Json.Str(n, "tone");
            return new FloatNumber
            {
                Total = Cut(Json.Str(n, "total") ?? "–", 24),
                Unit = Cut(Json.Str(n, "unit") ?? "", 16),
                Rate = Cut(Json.Str(n, "rate") ?? "", 30),
                Tone = tone == "positive" || tone == "negative" ? tone : null,
            };
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
        /// <summary>The own shop's two answers: Always, Not now.</summary>
        public RectangleF ShopAlways, ShopNotNow;
        public int MonstersShown;
        /// <summary>The last monster the list may start at: from there on everything fits.</summary>
        public int ScrollMost;
        /// <summary>How much larger than usual the large float's letters and paddings are drawn (1 to FloatPainter.MostZoom): every box here is in the float's own measure, before this and before the screen's scale.</summary>
        public float Zoom = 1f;
        /// <summary>How large a monster's picture and an item's picture are, in pixels at 96 dpi (24 and 14 at the usual width).</summary>
        public float MonsterPx = 24f, ItemPx = 14f;
        /// <summary>What did not fit while drawing (a number cut, a row past the edge): empty in a float that is right. The pictures' check reads it.</summary>
        public List<string> Problems = new List<string>();
        /// <summary>How many of the own shop's lines went on to a next line (the pictures' check asks it of a long map's name).</summary>
        public int Wrapped;
        /// <summary>The map's name as the head shows it (null when it shows none), and where: "beside" the clock, "under" it, "first, on more lines" (before the clock runs), or "".</summary>
        public string HeadMap;
        public string HeadPlace = "";
        /// <summary>How much taller than 30 the head is (a map under the clock, or before the clock runs on more lines).</summary>
        public float HeadExtra;
        /// <summary>A monster's own line, by its key: a click folds its drops away, or open again.</summary>
        public List<KeyValuePair<string, RectangleF>> Folds = new List<KeyValuePair<string, RectangleF>>();
        /// <summary>The pictures that say something when the pointer rests on them: an item's name and count.</summary>
        public List<KeyValuePair<RectangleF, string>> Tips = new List<KeyValuePair<RectangleF, string>>();
    }

    /// <summary>
    /// The float's drawing, apart from its window so a picture of it can be made without one. Its
    /// head is the session's time as a stopwatch with the map's own name beside it or under it (PlanHead; no product name, no "read
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
    ///
    /// From a page that sends `totals`, the numbers are the session's totals with their words, in two by two cells (EXP,
    /// profit; mesos, the next level): per cell the total semibold with its word small and grey right after it on the
    /// same baseline, and under them the rate, grey. A cell never cuts: the total goes from 13.5 to 11.5 to 10.5 (its word
    /// 10.5 beside 13.5, else 9.5) and the rate from 10.5 to 9.5 until they fit; what does not fit at the least is a
    /// problem the pictures' check reports. The small size is then 242 wide and 114 tall; the large one has the level's
    /// bar under the cells. The player's own shop (the page's `shop`) stands under the numbers: the question with Always
    /// and Not now, or one line with a green dot while it is online; the bar shows that line, never the question. A shop's
    /// line never ends in "…": too long for the float's width (a map's long name), it goes on to a second line and the
    /// float grows by it; in the bar it stands on two lines as a tile's total over its rate ("Shop online" over "16 offers").
    /// </summary>
    internal static class FloatPainter
    {
        public const int LargeWidth = 280;
        public const int SmallWidth = 208;
        /// <summary>The small size with totals: wider for "13,920 mesos", as tall as two cells of two lines.</summary>
        public const int SmallTotalsWidth = 242, SmallTotalsHeight = 114;
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

        /// <summary>Where the cells of totals begin and how far apart their rows are: the small size, the large one.</summary>
        private const float SmallCellTop = Head + 9, SmallCellStep = 36, LargeCellTop = Head + 10, LargeCellStep = 38;
        /// <summary>Between a total and its word.</summary>
        private const float WordGap = 3;

        /// <summary>The numbers over the monsters, and the foot under them (the fixed row, the own shop and the buttons): what the large size always shows.</summary>
        private static float Over(FloatValues v)
        {
            // Totals: two rows of cells, the level's bar (5 high) 2 under them, and its air.
            if (v.HasTotals) return LargeCellTop + 2 * LargeCellStep + 2 + 9;
            return Head + 8 + 3 * Row + 2 + (v.HasNext ? 31 : 0);
        }

        private static float Under(FloatValues v, float w)
        {
            float here = 0;
            if (v.Here != null) lock (Ruler) here = 9 + HereHeight(Ruler, v.Here, w);
            return here + ShopHeight(v, false, w) + (_locked ? Foot : 8 + Button + 10);
        }

        /// <summary>A line of the own shop's block wraps at this many pixels: 18 for its first words (the title, the small size's line), 17 for the large size's note.</summary>
        private const float ShopLine = 18, ShopNoteLine = 17;

        /// <summary>
        /// The own shop's room under the numbers: in the small size the question with its two buttons (58), else one line
        /// (30); in the large one the title and the note (45), with the two buttons under them when it asks (79). Every
        /// line that does not fit the float's width goes on to a next line (ShopLines), and the block grows by it: a map's
        /// name is never cut. A locked float has no buttons. Nothing while waiting for the game.
        /// </summary>
        private static float ShopHeight(FloatValues v, bool small, float w)
        {
            if (v.Shop == null || v.Out) return 0;
            var s = v.Shop;
            var asks = s.Asks && !_locked;
            if (small)
            {
                var ask = s.State == "ask";
                var lines = ShopLines(s.Line, F(12f, true), ask ? w - 20 : w - 32).Count;
                var more = Math.Max(0, lines - 1) * ShopLine;
                return (asks ? 58 : 30) + more;
            }
            var left = s.State == "ask" ? 10f : 22f;
            var title = ShopLines(s.Title.Length > 0 ? s.Title : s.Short, F(12.5f, true), w - 10 - left).Count;
            var note = ShopLines(s.Note, F(12f, false), w - 10 - left).Count;
            return (asks ? 79 : 45) + Math.Max(0, title - 1) * ShopLine + Math.Max(0, note - 1) * ShopNoteLine;
        }

        /// <summary>
        /// `text` in lines no wider than `width` in `font`, as the float draws them: one line when it fits; else two,
        /// broken at the last " · " where both halves fit ("16 items · Channel 14" over "Victoria Road: The Hill East of
        /// Henesys"), the dot left out at the break; else word by word on as many lines as it takes. Never cut: a single
        /// word wider than the width stands on a line of its own (and the pictures' check says so). Measured with a little
        /// room to spare, so a line that fits here fits at every screen scale.
        /// </summary>
        internal static List<string> ShopLines(string text, Font font, float width)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            lock (Ruler)
            {
                Func<string, bool> fits = delegate(string s) { return WidthOf(Ruler, s, font) <= width - 2; };
                if (fits(text))
                {
                    lines.Add(text);
                    return lines;
                }
                var parts = text.Split(new[] { " · " }, StringSplitOptions.None);
                for (var k = parts.Length - 1; k >= 1; k--)
                {
                    var first = string.Join(" · ", parts, 0, k);
                    var rest = string.Join(" · ", parts, k, parts.Length - k);
                    if (!fits(first) || !fits(rest)) continue;
                    lines.Add(first);
                    lines.Add(rest);
                    return lines;
                }
                var line = "";
                foreach (var word in text.Split(' '))
                {
                    if (word.Length == 0) continue;
                    var longer = line.Length == 0 ? word : line + " " + word;
                    if (line.Length == 0 || fits(longer))
                    {
                        line = longer;
                        continue;
                    }
                    lines.Add(TrimDot(line));
                    // A line never starts with the dot that stood between two parts.
                    line = word == "·" ? "" : word;
                }
                if (line.Length > 0) lines.Add(TrimDot(line));
                return lines;
            }
        }

        /// <summary>A line that ends in the dot between two parts loses it: the break says the same.</summary>
        private static string TrimDot(string line)
        {
            return line.EndsWith(" ·", StringComparison.Ordinal) ? line.Substring(0, line.Length - 2) : line;
        }

        /// <summary>What the head shows and where (PlanHead), in the float's own measure.</summary>
        private sealed class HeadPlan
        {
            /// <summary>The first words (the clock; before the clock runs the map or "Not started"; "Waiting"), semibold 13, and their width.</summary>
            public string First = "";
            public float FirstWidth;
            /// <summary>The first words' further lines when they do not fit beside the buttons (a long map before the clock runs).</summary>
            public List<string> FirstMore = new List<string>();
            /// <summary>The map (or "Paused") beside the clock, grey 12; null when it is not beside it.</summary>
            public string Beside;
            /// <summary>The map on lines of its own under the clock, when it does not fit beside it.</summary>
            public List<string> Under = new List<string>();
            /// <summary>Where the words end at the right (the buttons, or the lock and its word), the lock's left, and the word's width.</summary>
            public float HeadRight, LockLeft, WordWidth;
            /// <summary>How much taller than 30 the head is: 16 a line of the map under the clock (18 a line of the first words), and 2 of air.</summary>
            public float Extra { get { var n = FirstMore.Count + Under.Count; return n == 0 ? 0 : FirstMore.Count * FirstStep + Under.Count * HeadStep + 2; } }
            public float Height { get { return Head + Extra; } }
            /// <summary>The map's own name as it stands in the head (none when it does not show), for the pictures' check.</summary>
            public string Map;
        }

        /// <summary>A line under the head's first one.</summary>
        private const float HeadStep = 16;
        /// <summary>A line of the first words (semibold 13) under the first one: a little more air than the map's grey 12.</summary>
        private const float FirstStep = 18;

        /// <summary>
        /// The map's own name: what stands after "Region : " ("Victoria Road : Construction Site North of Kerning City" is
        /// "Construction Site North of Kerning City"). The region is the same on many maps; the map's own name is what the
        /// game's minimap says.
        /// </summary>
        internal static string OwnMapName(string place)
        {
            place = (place ?? "").Trim();
            var at = place.LastIndexOf(": ", StringComparison.Ordinal);
            if (at < 0) return place;
            var own = place.Substring(at + 2).Trim();
            return own.Length > 0 ? own : place;
        }

        /// <summary>
        /// The head of the float (small or large) at width `w`: the clock with the map's own name beside it in the large size
        /// ("Paused" in both sizes) when it fits there, else on a line of its own under the clock, and on more lines when it
        /// needs them; before the clock runs the map's own name in the clock's place, going on to the lines under it when
        /// it does not fit beside the buttons. Never cut, never "…": the head grows instead. The small size shows no map while
        /// Tempo reads (only the clock), as before. Measured with a little room to spare, so the plan holds at every scale.
        /// </summary>
        private static HeadPlan PlanHead(FloatValues v, bool small, float w, bool locked, string lockedWord)
        {
            var p = new HeadPlan { HeadRight = w - 4 - 22 - CycleWidth };
            lock (Ruler)
            {
                Func<string, Font, float> wide = delegate(string text, Font font) { return string.IsNullOrEmpty(text) ? 0 : Ruler.MeasureString(text, font, 4000, Near).Width; };
                if (locked)
                {
                    var word = lockedWord ?? "";
                    p.WordWidth = word.Length > 0 ? (float)Math.Ceiling(wide(word, F(11.5f, false))) : 0;
                    // The small size has no room for the word beside a long clock: the lock alone says it.
                    if (small && p.WordWidth > 70) p.WordWidth = 0;
                    p.LockLeft = w - 10 - p.WordWidth - (p.WordWidth > 0 ? 5 : 0) - 10;
                    p.HeadRight = p.LockLeft - 2;
                }
                var room = p.HeadRight - 6 - 24;
                var under = w - 10 - 24;
                var reading = v.State == "reading" && !v.Out;
                var place = reading ? OwnMapName(v.Place) : v.Place ?? "";
                if (v.Out || v.Clock.Length == 0)
                {
                    var lines = HeadLines(v.Out ? v.OutTitle ?? "" : place, F(13f, true), room, under, wide);
                    p.First = lines.Count > 0 ? lines[0] : "";
                    for (var i = 1; i < lines.Count; i++) p.FirstMore.Add(lines[i]);
                    p.FirstWidth = Math.Min(room, wide(p.First, F(13f, true)) + 1);
                    if (reading && !v.Out) p.Map = string.Join(" ", lines.ToArray());
                    return p;
                }
                p.First = v.Clock;
                p.FirstWidth = Math.Min(room, wide(p.First, F(13f, true)) + 1);
                // The small size has room for a word ("Paused"), not for a map's name.
                var second = small && reading ? "" : place;
                if (second.Length == 0) return p;
                if (wide(second, F(12f, false)) <= room - p.FirstWidth - 7 - 2) p.Beside = second;
                else p.Under = HeadLines(second, F(12f, false), under, under, wide);
                if (reading) p.Map = p.Beside ?? string.Join(" ", p.Under.ToArray());
            }
            return p;
        }

        /// <summary>`text` in lines: the first no wider than `first`, the rest no wider than `rest`, broken between words. A word wider than its line stands alone on it (the pictures' check says so).</summary>
        private static List<string> HeadLines(string text, Font font, float first, float rest, Func<string, Font, float> wide)
        {
            var lines = new List<string>();
            var line = "";
            foreach (var word in (text ?? "").Split(' '))
            {
                if (word.Length == 0) continue;
                var longer = line.Length == 0 ? word : line + " " + word;
                var room = lines.Count == 0 ? first : rest;
                if (line.Length == 0 || wide(longer, font) <= room - 2)
                {
                    line = longer;
                    continue;
                }
                lines.Add(line);
                line = word;
            }
            if (line.Length > 0 || lines.Count == 0) lines.Add(line);
            return lines;
        }

        /// <summary>How much taller than 30 the head of this float is (PlanHead), with the program's word for "locked".</summary>
        private static float HeadExtra(FloatValues v, bool small, float w, bool locked)
        {
            return PlanHead(v, small, w, locked, Texts.T("locked")).Extra;
        }

        /// <summary>The least the large size may be tall at this width (in the float's own measure): room for one monster with a line of drops.</summary>
        private static float Least(FloatValues v, float w)
        {
            return HeadExtra(v, false, w, false) + Over(v) + 9 + Math.Max(25 + DropLine + 3, _monster + 4) + Under(v, w);
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
                var width = v.HasTotals ? SmallTotalsWidth : SmallWidth;
                if (v.Out) return new Size(width, (int)(HeadExtra(v, true, width, locked) + Head + 8 + NoteHeight(v, width) + 10));
                return new Size(width, (int)Math.Ceiling(HeadExtra(v, true, width, locked) + SmallBase(v) + ShopHeight(v, true, width)));
            }
            var buttons = locked ? Foot : 8 + Button + 10;
            if (user.IsEmpty)
            {
                if (v.Out) return new Size(LargeWidth, (int)Math.Ceiling(HeadExtra(v, false, LargeWidth, locked) + Head + 8 + NoteHeight(v, LargeWidth) + 4 + buttons));
                float h = HeadExtra(v, false, LargeWidth, locked) + Over(v);
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
            if (v.Out) return new Size(w, (int)Math.Ceiling((HeadExtra(v, false, w / z, locked) + Head + 8 + NoteHeight(v, w / z) + 4 + buttons) * z));
            // The height is the player's too (never less than one monster's room); locked, the buttons' room goes off it, so the monsters keep theirs.
            _locked = false;
            var least = Least(v, w / z) * z;
            _locked = locked;
            var lockedMore = locked ? Math.Max(0, HeadExtra(v, false, w / z, true) - HeadExtra(v, false, w / z, false)) * z : 0;
            var tall = Math.Max(least, user.Height) - (locked ? (8 + Button + 10 - Foot) * z : 0) + lockedMore;
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
                // The head: the dot, the stopwatch, the map; the size button and the cross. The head is where the float is
                // dragged. A map that does not fit beside the clock stands under it, and the head is that much taller.
                var head = PlanHead(v, small, w, locked, lockedWord);
                var top = head.Height;
                g.DrawLine(line, 0, top, w, top);
                var reading = v.State == "reading";
                using (var dot = new SolidBrush(reading ? t.Positive : t.Muted)) g.FillEllipse(dot, 10, (Head - 7) / 2f, 7, 7);
                if (locked)
                {
                    // Where the two buttons were: a small lock and the word. Nothing in a locked float can be clicked, so nothing looks as if it could.
                    Lock(g, new RectangleF(head.LockLeft, (Head - 12) / 2f, 10, 12), t.Muted);
                    if (head.WordWidth > 0) g.DrawString(lockedWord, F(11.5f, false), muted, new RectangleF(head.LockLeft + 15, 0.5f, head.WordWidth + 2, Head), Near);
                }
                else
                {
                    hits.Close = new RectangleF(w - 4 - 22, 4, 22, 22);
                    hits.Size = new RectangleF(w - 4 - 22 - CycleWidth, 4, CycleWidth, 22);
                }
                var room = head.HeadRight - 6 - 24;
                var under = w - 10 - 24;
                HeadText(g, head.First, F(13f, true), fg, 24, 0, room, Head, hits, "the head's first words");
                for (var i = 0; i < head.FirstMore.Count; i++) HeadText(g, head.FirstMore[i], F(13f, true), fg, 24, Head - 4 + i * FirstStep, under, FirstStep, hits, "the head's first words");
                if (head.Beside != null) HeadText(g, head.Beside, F(12f, false), muted, 24 + head.FirstWidth + 7, 0.5f, room - head.FirstWidth - 7, Head, hits, "the head's map");
                for (var i = 0; i < head.Under.Count; i++) HeadText(g, head.Under[i], F(12f, false), muted, 24, Head - 5 + i * HeadStep, under, HeadStep, hits, "the head's map");
                hits.HeadMap = head.Map;
                hits.HeadPlace = head.Beside != null ? "beside" : head.Under.Count > 0 ? "under" : head.FirstMore.Count > 0 ? "first, on more lines" : "";
                hits.HeadExtra = head.Extra;
                hits.Strip = new RectangleF(0, 0, head.HeadRight, top);
                if (!locked)
                {
                    CycleButton(g, t, hits.Size, hot == "size", small ? "small" : "large");
                    IconButton(g, t, hits.Close, hot == "close", "close");
                }

                // Under a taller head everything stands that much lower: drawn as before and moved down as a whole.
                var down = head.Extra;
                var state = g.Save();
                g.TranslateTransform(0, down);
                if (small) PaintSmall(g, v, t, hits, hot, w, fg, muted);
                else PaintLarge(g, v, t, hits, hot, scroll, images, w, h - down, fg, muted, line, folded);
                g.Restore(state);
                if (down > 0) Lower(hits, down);

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

        /// <summary>A line of the head in its box, whole (never "…"); one wider than its room is a problem the pictures' check reports.</summary>
        private static void HeadText(Graphics g, string text, Font font, Brush brush, float x, float y, float width, float height, FloatHits hits, string what)
        {
            if (string.IsNullOrEmpty(text)) return;
            var wide = WidthOf(g, text, font);
            if (wide > width + 0.5f) hits.Problems.Add(what + " pass their room: " + text + " (" + Math.Ceiling(wide) + " in " + Math.Floor(width) + ")");
            g.DrawString(text, font, brush, new RectangleF(x, y, Math.Max(width, wide) + 2, height), NearWhole);
        }

        /// <summary>What was drawn under the head, moved `down` with it: its buttons, the monsters' list, the folds and the names on hover.</summary>
        private static void Lower(FloatHits hits, float down)
        {
            Func<RectangleF, RectangleF> lower = delegate(RectangleF r) { return r.IsEmpty ? r : new RectangleF(r.X, r.Y + down, r.Width, r.Height); };
            hits.Stop = lower(hits.Stop);
            hits.Start = lower(hits.Start);
            hits.Reset = lower(hits.Reset);
            hits.ShopAlways = lower(hits.ShopAlways);
            hits.ShopNotNow = lower(hits.ShopNotNow);
            hits.Monsters = lower(hits.Monsters);
            for (var i = 0; i < hits.Folds.Count; i++) hits.Folds[i] = new KeyValuePair<string, RectangleF>(hits.Folds[i].Key, lower(hits.Folds[i].Value));
            for (var i = 0; i < hits.Tips.Count; i++) hits.Tips[i] = new KeyValuePair<RectangleF, string>(lower(hits.Tips[i].Key), hits.Tips[i].Value);
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
                if (v.HasTotals)
                {
                    // The four totals, two by two (EXP, profit; mesos, the next level), and the level's bar under them.
                    var column = (w - 20 - 14) / 2f;
                    for (var i = 0; i < 4; i++) Cell(g, t, v.Totals[i], RateOf(v.Totals[i], i), 10 + (i % 2) * (column + 14), LargeCellTop + (i / 2) * LargeCellStep, column, hits, "the large float's cell");
                    Bar(g, t, new RectangleF(10, LargeCellTop + 2 * LargeCellStep + 2, w - 20, 5), v.HasNext ? v.NextPercent : 0);
                    y = Over(v);
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
                // The own shop, over the buttons.
                if (v.Shop != null)
                {
                    PaintShop(g, t, v.Shop, hits, hot, y, w, false);
                    y += ShopHeight(v, false, w);
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
            if (v.HasTotals) PaintSmallTotals(g, v, t, hits, hot, w);
            else PaintSmallRows(g, v, t, hits, hot, w, fg, muted);
            // The own shop under the numbers.
            if (v.Shop != null) PaintShop(g, t, v.Shop, hits, hot, SmallBase(v), w, true);
        }

        /// <summary>Where the small size's numbers end (and the own shop begins).</summary>
        private static float SmallBase(FloatValues v)
        {
            return v.HasTotals ? SmallTotalsHeight : Head + 8 + 2 * 34 + 6;
        }

        /// <summary>
        /// The small size with totals: EXP and profit on the first row, mesos and the next level on the second, each a
        /// cell of two lines; the round button (or two) at the right, its middle 2 under the cells' middle.
        /// </summary>
        private static void PaintSmallTotals(Graphics g, FloatValues v, Palette t, FloatHits hits, string hot, float w)
        {
            const float button = 28;
            var right = _locked ? w - 2 : w - 10 - button;
            var column = (right - 10 - 10 - 10) / 2f;
            for (var i = 0; i < 4; i++) Cell(g, t, v.Totals[i], RateOf(v.Totals[i], i), 10 + (i % 2) * (column + 10), SmallCellTop + (i / 2) * SmallCellStep, column, hits, "the small float's cell");
            if (_locked) return;
            var middle = SmallCellTop + SmallCellStep;
            if (v.State == "paused")
            {
                hits.Start = new RectangleF(right, middle - 31, button, button);
                hits.Reset = new RectangleF(right, middle + 3, button, button);
                RoundButton(g, t, hits.Start, hot == "start", "play");
                RoundButton(g, t, hits.Reset, hot == "reset", "reset");
            }
            else if (v.State == "idle")
            {
                hits.Start = new RectangleF(right, middle - 14, button, button);
                RoundButton(g, t, hits.Start, hot == "start", "play");
            }
            else
            {
                hits.Stop = new RectangleF(right, middle - 14, button, button);
                RoundButton(g, t, hits.Stop, hot == "stop", "pause");
            }
        }

        /// <summary>One number as a cell: the total with its word (19 high), the rate under it (13 high).</summary>
        private static void Cell(Graphics g, Palette t, FloatNumber n, string rate, float x, float y, float room, FloatHits hits, string where)
        {
            BigLine(g, t, n, x, y, room, true, hits, where);
            RateLine(g, t, rate, x, y + 19, room, hits, where);
        }

        /// <summary>
        /// The small line under a total (`index` in the float's order: EXP, profit, mesos, the next level). Before a session
        /// the page sends no rate; every cell keeps its two lines all the same, so the rows stand even: "– / hour" under
        /// EXP, profit and mesos, in the program's language (the next level's line, "to next level", the page always sends).
        /// </summary>
        internal static string RateOf(FloatNumber n, int index)
        {
            if (!string.IsNullOrEmpty(n.Rate) || index == 3) return n.Rate;
            return Texts.T("– / hour");
        }

        private static readonly float[] TotalSizes = { 13.5f, 11.5f, 10.5f };
        private static readonly StringFormat Top = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces, Trimming = StringTrimming.None };

        private static float Ascent(Font f)
        {
            var family = f.FontFamily;
            return f.Size * family.GetCellAscent(f.Style) / family.GetEmHeight(f.Style);
        }

        private static float Descent(Font f)
        {
            var family = f.FontFamily;
            return f.Size * family.GetCellDescent(f.Style) / family.GetEmHeight(f.Style);
        }

        private static float WidthOf(Graphics g, string text, Font font)
        {
            return string.IsNullOrEmpty(text) ? 0 : g.MeasureString(text, font, 4000, Top).Width;
        }

        /// <summary>
        /// A total and its word on one line 19 high at `x`, `y`: the total semibold in its tone (green or red for profit),
        /// then 3 further its word, regular and grey, on the same baseline. `shrink`: smaller as a whole until it fits
        /// `room` (13.5, 11.5, 10.5; the word 10.5 beside 13.5, else 9.5); what does not fit at the least is a problem. The
        /// baseline is where a line of the float's 13 px type puts it, whatever the sizes. Answers the width drawn.
        /// </summary>
        private static float BigLine(Graphics g, Palette t, FloatNumber n, float x, float y, float room, bool shrink, FloatHits hits, string where)
        {
            Font number = F(13.5f, true), word = F(10.5f, false);
            float numberWidth = 0, width = 0;
            for (var i = 0; i < (shrink ? TotalSizes.Length : 1); i++)
            {
                number = F(TotalSizes[i], true);
                word = F(TotalSizes[i] >= 13.5f ? 10.5f : 9.5f, false);
                numberWidth = WidthOf(g, n.Total, number);
                width = numberWidth + (n.Unit.Length > 0 ? WordGap + WidthOf(g, n.Unit, word) : 0);
                if (width <= room) break;
            }
            if (width > room + 0.5f) hits.Problems.Add(where + ": \"" + (n.Total + " " + n.Unit).Trim() + "\" is cut (" + Math.Ceiling(width) + " in " + Math.Floor(room) + ")");
            var strut = F(13f, false);
            var ascent = Math.Max(Ascent(strut), Math.Max(Ascent(number), Ascent(word)));
            var descent = Math.Max(Descent(strut), Math.Max(Descent(number), Descent(word)));
            var baseline = y + (19 - ascent - descent) / 2 + ascent;
            using (var tone = new SolidBrush(n.Tone == "positive" ? t.Positive : n.Tone == "negative" ? t.Negative : t.Foreground))
                g.DrawString(n.Total, number, tone, new PointF(x, baseline - Ascent(number)), Top);
            if (n.Unit.Length > 0)
                using (var muted = new SolidBrush(t.Muted)) g.DrawString(n.Unit, word, muted, new PointF(x + numberWidth + WordGap, baseline - Ascent(word)), Top);
            return width;
        }

        /// <summary>The rate under a total ("48,212 / hour"), grey, 13 high: 10.5, or 9.5 where that does not fit; a problem where neither does.</summary>
        private static void RateLine(Graphics g, Palette t, string rate, float x, float y, float room, FloatHits hits, string where)
        {
            if (string.IsNullOrEmpty(rate)) return;
            var font = F(10.5f, false);
            if (WidthOf(g, rate, font) > room) font = F(9.5f, false);
            var width = WidthOf(g, rate, font);
            if (width > room + 0.5f) hits.Problems.Add(where + ": \"" + rate + "\" is cut (" + Math.Ceiling(width) + " in " + Math.Floor(room) + ")");
            using (var muted = new SolidBrush(t.Muted)) g.DrawString(rate, font, muted, new RectangleF(x, y, Math.Max(room, width) + 2, 13), Near);
        }

        /// <summary>
        /// The own shop at `y` under the numbers, after a rule. Small: the question with Always and Not now under it (24
        /// high), or its line with a dot (green while online, grey while only open, orange while a shop waits for the channel). Large: the title and the note, the two
        /// buttons under them (28 high) while it asks; the dot before the title otherwise. Every line too long for the
        /// float's width goes on to a next line (ShopLines: a map's name is never cut), and what is under it moves down.
        /// Locked: no buttons.
        /// </summary>
        private static void PaintShop(Graphics g, Palette t, FloatShop s, FloatHits hits, string hot, float y, float w, bool small)
        {
            var asks = s.Asks && !_locked;
            using (var rule = new Pen(t.Border, 1f)) g.DrawLine(rule, 0, small ? y : y + 4, w, small ? y : y + 4);
            using (var fg = new SolidBrush(t.Foreground))
            using (var muted = new SolidBrush(t.Muted))
            using (var dot = new SolidBrush(s.State == "online" ? t.Positive : s.State == "wait" ? t.Warning : t.Muted))
            {
                var ask = s.State == "ask";
                if (small)
                {
                    float more;
                    if (ask) more = ShopText(g, s.Line, F(12f, true), fg, 10, y + 4, w - 20, 18, ShopLine, hits);
                    else
                    {
                        g.FillEllipse(dot, 10, y + 11.5f, 7, 7);
                        more = ShopText(g, s.Line, F(12f, true), fg, 22, y + 4, w - 32, 22, ShopLine, hits);
                    }
                    if (!asks) return;
                    var half = (w - 28) / 2f;
                    hits.ShopAlways = new RectangleF(10, y + 26 + more, half, 24);
                    hits.ShopNotNow = new RectangleF(10 + half + 8, y + 26 + more, half, 24);
                }
                else
                {
                    var left = ask ? 10f : 22f;
                    if (!ask) g.FillEllipse(dot, 10, y + 14.5f, 7, 7);
                    var more = ShopText(g, s.Title.Length > 0 ? s.Title : s.Short, F(12.5f, true), fg, left, y + 9, w - 10 - left, 18, ShopLine, hits);
                    if (s.Note.Length > 0) more += ShopText(g, s.Note, F(12f, false), muted, left, y + 27 + more, w - 10 - left, 17, ShopNoteLine, hits);
                    if (!asks) return;
                    var half = (w - 28) / 2f;
                    hits.ShopAlways = new RectangleF(10, y + 51 + more, half, Button);
                    hits.ShopNotNow = new RectangleF(10 + half + 8, y + 51 + more, half, Button);
                }
                var size = small ? 12f : 12.5f;
                TextButton(g, t, hits.ShopAlways, hot == "shop-always", s.Always, size);
                TextButton(g, t, hits.ShopNotNow, hot == "shop-not-now", s.NotNow, size);
            }
        }

        /// <summary>
        /// A line of the own shop at `y`, its first line in a box `height` high and every next one `step` further down
        /// (ShopLines). Answers how much lower that put what comes under it. A line still too wide for the float (one word
        /// wider than it) is a problem the pictures' check reports.
        /// </summary>
        private static float ShopText(Graphics g, string text, Font font, Brush brush, float x, float y, float width, float height, float step, FloatHits hits)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var lines = ShopLines(text, font, width);
            for (var i = 0; i < lines.Count; i++)
            {
                if (WidthOf(g, lines[i], font) > width + 0.5f) hits.Problems.Add("the shop's line is cut: " + lines[i]);
                if (lines.Count > 1 && i == lines.Count - 1) hits.Wrapped++;
                // Wider than its room by a hair at some scale rather than "…": the words are whole whatever happens.
                g.DrawString(lines[i], font, brush, new RectangleF(x, y + i * step, width + 2, height), NearWhole);
            }
            return Math.Max(0, lines.Count - 1) * step;
        }

        /// <summary>As Near, without "…": for lines that were measured to fit (a shop's line broken over two).</summary>
        private static readonly StringFormat NearWhole = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces, Trimming = StringTrimming.None, LineAlignment = StringAlignment.Center };

        /// <summary>The small size as before totals: four words over four numbers, and the round button.</summary>
        private static void PaintSmallRows(Graphics g, FloatValues v, Palette t, FloatHits hits, string hot, float w, Brush fg, Brush muted)
        {
            float top = Head + 8;
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

        // ---- the button that goes round the three sizes: Small, then Large, then the bar, then Small again

        /// <summary>How wide the button that goes round the sizes is, beside the cross of 22: room for its three small shapes.</summary>
        public const float CycleWidth = 38f;

        /// <summary>Where a click on that button goes: Small → Large → the bar → Small, one step a click.</summary>
        public static string NextSize(string size)
        {
            return size == "small" ? "large" : size == "large" ? "bar" : "small";
        }

        private static void CycleButton(Graphics g, Palette t, RectangleF r, bool hot, string size)
        {
            if (hot)
            {
                using (var path = Look.Rounded(r, 4))
                using (var back = new SolidBrush(t.Raised))
                {
                    g.FillPath(back, path);
                }
            }
            CycleIcon(g, r, size, t);
        }

        /// <summary>
        /// The picture on the button that goes round, for the size the float is in now (`size`: "small", "large" or "bar"),
        /// drawn in the middle of the button's box.
        /// The three sizes as three small shapes side by side in a strip of 33 × 12, in their proportions (Small 9 × 5 at 0,
        /// Large 6 × 10 at 12, the bar 12 × 3 at 21; from 208 × 112, 280 × 470 and 553 × 40), each in the middle of the
        /// strip's height, corners of 1. The size the float is in is filled in the text's colour (what is chosen); the other
        /// two are a line of one pixel in the muted grey. The same picture in every size, only the filled shape differs. The
        /// whole of that picture is here.
        /// </summary>
        public static void CycleIcon(Graphics g, RectangleF button, string size, Palette t)
        {
            var left = button.X + (button.Width - 33f) / 2;
            var top = button.Y + (button.Height - 12f) / 2;
            var names = new[] { "small", "large", "bar" };
            var xs = new[] { 0f, 12f, 21f };
            var widths = new[] { 9f, 6f, 12f };
            var heights = new[] { 5f, 10f, 3f };
            float scale;
            for (var i = 0; i < 3; i++)
            {
                var box = Crisp(g, new RectangleF(left + xs[i], top + (12f - heights[i]) / 2, widths[i], heights[i]), out scale);
                if (names[i] == size)
                {
                    using (var path = Look.Rounded(box, 1f))
                    using (var fill = new SolidBrush(t.Foreground))
                    {
                        g.FillPath(fill, path);
                    }
                    continue;
                }
                // A line of one pixel inside the shape's box, whole pixels of the screen so it is sharp (two at 150% and more).
                var stroke = Math.Max(1f, (float)Math.Round(scale)) / scale;
                using (var ring = new GraphicsPath(FillMode.Alternate))
                using (var muted = new SolidBrush(t.Muted))
                {
                    ring.AddPath(Look.Rounded(box, 1.5f), false);
                    ring.AddPath(Look.Rounded(new RectangleF(box.X + stroke, box.Y + stroke, Math.Max(0.1f, box.Width - 2 * stroke), Math.Max(0.1f, box.Height - 2 * stroke)), 0.5f), false);
                    g.FillPath(muted, ring);
                }
            }
        }

        /// <summary>`r` on whole pixels of the surface it is drawn on (the float is drawn scaled), so its edges are sharp; `scale` says how many of those pixels one of the float's is.</summary>
        private static RectangleF Crisp(Graphics g, RectangleF r, out float scale)
        {
            float[] e;
            using (var m = g.Transform) e = m.Elements;
            float sx = e[0], sy = e[3], ox = e[4], oy = e[5];
            scale = sx;
            if (sx <= 0 || sy <= 0)
            {
                scale = 1f;
                return r;
            }
            var x = (float)Math.Floor(r.X * sx + ox + 0.5f);
            var y = (float)Math.Floor(r.Y * sy + oy + 0.5f);
            var w = Math.Max(1f, (float)Math.Round(r.Width * sx));
            var h = Math.Max(1f, (float)Math.Round(r.Height * sy));
            return new RectangleF((x - ox) / sx, (y - oy) / sy, w / sx, h / sy);
        }

        // ---- the bar: tiles in one strip, as tall as the game's dashboard

        /// <summary>The bar is as tall as the game's dashboard.</summary>
        public const int BarHeight = 40;
        private const float BarLeft = 10f, BarRight = 6f, BarGap = 12f;
        /// <summary>The head's lines stand after the dot and its air (the map under the clock as wide as its own name: never cut).</summary>
        private const float BarIndent = 14f;
        /// <summary>The tiles' least widths (EXP, mesos and kills an hour, the next level), so the bar does not change its width with every number.</summary>
        private static readonly float[] BarTileLeast = { 66f, 66f, 56f, 84f };
        /// <summary>With totals: every tile at least this wide, and 14 between tiles and before the buttons.</summary>
        private const float TotalsTileLeast = 66f, TotalsGap = 14f;

        /// <summary>What the bar shows and how wide each part is, in the float's own measure as it is drawn at a scale.</summary>
        private sealed class BarPlan
        {
            public string First = "", Second = "";
            public bool FirstIsMap;
            public float FirstWidth, SecondWidth, Head;
            public string[] Note;
            public float NoteWidth;
            public FloatRow[] Cells;
            /// <summary>With totals: the four numbers in the bar's order (EXP, profit, mesos, the next level) in place of Cells.</summary>
            public FloatNumber[] Numbers;
            public float[] Tiles;
            /// <summary>Between two tiles, and between the last tile and the buttons (12 and 24 before totals; 14 and 14 with them).</summary>
            public float TileGap = BarGap, BeforeEnd = 2 * BarGap;
            /// <summary>The own shop's tile (its line with the dot before it), and its width; none while it asks or there is none. `ShopSecond`: the line's second half under the first, small and grey, as a tile's rate stands under its total (BarShopLines); null when it is one line.</summary>
            public string ShopLine, ShopSecond;
            public bool ShopOnline;
            /// <summary>The tile is a shop that waits for the channel: its dot orange (pick 4 B).</summary>
            public bool ShopWait;
            public float ShopWidth;
            /// <summary>Round buttons: pause while reading; play and reset while paused; play while not started; none while waiting or locked.</summary>
            public int Rounds;
            public float WordWidth, End, Width;
        }

        /// <summary>How wide `text` is in `font`, in the float's own measure, as it is drawn at `scale` (letters are fitted to the screen's pixels, so a width differs a little from scale to scale).</summary>
        private static float TextWidth(string text, Font font, float scale)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            lock (Ruler)
            {
                Ruler.ResetTransform();
                Ruler.ScaleTransform(scale, scale);
                try
                {
                    return Ruler.MeasureString(text, font, 4000, Near).Width;
                }
                finally
                {
                    Ruler.ResetTransform();
                }
            }
        }

        /// <summary>The waiting note on two lines: its first sentence, then the rest ("Choosing a world and channel." / "Tempo starts counting when you're in the game.").</summary>
        internal static string[] NoteLines(string note)
        {
            note = (note ?? "").Trim();
            for (var i = 0; i < note.Length - 1; i++)
            {
                var c = note[i];
                var end = c == '。' || ((c == '.' || c == '!' || c == '?') && note[i + 1] == ' ');
                if (!end) continue;
                var rest = note.Substring(i + 1).Trim();
                if (rest.Length > 0) return new[] { note.Substring(0, i + 1), rest };
            }
            return new[] { note };
        }

        /// <summary>The own shop's tile is one line up to this wide; past it (or with a " · " in it) it goes on two, as the other tiles stand.</summary>
        private const float BarShopMost = 150f;

        /// <summary>
        /// The own shop's line for the bar, as short a tile as it can be: a line with " · " in it on two lines, broken at the
        /// dot that leaves the narrower tile ("Shop online" over "16 offers"); a line wider than BarShopMost without one,
        /// broken at the space that does; else one line (`second` null). The first half semibold 12, the second 10.5 and
        /// grey under it, as a tile's total over its rate. Never cut: the tile is as wide as its wider half.
        /// </summary>
        internal static void BarShopLines(string line, float scale, out string first, out string second)
        {
            first = line ?? "";
            second = null;
            var dotted = first.Contains(" · ");
            if (!dotted && TextWidth(first, F(12f, true), scale) + 1 <= BarShopMost) return;
            var separator = dotted ? " · " : " ";
            var parts = first.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;
            var best = float.MaxValue;
            string top = first, bottom = null;
            for (var k = 1; k < parts.Length; k++)
            {
                var a = string.Join(separator, parts, 0, k);
                var b = string.Join(separator, parts, k, parts.Length - k);
                var wide = Math.Max(TextWidth(a, F(12f, true), scale), TextWidth(b, F(10.5f, false), scale));
                if (wide >= best) continue;
                best = wide;
                top = a;
                bottom = b;
            }
            first = top;
            second = bottom;
        }

        private static BarPlan PlanBar(FloatValues v, bool locked, string lockedWord, float scale)
        {
            var p = new BarPlan();
            if (v.Out) p.First = v.OutTitle ?? "";
            else
            {
                // While Tempo reads, the place is the map: its own name, without the region before " : ".
                var place = v.State == "reading" ? OwnMapName(v.Place) : v.Place;
                p.First = v.Clock.Length > 0 ? v.Clock : place;
                p.Second = v.Clock.Length > 0 ? place : "";
                // The first minute of reading has no clock yet: the map stands in its place.
                p.FirstIsMap = v.Clock.Length == 0 && v.State == "reading";
            }
            // Never cut: the head is as wide as its words.
            p.FirstWidth = TextWidth(p.First, F(13.5f, true), scale) + 1;
            p.SecondWidth = p.Second.Length > 0 ? TextWidth(p.Second, F(10.5f, false), scale) + 1 : 0;
            p.Head = BarIndent + Math.Max(p.FirstWidth, p.SecondWidth);
            float middle = 0;
            if (v.Out)
            {
                // Not in the game: the note on two lines, never cut (the bar grows instead).
                p.Note = NoteLines(v.OutNote);
                foreach (var line in p.Note) p.NoteWidth = Math.Max(p.NoteWidth, TextWidth(line, F(11.5f, false), scale) + 1);
                middle = p.NoteWidth;
            }
            else if (v.HasTotals)
            {
                // Four tiles of two lines, the total with its word over the rate, 14 apart: as wide as their lines, never
                // less than TotalsTileLeast; then the own shop's line as a tile of its own (never the question).
                p.Numbers = v.Totals;
                p.TileGap = TotalsGap;
                p.BeforeEnd = TotalsGap;
                p.Tiles = new float[4];
                for (var i = 0; i < 4; i++)
                {
                    var n = p.Numbers[i];
                    var big = TextWidth(n.Total, F(13.5f, true), scale) + (n.Unit.Length > 0 ? WordGap + TextWidth(n.Unit, F(10.5f, false), scale) : 0);
                    var rate = TextWidth(RateOf(n, i), F(10.5f, false), scale);
                    p.Tiles[i] = Math.Max(TotalsTileLeast, Math.Max((float)Math.Ceiling(big) + 1, (float)Math.Ceiling(rate) + 1));
                    middle += p.Tiles[i] + (i > 0 ? p.TileGap : 0);
                }
                if (v.Shop != null && v.Shop.State != "ask" && v.Shop.Line.Length > 0)
                {
                    BarShopLines(v.Shop.Line, scale, out p.ShopLine, out p.ShopSecond);
                    p.ShopOnline = v.Shop.State == "online";
                    p.ShopWait = v.Shop.State == "wait";
                    var wide = TextWidth(p.ShopLine, F(12f, true), scale);
                    if (p.ShopSecond != null) wide = Math.Max(wide, TextWidth(p.ShopSecond, F(10.5f, false), scale));
                    p.ShopWidth = 12 + (float)Math.Ceiling(wide) + 1;
                    middle += p.TileGap + p.ShopWidth;
                }
            }
            else
            {
                p.Cells = new[]
                {
                    v.Rows.Count > 0 ? v.Rows[0] : new FloatRow(),
                    v.Rows.Count > 1 ? v.Rows[1] : new FloatRow(),
                    v.Rows.Count > 2 ? v.Rows[2] : new FloatRow(),
                    v.HasNext ? new FloatRow { Label = v.NextLabel, Value = v.NextValue } : new FloatRow(),
                };
                p.Tiles = new float[4];
                for (var i = 0; i < 4; i++)
                {
                    // A number wider than its tile makes the tile wider, and the bar with it: never cut.
                    var need = Math.Max(TextWidth(p.Cells[i].Label, F(10.5f, false), scale), TextWidth(p.Cells[i].Value, F(13.5f, true), scale)) + 1;
                    p.Tiles[i] = Math.Max(BarTileLeast[i], need);
                    middle += p.Tiles[i] + (i > 0 ? BarGap : 0);
                }
            }
            p.Rounds = locked || v.Out ? 0 : v.State == "paused" ? 2 : 1;
            if (locked)
            {
                // Where the buttons were: a small lock and the word, as in the float's head.
                p.WordWidth = string.IsNullOrEmpty(lockedWord) ? 0 : TextWidth(lockedWord, F(11.5f, false), scale) + 1;
                p.End = 10 + (p.WordWidth > 0 ? 5 + p.WordWidth : 0) + 4;
            }
            else p.End = (p.Rounds > 0 ? p.Rounds * Button + (p.Rounds - 1) * 6 + BarGap : 0) + CycleWidth + 2 + 22;
            // The head, its line, the tiles (or the note), the room that is left, the buttons: 12 between each (with totals
            // 14 between the tiles and before the buttons).
            p.Width = BarLeft + p.Head + BarGap + 1 + BarGap + middle + p.BeforeEnd + p.End + BarRight;
            return p;
        }

        /// <summary>The bar's size in pixels at 96 dpi for what it shows, measured as it is drawn at `scale`: as wide as its parts, 40 tall.</summary>
        public static Size MeasureBar(FloatValues v, bool locked, string lockedWord, float scale)
        {
            return new Size((int)Math.Ceiling(PlanBar(v, locked, lockedWord, scale).Width - 0.01f), BarHeight);
        }

        /// <summary>
        /// Draws the bar at `scale` into a surface of `width` × 40 times that scale (`width` at least MeasureBar's: what is
        /// more is room before the buttons, so the bar keeps its width while its words change). The head (the dot, the
        /// stopwatch and under it the map, "Paused"; "Not started"; "Waiting"), a thin line, four tiles (a word over a
        /// number; under the next level a line that measures the level), and at the right the round buttons, the button
        /// that goes round and the cross. Locked: a lock and the word in their place. Waiting: the note on two lines in
        /// place of the tiles, no round buttons.
        /// </summary>
        public static FloatHits PaintBar(Graphics g, FloatValues v, float scale, string hot, bool light, bool locked, string lockedWord, int width)
        {
            var t = Look.Of(light);
            var hits = new FloatHits();
            var p = PlanBar(v, locked, lockedWord, scale);
            var w = Math.Max(width, (float)Math.Ceiling(p.Width - 0.01f));
            float h = BarHeight;
            using (var face = new SolidBrush(t.FloatFace)) g.FillRectangle(face, 0, 0, (float)Math.Ceiling(w * scale), (float)Math.Ceiling(h * scale));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            // Drawn in the float's own measure, then back to the surface's for the edge (a picture may have moved the surface).
            var surface = g.Save();
            g.ScaleTransform(scale, scale);
            using (var fg = new SolidBrush(t.Foreground))
            using (var muted = new SolidBrush(t.Muted))
            using (var rule = new SolidBrush(t.Border))
            {
                var x = BarLeft;
                // The head: the dot (green while Tempo reads) and the stopwatch; under it, small, the map or "Paused".
                var two = p.Second.Length > 0;
                var firstTop = two ? 4f : (h - 19) / 2;
                using (var dot = new SolidBrush(v.State == "reading" && !v.Out ? t.Positive : t.Muted)) g.FillEllipse(dot, x, firstTop + 6, 7, 7);
                BarText(g, p.First, F(13.5f, true), fg, x + BarIndent, firstTop, p.FirstWidth, 19, hits, "the head");
                if (two) BarText(g, p.Second, F(10.5f, false), muted, x + BarIndent, firstTop + 19, p.SecondWidth, 13, hits, "the head's second line");
                if (v.State == "reading" && !v.Out) hits.HeadMap = p.FirstIsMap ? p.First : p.Second;
                x += p.Head + BarGap;
                g.FillRectangle(rule, x, (h - 26) / 2, 1, 26);
                x += 1 + BarGap;
                if (v.Out)
                {
                    var top = p.Note.Length > 1 ? (h - 30) / 2 : (h - 15) / 2;
                    for (var i = 0; i < p.Note.Length; i++) BarText(g, p.Note[i], F(11.5f, false), muted, x, top + i * 15, p.NoteWidth, 15, hits, "the waiting note");
                    x += p.NoteWidth;
                }
                else if (p.Numbers != null)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var tile = p.Tiles[i];
                        BigLine(g, t, p.Numbers[i], x, 4, tile, false, hits, "a tile's total");
                        RateLine(g, t, RateOf(p.Numbers[i], i), x, 23, tile, hits, "a tile's rate");
                        // Under the next level: how far the level is, 2 px, 2 px above the edge.
                        if (i == 3 && v.HasNext)
                        {
                            using (var track = new SolidBrush(t.Raised)) g.FillRectangle(track, x, h - 4, tile, 2);
                            var done = (float)(tile * v.NextPercent / 100.0);
                            if (done > 0) using (var blue = new SolidBrush(t.Measure)) g.FillRectangle(blue, x, h - 4, done, 2);
                        }
                        x += tile + (i < 3 ? p.TileGap : 0);
                    }
                    if (p.ShopWidth > 0)
                    {
                        // The own shop: a dot (green while it is online, grey while only open) and its line, or its two
                        // lines as a tile's total over its rate.
                        x += p.TileGap;
                        var twoLines = p.ShopSecond != null;
                        using (var dot = new SolidBrush(p.ShopOnline ? t.Positive : p.ShopWait ? t.Warning : t.Muted)) g.FillEllipse(dot, x, twoLines ? 4 + 9.5f - 3.5f : (h - 7) / 2, 7, 7);
                        if (twoLines)
                        {
                            hits.Wrapped++;
                            BarText(g, p.ShopLine, F(12f, true), fg, x + 12, 4, p.ShopWidth - 12, 19, hits, "the shop's tile");
                            BarText(g, p.ShopSecond, F(10.5f, false), muted, x + 12, 23, p.ShopWidth - 12, 13, hits, "the shop's tile");
                        }
                        else BarText(g, p.ShopLine, F(12f, true), fg, x + 12, 0, p.ShopWidth - 12, h, hits, "the shop's tile");
                        x += p.ShopWidth;
                    }
                }
                else
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var tile = p.Tiles[i];
                        BarText(g, p.Cells[i].Label, F(10.5f, false), muted, x, 4, tile, 13, hits, "a tile's word");
                        BarText(g, p.Cells[i].Value, F(13.5f, true), fg, x, 17, tile, 19, hits, "a tile's number");
                        // Under the next level: how far the level is, 2 px, 2 px above the edge.
                        if (i == 3 && v.HasNext)
                        {
                            using (var track = new SolidBrush(t.Raised)) g.FillRectangle(track, x, h - 4, tile, 2);
                            var done = (float)(tile * v.NextPercent / 100.0);
                            if (done > 0) using (var blue = new SolidBrush(t.Measure)) g.FillRectangle(blue, x, h - 4, done, 2);
                        }
                        x += tile + (i < 3 ? BarGap : 0);
                    }
                }
                // The right end stands at the bar's right; whatever room is left lies before it.
                var end = w - BarRight - p.End;
                if (end < x + p.BeforeEnd - 0.5f) hits.Problems.Add("the bar's buttons lie over its numbers");
                if (locked)
                {
                    Lock(g, new RectangleF(end, (h - 12) / 2, 10, 12), t.Muted);
                    if (p.WordWidth > 0) BarText(g, lockedWord, F(11.5f, false), muted, end + 15, 0.5f, p.WordWidth, h, hits, "the word locked");
                }
                else
                {
                    var bx = end;
                    var top = (h - Button) / 2;
                    if (p.Rounds > 0)
                    {
                        if (v.State == "paused")
                        {
                            hits.Start = new RectangleF(bx, top, Button, Button);
                            hits.Reset = new RectangleF(bx + Button + 6, top, Button, Button);
                            RoundButton(g, t, hits.Start, hot == "start", "play");
                            RoundButton(g, t, hits.Reset, hot == "reset", "reset");
                        }
                        else if (v.State == "idle")
                        {
                            hits.Start = new RectangleF(bx, top, Button, Button);
                            RoundButton(g, t, hits.Start, hot == "start", "play");
                        }
                        else
                        {
                            hits.Stop = new RectangleF(bx, top, Button, Button);
                            RoundButton(g, t, hits.Stop, hot == "stop", "pause");
                        }
                        bx += p.Rounds * Button + (p.Rounds - 1) * 6 + BarGap;
                    }
                    hits.Size = new RectangleF(bx, (h - 22) / 2, CycleWidth, 22);
                    hits.Close = new RectangleF(bx + CycleWidth + 2, (h - 22) / 2, 22, 22);
                    CycleButton(g, t, hits.Size, hot == "size", "bar");
                    IconButton(g, t, hits.Close, hot == "close", "close");
                    if (hits.Close.Right > w - BarRight + 0.5f) hits.Problems.Add("the cross passes the bar's edge");
                }
            }
            hits.Strip = new RectangleF(0, 0, w, h);
            g.Restore(surface);
            // One thin edge all round, as the float has.
            using (var edge = new Pen(t.FloatEdge, scale)) g.DrawRectangle(edge, 0.5f * scale, 0.5f * scale, (w - 1) * scale, (h - 1) * scale);
            return hits;
        }

        /// <summary>One line of the bar in its box. `what` names it when it does not fit whole (null: no check).</summary>
        private static void BarText(Graphics g, string text, Font font, Brush brush, float x, float y, float width, float height, FloatHits hits, string what)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (what != null && g.MeasureString(text, font, 4000, Near).Width > width + 0.25f) hits.Problems.Add(what + " is cut: " + text);
            g.DrawString(text, font, brush, new RectangleF(x, y, width, height), Near);
        }

        private static void TextButton(Graphics g, Palette t, RectangleF r, bool hot, string text)
        {
            TextButton(g, t, r, hot, text, 12.5f);
        }

        /// <summary>A grey button with its word, semibold at `size` (12 on the small float's shop buttons).</summary>
        private static void TextButton(Graphics g, Palette t, RectangleF r, bool hot, string text, float size)
        {
            using (var path = Look.Rounded(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1), Look.Radius))
            using (var back = new SolidBrush(hot ? t.SelectedSurface : t.Raised))
            using (var edge = new Pen(hot ? t.Edge : t.Border, 1f))
            using (var fg = new SolidBrush(t.Foreground))
            {
                g.FillPath(back, path);
                g.DrawPath(edge, path);
                g.DrawString(text, F(size, true), fg, new RectangleF(r.X, r.Y - 0.5f, r.Width, r.Height), Mid);
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
        /// <summary>"small", "large" or "bar".</summary>
        private string _size;
        private bool Small { get { return _size == "small"; } }
        private bool Bar { get { return _size == "bar"; } }
        /// <summary>The bar is never narrower than it was this session (in the float's own measure): its buttons stay where they are while its words change.</summary>
        private int _barLeast;
        /// <summary>Where the float (small or large) and the bar last stood while this window was open: each has a place of its own. Empty until they did.</summary>
        private Rectangle _floatWas, _barWas;
        /// <summary>The bar was put at the screen's top for want of a game; it goes to the game's top the first time the game is there.</summary>
        private bool _barWantsGame;
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
        /// <summary>
        /// Hidden because the game's window is not there (it was closed, or the float came back with the program before the
        /// game started): open all the same, and shown again the moment the game's window is there.
        /// </summary>
        private bool _hiddenNoGame;
        /// <summary>The float has been at a game since it was last shown without one: when the game goes, so does the float.</summary>
        private bool _sawGame;
        /// <summary>The game's window at the last look, to notice a new one; and that the float has yet to go to its place at it.</summary>
        private IntPtr _gameSeenLast;
        private bool _toGame;
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

        /// <summary>Open: shown, or hidden only while the game is minimised or not there.</summary>
        public bool Open { get { return Visible || _hiddenWithGame || _hiddenNoGame; } }

        /// <summary>A button was pressed: "start", "stop" or "reset"; the own shop's "shop-always" or "shop-not-now". The page does the rest.</summary>
        public Action<string> Command;
        /// <summary>The cross was pressed.</summary>
        public Action CloseAsked;
        /// <summary>The size was switched to "small", "large" or "bar".</summary>
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
            _size = settings.FloatSize == "small" || settings.FloatSize == "bar" ? settings.FloatSize : "large";
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

        public string SizeName { get { return _size; } }

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

        /// <summary>
        /// Shows the float where it last stood on this arrangement of screens; the first time beside the game's window, else
        /// at the screen's top right. The bar has a place of its own: the first time at the top of the game's picture, in
        /// the middle, just under its title bar. `withGameOnly`: the float comes back as the player left it (the program
        /// started again, Tempo's page loaded), so it shows only with the game: without the game's window it waits, open
        /// and out of sight, and comes when the game does. Asked for by the player (Tempo › Float) it shows at once, game
        /// or not, and goes when a game it was at closes.
        /// </summary>
        public void ShowAt(Rectangle gameBounds, bool withGameOnly)
        {
            if (withGameOnly && !Visible && Game() == IntPtr.Zero)
            {
                _hiddenNoGame = true;
                // The window exists without being shown, so its looks (WithGame) run and see the game come.
                GC.KeepAlive(Handle);
                return;
            }
            _hiddenNoGame = false;
            if (!Visible) _sawGame = false;
            if (!_placed)
            {
                _placed = true;
                Bounds = Bar ? BarPlace() : FloatPlace(gameBounds);
            }
            if (!Visible) Show();
            Fit(false);
        }

        /// <summary>
        /// Where the float (small or large) goes when it has not stood anywhere in this window yet: with the game in view
        /// always at the game (AtGamePlace: where it stood there, else its first place inside the game); without the game
        /// its kept place on the screen when that still shows on a screen connected now, else beside the game, else the
        /// main screen's top right.
        /// </summary>
        private Rectangle FloatPlace(Rectangle gameBounds)
        {
            var size = PixelSize();
            var atGame = AtGamePlace(_settings.FloatAtGame, size);
            if (!atGame.IsEmpty) return atGame;
            Point topRight;
            string kept;
            if (_settings.FloatPlaces.TryGetValue(LayoutKey(), out kept) && TryPoint(kept, out topRight) && ScreenPlace.Shows(new Rectangle(topRight.X - size.Width, topRight.Y, size.Width, size.Height), ScreenPlace.Areas())) { }
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
            return Clamp(new Rectangle(topRight.X - size.Width, topRight.Y, size.Width, size.Height));
        }

        /// <summary>Where the bar goes when it has not stood anywhere in this window yet: with the game in view always at the game (where it stood there, else the top of the game's picture in the middle); without it its kept place when that still shows on a screen connected now, else the top of the main screen in the middle, until the game is there.</summary>
        private Rectangle BarPlace()
        {
            var size = PixelSize();
            string kept;
            var at = AtGamePlace(_settings.FloatBarAtGame, size);
            if (!at.IsEmpty) return at;
            if (_settings.FloatBarPlaces.TryGetValue(LayoutKey(), out kept) && TryBarPlace(kept, size, out at) && ScreenPlace.Shows(at, ScreenPlace.Areas())) return Clamp(at);
            at = BarAtGame(Game(), size);
            if (!at.IsEmpty) return Clamp(at);
            _barWantsGame = true;
            var area = Screen.PrimaryScreen.WorkingArea;
            return Clamp(new Rectangle(area.Left + (area.Width - size.Width) / 2, area.Top, size.Width, size.Height));
        }

        /// <summary>
        /// A screen came or went, or the screens were arranged anew (Windows' display settings): with the game in view the
        /// float goes to its place at the game; without it, a float (shown or waiting for the game) that would stand where no
        /// screen shows any more goes to its first place on the main screen. Where the other kind last stood is forgotten
        /// when it no longer shows, so the button that goes round finds a place that does.
        /// </summary>
        public void ScreensChanged()
        {
            if (IsDisposed || !IsHandleCreated || _dragging || _resizing) return;
            var areas = ScreenPlace.Areas();
            if (!ScreenPlace.Shows(_floatWas, areas)) _floatWas = Rectangle.Empty;
            if (!ScreenPlace.Shows(_barWas, areas)) _barWas = Rectangle.Empty;
            var game = Game();
            if (game != IntPtr.Zero && !IsIconic(game))
            {
                ToGame(game);
                return;
            }
            // The game is not in view: the float goes to its place at the game the moment it is.
            _toGame = true;
            if (!_placed || ScreenPlace.Shows(Bounds, areas)) return;
            Bounds = Bar ? BarPlace() : FloatPlace(Rectangle.Empty);
        }

        // ---- the place at the game, kept for when the game comes back

        /// <summary>
        /// The float's place at the game while the game is in view: as `kept` says it (AtGameText), at `size`; the first
        /// time (nothing kept) the float inside the game's right side under its quest helper (DefaultAtGame) and the bar at
        /// the top of the game's picture in its middle. On the screen the game is on. Empty without a game in view.
        /// </summary>
        private Rectangle AtGamePlace(string kept, Size size)
        {
            var game = Game();
            if (game == IntPtr.Zero || IsIconic(game)) return Rectangle.Empty;
            var frame = FrameOf(game);
            if (frame.IsEmpty) return Rectangle.Empty;
            var at = FromAtGame(kept, frame, size, MonitorScale.OfWindow(game));
            if (at.IsEmpty) at = Bar ? BarAtGame(game, size) : DefaultAtGame(PictureOf(game), size, MonitorScale.OfWindow(game));
            return at.IsEmpty ? at : Clamp(at);
        }

        /// <summary>
        /// The float's first place at the game, before it stood anywhere there: inside the game's picture at its right side, 8
        /// from the edge, 64 under its top (the game's own minimap is at the top left; at the top right stand its event icon
        /// and the quest helper's line, down to about 56), on the game's screen's scale.
        /// </summary>
        internal static Rectangle DefaultAtGame(Rectangle picture, Size size, float scale)
        {
            if (picture.IsEmpty) return Rectangle.Empty;
            var s = scale > 0 ? scale : 1f;
            return new Rectangle(picture.Right - (int)Math.Round(8 * s) - size.Width, picture.Top + (int)Math.Round(64 * s), size.Width, size.Height);
        }

        /// <summary>
        /// Where `me` stands at the game's window `game`, to be put there again whenever the float shows with the game: at a
        /// new window after the game was closed, after the program started again, after a screen went away (until 0.4.2
        /// the float then stood loose on its old place on the screen). The float's place is always the game's: per
        /// direction the side of the float nearest a side of the game, with which, and how far from it in pixels at 96 dpi
        /// on the game's screen (`scale`): "LL" its left at the game's left (inside), "RR" its right at the game's right,
        /// "LR" its left at the game's right (outside, to the right), "RL" its right at the game's left, and for the bar
        /// "MM", its middle on the game's middle; "TT", "BB", "TB" (under the game) and "BT" (over it) the same up and down.
        /// "RR,0,TT,40": inside the game's right side, 40 under its top.
        /// </summary>
        internal static string AtGameText(Rectangle me, Rectangle game, bool bar, float scale)
        {
            if (me.IsEmpty || game.IsEmpty) return null;
            string h, v;
            int dx, dy;
            var middle = me.Left + me.Width / 2 - (game.Left + game.Width / 2);
            if (bar && Math.Abs(middle) <= 2)
            {
                h = "MM";
                dx = middle;
            }
            else Nearest(new[] { "LL", "RR", "LR", "RL" }, new[] { me.Left - game.Left, me.Right - game.Right, me.Left - game.Right, me.Right - game.Left }, out h, out dx);
            Nearest(new[] { "TT", "BB", "TB", "BT" }, new[] { me.Top - game.Top, me.Bottom - game.Bottom, me.Top - game.Bottom, me.Bottom - game.Top }, out v, out dy);
            var s = scale > 0 ? scale : 1f;
            return h + "," + (int)Math.Round(dx / s) + "," + v + "," + (int)Math.Round(dy / s);
        }

        private static void Nearest(string[] names, int[] gaps, out string name, out int gap)
        {
            name = names[0];
            gap = gaps[0];
            for (var i = 1; i < names.Length; i++)
            {
                if (Math.Abs(gaps[i]) >= Math.Abs(gap)) continue;
                name = names[i];
                gap = gaps[i];
            }
        }

        /// <summary>The place `text` (AtGameText) names at the game's window `game`, for a float of `size`; empty when the text says none.</summary>
        internal static Rectangle FromAtGame(string text, Rectangle game, Size size, float scale)
        {
            var parts = (text ?? "").Split(',');
            int dx, dy;
            if (parts.Length != 4 || game.IsEmpty || !int.TryParse(parts[1], out dx) || !int.TryParse(parts[3], out dy)) return Rectangle.Empty;
            var s = scale > 0 ? scale : 1f;
            var x = (int)Math.Round(dx * s);
            var y = (int)Math.Round(dy * s);
            int left, top;
            switch (parts[0])
            {
                case "LL": left = game.Left + x; break;
                case "RR": left = game.Right + x - size.Width; break;
                case "LR": left = game.Right + x; break;
                case "RL": left = game.Left + x - size.Width; break;
                case "MM": left = game.Left + game.Width / 2 + x - size.Width / 2; break;
                default: return Rectangle.Empty;
            }
            switch (parts[2])
            {
                case "TT": top = game.Top + y; break;
                case "BB": top = game.Bottom + y - size.Height; break;
                case "TB": top = game.Bottom + y; break;
                case "BT": top = game.Top + y - size.Height; break;
                default: return Rectangle.Empty;
            }
            return new Rectangle(left, top, size.Width, size.Height);
        }

        /// <summary>
        /// A new game window (the game was closed and started again, or the float came before the game), or a screen went
        /// away: the float goes to its place at the game (AtGamePlace), and where the other kind (the bar, or the float)
        /// last stood goes along, so the button that goes round finds it at the game too.
        /// </summary>
        private void ToGame(IntPtr game)
        {
            var frame = FrameOf(game);
            if (frame.IsEmpty) return;
            var at = AtGamePlace(Bar ? _settings.FloatBarAtGame : _settings.FloatAtGame, Size);
            if (!at.IsEmpty)
            {
                Bounds = at;
                _gameWas = Rectangle.Empty;
                _barWantsGame = false;
            }
            var other = Bar ? _floatWas : _barWas;
            if (other.IsEmpty) return;
            var moved = FromAtGame(Bar ? _settings.FloatAtGame : _settings.FloatBarAtGame, frame, other.Size, MonitorScale.OfWindow(game));
            // Nothing kept for the other kind: it finds its first place at the game when the button goes round to it.
            if (Bar) _floatWas = moved.IsEmpty ? Rectangle.Empty : Clamp(moved);
            else _barWas = moved.IsEmpty ? Rectangle.Empty : Clamp(moved);
        }

        /// <summary>Keeps where the float and the bar stand at the game (AtGameText), while the game is in view.</summary>
        private void KeepAtGame(Rectangle floatAt, Rectangle barAt)
        {
            var game = Game();
            if (game == IntPtr.Zero || IsIconic(game)) return;
            var frame = FrameOf(game);
            if (frame.IsEmpty) return;
            var scale = MonitorScale.OfWindow(game);
            if (!floatAt.IsEmpty) _settings.FloatAtGame = AtGameText(floatAt, frame, false, scale);
            if (!barAt.IsEmpty) _settings.FloatBarAtGame = AtGameText(barAt, frame, true, scale);
        }

        /// <summary>The bar at the top of the game's picture (just under its title bar), in the middle; empty without a game.</summary>
        private static Rectangle BarAtGame(IntPtr game, Size size)
        {
            if (game == IntPtr.Zero || IsIconic(game)) return Rectangle.Empty;
            var picture = PictureOf(game);
            if (picture.IsEmpty) return Rectangle.Empty;
            return new Rectangle(picture.Left + picture.Width / 2 - size.Width / 2, picture.Top, size.Width, size.Height);
        }

        /// <summary>The bar's kept place, "x,y,side": x is its left edge, its right edge or its middle ("left", "right", "middle"), so the side it stuck to stays whatever its width is now.</summary>
        private static bool TryBarPlace(string text, Size size, out Rectangle at)
        {
            at = Rectangle.Empty;
            var parts = (text ?? "").Split(',');
            int x, y;
            if (parts.Length != 3 || !int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y)) return false;
            var left = parts[2] == "left" ? x : parts[2] == "middle" ? x - size.Width / 2 : x - size.Width;
            at = new Rectangle(left, y, size.Width, size.Height);
            return true;
        }

        /// <summary>The bar's place as it is kept: by the side it sticks to (its middle on the game's, its left or its right), else by its right, as the float keeps its top right corner.</summary>
        private string BarPlaceText(Rectangle r)
        {
            var xs = new List<int>();
            var ys = new List<int>();
            var mids = new List<int>();
            Lines(r, Screen.FromRectangle(r).WorkingArea, 1, xs, ys, mids);
            if (On(r.Left + r.Width / 2, mids)) return (r.Left + r.Width / 2) + "," + r.Top + ",middle";
            if (On(r.Left, xs) && !On(r.Right, xs)) return r.Left + "," + r.Top + ",left";
            return r.Right + "," + r.Top + ",right";
        }

        public void SetValues(FloatValues values)
        {
            var before = LogicalSize(_values);
            if (values.SessionNo != _sessionNo)
            {
                _sessionNo = values.SessionNo;
                _folded.Clear();
                // A new session: the bar takes the width of what it shows again.
                _barLeast = 0;
            }
            _values = values;
            if (_scroll > Math.Max(0, values.Monsters.Count - 1)) _scroll = 0;
            if (!_resizing && LogicalSize(values) != before) Fit(false);
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

        /// <summary>The size of what `v` shows in the size the float is in, in pixels at 96 dpi; the bar never narrower than it was this session.</summary>
        private Size LogicalSize(FloatValues v)
        {
            if (!Bar) return FloatPainter.Measure(v, Small, _user, _folded, _locked);
            var bar = FloatPainter.MeasureBar(v, _locked, Texts.T("locked"), _scale);
            return new Size(Math.Max(bar.Width, _barLeast), bar.Height);
        }

        private Size PixelSize()
        {
            var logical = LogicalSize(_values);
            return new Size((int)Math.Round(logical.Width * _scale), (int)Math.Round(logical.Height * _scale));
        }

        /// <summary>The window takes the size of what it shows, and keeps the side it sticks to.</summary>
        private void Fit(bool remember)
        {
            if (Bar) _barLeast = LogicalSize(_values).Width;
            var size = PixelSize();
            if (Size != size) Bounds = Clamp(Keep(Bounds, size));
            if (remember) Remember();
        }

        /// <summary>
        /// `b` at a new size, with the side that sticks to an edge (the game's or the screen's) still on it: a float against
        /// the game's right side stays against it when it turns small (the owner's test of 24 November 2026), one on the
        /// game's bottom grows upward, and a bar in the middle of the game stays in the middle; otherwise the top right
        /// corner stays, as before.
        /// </summary>
        private Rectangle Keep(Rectangle b, Size size)
        {
            var xs = new List<int>();
            var ys = new List<int>();
            var mids = Bar ? new List<int>() : null;
            Lines(b, Screen.FromRectangle(b).WorkingArea, 1, xs, ys, mids);
            var bottom = On(b.Bottom, ys) && !On(b.Top, ys);
            int x;
            if (mids != null && On(b.Left + b.Width / 2, mids)) x = b.Left + b.Width / 2 - size.Width / 2;
            else if (On(b.Left, xs) && !On(b.Right, xs)) x = b.Left;
            else x = b.Right - size.Width;
            return new Rectangle(x, bottom ? b.Bottom - size.Height : b.Top, size.Width, size.Height);
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
        /// Twenty times a second: hidden while the game is minimised and back with it; hidden when the game closes and back
        /// when it is there again (it used to stay on the screen, "Paused");
        /// moved with the game's window when it is at the game; on top while the game (or the float itself) is in front,
        /// else right above the game. Shown by the player without a game, it stays (on top of nothing) until a game it was
        /// at goes.
        /// </summary>
        private void WithGame()
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                var game = Game();
                if (game == IntPtr.Zero)
                {
                    // Minimised and then closed: out of sight it stays, until the game is there again.
                    if (_hiddenWithGame)
                    {
                        _hiddenWithGame = false;
                        _hiddenNoGame = true;
                    }
                    else if (Visible && _sawGame) HideNoGame();
                    _gameWas = Rectangle.Empty;
                    if (_onTop && Visible)
                    {
                        SetWindowPos(Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
                        _onTop = false;
                    }
                    return;
                }
                _sawGame = true;
                // A game window the float has not seen yet (the game started again): the float goes to its place at it.
                if (game != _gameSeenLast)
                {
                    _gameSeenLast = game;
                    _toGame = true;
                }
                if (IsIconic(game))
                {
                    // The game came back minimised: the float comes when it unfolds, as from the taskbar.
                    if (_hiddenNoGame)
                    {
                        _hiddenNoGame = false;
                        _hiddenWithGame = true;
                    }
                    HideWithGame();
                    return;
                }
                // The game's window is there again (or for the first time): the float shows with it, at its place there.
                if (_hiddenNoGame)
                {
                    _hiddenNoGame = false;
                    _gameWas = Rectangle.Empty;
                    if (!_placed)
                    {
                        // It came back with the program before the game: its first place, now that the game is there.
                        _placed = true;
                        _toGame = false;
                        Bounds = Bar ? BarPlace() : FloatPlace(FrameOf(game));
                    }
                    else if (_toGame && !_dragging && !_resizing)
                    {
                        _toGame = false;
                        ToGame(game);
                    }
                    Show();
                    Fit(false);
                }
                // Back from the taskbar, and the hook did not say so first: in now.
                if (_hiddenWithGame) FadeIn();
                if (!Visible) return;
                if (_toGame && !_dragging && !_resizing)
                {
                    _toGame = false;
                    ToGame(game);
                }
                Hook(game);
                // A bar put at the screen's top for want of a game goes to the game's top the first time the game is there.
                if (_barWantsGame)
                {
                    _barWantsGame = false;
                    var at = Bar && !_dragging ? BarAtGame(game, Size) : Rectangle.Empty;
                    if (!at.IsEmpty)
                    {
                        Bounds = Clamp(at);
                        _gameWas = Rectangle.Empty;
                    }
                }
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
                    else if (!_followed && !_dragging && !_resizing && frame == _gameWas && (Bar ? _settings.FloatBarAtGame : _settings.FloatAtGame) == null)
                    {
                        // Its place at the game not known yet (a float from before 0.4.2 that was not moved since): kept now.
                        KeepAtGame(Bar ? Rectangle.Empty : Bounds, Bar ? Bounds : Rectangle.Empty);
                        _settings.Save();
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

        /// <summary>The game's window went (the game was closed): the float goes with it, open, and is back with the game.</summary>
        private void HideNoGame()
        {
            if (!Visible) return;
            NoTip();
            EndFade();
            _hiddenNoGame = true;
            // A drag or a resize that was under way ends here: the float is not on the screen to finish it.
            _dragging = false;
            _resizing = false;
            Capture = false;
            Hide();
        }

        /// <summary>Shows the float again with the game: fading in when Windows animates, at once when it does not.</summary>
        private void FadeIn()
        {
            _hiddenWithGame = false;
            _gameWas = Rectangle.Empty;
            if (!_placed)
            {
                // It came back with the program while the game was on the taskbar: its first place, now that the game unfolds.
                var game = Game();
                _placed = true;
                _toGame = false;
                Bounds = Bar ? BarPlace() : FloatPlace(game != IntPtr.Zero ? FrameOf(game) : Rectangle.Empty);
                Fit(false);
            }
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

        /// <summary>
        /// The game's window moved or changed size: the float keeps its place to the game's nearer sides, and a bar in the
        /// game's middle stays in its middle. Where the other kind (the float while the bar shows, or the bar while the float
        /// does) last stood at the game moves along too, so it is back at the game when the button goes round to it.
        /// </summary>
        private void Follow(Rectangle was, Rectangle now)
        {
            var other = Bar ? _floatWas : _barWas;
            if (!other.IsEmpty && AtGame(other, was))
            {
                var moved = Shifted(other, was, now, !Bar);
                if (Bar) _floatWas = moved;
                else _barWas = moved;
            }
            var b = Bounds;
            var to = Shifted(b, was, now, Bar);
            if (to.Location == b.Location) return;
            Location = to.Location;
            _followed = true;
        }

        /// <summary>`b` moved as the game's window moved from `was` to `now`: by the nearer sides; a bar (`bar`) whose middle is the game's middle by the middle.</summary>
        private static Rectangle Shifted(Rectangle b, Rectangle was, Rectangle now, bool bar)
        {
            var middle = was.Left + was.Width / 2;
            int dx;
            if (bar && Math.Abs(b.Left + b.Width / 2 - middle) <= 2) dx = now.Left + now.Width / 2 - middle;
            else dx = b.Left + b.Width / 2 > middle ? now.Right - was.Right : now.Left - was.Left;
            var dy = b.Top + b.Height / 2 > was.Top + was.Height / 2 ? now.Bottom - was.Bottom : now.Top - was.Top;
            b.Offset(dx, dy);
            return b;
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
            // The bar also sticks with its middle to the game's middle.
            var mids = Bar ? new List<int>() : null;
            Lines(new Rectangle(want, size), Screen.FromPoint(Cursor.Position).WorkingArea, reach, xs, ys, mids);
            return new Point(Nearest(want.X, size.Width, xs, reach, mids), Nearest(want.Y, size.Height, ys, reach, null));
        }

        /// <summary>
        /// The lines the float sticks to: the screen's work area, and the game's frame and picture where `me` is beside them
        /// (within `reach`); in `mids` (when asked) the middles of the game's frame and picture, for the bar.
        /// </summary>
        private void Lines(Rectangle me, Rectangle area, int reach, List<int> xs, List<int> ys, List<int> mids)
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
                    if (mids != null) mids.Add(r.Left + r.Width / 2);
                }
                if (me.Right >= r.Left - reach && me.Left <= r.Right + reach)
                {
                    ys.Add(r.Top);
                    ys.Add(r.Bottom);
                }
            }
        }

        /// <summary>`start` moved so that its start or its end is on the nearest line within `reach`, or its middle on one of `mids`; else as it is.</summary>
        private static int Nearest(int start, int length, List<int> lines, int reach, List<int> mids)
        {
            var best = start;
            var gap = reach + 1;
            if (mids != null)
            {
                foreach (var mid in mids)
                {
                    var c = Math.Abs(start + length / 2 - mid);
                    if (c < gap)
                    {
                        gap = c;
                        best = mid - length / 2;
                    }
                }
            }
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

        /// <summary>Keeps the size and the place: the float's top right corner, or the bar's side; and where the other kind last stood in this window, should it have moved along with the game.</summary>
        private void Remember()
        {
            var key = LayoutKey();
            var me = Bounds;
            var floatAt = Bar ? _floatWas : me;
            var barAt = Bar ? me : _barWas;
            if (!floatAt.IsEmpty) _settings.FloatPlaces[key] = floatAt.Right + "," + floatAt.Top;
            if (!barAt.IsEmpty) _settings.FloatBarPlaces[key] = BarPlaceText(barAt);
            // And where each stands at the game, for when the game comes back.
            KeepAtGame(floatAt, barAt);
            _settings.FloatSize = SizeName;
            _settings.Save();
        }

        /// <summary>
        /// The button that goes round: Small → Large → the bar → Small. Small and large share a place and switch keeping the
        /// side they stick to, as before; the bar has a place of its own, so going to it or back from it puts the window where
        /// that kind last stood (its side kept), else where it is kept, else at its first place.
        /// </summary>
        private void SwitchTo(string size)
        {
            if (size == _size) return;
            NoTip();
            _hot = null;
            var wasBar = Bar;
            if (wasBar) _barWas = Bounds;
            else _floatWas = Bounds;
            _size = size;
            if (Bar) _barLeast = 0;
            if (wasBar != Bar)
            {
                var back = Bar ? _barWas : _floatWas;
                var game = Game();
                Bounds = !back.IsEmpty ? Clamp(Keep(back, PixelSize())) : Bar ? BarPlace() : FloatPlace(game != IntPtr.Zero ? FrameOf(game) : Rectangle.Empty);
            }
            Fit(true);
            Invalidate();
            if (SizeSwitched != null) SizeSwitched(SizeName);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Bar) _hits = FloatPainter.PaintBar(e.Graphics, _values, _scale, _hot, Look.IsLight, _locked, Texts.T("locked"), LogicalSize(_values).Width);
            else _hits = FloatPainter.Paint(e.Graphics, _values, Small, _scale, _hot, _scroll, _images, Look.IsLight, _user, _locked, _folded, Texts.T("locked"));
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        private string Under(Point pixel)
        {
            var p = new PointF(pixel.X / (_scale * _hits.Zoom), pixel.Y / (_scale * _hits.Zoom));
            if (_size == "large" && !_hits.Grip.IsEmpty && _hits.Grip.Contains(p)) return "grip";
            if (_hits.Close.Contains(p)) return "close";
            if (_hits.Size.Contains(p)) return "size";
            if (!_hits.Stop.IsEmpty && _hits.Stop.Contains(p)) return "stop";
            if (!_hits.Start.IsEmpty && _hits.Start.Contains(p)) return "start";
            if (!_hits.Reset.IsEmpty && _hits.Reset.Contains(p)) return "reset";
            if (!_hits.ShopAlways.IsEmpty && _hits.ShopAlways.Contains(p)) return "shop-always";
            if (!_hits.ShopNotNow.IsEmpty && _hits.ShopNotNow.Contains(p)) return "shop-not-now";
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
            // Held by the player: it stays where they put it.
            _barWantsGame = false;
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
            // The button that goes round says where a click goes: the next size's name. The round buttons (small and bar) say their word.
            var round = _size != "large";
            string tip = hot == "close" ? _values.CloseLabel : hot == "size" ? _values.LabelOf(FloatPainter.NextSize(_size)) : hot == "stop" && round ? _values.Stop : hot == "start" && round ? _values.Start : hot == "reset" && round ? _values.Reset : null;
            var box = hot == "close" ? _hits.Close : hot == "size" ? _hits.Size : hot == "stop" ? _hits.Stop : hot == "start" ? _hits.Start : hot == "reset" ? _hits.Reset : RectangleF.Empty;
            if (tip == null && _size == "large")
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
            Released(e);
            // Every click on the float (a button, its face, the end of a drag) is a click in the game: the game comes to the front.
            if (!IsDisposed) GameToFront();
        }

        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool AttachThreadInput(uint attach, uint to, bool on);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool BringWindowToTop(IntPtr h);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, System.Text.StringBuilder name, int most);
        /// <summary>How a click on the float brought the game to the front is said once in tempo.log; that it could not, at most once in ten minutes.</summary>
        private bool _frontSaid;
        private DateTime _frontFailedSaid = DateTime.MinValue;

        /// <summary>
        /// The game's window to the front after a click on the float, without the float ever taking the keyboard. The click
        /// was this program's, the last input Windows saw, so Windows lets it hand the front to another window
        /// (SetForegroundWindow); MapleStory running as administrator changes nothing in that. Should Windows refuse, the
        /// program joins the input of the window in front for a moment and asks again (AttachThreadInput: possible with a
        /// window of a program that does not run as administrator). The game's own window is never moved or sent anything
        /// (a program that is not administrator may not, and the float does not need it). A refusal is said in tempo.log.
        /// </summary>
        internal string GameToFront()
        {
            var game = Game();
            if (game == IntPtr.Zero || IsIconic(game)) return "no game";
            var front = GetForegroundWindow();
            if (front == game) return "in front already";
            var how = "SetForegroundWindow";
            var done = SetForegroundWindow(game) && GetForegroundWindow() == game;
            var error = done ? 0 : Marshal.GetLastWin32Error();
            if (!done)
            {
                how = "AttachThreadInput";
                uint process;
                var theirs = front != IntPtr.Zero ? GetWindowThreadProcessId(front, out process) : 0;
                var mine = GetCurrentThreadId();
                if (theirs != 0 && theirs != mine && AttachThreadInput(mine, theirs, true))
                {
                    try
                    {
                        BringWindowToTop(game);
                        done = SetForegroundWindow(game) && GetForegroundWindow() == game;
                    }
                    finally
                    {
                        AttachThreadInput(mine, theirs, false);
                    }
                }
                else error = Marshal.GetLastWin32Error();
            }
            if (done)
            {
                if (!_frontSaid) Tempo.Log("a click on the float brought the game to the front (" + how + ")");
                _frontSaid = true;
                return how;
            }
            if ((DateTime.UtcNow - _frontFailedSaid).TotalMinutes >= 10)
            {
                _frontFailedSaid = DateTime.UtcNow;
                var name = new System.Text.StringBuilder(128);
                try { if (front != IntPtr.Zero) GetClassName(front, name, 128); } catch (Exception) { }
                Tempo.Log("a click on the float could not bring the game to the front: Windows said no (error " + error + "); in front: a window of class \"" + name + "\"");
            }
            return "refused";
        }

        /// <summary>The mouse let go over the float: the end of a drag or a resize, or a click on one of its buttons.</summary>
        private void Released(MouseEventArgs e)
        {
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
                    SwitchTo(FloatPainter.NextSize(_size));
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
                // The own shop's question: the page puts it online (and keeps "Always"), or leaves this one shop off.
                case "shop-always":
                case "shop-not-now":
                    if (Command != null) Command(under);
                    break;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_size != "large" || _hits.MonstersShown == 0) return;
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
