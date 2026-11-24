// Pictures of everything the program draws, made without any window:
//
//   GameMarginsDesktop.exe --shots <folder>
//
// Every screen of the program's window (the first start's steps for one game and for two, Overview, MapleStory
// Classic, WoW: Forever, Settings; a program used for WoW: Forever alone; the page of an account the site refused Tempo,
// and of a Windows that can't look at a window), the float (both sizes; waiting, reading, paused, not started), the menu by the
// clock and a drawing of a notification, each in dark and in light, at 100% and at 150%, as PNGs in the folder
// ("<name>-dark.png", "<name>-light@1.5.png"). Everything is drawn straight into a picture with made-up states: no
// window is created or shown, the keyboard and the screen are left alone, nothing is asked of the site, and the
// settings are not read or written (a "data" folder inside the folder stands in for them). The window's title bar in
// the pictures is drawn here to look like Windows 11's; the real one is Windows' own. A notification is Windows' own
// too: its picture is a drawing of how Windows 11 shows the title and the line the program hands it.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    internal sealed partial class MainForm
    {
        private string _fakeFolder;
        private List<WowAccount> _fakeAccounts = new List<WowAccount>();

        /// <summary>The WoW folder and its accounts: the scanner's, or made-up ones for a picture.</summary>
        private string WowFolderNow { get { return _shots ? _fakeFolder : _scanner.WowFolder; } }
        private List<WowAccount> AccountsNow { get { return _shots ? _fakeAccounts : new List<WowAccount>(_scanner.Accounts); } }

        /// <summary>A made-up state for one picture (only in a window made for pictures).</summary>
        internal void Scene(string name)
        {
            if (!_shots) return;
            var s = _settings;
            s.GamesAsked = true;
            s.LanguageAsked = true;
            s.LanguageStep = true;
            s.Appearance = "system";
            s.SetupDone = false;
            s.Maple = true;
            s.Wow = false;
            s.Token = null;
            s.TempoToken = null;
            s.TempoConsent = false;
            s.TempoAsked = false;
            s.StartWithWindows = true;
            s.KeepProgramUpdated = true;
            s.KeepAddonUpdated = true;
            s.FloatShown = true;
            s.UpdatedTo = null;
            s.Uploads.Clear();
            _gamesEdit = false;
            _askAgain = false;
            _unlinkAsk = false;
            _linkNotice = null;
            _waitText = "";
            _waitTone = "";
            _result = "";
            _addonText = null;
            _offered = null;
            _checked = true;
            _fakeFolder = null;
            _fakeAccounts = new List<WowAccount>();
            _accountStatus.Clear();
            _tempo.Link = null;
            _link = null;
            _tempo.LinkLine = "";
            _tempo.LinkTone = "";
            _tempo.GameFound = null;
            _tempo.SessionState = "idle";
            _tickMaple = true;
            _tickWow = false;
            _tempo.SiteRefused = false;
            _tempo.Problem = null;
            _tempo.CheckNote = null;
            _tempo.CantLook = false;
            _tempo.RuntimeMissing = false;
            var code =new LinkStart { UserCode = "K7QM-42XD", DeviceCode = "x", ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
            var now = new DateTime(2026, 11, 23, 14, 26, 0);
            ShotNow = now;
            Action linked = delegate
            {
                s.SetupDone = true;
                s.TempoToken = "gm_mt_made-up";
                s.TempoConsent = true;
                s.TempoAsked = true;
                _tempo.GameFound = true;
                _tempo.SessionState = "reading";
                _tempo.SessionSeconds = 26 * 60 + 14;
                _tempo.SessionSaid = default(DateTime);
            };
            Action wow = delegate
            {
                s.Wow = true;
                s.Token = "gm_wu_made-up";
                _fakeFolder = @"C:\Games\World of Warcraft";
                _fakeAccounts.Add(new WowAccount { Number = 1, Folder = @"C:\nowhere" });
                _accountStatus[1] = new[] { Texts.T("file found · watching"), "muted" };
                _addonText = Texts.T("{0} · up to date", "0.7.0");
                _addonTone = "";
                s.Uploads.Add(new UploadRow { At = now.AddMinutes(-3).ToUniversalTime(), Market = "eu-normal-horde", Auctions = 240910, Status = "stored" });
                s.Uploads.Add(new UploadRow { At = now.AddDays(-1).Date.AddHours(21).AddMinutes(40).ToUniversalTime(), Market = "eu-normal-horde", Auctions = 244569, Status = "stored" });
                s.Uploads.Add(new UploadRow { At = now.AddDays(-2).Date.AddHours(19).AddMinutes(5).ToUniversalTime(), Market = "eu-normal-horde", Auctions = 239112, Status = "stored" });
            };
            // The program of someone who plays WoW: Forever alone: linked, set up.
            Action wowOnly = delegate
            {
                wow();
                s.Maple = false;
                s.SetupDone = true;
            };
            // MapleStory Classic on and linked, and the site refused to open Tempo for this account.
            Action refused = delegate
            {
                s.SetupDone = true;
                s.TempoToken = "gm_mt_made-up";
                _tempo.SiteRefused = true;
            };
            switch (name)
            {
                case "first-start-1-language":
                    s.GamesAsked = false;
                    s.LanguageAsked = false;
                    s.LanguageStep = false;
                    break;
                case "first-start-2-games":
                    s.GamesAsked = false;
                    break;
                case "first-start-3-link":
                    _tempo.Link = code;
                    break;
                case "first-start-4-question":
                    s.TempoToken = "gm_mt_made-up";
                    break;
                case "first-start-two-games-2-games":
                    s.GamesAsked = false;
                    _tickWow = true;
                    break;
                case "first-start-two-games-3-link-tempo":
                    s.Wow = true;
                    _tempo.Link = code;
                    break;
                case "first-start-two-games-4-link-wow":
                    s.Wow = true;
                    s.TempoToken = "gm_mt_made-up";
                    _link = new LinkStart { UserCode = "R3VD-8KPA", DeviceCode = "x", ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
                    break;
                case "first-start-two-games-5-question":
                    s.Wow = true;
                    s.TempoToken = "gm_mt_made-up";
                    s.Token = "gm_wu_made-up";
                    break;
                case "overview":
                    linked();
                    wow();
                    s.UpdatedTo = Program.Version;
                    s.UpdatedAt = now.AddDays(-1).Date.AddHours(23).ToUniversalTime();
                    _page = "overview";
                    break;
                case "overview-one-game":
                    linked();
                    _tempo.SessionState = "idle";
                    _tempo.GameFound = false;
                    _page = "overview";
                    break;
                case "maplestory-classic":
                    linked();
                    wow();
                    _page = "maple";
                    break;
                case "wow-forever":
                    linked();
                    wow();
                    _page = "wow";
                    break;
                case "settings":
                    linked();
                    wow();
                    _page = "settings";
                    break;
                case "add-or-remove-a-game":
                    linked();
                    wow();
                    _tickWow = true;
                    _gamesEdit = true;
                    break;

                // ---- A program used for WoW: Forever alone.
                case "wow-only-overview":
                    // Someone who updated from an older version: WoW only, nothing new to click.
                    wowOnly();
                    s.UpdatedTo = Program.Version;
                    s.UpdatedAt = now.AddDays(-1).Date.AddHours(23).ToUniversalTime();
                    _page = "overview";
                    break;
                case "wow-only-wow-forever":
                    // WoW's page with everything on it: three accounts, a long folder, uploads on two auction houses.
                    wowOnly();
                    _fakeFolder = @"D:\Program Files (x86)\Battle.net Games\World of Warcraft";
                    _fakeAccounts.Add(new WowAccount { Number = 2, Folder = @"C:\nowhere" });
                    _fakeAccounts.Add(new WowAccount { Number = 3, Folder = @"C:\nowhere" });
                    _accountStatus[1] = new[] { Texts.T("uploaded · watching"), "muted" };
                    _accountStatus[2] = new[] { Texts.T("file found · watching"), "muted" };
                    _accountStatus[3] = new[] { Texts.T("no scans yet"), "muted" };
                    s.Uploads.Insert(1, new UploadRow { At = now.AddMinutes(-41).ToUniversalTime(), Market = "eu-pvp-alliance", Auctions = 198204, Status = "stored" });
                    s.Uploads.Insert(3, new UploadRow { At = now.AddDays(-1).Date.AddHours(18).AddMinutes(12).ToUniversalTime(), Market = "eu-pvp-alliance", Auctions = 201377, Status = "stored" });
                    _page = "wow";
                    break;
                case "wow-only-settings":
                    wowOnly();
                    _page = "settings";
                    break;

                // ---- MapleStory Classic is on, and the site refused to open Tempo for the account: the plain card.
                case "maplestory-classic-refused":
                    refused();
                    wow();
                    _page = "maple";
                    break;
                case "maplestory-classic-refused-no-site":
                    // "Try again" pressed while the site is out of reach.
                    refused();
                    wow();
                    _tempo.CheckNote = Texts.T("Can't reach {0}; trying again later.", "gamemargins.com");
                    _page = "maple";
                    break;
                case "overview-refused":
                    refused();
                    wow();
                    _page = "overview";
                    break;

                // ---- A Windows that can't hand over a window's picture (before Windows 10 version 1903).
                case "maplestory-classic-old-windows":
                    linked();
                    wow();
                    _tempo.SessionState = "idle";
                    _tempo.GameFound = null;
                    _tempo.CantLook = true;
                    _page = "maple";
                    break;
                case "overview-old-windows":
                    linked();
                    wow();
                    _tempo.SessionState = "idle";
                    _tempo.GameFound = null;
                    _tempo.CantLook = true;
                    _page = "overview";
                    break;
            }
        }

        /// <summary>The menu's lines for a picture of one scene.</summary>
        internal List<TrayLine> SceneMenuOf(string scene)
        {
            Scene(scene);
            return TrayLines();
        }

        /// <summary>The menu's lines for a picture: both games, Tempo reading.</summary>
        internal List<TrayLine> SceneMenu(bool both)
        {
            Scene(both ? "overview" : "overview-one-game");
            if (!both)
            {
                _tempo.SessionState = "reading";
                _tempo.GameFound = true;
            }
            return TrayLines();
        }
    }

    internal static class Shots
    {
        private static readonly string[] Windows =
        {
            "first-start-1-language", "first-start-2-games", "first-start-3-link", "first-start-4-question",
            "first-start-two-games-2-games", "first-start-two-games-3-link-tempo", "first-start-two-games-4-link-wow", "first-start-two-games-5-question",
            "overview", "overview-one-game", "maplestory-classic", "wow-forever", "settings", "add-or-remove-a-game",
            // A program used for WoW: Forever alone, and the page of an account the site refused Tempo.
            "wow-only-overview", "wow-only-wow-forever", "wow-only-settings",
            "maplestory-classic-refused", "maplestory-classic-refused-no-site", "overview-refused",
            "maplestory-classic-old-windows", "overview-old-windows",
        };

        /// <summary>The menu by the clock in the states it has beside "two games" and "MapleStory only": a picture's name and the scene it shows.</summary>
        private static readonly string[][] Menus =
        {
            new[] { "menu-wow-only", "wow-only-overview" },
            new[] { "menu-refused", "overview-refused" },
        };

        public static int All(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                Settings.FolderOverride = Path.Combine(folder, "data");
                Program.Isolated = true;
                // GM_SHOTS_LANG=nl (de, fr, es, pt-br) draws the pictures in that language, to read the translations in place; English without it.
                var shotsLanguage = Texts.Pick(Environment.GetEnvironmentVariable("GM_SHOTS_LANG") ?? "en");
                Texts.Language = shotsLanguage;
                var settings = new Settings { Site = Program.DefaultSite };
                using (var form = new MainForm(settings, null, true))
                {
                    foreach (var light in new[] { false, true })
                    {
                        Look.Force(light);
                        foreach (var scale in new[] { 1f, 1.5f })
                        {
                            var tail = "-" + (light ? "light" : "dark") + (scale > 1f ? "@1.5" : "") + ".png";
                            foreach (var name in Windows)
                            {
                                form.Scene(name);
                                Window(form, Path.Combine(folder, "window-" + name + tail), scale);
                            }
                            foreach (var shot in Floats())
                            {
                                foreach (var small in new[] { false, true })
                                {
                                    var size = FloatPainter.Measure(shot.Value, small, Size.Empty);
                                    using (var bitmap = new Bitmap((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale), PixelFormat.Format32bppArgb))
                                    {
                                        using (var g = Graphics.FromImage(bitmap)) FloatPainter.Paint(g, shot.Value, small, scale, null, 0, null, light, Size.Empty, false, null, Texts.T("locked"));
                                        bitmap.Save(Path.Combine(folder, "float-" + (small ? "small" : "large") + "-" + shot.Key + tail), ImageFormat.Png);
                                    }
                                }
                            }
                            RealFloat(Path.Combine(folder, "float-large-real-pictures" + tail), scale, light, Size.Empty, null, false, false);
                            RealFloat(Path.Combine(folder, "float-large-real-pictures-dragged-larger" + tail), scale, light, new Size(340, 520), null, false, false);
                            RealFloat(Path.Combine(folder, "float-large-real-pictures-item-name" + tail), scale, light, Size.Empty, "Wild Boar Tooth × 28", false, false);
                            // The float grows with its width: the usual size, about 390 by 470, and the largest sizes (from 445 wide); each also locked.
                            RealFloat(Path.Combine(folder, "float-large-real-pictures-locked" + tail), scale, light, Size.Empty, null, true, false);
                            RealFloat(Path.Combine(folder, "float-large-real-pictures-390x470" + tail), scale, light, new Size(390, 470), null, false, false);
                            RealFloat(Path.Combine(folder, "float-large-real-pictures-390x470-locked" + tail), scale, light, new Size(390, 470), null, true, false);
                            RealFloat(Path.Combine(folder, "float-large-real-pictures-390x470-item-name" + tail), scale, light, new Size(390, 470), "Wild Boar Tooth × 28", false, false);
                            RealFloat(Path.Combine(folder, "float-large-real-pictures-largest-445x620" + tail), scale, light, new Size(445, 620), null, false, false);
                            RealFloat(Path.Combine(folder, "float-large-real-pictures-largest-445x620-locked" + tail), scale, light, new Size(445, 620), null, true, false);
                            RealFloat(Path.Combine(folder, "float-small-real-locked" + tail), scale, light, Size.Empty, null, true, true);
                            // Windows' own menu, filled and laid out as by the clock (never shown), with the longest first line.
                            RealMenuCheck(LongStateMenu(form), "picture", Texts.Language, new KeyValuePair<string, float>(Path.Combine(folder, "menu-real-long-first-line" + tail), scale));
                            Menu(form.SceneMenu(true), Path.Combine(folder, "menu-two-games" + tail), scale);
                            Menu(form.SceneMenu(false), Path.Combine(folder, "menu-maplestory-only" + tail), scale);
                            foreach (var menu in Menus) Menu(form.SceneMenuOf(menu[1]), Path.Combine(folder, menu[0] + tail), scale);
                            Toast(ToastTexts.UploadFailed("unreadable"), Path.Combine(folder, "notification-upload-failed" + tail), scale, light);
                            Toast(ToastTexts.Unlinked(), Path.Combine(folder, "notification-link-again" + tail), scale, light);
                        }
                    }
                    // Every page once more in each of the six languages, without a picture: nothing may stick out of the window.
                    using (var scrap = new Bitmap(MainForm.ViewWidth, MainForm.ViewHeight))
                    using (var g = Graphics.FromImage(scrap))
                    {
                        foreach (var language in Texts.Languages)
                        {
                            Texts.Language = language;
                            foreach (var name in Windows)
                            {
                                form.Scene(name);
                                var ui = new Ui(g, Look.Now);
                                form.Draw(ui, MainForm.ViewWidth, MainForm.ViewHeight);
                                Check(ui, name + " (" + language + ")");
                            }
                        }
                        // The menu by the clock in each language, at every screen scale: every line whole, with air at its right.
                        foreach (var language in Texts.Languages)
                        {
                            Texts.Language = language;
                            MenuCheck(form.SceneMenu(true), "two games", language);
                            MenuCheck(form.SceneMenu(false), "MapleStory only", language);
                            MenuCheck(LongestStatusLines(), "every status line", language);
                            RealMenuCheck(LongStateMenu(form), "MapleStory open, Tempo not started", language, null);
                            RealMenuCheck(form.SceneMenu(true), "two games", language, null);
                            foreach (var menu in Menus)
                            {
                                MenuCheck(form.SceneMenuOf(menu[1]), menu[0], language);
                                RealMenuCheck(form.SceneMenuOf(menu[1]), menu[0], language, null);
                            }
                        }
                        Texts.Language = shotsLanguage;
                    }
                }
                FloatSweep();
                var problems = Path.Combine(folder, "shots-problems.txt");
                if (Problems.Count > 0) File.WriteAllLines(problems, Problems.ToArray());
                else if (File.Exists(problems)) File.Delete(problems);
                return Problems.Count > 0 ? 2 : 0;
            }
            catch (Exception e)
            {
                try { File.WriteAllText(Path.Combine(folder, "shots-failed.txt"), e.ToString()); } catch (Exception) { }
                return 1;
            }
        }

        /// <summary>The window's page under a drawn title bar, on a little of the desktop's colour so its edge shows.</summary>
        private static void Window(MainForm form, string file, float scale)
        {
            const int bar = 32, margin = 12;
            var p = Look.Now;
            var w = MainForm.ViewWidth + 2;
            var h = MainForm.ViewHeight + bar + 2;
            using (var bitmap = new Bitmap((int)Math.Round((w + 2 * margin) * scale), (int)Math.Round((h + 2 * margin) * scale), PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Look.IsLight ? Color.FromArgb(0xdd, 0xe1, 0xe8) : Color.FromArgb(0x05, 0x06, 0x08));
                    g.ScaleTransform(scale, scale);
                    g.TranslateTransform(margin, margin);
                    var ui = new Ui(g, p);
                    using (var frame = Look.Rounded(new RectangleF(0.5f, 0.5f, w - 1, h - 1), 8))
                    using (var back = new SolidBrush(p.Background))
                    using (var edge = new Pen(p.Edge, 1f))
                    {
                        g.FillPath(back, frame);
                        g.SetClip(frame);
                        // The title bar, as Windows 11 draws it in this mode.
                        ui.Fill(Look.IsLight ? Color.FromArgb(0xf3, 0xf3, 0xf3) : Color.FromArgb(0x1f, 0x1f, 0x1f), 1, 1, w - 2, bar);
                        using (var mark = new SolidBrush(p.AccentFill)) g.FillEllipse(mark, 11, 9, 14, 14);
                        ui.Line("GameMargins Desktop", ui.Small, p.Foreground, 33, 1, 300, bar, 0);
                        using (var pen = new Pen(p.Foreground, 1f))
                        {
                            g.DrawLine(pen, w - 82, 17, w - 72, 17);
                            g.DrawLine(pen, w - 30, 12, w - 20, 22);
                            g.DrawLine(pen, w - 20, 12, w - 30, 22);
                        }
                        g.TranslateTransform(1, bar + 1);
                        g.SetClip(new RectangleF(0, 0, MainForm.ViewWidth, MainForm.ViewHeight), CombineMode.Intersect);
                        form.Draw(ui, MainForm.ViewWidth, MainForm.ViewHeight);
                        Check(ui, Path.GetFileNameWithoutExtension(file));
                        g.ResetClip();
                        g.TranslateTransform(-1, -(bar + 1));
                        g.DrawPath(edge, frame);
                    }
                }
                bitmap.Save(file, ImageFormat.Png);
            }
        }

        /// <summary>What a picture's page got wrong, for "shots-problems.txt": a button that sticks out of the window, or two parts with one name.</summary>
        private static readonly List<string> Problems = new List<string>();

        private static void Check(Ui ui, string name)
        {
            // What a page said itself does not fit (a row whose control runs into its name).
            Problems.AddRange(MainForm.LayoutProblems);
            MainForm.LayoutProblems.Clear();
            var seen = new HashSet<string>();
            foreach (var hit in ui.Hits)
            {
                if (!seen.Add(hit.Id)) Problems.Add(name + ": two parts are called " + hit.Id);
                if (hit.Box.Left < 0 || hit.Box.Top < 0 || hit.Box.Right > MainForm.ViewWidth || hit.Box.Bottom > MainForm.ViewHeight) Problems.Add(name + ": " + hit.Id + " sticks out of the window (" + hit.Box + ")");
            }
        }

        /// <summary>Every line the menu's two status lines can say (MainForm's TempoStatus and WowStatus), the long ones among them: a menu's width is its longest line's.</summary>
        private static List<TrayLine> LongestStatusLines()
        {
            var texts = new[]
            {
                Texts.T("MapleStory is running · Tempo is not started"),
                Texts.T("Tempo is off: it may not look at the game yet"),
                Texts.T("Tempo is not linked to your account yet"),
                Texts.T("Tempo needs Microsoft's WebView2"),
                Texts.T("Tempo is reading · {0}", "12 h 48 min"),
                Texts.T("Tempo is paused · {0}", "12 h 48 min"),
                Texts.T("Tempo is not started"),
                Texts.T("The site did not open Tempo for this account"),
                Texts.T("Tempo can't look at the game on this Windows"),
                "WoW: " + Texts.T("Not linked to your account yet"),
                "WoW: " + Texts.T("WoW folder not found."),
                "WoW: " + Texts.T("The last upload failed"),
                "WoW: " + Texts.T("No uploads yet"),
                "WoW: " + Texts.T("Uploading…"),
            };
            var lines = new List<TrayLine>();
            foreach (var text in texts) lines.Add(new TrayLine { Kind = "status", Text = text, Tone = "ok", Enabled = true });
            return lines;
        }

        /// <summary>
        /// Draws every line of a menu alone, at each screen scale, and looks at the picture itself: the last inked column
        /// must leave the menu's air at the right free. A line that touches the edge (or lost letters to "…") is a problem,
        /// as a button that leaves the window is (a Dutch line once touched the edge).
        /// </summary>
        private static void MenuCheck(List<TrayLine> lines, string what, string language)
        {
            var p = Look.Now;
            foreach (var scale in new[] { 1f, 1.25f, 1.5f, 1.75f, 2f })
            {
                var width = TrayPaint.Width(lines, scale);
                var w = (int)Math.Round(width * scale);
                var h = (int)Math.Round(TrayPaint.LineHeight * scale);
                foreach (var line in lines)
                {
                    if (line.Kind == "sep") continue;
                    using (var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.Clear(p.MenuFace);
                            g.ScaleTransform(scale, scale);
                            TrayPaint.Line(g, p, line, new RectangleF(0, 0, width, TrayPaint.LineHeight), false);
                        }
                        var face = p.MenuFace.ToArgb();
                        var last = -1;
                        for (var x = w - 1; x >= 0 && last < 0; x--)
                            for (var y = 0; y < h; y++)
                                if (bitmap.GetPixel(x, y).ToArgb() != face) { last = x; break; }
                        var free = (w - 1 - last) / scale;
                        // Letters cut off end in "…" at the box's edge, which leaves only the inset free: both are caught by asking for the air.
                        if (free < TrayPaint.RightInset + TrayPaint.RightAir - 4)
                            Problems.Add("menu (" + what + ", " + language + ", " + scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "x): \"" + line.Text + "\" leaves " + free.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " px at its right, not " + (TrayPaint.RightInset + TrayPaint.RightAir) + ".");
                    }
                }
            }
        }

        /// <summary>The menu with its longest first line: MapleStory is open and Tempo is not started (the state in which a letter was once cut).</summary>
        private static List<TrayLine> LongStateMenu(MainForm form)
        {
            var lines = form.SceneMenu(true);
            foreach (var line in lines)
            {
                if (line.Kind != "status") continue;
                line.Text = Texts.T("MapleStory is running · Tempo is not started");
                break;
            }
            return lines;
        }

        /// <summary>
        /// The menu as Windows lays it out: a real ContextMenuStrip, made and filled as the one by the clock (TrayMenu.Make,
        /// TrayMenu.Fill), its window created but never shown, then printed into a picture. TrayPaint's own drawing cannot
        /// see what Windows' menu does with a line: a Dutch first line once lost half its last
        /// letter at the menu's edge although every word fitted its line's box, because the line's box reached past the
        /// menu's own window. Two things are asked of every line, in six languages at five scales: its box ends inside
        /// the menu's window, and its last inked column leaves the air at the right free. `save`: also keep the picture
        /// (a file and the one scale to draw), for the pictures' folder.
        /// </summary>
        private static void RealMenuCheck(List<TrayLine> lines, string what, string language, KeyValuePair<string, float>? save)
        {
            var p = Look.Now;
            var scales = save.HasValue ? new[] { save.Value.Value } : new[] { 1f, 1.25f, 1.5f, 1.75f, 2f };
            foreach (var scale in scales)
            {
                var kept = lines;
                using (var menu = TrayMenu.Make(delegate { return kept; }))
                {
                    var handle = menu.Handle;
                    GC.KeepAlive(handle);
                    TrayMenu.Fill(menu, lines, scale);
                    menu.PerformLayout();
                    var where = "menu as Windows lays it out (" + what + ", " + language + ", " + scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "x)";
                    var inside = menu.ClientSize.Width - (int)Math.Round(TrayPaint.Pad * scale);
                    foreach (ToolStripItem item in menu.Items)
                        // A line of words keeps inside the menu's padding; a separator may run to the window's own edge, as Windows lays it.
                        if (item.Bounds.Right > (item.Tag is TrayLine ? inside : menu.ClientSize.Width) || item.Bounds.Left < 0)
                            Problems.Add(where + ": the line \"" + item.Text + "\" spans " + item.Bounds.Left + " to " + item.Bounds.Right + " in a menu " + menu.ClientSize.Width + " wide.");
                    if (menu.Width <= 0 || menu.Height <= 0) { Problems.Add(where + ": the menu has no size."); continue; }
                    using (var bitmap = new Bitmap(menu.Width, menu.Height, PixelFormat.Format32bppArgb))
                    {
                        menu.DrawToBitmap(bitmap, new Rectangle(0, 0, menu.Width, menu.Height));
                        var face = p.MenuFace.ToArgb();
                        var edge = p.MenuEdge.ToArgb();
                        var inked = false;
                        foreach (ToolStripItem item in menu.Items)
                        {
                            var line = item.Tag as TrayLine;
                            if (line == null) continue;
                            var last = -1;
                            for (var x = menu.Width - 1; x >= 0 && last < 0; x--)
                                for (var y = Math.Max(0, item.Bounds.Top + 2); y < Math.Min(menu.Height, item.Bounds.Bottom - 2); y++)
                                {
                                    var c = bitmap.GetPixel(x, y).ToArgb();
                                    if (c != face && c != edge) { last = x; break; }
                                }
                            if (last >= 0) inked = true;
                            var free = (menu.Width - 1 - last) / scale;
                            var wanted = TrayPaint.Pad + TrayPaint.RightInset + TrayPaint.RightAir - 4;
                            if (last >= 0 && free < wanted)
                                Problems.Add(where + ": \"" + line.Text + "\" leaves " + free.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " px before the menu's edge, not " + wanted + ".");
                        }
                        // A picture with no ink at all would pass everything: the printing itself must have worked.
                        if (!inked) Problems.Add(where + ": the menu printed empty, so nothing was checked.");
                        if (save.HasValue) bitmap.Save(save.Value.Key, ImageFormat.Png);
                    }
                }
            }
        }

        private static void Menu(List<TrayLine> lines, string file, float scale)
        {
            var size = TrayPaint.Size(lines, scale);
            const int margin = 12;
            using (var bitmap = new Bitmap((int)Math.Round((size.Width + 2 * margin) * scale), (int)Math.Round((size.Height + 2 * margin) * scale), PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Look.IsLight ? Color.FromArgb(0xdd, 0xe1, 0xe8) : Color.FromArgb(0x05, 0x06, 0x08));
                    g.TranslateTransform(margin * scale, margin * scale);
                    TrayPaint.Menu(g, Look.Now, lines, scale);
                }
                bitmap.Save(file, ImageFormat.Png);
            }
        }

        /// <summary>A drawing of a notification as Windows 11 shows it: the program's icon and name, the title, the line.</summary>
        private static void Toast(string[] words, string file, float scale, bool light)
        {
            const int w = 364, h = 92, margin = 12;
            using (var bitmap = new Bitmap((int)Math.Round((w + 2 * margin) * scale), (int)Math.Round((h + 2 * margin) * scale), PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(light ? Color.FromArgb(0xdd, 0xe1, 0xe8) : Color.FromArgb(0x05, 0x06, 0x08));
                    g.ScaleTransform(scale, scale);
                    g.TranslateTransform(margin, margin);
                    var p = Look.Of(light);
                    var ui = new Ui(g, p);
                    // Windows' own colours for a notification, not the program's: it is Windows that draws it.
                    var face = light ? Color.FromArgb(0xf9, 0xf9, 0xf9) : Color.FromArgb(0x2b, 0x2b, 0x2b);
                    var edge = light ? Color.FromArgb(0xd0, 0xd0, 0xd0) : Color.FromArgb(0x45, 0x45, 0x45);
                    var ink = light ? Color.FromArgb(0x1a, 0x1a, 0x1a) : Color.FromArgb(0xf1, 0xf1, 0xf1);
                    var soft = light ? Color.FromArgb(0x5f, 0x5f, 0x5f) : Color.FromArgb(0xb0, 0xb0, 0xb0);
                    ui.Card(0, 0, w, h, face, edge);
                    using (var mark = new SolidBrush(p.AccentFill)) g.FillEllipse(mark, 14, 13, 16, 16);
                    ui.Line("GameMargins Desktop", ui.Small, soft, 38, 11, 250, 20, 0);
                    ui.Line("×", ui.Body, soft, w - 30, 11, 16, 20, 1);
                    ui.Line(words[0], ui.Strong, ink, 14, 40, w - 28, 20, 0);
                    ui.Line(words[1], ui.Body, soft, 14, 60, w - 28, 20, 0);
                }
                bitmap.Save(file, ImageFormat.Png);
            }
        }

        /// <summary>
        /// The large float with the game's own pictures, as the real float shows them. The real float fetches each picture
        /// from the site (FloatImages: the page hands over the addresses, only our own site is asked); a drawing made
        /// without the site reads the same files from a folder named in GM_SHOTS_ART (a copy of the site's MapleStory
        /// pictures, with "mobs" and "items" in it). Without that folder this picture is not made.
        /// </summary>
        private static void RealFloat(string file, float scale, bool light, Size user, string tip, bool locked, bool small)
        {
            var art = Environment.GetEnvironmentVariable("GM_SHOTS_ART");
            if (string.IsNullOrEmpty(art) || !Directory.Exists(art)) return;
            var images = new FloatImages(null, null);
            var v = Sample("reading", 2);
            var mobs = new[] { "mobs/22.png", "mobs/6.png", "mobs/900001.png" };
            // Every drop of each monster, the most pieces first: the first monster with many kinds, so its row wraps.
            var drops = new[]
            {
                new[] { "4000021:28", "4000020:11", "2000000:9", "4003004:6", "4000003:5", "2000001:4", "4010000:3", "4020000:2", "2060000:2", "2040502:1", "1002019:1" },
                new[] { "4000005:2", "4000003:1" },
                new[] { "4031147:6" },
            };
            for (var i = 0; i < 3 && i < v.Monsters.Count; i++)
            {
                v.Monsters[i].Picture = Known(images, art, mobs[i]);
                v.Monsters[i].Drops.Clear();
                foreach (var drop in drops[i])
                {
                    var parts = drop.Split(':');
                    v.Monsters[i].Drops.Add(new FloatHereItem { Icon = Known(images, art, "items/" + parts[0] + ".png"), Count = parts[1], Name = "item" });
                }
            }
            var here = new[] { "items/4000021.png", "items/4000049.png", "items/4031147.png", "items/4000005.png", "items/2000000.png" };
            for (var i = 0; i < here.Length && v.Here != null && i < v.Here.Items.Count; i++) v.Here.Items[i].Icon = Known(images, art, here[i]);
            // One monster folded (the second), the others open.
            var folded = new HashSet<string> { v.Monsters[1].Key };
            var size = FloatPainter.Measure(v, small, user, folded, locked);
            var tipSize = tip != null ? FloatPainter.TipSize(tip) : Size.Empty;
            var extra = tip != null ? 90 : 0;
            using (var bitmap = new Bitmap((int)Math.Round((size.Width + extra) * scale), (int)Math.Round(size.Height * scale), PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    if (tip != null) g.Clear(light ? Color.FromArgb(0xdd, 0xe1, 0xe8) : Color.FromArgb(0x05, 0x06, 0x08));
                    var hits = FloatPainter.Paint(g, v, small, scale, null, 0, images, light, user, locked, folded, Texts.T("locked"));
                    if (tip != null && hits.Tips.Count > 0)
                    {
                        // The label as the pointer resting on the first drop's picture shows it, a little under the picture.
                        var box = hits.Tips[0].Key;
                        // Everything of the float is in its own measure: the scale it is drawn at is the screen's times the float's.
                        scale *= hits.Zoom;
                        g.TranslateTransform(box.Left * scale, (box.Bottom + 4) * scale);
                        FloatPainter.PaintTip(g, tip, scale, light);
                        using (var pen = new Pen(light ? Color.Black : Color.White, 1f)) g.DrawLines(pen, new[] { new PointF((box.Left + 6) * scale, (box.Top + 6) * scale), new PointF((box.Left + 6) * scale, (box.Top + 15) * scale), new PointF((box.Left + 12) * scale, (box.Top + 12) * scale), new PointF((box.Left + 6) * scale, (box.Top + 6) * scale) });
                    }
                }
                bitmap.Save(file, ImageFormat.Png);
            }
        }

        /// <summary>
        /// The float at every width it can be dragged to, without a picture kept: from the usual 280 to 460 pixel by pixel and on
        /// to 760 in steps of 3, at the least height, at 470 and at 800, unlocked and locked, at 100% and 150%. Nothing may be cut or pass
        /// an edge (the head's clock, the numbers' rows, the next level, a monster's name beside its kills, a drop past the
        /// right edge, "Picked up here" with all its items, the buttons or the last row past the bottom); the size
        /// asked for is the size drawn; and at the usual width the dragged float is the usual float, pixel for pixel.
        /// </summary>
        private static void FloatSweep()
        {
            var v = Sample("reading", 2);
            var counts = new[] { "28", "11", "9", "6", "5", "4", "3", "2", "2", "1", "1" };
            v.Monsters[0].Drops.Clear();
            foreach (var count in counts) v.Monsters[0].Drops.Add(new FloatHereItem { Count = count, Name = "item" });
            var folded = new HashSet<string> { v.Monsters[1].Key };
            var seen = new HashSet<string>();
            foreach (var scale in new[] { 1f, 1.5f })
            {
                foreach (var locked in new[] { false, true })
                {
                    for (var w = FloatPainter.LargeWidth; w <= 760; w += (w < 460 ? 1 : 3))
                    {
                        foreach (var tall in new[] { 0, 470, 800 })
                        {
                            var least = FloatPainter.LeastHeight(v, w);
                            var user = new Size(w, Math.Max(least, tall));
                            var size = FloatPainter.Measure(v, false, user, folded, locked);
                            var where = "float " + user.Width + " x " + user.Height + (locked ? " locked" : "") + " at " + (int)(scale * 100) + "%: ";
                            if (size.Width != w) Problems.Add(where + "it is " + size.Width + " wide");
                            if (!locked && size.Height != user.Height) Problems.Add(where + "it is " + size.Height + " tall");
                            if (locked && size.Height >= user.Height) Problems.Add(where + "locked it is not shorter");
                            using (var bitmap = new Bitmap((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale), PixelFormat.Format32bppArgb))
                            using (var g = Graphics.FromImage(bitmap))
                            {
                                var hits = FloatPainter.Paint(g, v, false, scale, null, 0, null, false, user, locked, folded, Texts.T("locked"));
                                // The rule in numbers: with t = (width - 280) / 110, from 0 to 1.5, letters 1 + 0.12 t, a monster 24 + 12 t, an item 14 + 12 t.
                                var grown = Math.Max(0f, Math.Min(1.5f, (w - 280) / 110f));
                                if (Math.Abs(hits.Zoom - (1f + 0.12f * grown)) > 0.001f) Problems.Add(where + "its scale is " + hits.Zoom);
                                if (Math.Abs(hits.MonsterPx - (24f + 12f * grown)) > 0.01f) Problems.Add(where + "a monster's picture is " + hits.MonsterPx);
                                if (Math.Abs(hits.ItemPx - (14f + 12f * grown)) > 0.01f) Problems.Add(where + "an item's picture is " + hits.ItemPx);
                                if (hits.MonstersShown < 1) Problems.Add(where + "no monster shows");
                                if (!locked && (hits.Grip.Right * hits.Zoom > size.Width + 1 || hits.Grip.Bottom * hits.Zoom > size.Height + 1 || hits.Stop.Bottom * hits.Zoom > size.Height)) Problems.Add(where + "a button lies outside it");
                                if (locked && (!hits.Grip.IsEmpty || !hits.Stop.IsEmpty || !hits.Close.IsEmpty || !hits.Size.IsEmpty || hits.Folds.Count > 0)) Problems.Add(where + "something can still be clicked");
                                foreach (var problem in hits.Problems) if (seen.Add(where + problem)) Problems.Add(where + problem);
                            }
                        }
                    }
                    // The dragged float at the usual width and its own height is the float nobody dragged, to the pixel.
                    var auto = FloatPainter.Measure(v, false, Size.Empty, folded, false);
                    using (var a = new Bitmap((int)Math.Round(auto.Width * scale), (int)Math.Round(auto.Height * scale), PixelFormat.Format32bppArgb))
                    using (var b = new Bitmap(a.Width, a.Height, PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(a)) FloatPainter.Paint(g, v, false, scale, null, 0, null, false, Size.Empty, false, folded, "locked");
                        using (var g = Graphics.FromImage(b)) FloatPainter.Paint(g, v, false, scale, null, 0, null, false, auto, false, folded, "locked");
                        var differ = 0;
                        for (var y = 0; y < a.Height; y++) for (var x = 0; x < a.Width; x++) if (a.GetPixel(x, y) != b.GetPixel(x, y)) differ++;
                        if (differ > 0 && !locked) Problems.Add("float at the usual width, " + (int)(scale * 100) + "%: " + differ + " pixels differ from the float nobody dragged");
                    }
                }
            }
            // The small float, locked and not, in every language's word: the head's clock is whole.
            foreach (var language in Texts.Languages)
            {
                var kept = Texts.Language;
                Texts.Language = language;
                foreach (var locked in new[] { false, true })
                {
                    foreach (var small in new[] { true, false })
                    {
                        var size = FloatPainter.Measure(v, small, Size.Empty, folded, locked);
                        using (var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb))
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            var hits = FloatPainter.Paint(g, v, small, 1f, null, 0, null, false, Size.Empty, locked, folded, Texts.T("locked"));
                            foreach (var problem in hits.Problems) Problems.Add((small ? "small" : "large") + " float" + (locked ? " locked" : "") + " (" + language + "): " + problem);
                        }
                    }
                }
                Texts.Language = kept;
            }
        }

        /// <summary>Reads one of the site's pictures from its folder and hands back the name the float asks it by; null when the file is not there.</summary>
        private static string Known(FloatImages images, string art, string relative)
        {
            var path = Path.Combine(art, relative);
            if (!File.Exists(path)) return null;
            var key = "file:" + relative;
            images.Put(key, Image.FromStream(new MemoryStream(File.ReadAllBytes(path))));
            return key;
        }

        private static FloatValues Sample(string state, int monsters)
        {
            var v = new FloatValues();
            v.State = state;
            v.Clock = state == "idle" ? "" : "26:14";
            v.Place = state == "paused" ? "Paused" : state == "idle" ? "Not started" : "Land of Wild Boar";
            var idle = state == "idle";
            v.Rows.Add(new FloatRow { Label = "EXP / hour", Value = idle ? "–" : "8,273" });
            v.Rows.Add(new FloatRow { Label = "Mesos / hour", Value = idle ? "–" : "5,827" });
            v.Rows.Add(new FloatRow { Label = "Kills / hour", Value = idle ? "–" : "201" });
            if (!idle)
            {
                v.NextLabel = "Next level";
                // The level's number is not read from the game (its bar shows EXP and a percentage), so the row says "Next level".
                v.NextLevel = "";
                v.NextValue = "50 h 13 min";
                v.NextIn = "in 50 h 13 min";
                v.NextPercent = 2.81;
            }
            if (monsters > 0)
            {
                v.Monsters.Add(new FloatMonster { Key = "22", Name = "Wild Boar", Kills = "69 kills", DropText = "Wild Boar Tooth × 28" });
                v.Monsters.Add(new FloatMonster { Key = "6", Name = "Stump", Kills = "4 kills", DropText = "Tree Branch × 2" });
                v.Monsters.Add(new FloatMonster { Key = "900001", Name = "Turkey", Kills = "4 kills", DropText = "Pumpkin Piece × 6" });
                // Every drop as a picture and a count (grey squares in a drawing made without the game's pictures).
                foreach (var count in new[] { "28", "3", "2" }) v.Monsters[0].Drops.Add(new FloatHereItem { Count = count, Name = "item" });
                v.Monsters[1].Drops.Add(new FloatHereItem { Count = "2", Name = "item" });
                v.Monsters[2].Drops.Add(new FloatHereItem { Count = "6", Name = "item" });
            }
            if (monsters > 1)
            {
                v.Monsters.Add(new FloatMonster { Key = "0", Name = "Shared kills", Kills = "10 kills" });
                // The row "Picked up here": no pictures in a drawing made without the site, so the counts alone.
                v.Here = new FloatHere { Name = "Picked up here", Note = "not certain", Total = "205 items", More = "+6" };
                foreach (var count in new[] { "66", "54", "49", "28", "2" }) v.Here.Items.Add(new FloatHereItem { Count = count });
            }
            return v;
        }

        private static List<KeyValuePair<string, FloatValues>> Floats()
        {
            var shots = new List<KeyValuePair<string, FloatValues>>();
            shots.Add(new KeyValuePair<string, FloatValues>("reading", Sample("reading", 1)));
            shots.Add(new KeyValuePair<string, FloatValues>("reading-picked-up-here", Sample("reading", 2)));
            shots.Add(new KeyValuePair<string, FloatValues>("reading-first-minute", Sample("reading", 0)));
            shots.Add(new KeyValuePair<string, FloatValues>("paused", Sample("paused", 1)));
            shots.Add(new KeyValuePair<string, FloatValues>("not-started", Sample("idle", 0)));
            var waiting = Sample("waiting", 0);
            waiting.Clock = "";
            waiting.OutTitle = "Waiting";
            waiting.OutNote = "Choose a world and channel. Tempo starts counting when you are in the game.";
            shots.Add(new KeyValuePair<string, FloatValues>("waiting", waiting));
            return shots;
        }
    }
}
