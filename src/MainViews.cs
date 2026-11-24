// What the program's window looks like (MainForm.cs holds what it does). Drawn with Ui.cs at 660 by 440 (at 96 dpi,
// scaled as a whole for the screen), so every page is the same parts in the same places:
//
//   the first start, one thing a screen, with a bar per step: your language; your games; the link
//   (the code, one gold button that opens the site with the code filled in); the question "May Tempo look at your MapleStory window?" with in green
//   what Tempo looks at and in red what it never does;
//   after that a list at the left (Overview, MapleStory Classic, WoW: Forever, Settings; the version under it) and
//   the picked page at the right: a title, at most one gold button, one line that says how it is going, and one or
//   two cards. Where Tempo can't be used (the site refused to open it, or this Windows can't look at a window)
//   MapleStory's page is one plain card that says so, with no gold button.
//
// Nothing stands twice: the float's and Tempo's own settings are in Tempo; the program's are on Settings; the menu by
// the clock has none.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    internal sealed partial class MainForm
    {
        private const float NavWidth = 176f;
        private const float RowHeight = 36f;

        private static Color ToneOf(Palette p, string tone)
        {
            switch (tone ?? "")
            {
                case "negative":
                case "bad": return p.Negative;
                case "warning":
                case "warn": return p.Warning;
                case "info": return p.Info;
                case "positive":
                case "ok": return p.Positive;
                case "fg": return p.Foreground;
                default: return p.Muted;
            }
        }

        /// <summary>The whole window: the step that is open, or the list and its page.</summary>
        internal void Draw(Ui ui, float w, float h)
        {
            ui.Fill(ui.P.Background, 0, 0, w, h);
            _canvas.ScrollMost = 0;
            var stage = Stage();
            if (stage == "language") DrawLanguage(ui, w);
            else if (stage == "games") DrawGames(ui, w);
            else if (stage == "link-maple" || stage == "link-wow") DrawLink(ui, w, stage);
            else if (stage == "ask") DrawAsk(ui, w);
            else DrawMain(ui, w, h);
        }

        // ------------------------------------------------------------------------------------------------ the first start

        private const float StepWidth = 480f;

        /// <summary>The step bars, during the first start only. Answers where the step's own content begins.</summary>
        private float StepHead(Ui ui, float x, string stage)
        {
            float y = 28;
            if (_settings.SetupDone) return y;
            var steps = Steps();
            var at = Math.Max(1, steps.IndexOf(stage) + 1);
            ui.Steps(x, y, at, steps.Count, Texts.T("Step {0} of {1}", at, steps.Count));
            return y + 30;
        }

        /// <summary>A first start's first step: the six languages, Windows' own already picked; a click shows the step in that language at once.</summary>
        private void DrawLanguage(Ui ui, float w)
        {
            var p = ui.P;
            var x = (w - StepWidth) / 2;
            var y = StepHead(ui, x, "language");
            ui.Line(Texts.T("Choose your language"), ui.Question, p.Foreground, x, y, StepWidth, 22, 0);
            y += 34;
            const float gap = 10, height = 42;
            var half = (StepWidth - gap) / 2;
            for (var i = 0; i < Texts.Languages.Length; i++)
            {
                var code = Texts.Languages[i];
                var on = code == Texts.Language;
                var cx = x + (i % 2) * (half + gap);
                var cy = y + (i / 2) * (height + gap);
                var id = "language-" + code;
                ui.Card(cx, cy, half, height, ui.Hot == id && !on ? p.Raised : p.Surface, on ? p.Selected : p.Border);
                ui.Radio(cx + 14, cy + 13.5f, on);
                ui.Line(LanguageName(code), ui.Strong, p.Foreground, cx + 40, cy, half - 50, height, 0);
                ui.Add(id, new RectangleF(cx, cy, half, height), delegate { TryLanguage(code); }, true);
            }
            y += 3 * (height + gap) + 4;
            ui.Button("continue", x, y, Texts.T("Continue"), Ui.Gold, KeepLanguage, true);
            y += Look.ButtonHeight + 14;
            // "Preset from Windows" only while that is true: once the player picked another language the line says only
            // where to change it.
            var preset = Texts.Language == Texts.Pick(null);
            ui.Para(preset ? Texts.T("Preset from Windows. You can change it later in Settings.") : Texts.T("You can change it later in Settings."), ui.Small, p.Muted, x, y, StepWidth);
        }

        private void DrawGames(Ui ui, float w)
        {
            var p = ui.P;
            var x = (w - StepWidth) / 2;
            var y = StepHead(ui, x, "games");
            ui.Line(Texts.T("Which games do you play?"), ui.Question, p.Foreground, x, y, StepWidth, 22, 0);
            y += 34;
            y = GameCard(ui, x, y, "maple", "MapleStory Classic", Texts.T("Tempo: EXP, mesos and drops an hour, read from the game window."), _tickMaple, delegate { _tickMaple = !_tickMaple; Repaint(); });
            y = GameCard(ui, x, y, "wow", "WoW: Forever", Texts.T("Uploads your auction scans by itself."), _tickWow, delegate { _tickWow = !_tickWow; Repaint(); });
            y += 4;
            var go = ui.Button("continue", x, y, Texts.T("Continue"), Ui.Gold, async delegate { await ApplyGames(_tickWow, _tickMaple); }, _tickMaple || _tickWow);
            if (_settings.GamesAsked)
            {
                ui.Button("cancel", go.Right + 8, y, Texts.T("Cancel"), Ui.Plain, delegate
                {
                    _gamesEdit = false;
                    _tickMaple = _settings.Maple;
                    _tickWow = _settings.Wow;
                    Repaint();
                }, true);
            }
        }

        /// <summary>A game to tick: its name and one line of what the program does for it.</summary>
        private float GameCard(Ui ui, float x, float y, string id, string name, string line, bool on, Action toggle)
        {
            var p = ui.P;
            const float height = 60;
            ui.Card(x, y, StepWidth, height, ui.Hot == "game-" + id ? p.Raised : p.Surface, on ? p.Edge : p.Border);
            ui.Tick(x + 14, y + 13, on);
            ui.Line(name, ui.Strong, p.Foreground, x + 40, y + 10, StepWidth - 54, 20, 0);
            ui.Line(line, ui.Small, p.Muted, x + 40, y + 31, StepWidth - 54, 18, 0);
            ui.Add("game-" + id, new RectangleF(x, y, StepWidth, height), toggle, true);
            return y + height + 10;
        }

        private void DrawLink(Ui ui, float w, string stage)
        {
            var p = ui.P;
            var x = (w - StepWidth) / 2;
            var y = StepHead(ui, x, stage);
            var maple = stage == "link-maple";
            var both = _settings.Maple && _settings.Wow;
            var title = !both ? Texts.T("Link this program to your account") : maple ? Texts.T("Link Tempo to your account") : Texts.T("Link WoW: Forever to your account");
            ui.Line(title, ui.Question, p.Foreground, x, y, StepWidth, 22, 0);
            y += 32;
            var notice = maple ? _tempo.LinkNotice : _linkNotice;
            if (!string.IsNullOrEmpty(notice)) y += ui.Para(notice, ui.Body, p.Warning, x, y, StepWidth) + 10;

            // The code, as the site's page shows it again: the player checks that the two are the same.
            var link = CodeOf(stage);
            var box = new RectangleF(x + 0.5f, y + 0.5f, StepWidth - 1, 57);
            using (var path = Look.Rounded(box, Look.CardRadius))
            using (var back = new SolidBrush(p.Surface))
            using (var pen = new Pen(p.Edge, 1f) { DashStyle = DashStyle.Dash })
            {
                ui.G.FillPath(back, path);
                ui.G.DrawPath(pen, path);
            }
            ui.Line(link != null ? link.UserCode : "· · ·", Look.Mono(26f), link != null ? p.Foreground : p.Muted, x, y, StepWidth, 58, 1);
            y += 70;
            y += ui.Para(Texts.T("The button opens {0} with this code filled in. Log in as always and press Link there. No password or mail code ever goes into this program.", LinkAddress()), ui.Small, p.Muted, x, y, StepWidth) + 14;

            var open = ui.Button("open-link", x, y, Texts.T("Open {0}", LinkAddress()), Ui.Gold, delegate { OpenLink(stage); }, link != null);
            var copied = (DateTime.UtcNow - _copiedAt).TotalMilliseconds < 2500;
            ui.Button("copy-link", open.Right + 8, y, copied ? Texts.T("Copied") : Texts.T("Copy link"), Ui.Plain, delegate { CopyLink(stage); }, link != null);
            y += Look.ButtonHeight + 14;

            // How it is going: waiting (grey), a new code (orange), the site out of reach (red).
            var text = maple ? _tempo.LinkLine : _waitText;
            var tone = maple ? _tempo.LinkTone : _waitTone;
            var paused = maple ? _tempo.LinkPaused : _linkPaused;
            if (!paused && string.IsNullOrEmpty(tone)) text = link != null ? Texts.T("Waiting for the site… the code works for 10 minutes") : Texts.T("Asking the site for a code…");
            ui.Status(x, y, StepWidth, ToneOf(p, tone), text, ToneOf(p, tone));
        }

        private void DrawAsk(Ui ui, float w)
        {
            var p = ui.P;
            var x = (w - StepWidth) / 2;
            var y = StepHead(ui, x, "ask");
            ui.Line(Texts.T("May Tempo look at your MapleStory window?"), ui.Question, p.Foreground, x, y, StepWidth, 22, 0);
            y += 34;

            // Two cards side by side: in green what Tempo looks at, in red what it never does. The explanation stands here, once.
            var half = (StepWidth - 10) / 2;
            var looks = new[] { Texts.T("The picture of your MapleStory window, as you see it") };
            var never = new[] { Texts.T("Reads the game's memory, network or files"), Texts.T("Presses a key or clicks for you"), Texts.T("Sends a picture off this PC") };
            Func<string[], float> tall = delegate(string[] lines)
            {
                float sum = 10 + 22;
                foreach (var line in lines) sum += ui.Height(line, ui.Body, half - 24 - 20) + 6;
                return sum + 6;
            };
            var height = Math.Max(tall(looks), tall(never));
            ui.Card(x, y, half, height);
            ui.Card(x + half + 10, y, half, height);
            ui.Line(Texts.T("Tempo looks at"), ui.Small, p.Positive, x + 12, y + 10, half - 24, 18, 0);
            ui.Line(Texts.T("Tempo never"), ui.Small, p.Negative, x + half + 22, y + 10, half - 24, 18, 0);
            var ly = y + 32;
            foreach (var line in looks) ly += ui.Mark(x + 12, ly, half - 24, true, line) + 6;
            ly = y + 32;
            foreach (var line in never) ly += ui.Mark(x + half + 22, ly, half - 24, false, line) + 6;
            y += height + 14;

            var yes = ui.Button("yes", x, y, Texts.T("Yes, Tempo may look"), Ui.Gold, SayYes, true);
            ui.Button("no", yes.Right + 8, y, Texts.T("No, not now"), Ui.Plain, SayNo, true);
            y += Look.ButtonHeight + 14;
            ui.Para(Texts.T("Only what Tempo reads goes to GameMargins: shop prices and your session's numbers. You can turn this off later in Settings."), ui.Small, p.Muted, x, y, StepWidth);
        }

        // ------------------------------------------------------------------------------------------------ the list and its pages

        private void DrawMain(Ui ui, float w, float h)
        {
            var p = ui.P;
            var maple = _settings.Maple;
            var wow = _settings.Wow;
            if (_page == "maple" && !maple) _page = "overview";
            if (_page == "wow" && !wow) _page = "overview";

            // The list: the pages on top, Settings and the version at the foot.
            float y = 10;
            const float left = 10;
            var width = NavWidth - 20;
            ui.Nav("nav-overview", left, y, width, Texts.T("Overview"), _page == "overview", delegate { Go("overview"); });
            y += 32;
            if (maple)
            {
                ui.Nav("nav-maple", left, y, width, "MapleStory Classic", _page == "maple", delegate { Go("maple"); });
                y += ui.NavHeight("MapleStory Classic", width) + 2;
            }
            if (wow)
            {
                ui.Nav("nav-wow", left, y, width, "WoW: Forever", _page == "wow", delegate { Go("wow"); });
                y += 32;
            }
            // While a game is not turned on: a quiet line that leads to "Which games do you play?". Turning one off is in Settings.
            if (!maple || !wow) ui.NavQuiet("nav-add-game", left, y, width, "+ " + Texts.T("Add a game"), EditGames);
            ui.Nav("nav-settings", left, h - 10 - 20 - 32, width, Texts.T("Settings"), _page == "settings", delegate { Go("settings"); });
            ui.Line(VersionLine(), ui.Tiny, p.Muted, left + 12, h - 10 - 18, width - 14, 16, 0);
            ui.Fill(p.Border, NavWidth, 0, 1, h);

            // The page, in its own part of the window; it scrolls up and down only when it is taller than the window (a
            // player with many WoW accounts), never sideways.
            var px = NavWidth + 1 + 18;
            var pw = w - px - 18;
            var state = ui.G.Save();
            ui.G.SetClip(new RectangleF(NavWidth + 1, 0, w - NavWidth - 1, h));
            ui.Clip = new RectangleF(NavWidth + 1, 0, w - NavWidth - 1, h);
            var top = 16 - _canvas.Scroll;
            float bottom;
            if (_page == "maple") bottom = DrawMaple(ui, px, top, pw);
            else if (_page == "wow") bottom = DrawWow(ui, px, top, pw, h);
            else if (_page == "settings") bottom = DrawSettings(ui, px, top, pw);
            else bottom = DrawOverview(ui, px, top, pw);
            ui.G.Restore(state);
            ui.Clip = RectangleF.Empty;
            var content = bottom + _canvas.Scroll + 16;
            _canvas.ScrollMost = Math.Max(0, content - h);
            if (_canvas.ScrollMost > 0)
            {
                var track = h - 8;
                var thumb = Math.Max(24, track * h / content);
                ui.Fill(p.Edge, w - 5, 4 + (track - thumb) * _canvas.Scroll / _canvas.ScrollMost, 3, thumb);
            }
        }

        /// <summary>A page's head: its title and, at the right, its one main button. Answers the button's left edge.</summary>
        private float Head(Ui ui, float x, float y, float w, string title, string buttonId, string button, int kind, Action act, bool enabled)
        {
            var right = x + w;
            if (button != null)
            {
                var bw = ui.ButtonWidth(button);
                ui.Button(buttonId, right - bw, y, button, kind, act, enabled);
                right -= bw + 12;
            }
            ui.Line(title, ui.Title, ui.P.Foreground, x, y, right - x, Look.ButtonHeight, 0);
            return right;
        }

        /// <summary>A row of a card: a name at the left, a value (or a control) at the right, a thin rule over every row but the first.</summary>
        private void Row(Ui ui, float x, float y, float w, bool first, string name, string value, string tone, float reserve)
        {
            var p = ui.P;
            if (!first) ui.Rule(x + 12, y, w - 24);
            var nameWidth = Math.Min(ui.Width(name, ui.Body) + 4, w - 24 - reserve);
            ui.Line(name, ui.Body, p.Foreground, x + 12, y, nameWidth, RowHeight, 0);
            if (!string.IsNullOrEmpty(value)) ui.Line(value, ui.Body, tone == "" ? p.Foreground : ToneOf(p, tone), x + 12 + nameWidth + 12, y, w - 24 - nameWidth - 12 - reserve, RowHeight, 2);
        }

        private float DrawOverview(Ui ui, float x, float y, float w)
        {
            var p = ui.P;
            Head(ui, x, y, w, Texts.T("Overview"), null, null, 0, null, false);
            y += Look.ButtonHeight + 10;
            // Each game: its name, a dot with how it is going, and one button. One gold button on the page: Tempo's, else
            // WoW's. Where Tempo can't be used there is nothing to open: its row leads to MapleStory's page with a plain
            // button, and the gold one is WoW's.
            var shut = _settings.Maple && TempoShut;
            if (_settings.Maple)
            {
                var status = TempoStatus();
                var ready = _tempo.Linked && _tempo.Consent;
                if (shut) y = GameRow(ui, x, y, w, "maple", "MapleStory Classic", status, Texts.T("Details"), Ui.Plain, delegate { Go("maple"); }, true);
                else y = GameRow(ui, x, y, w, "maple", "MapleStory Classic", status, ready ? Texts.T("Open Tempo") : Texts.T("Answer the question"), Ui.Gold, OpenTempoOrWindow, true);
            }
            if (_settings.Wow)
            {
                var status = WowStatus();
                y = GameRow(ui, x, y, w, "wow", "WoW: Forever", status, Texts.T("Upload now"), _settings.Maple && !shut ? Ui.Plain : Ui.Gold, async delegate { await UploadAll(true); }, _uploadsRunning == 0 && WowFolderNow != null);
            }
            // After the program updated itself: one quiet line, with what is new a click away.
            var note = UpdatedNote();
            if (note != null)
            {
                ui.Card(x, y, w, 34, p.Background, p.Border);
                var nw = ui.Width(note + " · ", ui.Small);
                ui.Line(note + " · ", ui.Small, p.Muted, x + 12, y, nw + 2, 34, 0);
                // The site's timeline gives each release of the program the id "desktop-<version>": the link lands on this version's own entry.
                ui.LinkText("whats-new", x + 12 + nw, y + 8, Texts.T("What's new"), ui.Small, delegate { OpenSite("/whats-new#desktop-" + Program.Version); });
                y += 34 + 10;
            }
            return y;
        }

        private float GameRow(Ui ui, float x, float y, float w, string id, string name, string[] status, string button, int kind, Action act, bool enabled)
        {
            var p = ui.P;
            var bw = ui.ButtonWidth(button);
            var textWidth = w - 24 - bw - 12;
            var statusHeight = Math.Max(18, ui.Height(status[0], ui.Small, textWidth - 12) + 2);
            var height = 12 + 20 + statusHeight + 10;
            ui.Card(x, y, w, height);
            ui.Line(name, ui.Strong, p.Foreground, x + 12, y + 11, textWidth, 20, 0);
            ui.Status(x + 12, y + 32, textWidth, TrayPaint.ToneColour(p, status[1]), status[0], status[1] == "bad" ? p.Negative : status[1] == "warn" ? p.Warning : p.Muted);
            ui.Button("go-" + id, x + w - 12 - bw, y + (height - Look.ButtonHeight) / 2, button, kind, act, enabled);
            return y + height + 10;
        }

        private float DrawMaple(Ui ui, float x, float y, float w)
        {
            var p = ui.P;
            if (_tempo.Refused) return DrawMapleRefused(ui, x, y, w);
            if (_tempo.CantLook) return DrawMapleCantLook(ui, x, y, w);
            var ready = _tempo.Linked && _tempo.Consent;
            Head(ui, x, y, w, "MapleStory Classic", "open-tempo", ready ? Texts.T("Open Tempo") : Texts.T("Answer the question"), Ui.Gold, OpenTempoOrWindow, _tempo.SiteAllowed);
            y += Look.ButtonHeight + 6;
            var status = TempoStatus();
            y += ui.Status(x, y, w, TrayPaint.ToneColour(p, status[1]), status[0], status[1] == "bad" ? p.Negative : status[1] == "warn" ? p.Warning : p.Muted) + 8;

            if (_tempo.RuntimeMissing)
            {
                y += ui.Para(Texts.T("Tempo's window needs Microsoft's WebView2, which this Windows doesn't have yet. Get it from Microsoft, then press Open Tempo again."), ui.Body, p.Foreground, x, y, w) + 10;
                ui.Button("webview2", x, y, Texts.T("Get WebView2 from Microsoft"), Ui.Plain, delegate { try { System.Diagnostics.Process.Start(WebRuntime.Download); } catch (Exception) { } }, true);
                y += Look.ButtonHeight + 12;
            }

            var rows = 3;
            ui.Card(x, y, w, rows * RowHeight);
            Row(ui, x, y, w, true, Texts.T("Game window"), _tempo.GameFound == true ? Texts.T("found") : _tempo.GameFound == false ? Texts.T("MapleStory is not running") : Texts.T("not looking"), _tempo.GameFound == true ? "" : "muted", 0);
            Row(ui, x, y + RowHeight, w, false, Texts.T("Tempo may look at the game"), _tempo.Consent ? Texts.T("yes") : Texts.T("not yet"), _tempo.Consent ? "" : "muted", 0);
            Row(ui, x, y + 2 * RowHeight, w, false, Texts.T("Float"), !_settings.FloatShown ? Texts.T("hidden") : _settings.FloatLocked ? Texts.T("shown · locked") : Texts.T("shown"), _settings.FloatShown ? "" : "muted", 0);
            y += rows * RowHeight + 10;
            y += ui.Para(Texts.T("Tempo has its own window. Its settings, and the float's, are in that window under Settings."), ui.Small, p.Muted, x, y, w) + 4;
            return y;
        }

        /// <summary>
        /// MapleStory's page when the site refused to open Tempo for this account (it should not: Tempo is for every
        /// account): said plainly in red, with one plain button to ask again. No gold button, since "Open Tempo" would
        /// only be refused again. The moment the site says yes the usual page is back; a new start of the program asks
        /// afresh as well.
        /// </summary>
        private float DrawMapleRefused(Ui ui, float x, float y, float w)
        {
            var p = ui.P;
            Head(ui, x, y, w, "MapleStory Classic", null, null, 0, null, false);
            y += Look.ButtonHeight + 6;
            var status = TempoStatus();
            y += ui.Status(x, y, w, TrayPaint.ToneColour(p, status[1]), status[0], p.Negative) + 8;

            var body = Texts.T("gamemargins.com answered that this account can't open Tempo right now. Try again in a moment. If it stays this way, unlink this PC under Settings and link it again.");
            var inner = w - 24;
            var bodyHeight = ui.Height(body, ui.Body, inner);
            var height = 12 + bodyHeight + 12 + Look.ButtonHeight + 12;
            ui.Card(x, y, w, height);
            ui.Para(body, ui.Body, p.Foreground, x + 12, y + 12, inner);
            ui.Button("try-again", x + 12, y + 12 + bodyHeight + 12, Texts.T("Try again"), Ui.Plain, CheckTempoAgain, !_tempo.Asking);
            y += height;
            // A pressed "Try again" that could not reach the site says so, under the card.
            if (!string.IsNullOrEmpty(_tempo.CheckNote)) y += 10 + ui.Para(_tempo.CheckNote, ui.Small, p.Negative, x, y + 10, w);
            return y;
        }

        /// <summary>
        /// MapleStory's page on a Windows that can't hand over a window's picture (older than Windows 10 version 1903):
        /// said plainly, in red since it can't work here; the rest of the program is as it was.
        /// </summary>
        private float DrawMapleCantLook(Ui ui, float x, float y, float w)
        {
            var p = ui.P;
            Head(ui, x, y, w, "MapleStory Classic", null, null, 0, null, false);
            y += Look.ButtonHeight + 6;
            var status = TempoStatus();
            y += ui.Status(x, y, w, TrayPaint.ToneColour(p, status[1]), status[0], p.Negative) + 8;
            var text = Texts.T("Tempo looks at the game's window through a part of Windows that came with Windows 10 version 1903. This Windows is older, so Tempo can't look here. Update Windows to use Tempo; everything else in this program works as before.");
            var height = ui.Height(text, ui.Body, w - 24) + 24;
            ui.Card(x, y, w, height);
            ui.Para(text, ui.Body, p.Foreground, x + 12, y + 12, w - 24);
            return y + height;
        }

        private float DrawWow(Ui ui, float x, float y, float w, float h)
        {
            var p = ui.P;
            var linked = !string.IsNullOrEmpty(_settings.Token);
            Head(ui, x, y, w, "WoW: Forever", "upload", Texts.T("Upload now"), Ui.Gold, async delegate { await UploadAll(true); }, linked && _uploadsRunning == 0 && WowFolderNow != null);
            y += Look.ButtonHeight + 6;
            var status = WowStatus();
            var line = status[0];
            var next = Texts.T("next upload as soon as the game writes the file");
            // When the last upload was is the list's to say (its first row): this line then says only what comes next
            // (else "Uploaded just now" would stand here and "just now" in the list).
            if (status[1] == "ok" && _uploadsRunning == 0 && LastStored() != null) line = char.ToUpper(next[0], Texts.Culture) + next.Substring(1);
            else if (status[1] == "ok" || status[1] == "") line += " · " + next;
            y += ui.Status(x, y, w, TrayPaint.ToneColour(p, status[1]), line, status[1] == "bad" ? p.Negative : status[1] == "warn" ? p.Warning : p.Muted) + 8;

            // The accounts, the folder and the addon together in one card.
            var accounts = AccountsNow;
            var rows = Math.Max(1, accounts.Count) + 2;
            if (WowFolderNow == null) rows = 2;
            ui.Card(x, y, w, rows * RowHeight);
            var ry = y;
            if (WowFolderNow == null)
            {
                var bw = ui.ButtonWidth(Texts.T("Choose folder…"));
                Row(ui, x, ry, w, true, Texts.T("WoW folder"), Texts.T("not found"), "warning", bw + 12);
                ui.Button("folder", x + w - 12 - bw, ry + 3, Texts.T("Choose folder…"), Ui.Plain, PickWowFolder, true);
                ry += RowHeight;
            }
            else
            {
                if (accounts.Count == 0)
                {
                    Row(ui, x, ry, w, true, Texts.T("WoW accounts"), Texts.T("no scans yet"), "muted", 0);
                    ry += RowHeight;
                }
                var firstRow = true;
                foreach (var account in accounts)
                {
                    string[] said;
                    if (!_accountStatus.TryGetValue(account.Number, out said)) said = new[] { account.Exists ? Texts.T("file found · watching") : Texts.T("no scans yet"), "muted" };
                    Row(ui, x, ry, w, firstRow, Texts.T("WoW account {0}", account.Number), said[0], said[1] == "muted" ? "" : said[1], 0);
                    firstRow = false;
                    ry += RowHeight;
                }
                var bw = ui.ButtonWidth(Texts.T("Change"));
                Row(ui, x, ry, w, false, Texts.T("WoW folder"), ShortPath(ui, WowFolderNow, w - 24 - ui.Width(Texts.T("WoW folder"), ui.Body) - 28 - bw - 12), "", bw + 12);
                ui.Button("folder", x + w - 12 - bw, ry + 3, Texts.T("Change"), Ui.Plain, PickWowFolder, true);
                ry += RowHeight;
            }
            var installed = WowFolderNow != null ? Addon.InstalledVersion(WowFolderNow) : null;
            var addon = _addonText;
            var addonTone = _addonTone;
            if (addon == null)
            {
                addonTone = installed == null ? "muted" : "";
                if (installed == null) addon = _settings.KeepAddonUpdated ? Texts.T("not installed · installs when WoW is closed") : Texts.T("not installed · you install it yourself");
                else addon = _settings.KeepAddonUpdated ? Texts.T("{0} · up to date", installed) : Texts.T("{0} · you keep it up to date yourself", installed);
            }
            Row(ui, x, ry, w, false, Texts.T("GameMargins addon"), addon, addonTone, 0);
            y += rows * RowHeight + 10;

            // What the last try said, when it is something to know (a skipped scan, a failure, a wait).
            if (!string.IsNullOrEmpty(_result) && _resultTone != "muted") y += ui.Para(_result, ui.Small, ToneOf(p, _resultTone), x, y, w) + 10;

            // The last uploads: as many as the window has room for (three at least).
            var uploads = _settings.Uploads;
            if (uploads.Count == 0)
            {
                var text = Texts.T("No uploads yet. Scan the auction house in the game, then log out or type /reload: the upload starts by itself.");
                var th = ui.Height(text, ui.Small, w - 24);
                ui.Card(x, y, w, th + 20);
                ui.Para(text, ui.Small, p.Muted, x + 12, y + 10, w - 24);
                return y + th + 20;
            }
            const float lineHeight = 30;
            var room = (int)Math.Floor((h - 16 - (y + _canvas.Scroll) - 28) / lineHeight);
            var shown = Math.Min(uploads.Count, Math.Max(3, room));
            ui.Card(x, y, w, 28 + shown * lineHeight);
            var c2 = x + 12 + (w - 24) * 0.34f;
            ui.Line(Texts.T("When"), ui.Small, p.Muted, x + 12, y, c2 - x - 12, 28, 0);
            ui.Line(Texts.T("Auction house"), ui.Small, p.Muted, c2, y, 160, 28, 0);
            ui.Line(Texts.T("Auctions"), ui.Small, p.Muted, x + 12, y, w - 24, 28, 2);
            var uy = y + 28;
            for (var i = 0; i < shown; i++)
            {
                var row = uploads[i];
                ui.Rule(x + 1, uy, w - 2);
                ui.Line(WhenText(row.At, ShotNow ?? DateTime.Now), ui.Body, p.Foreground, x + 12, uy, c2 - x - 20, lineHeight, 0);
                ui.Line(MarketLabel(row.Market), ui.Body, p.Foreground, c2, uy, w - 24 - (c2 - x - 12) - 80, lineHeight, 0);
                ui.Line(row.Auctions.ToString("N0", Texts.Culture), ui.Body, p.Foreground, x + 12, uy, w - 24, lineHeight, 2);
                uy += lineHeight;
            }
            return uy;
        }

        /// <summary>A path that fits `width`: its start is left out ("…\Games\World of Warcraft") rather than its end.</summary>
        private static string ShortPath(Ui ui, string path, float width)
        {
            if (string.IsNullOrEmpty(path) || ui.Width(path, ui.Body) <= width) return path ?? "";
            var parts = path.Split('\\');
            for (var from = 1; from < parts.Length; from++)
            {
                var shorter = "…\\" + string.Join("\\", parts, from, parts.Length - from);
                if (ui.Width(shorter, ui.Body) <= width) return shorter;
            }
            return "…\\" + parts[parts.Length - 1];
        }

        private float DrawSettings(Ui ui, float x, float y, float w)
        {
            var p = ui.P;
            Head(ui, x, y, w, Texts.T("Settings"), null, null, 0, null, false);
            y += Look.ButtonHeight + 10;

            // Every setting of the program, on this page and nowhere else: a switch a row.
            var rows = new List<Action<float>>();
            rows.Add(delegate(float ry)
            {
                Row(ui, x, ry, w, ry == y, Texts.T("Start with Windows"), null, "", 50);
                ui.Switch("sw-autostart", x + w - 12, ry + RowHeight / 2, _settings.StartWithWindows, ToggleStartWithWindows, true);
            });
            rows.Add(delegate(float ry)
            {
                Row(ui, x, ry, w, false, Texts.T("Keep the program up to date"), null, "", 50);
                ui.Switch("sw-program", x + w - 12, ry + RowHeight / 2, _settings.KeepProgramUpdated, ToggleKeepProgram, true);
            });
            if (!_settings.KeepProgramUpdated && _offered != null)
            {
                rows.Add(delegate(float ry)
                {
                    var label = Texts.T("Update now");
                    var bw = ui.ButtonWidth(label);
                    Row(ui, x, ry, w, false, Texts.T("Version {0} is ready", _offered.Version), null, "", bw + 12);
                    ui.Button("update-now", x + w - 12 - bw, ry + 3, label, Ui.Plain, UpdateNow, !_updating);
                });
            }
            if (_settings.Wow)
            {
                rows.Add(delegate(float ry)
                {
                    Row(ui, x, ry, w, false, Texts.T("Keep the addon up to date"), null, "", 50);
                    ui.Switch("sw-addon", x + w - 12, ry + RowHeight / 2, _settings.KeepAddonUpdated, ToggleKeepAddon, true);
                });
            }
            if (_settings.Maple && _tempo.Linked && !TempoShut)
            {
                rows.Add(delegate(float ry)
                {
                    Row(ui, x, ry, w, false, Texts.T("Tempo may look at the MapleStory window"), null, "", 50);
                    ui.Switch("sw-consent", x + w - 12, ry + RowHeight / 2, _tempo.Consent, ToggleConsent, true);
                });
            }
            rows.Add(delegate(float ry)
            {
                // Light or dark: as Windows is, or always one of the two.
                var name = Texts.T("Appearance");
                var labels = new[] { Texts.T("Follow Windows"), Texts.T("Light"), Texts.T("Dark") };
                var modes = new[] { "system", "light", "dark" };
                Row(ui, x, ry, w, false, name, null, "", ui.SegmentsWidth(labels) + 12);
                var segLeft = ui.Segments("appearance", x + w - 12, ry + 3, labels, Array.IndexOf(modes, _settings.Appearance == "light" || _settings.Appearance == "dark" ? _settings.Appearance : "system"), delegate(int i) { PickAppearance(modes[i]); });
                if (_shots && segLeft < x + 12 + ui.Width(name, ui.Body) + 8) LayoutProblems.Add("Settings: the Appearance row does not fit (" + Texts.Language + ")");
            });
            rows.Add(delegate(float ry)
            {
                var label = LanguageName(Texts.Language) + "  ▾";
                var bw = ui.ButtonWidth(label);
                Row(ui, x, ry, w, false, Texts.T("Language"), null, "", bw + 12);
                var box = new RectangleF(x + w - 12 - bw, ry + 3, bw, Look.ButtonHeight);
                ui.Button("language", box.X, box.Y, label, Ui.Plain, delegate { ShowLanguages(box); }, true);
            });
            rows.Add(delegate(float ry)
            {
                var label = Texts.T("Add or remove a game");
                var bw = ui.ButtonWidth(label);
                var games = _settings.Maple && _settings.Wow ? "MapleStory Classic, WoW: Forever" : _settings.Maple ? "MapleStory Classic" : "WoW: Forever";
                Row(ui, x, ry, w, false, Texts.T("Games"), null, "", bw + 12);
                var nameWidth = ui.Width(Texts.T("Games"), ui.Body) + 10;
                ui.Line(games, ui.Body, p.Muted, x + 12 + nameWidth, ry, w - 24 - nameWidth - bw - 12, RowHeight, 0);
                ui.Button("games", x + w - 12 - bw, ry + 3, label, Ui.Plain, EditGames, true);
            });
            rows.Add(delegate(float ry)
            {
                if (_unlinkAsk)
                {
                    var no = Texts.T("Cancel");
                    var yes = Texts.T("Unlink");
                    var nw = ui.ButtonWidth(no);
                    var yw = ui.ButtonWidth(yes);
                    Row(ui, x, ry, w, false, Texts.T("Unlink this PC from your account?"), null, "", nw + yw + 20);
                    ui.Button("unlink-yes", x + w - 12 - nw - 8 - yw, ry + 3, yes, Ui.Danger, UnlinkHere, true);
                    ui.Button("unlink-no", x + w - 12 - nw, ry + 3, no, Ui.Plain, delegate { _unlinkAsk = false; Repaint(); }, true);
                    return;
                }
                var label = Texts.T("Unlink");
                var bw = ui.ButtonWidth(label);
                Row(ui, x, ry, w, false, Texts.T("Account"), null, "", bw + 12);
                var nameWidth = ui.Width(Texts.T("Account"), ui.Body) + 10;
                ui.Line(Texts.T("linked"), ui.Body, p.Muted, x + 12 + nameWidth, ry, 120, RowHeight, 0);
                ui.Button("unlink", x + w - 12 - bw, ry + 3, label, Ui.Danger, delegate { _unlinkAsk = true; Repaint(); }, true);
            });
            ui.Card(x, y, w, rows.Count * RowHeight);
            foreach (var row in rows)
            {
                row(y);
                y += RowHeight;
            }
            return y;
        }

        /// <summary>For the pictures: the clock the made-up uploads are read against; null in the running program.</summary>
        internal static DateTime? ShotNow;
        /// <summary>What a page found that does not fit while it drew (the pictures' check reads it).</summary>
        internal static readonly List<string> LayoutProblems = new List<string>();
    }
}
