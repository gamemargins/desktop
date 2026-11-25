// Pictures of everything the program draws, made without any window:
//
//   GameMarginsDesktop.exe --shots <folder>
//
// Every screen of the program's window (the first start's steps for one game and for two, Overview, MapleStory
// Classic, WoW: Forever, Settings; a program used for WoW: Forever alone; the page of an account the site refused Tempo,
// and of a Windows that can't look at a window; Tempo reading before Start, paused and without the game), the float (both
// sizes and the bar; waiting, reading, paused, not started; and as a page sends it with totals and the own shop: not
// started, a profit, a loss, eight digits, paused, the shop's question, online and open, and a shop on a map with a long
// name, which goes on to a second line), the menu by the
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
            s.TempoTurnedOff = false;
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
            _tempo.PictureWatching = false;
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

                // ---- Tempo reads whenever MapleStory is open: before Start, during a pause, and without the game.
                case "maplestory-classic-reads-not-started":
                case "overview-reads-not-started":
                    linked();
                    wow();
                    _tempo.SessionState = "idle";
                    _tempo.PictureWatching = true;
                    _page = name.StartsWith("overview", StringComparison.Ordinal) ? "overview" : "maple";
                    break;
                case "maplestory-classic-reads-paused":
                    linked();
                    wow();
                    _tempo.SessionState = "paused";
                    _tempo.SessionSeconds = 12 * 60 + 4;
                    _tempo.PictureWatching = true;
                    _page = "maple";
                    break;
                case "maplestory-classic-no-game":
                    linked();
                    wow();
                    _tempo.SessionState = "idle";
                    _tempo.GameFound = false;
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

                // ---- Looking turned off by the player himself (the owner's pick 3 A of 25 November 2026: "Tempo is off: you
                // turned it off yourself" and "Turn back on"), and after "No, not now" (its words as before).
                case "overview-turned-off":
                case "maplestory-classic-turned-off":
                case "maplestory-classic-not-now":
                    linked();
                    wow();
                    s.TempoConsent = false;
                    s.TempoAsked = true;
                    s.TempoTurnedOff = name.EndsWith("-turned-off", StringComparison.Ordinal);
                    _tempo.SessionState = "idle";
                    _tempo.PictureWatching = false;
                    _page = name.StartsWith("overview", StringComparison.Ordinal) ? "overview" : "maple";
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

        /// <summary>
        /// The menu while Tempo reads MapleStory and the farm session is `session` ("idle", "paused"), or without the game
        /// (`game` false): MapleStory alone, or both games (`both`).
        /// </summary>
        internal List<TrayLine> SceneMenuReads(bool both, string session, bool game)
        {
            Scene(both ? "overview" : "overview-one-game");
            _tempo.SessionState = session;
            _tempo.SessionSeconds = 12 * 60 + 4;
            _tempo.SessionSaid = default(DateTime);
            _tempo.GameFound = game;
            _tempo.PictureWatching = game;
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
            // Tempo reads whenever MapleStory is open: before Start, during a pause, and the game not running.
            "maplestory-classic-reads-not-started", "maplestory-classic-reads-paused", "maplestory-classic-no-game", "overview-reads-not-started",
            // Looking turned off by the player himself, and after "No, not now".
            "overview-turned-off", "maplestory-classic-turned-off", "maplestory-classic-not-now",
        };

        /// <summary>The menu by the clock in the states it has beside "two games" and "MapleStory only": a picture's name and the scene it shows.</summary>
        private static readonly string[][] Menus =
        {
            new[] { "menu-wow-only", "wow-only-overview" },
            new[] { "menu-refused", "overview-refused" },
            new[] { "menu-turned-off", "overview-turned-off" },
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
                            // The bar in every state, all states on one sheet, and the button that goes round with its name card.
                            BarShots(folder, tail, scale, light);
                            // The numbers as totals with their words, and the own shop, in the three sizes.
                            TotalsShots(folder, tail, scale, light);
                            // Windows' own menu, filled and laid out as by the clock (never shown), with the longest first line.
                            RealMenuCheck(LongStateMenu(form), "picture", Texts.Language, new KeyValuePair<string, float>(Path.Combine(folder, "menu-real-long-first-line" + tail), scale));
                            RealMenuCheck(form.SceneMenuReads(false, "idle", true), "picture", Texts.Language, new KeyValuePair<string, float>(Path.Combine(folder, "menu-real-reads-not-started" + tail), scale));
                            Menu(form.SceneMenu(true), Path.Combine(folder, "menu-two-games" + tail), scale);
                            Menu(form.SceneMenu(false), Path.Combine(folder, "menu-maplestory-only" + tail), scale);
                            // Tempo reads MapleStory: the session not started (as drawn), paused, and the game not running; and with WoW too.
                            Menu(form.SceneMenuReads(false, "idle", true), Path.Combine(folder, "menu-reads-not-started" + tail), scale);
                            Menu(form.SceneMenuReads(false, "paused", true), Path.Combine(folder, "menu-reads-paused" + tail), scale);
                            Menu(form.SceneMenuReads(false, "idle", false), Path.Combine(folder, "menu-no-game" + tail), scale);
                            Menu(form.SceneMenuReads(true, "idle", true), Path.Combine(folder, "menu-two-games-reads-not-started" + tail), scale);
                            // Where the menu opens: by the clock on a screen at 100%, and from the hidden icons on a screen at 150%.
                            if (scale == 1f) MenuPlaceShots(form, folder, light);
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
                            // The longer lines of Tempo reading always: "Tempo reads MapleStory · session not started", "… · session paused · 12 min".
                            foreach (var reads in new[] { new { Both = false, Session = "idle", Game = true, What = "Tempo reads, session not started" }, new { Both = true, Session = "idle", Game = true, What = "two games, Tempo reads, session not started" }, new { Both = true, Session = "paused", Game = true, What = "two games, Tempo reads, session paused" }, new { Both = false, Session = "idle", Game = false, What = "MapleStory not running" } })
                            {
                                MenuCheck(form.SceneMenuReads(reads.Both, reads.Session, reads.Game), reads.What, language);
                                RealMenuCheck(form.SceneMenuReads(reads.Both, reads.Session, reads.Game), reads.What, language, null);
                            }
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
                BarSweep();
                TotalsSweep();
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
                Texts.T("Tempo reads MapleStory · session not started"),
                Texts.T("Tempo reads MapleStory · session paused · {0}", "12 h 48 min"),
                Texts.T("MapleStory is not running"),
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

        /// <summary>
        /// Where the menu by the clock opens (TrayMenu.Place, then kept above the taskbar as Windows keeps it), drawn on a
        /// piece of a made-up screen around the pointer, at that screen's own scale: "menu-place-clock-100" (the icon by the
        /// clock on a screen at 100%) and "menu-place-hidden-icons-150" (the icon among the hidden icons, on a second
        /// screen at 150%, the menu as large as that screen draws it). A drawing of the rule, not of Windows: the real menu
        /// is checked against every screen of the computer in the logic checks.
        /// </summary>
        private static void MenuPlaceShots(MainForm form, string folder, bool light)
        {
            var lines = form.SceneMenuReads(false, "idle", true);
            var p = Look.Of(light);
            foreach (var hidden in new[] { false, true })
            {
                var s = hidden ? 1.5f : 1f;
                const int W = 1000, H = 640;
                var bar = (int)Math.Round(48 * s);
                var screen = new Rectangle(0, 0, W, H);
                var work = new Rectangle(0, 0, W, H - bar);
                Point pointer;
                var flyout = Rectangle.Empty;
                var cell = (int)Math.Round(40 * s);
                if (hidden)
                {
                    // The hidden icons above the taskbar's arrow: two rows of four, the program's icon the second of the first row.
                    flyout = new Rectangle(W - (int)Math.Round(330 * s), H - bar - (int)Math.Round(12 * s) - 2 * cell - (int)Math.Round(16 * s), 4 * cell + (int)Math.Round(16 * s), 2 * cell + (int)Math.Round(16 * s));
                    pointer = new Point(flyout.Left + (int)Math.Round(8 * s) + cell + cell / 2, flyout.Top + (int)Math.Round(8 * s) + cell / 2);
                }
                else pointer = new Point(W - (int)Math.Round(150 * s), H - bar / 2);
                var size = TrayPaint.Size(lines, s);
                var menu = new Size((int)Math.Round(size.Width * s), (int)Math.Round(size.Height * s));
                var at = TrayMenu.Place(pointer, menu, screen);
                if (at.Bottom > work.Bottom) at.Y = work.Bottom - at.Height;
                if (!work.Contains(at)) Problems.Add("menu place (" + (hidden ? "hidden icons, 150%" : "clock, 100%") + "): the menu at " + at + " is not inside the screen's work area " + work);
                using (var bitmap = new Bitmap(W, H, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.Clear(light ? Color.FromArgb(0xc9, 0xd3, 0xe0) : Color.FromArgb(0x10, 0x16, 0x24));
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                        using (var taskbar = new SolidBrush(light ? Color.FromArgb(0xee, 0xf1, 0xf5) : Color.FromArgb(0x1c, 0x1c, 0x1c)))
                        using (var ink = new SolidBrush(light ? Color.FromArgb(0x1a, 0x1a, 0x1a) : Color.FromArgb(0xf1, 0xf1, 0xf1)))
                        using (var soft = new SolidBrush(light ? Color.FromArgb(0x5f, 0x5f, 0x5f) : Color.FromArgb(0xa0, 0xa0, 0xa0)))
                        using (var mark = new SolidBrush(p.AccentFill))
                        {
                            // The program's fonts are kept for every drawing: not given back here.
                            var font = Look.Font(12f * s, 0);
                            g.FillRectangle(taskbar, 0, H - bar, W, bar);
                            g.DrawString("14:26", font, ink, W - (int)Math.Round(70 * s), H - bar + bar / 2 - font.Height / 2);
                            // The taskbar's arrow that opens the hidden icons, and the icons left of the clock.
                            g.DrawString("^", font, ink, W - (int)Math.Round(230 * s), H - bar + bar / 2 - font.Height / 2);
                            if (hidden)
                            {
                                using (var face = new SolidBrush(light ? Color.FromArgb(0xf9, 0xf9, 0xf9) : Color.FromArgb(0x2b, 0x2b, 0x2b)))
                                using (var path = Look.Rounded(flyout, 8 * s))
                                    g.FillPath(face, path);
                                for (var i = 0; i < 8; i++)
                                {
                                    var x = flyout.Left + (int)Math.Round(8 * s) + (i % 4) * cell + cell / 2;
                                    var y = flyout.Top + (int)Math.Round(8 * s) + (i / 4) * cell + cell / 2;
                                    var r = (int)Math.Round(8 * s);
                                    g.FillEllipse(i == 1 ? mark : soft, x - r, y - r, 2 * r, 2 * r);
                                }
                            }
                            else
                            {
                                var r = (int)Math.Round(8 * s);
                                g.FillEllipse(mark, pointer.X - r, pointer.Y - r, 2 * r, 2 * r);
                                g.FillEllipse(soft, pointer.X - r - (int)Math.Round(32 * s), pointer.Y - r, 2 * r, 2 * r);
                            }
                            g.DrawString(hidden ? "A second screen at 150% · the icon among the hidden icons" : "A screen at 100% · the icon by the clock", Look.Font(15f, 1), ink, 16, 14);
                            g.DrawString("The menu opens at the pointer, on this screen, at its scale", Look.Font(13f, 0), soft, 16, 40);
                        }
                        var state = g.Save();
                        g.TranslateTransform(at.X, at.Y);
                        TrayPaint.Menu(g, p, lines, s);
                        g.Restore(state);
                        // The pointer: an arrow with its tip at the point the menu was placed for.
                        var a = new[] { new PointF(0, 0), new PointF(0, 17), new PointF(4.5f, 13), new PointF(8, 20), new PointF(10.5f, 19), new PointF(7, 12), new PointF(12.5f, 12) };
                        for (var i = 0; i < a.Length; i++) a[i] = new PointF(pointer.X + a[i].X * s, pointer.Y + a[i].Y * s);
                        using (var white = new SolidBrush(Color.White))
                        using (var black = new Pen(Color.Black, 1.2f * s))
                        {
                            g.FillPolygon(white, a);
                            g.DrawPolygon(black, a);
                        }
                    }
                    bitmap.Save(Path.Combine(folder, (hidden ? "menu-place-hidden-icons-150" : "menu-place-clock-100") + "-" + (light ? "light" : "dark") + ".png"), ImageFormat.Png);
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

        // ------------------------------------------------------------------------------------------------ the bar

        /// <summary>A state of the bar for a picture: its name, what it shows, and whether it is locked.</summary>
        private sealed class BarShot
        {
            public string Name;
            public FloatValues Values;
            public bool Locked;
        }

        /// <summary>Made-up numbers for the bar: the usual ones, and the longest a bar meets (a long map, numbers in the millions, 2:26:14).</summary>
        private static FloatValues BarValues(string state, bool full)
        {
            var v = Sample(state, 0);
            if (state == "reading") v.Place = "Primeval Forest II";
            if (state != "idle") v.NextPercent = 34;
            if (full)
            {
                v.Clock = "2:26:14";
                if (state == "reading") v.Place = "Construction Site North of Kerning City";
                v.Rows[0].Value = "1,284,377";
                v.Rows[1].Value = "2,401,950";
                v.Rows[2].Value = "1,450";
                v.NextValue = "312 h 45 min";
                v.NextIn = "in 312 h 45 min";
                v.NextPercent = 3;
            }
            return v;
        }

        private static List<BarShot> Bars()
        {
            var list = new List<BarShot>();
            Action<string, FloatValues, bool> add = delegate(string name, FloatValues v, bool locked) { list.Add(new BarShot { Name = name, Values = v, Locked = locked }); };
            add("reading", BarValues("reading", false), false);
            add("reading-long", BarValues("reading", true), false);
            add("paused", BarValues("paused", false), false);
            // A not started session whose level is known shows "–" under the next level; without a known level that tile stays empty.
            var idle = BarValues("idle", false);
            idle.NextLabel = "Next level";
            idle.NextValue = "–";
            idle.NextIn = "–";
            add("not-started", idle, false);
            add("not-started-no-level", BarValues("idle", false), false);
            var waiting = BarValues("waiting", false);
            waiting.Clock = "";
            waiting.NextLabel = null;
            waiting.OutTitle = "Waiting";
            waiting.OutNote = "Choosing a world and channel. Tempo starts counting when you’re in the game.";
            add("waiting", waiting, false);
            add("locked", BarValues("reading", false), true);
            add("locked-long", BarValues("reading", true), true);
            add("paused-long", BarValues("paused", true), false);
            var first = BarValues("reading", false);
            first.Clock = "";
            add("reading-first-minute", first, false);
            var firstLong = BarValues("reading", true);
            firstLong.Clock = "";
            add("reading-first-minute-long-map", firstLong, false);
            return list;
        }

        /// <summary>
        /// The bar's pictures: every state at its own width ("bar-&lt;state&gt;"); all on one sheet ("bar-states":
        /// reading, the long map with large numbers, paused, not started, waiting, locked, each at least as wide as the
        /// usual reading one, as the bar keeps its width while it shows); and the button that goes round, pointed at, with its
        /// name card in each size ("cycle-small", "cycle-large", "cycle-bar") and four times as large ("cycle-button-zoom").
        /// </summary>
        private static void BarShots(string folder, string tail, float scale, bool light)
        {
            var locked = Texts.T("locked");
            var bars = Bars();
            foreach (var bar in bars)
            {
                var size = FloatPainter.MeasureBar(bar.Values, bar.Locked, locked, scale);
                using (var bitmap = new Bitmap((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale), PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bitmap)) FloatPainter.PaintBar(g, bar.Values, scale, null, light, bar.Locked, locked, size.Width);
                    bitmap.Save(Path.Combine(folder, "bar-" + bar.Name + tail), ImageFormat.Png);
                }
            }
            // The sheet: six states, one under the other, each at least as wide as the usual one.
            var sheet = new[] { "reading", "reading-long", "paused", "not-started", "waiting", "locked" };
            var least = FloatPainter.MeasureBar(bars[0].Values, false, locked, scale).Width;
            var widths = new List<int>();
            var widest = 0;
            foreach (var name in sheet)
            {
                var bar = bars.Find(delegate(BarShot b) { return b.Name == name; });
                var width = Math.Max(least, FloatPainter.MeasureBar(bar.Values, bar.Locked, locked, scale).Width);
                widths.Add(width);
                widest = Math.Max(widest, width);
            }
            const int pad = 16, gap = 14;
            using (var bitmap = new Bitmap((int)Math.Round((widest + 2 * pad) * scale), (int)Math.Round((pad * 2 + sheet.Length * FloatPainter.BarHeight + (sheet.Length - 1) * gap) * scale), PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(light ? Color.FromArgb(0xdd, 0xe1, 0xe8) : Color.FromArgb(0x05, 0x06, 0x08));
                    for (var i = 0; i < sheet.Length; i++)
                    {
                        var bar = bars.Find(delegate(BarShot b) { return b.Name == sheet[i]; });
                        var state = g.Save();
                        g.TranslateTransform((float)Math.Round(pad * scale), (float)Math.Round((pad + i * (FloatPainter.BarHeight + gap)) * scale));
                        FloatPainter.PaintBar(g, bar.Values, scale, null, light, bar.Locked, locked, widths[i]);
                        g.Restore(state);
                    }
                }
                bitmap.Save(Path.Combine(folder, "bar-states" + tail), ImageFormat.Png);
            }
            // The button that goes round, pointed at, with the name of where a click goes, under it as FloatTip shows it.
            var usual = BarValues("reading", false);
            CycleShot(Path.Combine(folder, "cycle-small" + tail), usual, "small", scale, light);
            CycleShot(Path.Combine(folder, "cycle-large" + tail), usual, "large", scale, light);
            CycleShot(Path.Combine(folder, "cycle-bar" + tail), usual, "bar", scale, light);
            var clock = BarValues("reading", true);
            CycleShot(Path.Combine(folder, "cycle-small-long-clock" + tail), clock, "small", scale, light);
            CycleShot(Path.Combine(folder, "cycle-small-paused" + tail), BarValues("paused", false), "small", scale, light);
            if (scale == 1f) CycleZoom(Path.Combine(folder, "cycle-button-zoom" + tail), light);
        }

        /// <summary>The float or the bar in `size` with its button that goes round under the pointer, and the name card a moment later.</summary>
        private static void CycleShot(string file, FloatValues v, string size, float scale, bool light)
        {
            var bar = size == "bar";
            var locked = Texts.T("locked");
            var measured = bar ? FloatPainter.MeasureBar(v, false, locked, scale) : FloatPainter.Measure(v, size == "small", Size.Empty);
            var tipText = v.LabelOf(FloatPainter.NextSize(size));
            var tip = FloatPainter.TipSize(tipText);
            var extraWidth = 40;
            var extraHeight = bar ? 34 : 0;
            using (var bitmap = new Bitmap((int)Math.Round((measured.Width + extraWidth) * scale), (int)Math.Round((measured.Height + extraHeight) * scale), PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(light ? Color.FromArgb(0xdd, 0xe1, 0xe8) : Color.FromArgb(0x05, 0x06, 0x08));
                    var state = g.Save();
                    var hits = bar ? FloatPainter.PaintBar(g, v, scale, "size", light, false, locked, measured.Width) : FloatPainter.Paint(g, v, size == "small", scale, "size", 0, null, light, Size.Empty, false, null, locked);
                    g.Restore(state);
                    foreach (var problem in hits.Problems) Problems.Add("button that goes round, " + size + ": " + problem);
                    var box = hits.Size;
                    g.TranslateTransform((float)Math.Round(box.Left * scale), (float)Math.Round((box.Bottom + 4) * scale));
                    FloatPainter.PaintTip(g, tipText, scale, light);
                }
                bitmap.Save(file, ImageFormat.Png);
            }
        }

        /// <summary>The button that goes round, four times as large, in each of the three sizes.</summary>
        private static void CycleZoom(string file, bool light)
        {
            const float zoom = 4f;
            var t = Look.Of(light);
            var sizes = new[] { "small", "large", "bar" };
            const float cell = FloatPainter.CycleWidth + 4, gap = 10;
            using (var bitmap = new Bitmap((int)Math.Round((sizes.Length * cell + (sizes.Length + 1) * gap) * zoom), (int)Math.Round((22 + 4 + 2 * gap) * zoom), PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(t.FloatFace);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.ScaleTransform(zoom, zoom);
                    for (var i = 0; i < sizes.Length; i++) FloatPainter.CycleIcon(g, new RectangleF(gap + i * (cell + gap) + 2, gap + 2, FloatPainter.CycleWidth, 22), sizes[i], t);
                }
                bitmap.Save(file, ImageFormat.Png);
            }
        }

        /// <summary>
        /// The bar in every state, in each language's word for "locked", at the screen scales Windows offers most (100% to
        /// 200%), locked and not, at its own width and wider (room before the buttons): nothing cut, nothing past its edge,
        /// nothing to click while locked, the buttons inside it and apart. And the heads of the small and the large float
        /// with the wider button that goes round, beside a long clock ("2:26:14") and beside "26:14" with "Paused".
        /// </summary>
        private static void BarSweep()
        {
            var kept = Texts.Language;
            var seen = new HashSet<string>();
            Action<string> problem = delegate(string text) { if (seen.Add(text)) Problems.Add(text); };
            foreach (var language in Texts.Languages)
            {
                Texts.Language = language;
                var word = Texts.T("locked");
                foreach (var scale in new[] { 1f, 1.25f, 1.5f, 1.75f, 2f })
                {
                    foreach (var bar in Bars())
                    {
                        foreach (var locked in new[] { false, true })
                        {
                            var size = FloatPainter.MeasureBar(bar.Values, locked, word, scale);
                            foreach (var more in new[] { 0, 80 })
                            {
                                var width = size.Width + more;
                                var where = "bar " + bar.Name + (locked ? " locked" : "") + (more > 0 ? " widened" : "") + " (" + language + ") at " + (int)(scale * 100) + "%: ";
                                if (size.Height != FloatPainter.BarHeight) problem(where + "it is " + size.Height + " tall");
                                using (var bitmap = new Bitmap((int)Math.Round(width * scale), (int)Math.Round(size.Height * scale), PixelFormat.Format32bppArgb))
                                using (var g = Graphics.FromImage(bitmap))
                                {
                                    var hits = FloatPainter.PaintBar(g, bar.Values, scale, null, false, locked, word, width);
                                    foreach (var p in hits.Problems) problem(where + p);
                                    var buttons = new[] { hits.Size, hits.Close, hits.Stop, hits.Start, hits.Reset };
                                    if (locked)
                                    {
                                        foreach (var b in buttons) if (!b.IsEmpty) problem(where + "something can still be clicked");
                                        continue;
                                    }
                                    if (hits.Size.IsEmpty || hits.Close.IsEmpty) problem(where + "the button that goes round or the cross is missing");
                                    for (var i = 0; i < buttons.Length; i++)
                                    {
                                        var b = buttons[i];
                                        if (b.IsEmpty) continue;
                                        if (b.Left < 0 || b.Top < 0 || b.Right > width + 0.01f || b.Bottom > FloatPainter.BarHeight + 0.01f) problem(where + "a button lies outside it");
                                        for (var j = i + 1; j < buttons.Length; j++) if (!buttons[j].IsEmpty && RectangleF.Intersect(b, buttons[j]).Width > 0.01f) problem(where + "two buttons lie over each other");
                                    }
                                    if (Math.Abs(hits.Close.Right - (width - 6)) > 0.01f) problem(where + "the cross is not at the right end");
                                    var rounds = bar.Values.Out ? 0 : bar.Values.State == "paused" ? 2 : 1;
                                    var found = (hits.Stop.IsEmpty ? 0 : 1) + (hits.Start.IsEmpty ? 0 : 1) + (hits.Reset.IsEmpty ? 0 : 1);
                                    if (found != rounds) problem(where + found + " round buttons, not " + rounds);
                                }
                            }
                        }
                    }
                    // The heads of the small and the large float beside the wider button: a long clock, and "26:14" with "Paused".
                    foreach (var v in new[] { BarValues("reading", true), BarValues("paused", false), BarValues("paused", true), BarValues("idle", false) })
                    {
                        foreach (var small in new[] { true, false })
                        {
                            foreach (var locked in new[] { false, true })
                            {
                                var size = FloatPainter.Measure(v, small, Size.Empty, null, locked);
                                using (var bitmap = new Bitmap((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale), PixelFormat.Format32bppArgb))
                                using (var g = Graphics.FromImage(bitmap))
                                {
                                    var hits = FloatPainter.Paint(g, v, small, scale, "size", 0, null, false, Size.Empty, locked, null, word);
                                    var where = (small ? "small" : "large") + " float, " + v.State + " " + v.Clock + (locked ? " locked" : "") + " (" + language + ") at " + (int)(scale * 100) + "%: ";
                                    foreach (var p in hits.Problems) problem(where + p);
                                    if (!locked && (hits.Size.Width != FloatPainter.CycleWidth || hits.Size.Right > hits.Close.Left + 0.01f || hits.Close.Right > size.Width)) problem(where + "the head's buttons do not fit");
                                    // A paused float's head says "Paused" beside the clock, or under it where it does not fit: whole, never left out.
                                    if (!locked && v.State == "paused" && hits.HeadPlace != "beside" && hits.HeadPlace != "under") problem(where + "\"" + v.Place + "\" is not in the head");
                                }
                            }
                        }
                    }
                }
            }
            Texts.Language = kept;
        }

        // ------------------------------------------------------------------------------------------------ the totals and the own shop

        private const string LongMap = "Victoria Road : Construction Site North of Kerning City";

        private static FloatNumber Number(string total, string unit, string rate, string tone)
        {
            return new FloatNumber { Total = total, Unit = unit, Rate = rate, Tone = tone };
        }

        /// <summary>
        /// The float as a page sends it with `totals`: "not-started" (dashes, the level's bar known), "positive" (26:14 with a
        /// profit), "negative" (26:14 with a loss), "big" (4:12:37: seven and eight digits, a loss of seven), "paused";
        /// "shop-ask", "shop-online" and "shop-open" are "not-started" with the own shop. With `art` (GM_SHOTS_ART) the
        /// monsters and their drops have the game's own pictures, else grey squares.
        /// </summary>
        private static FloatValues TotalsValues(string set, FloatImages images, string art)
        {
            // "nl-…": the same set as a page in Dutch sends it (the program opens the page in its own language).
            if (set.StartsWith("nl-", StringComparison.Ordinal)) return Dutch(TotalsValues(set.Substring(3), images, art));
            // "map-long", "map-middle", "map-short" (and "-first-minute", before the clock runs): a profit while reading on
            // a map with that name, as the page sends it ("Victoria Road : …"); the head shows the map's own name, beside
            // the clock where it fits, else under it, never cut.
            if (set.StartsWith("map-", StringComparison.Ordinal))
            {
                var first = set.EndsWith("-first-minute", StringComparison.Ordinal);
                var which = set.Substring(4).Replace("-first-minute", "");
                var m = TotalsValues("positive", images, art);
                m.Place = which == "long" ? LongMap : which == "middle" ? MiddleMap : ShortMap;
                if (first) m.Clock = "";
                return m;
            }
            var shop = set.StartsWith("shop-", StringComparison.Ordinal) ? set.Substring(5) : null;
            var idle = set == "not-started" || shop != null;
            var big = set == "big";
            var v = new FloatValues();
            v.State = idle ? "idle" : set == "paused" ? "paused" : "reading";
            v.SessionNo = 1;
            v.Clock = idle ? "" : big ? "4:12:37" : "26:14";
            v.Place = idle ? "Not started" : v.State == "paused" ? "Paused" : LongMap;
            // What a site sends for 0.4.0 and 0.4.1 beside the totals; not drawn when the totals are there.
            v.Rows.Add(new FloatRow { Label = "EXP / hour", Value = idle ? "–" : "48,212" });
            v.Rows.Add(new FloatRow { Label = "Mesos / hour", Value = idle ? "–" : "31,870" });
            v.Rows.Add(new FloatRow { Label = "Kills / hour", Value = idle ? "–" : "1,450" });
            v.NextLabel = "Next level";
            v.NextLevel = "";
            v.NextValue = idle ? "–" : big ? "38 min" : "2 h 46 min";
            v.NextIn = idle ? "–" : "in " + v.NextValue;
            v.NextPercent = big ? 91.2 : 63.47;
            if (idle) v.Totals = new[] { Number("–", "EXP", "", null), Number("–", "profit", "", null), Number("–", "mesos", "", null), Number("–", "", "to next level", null) };
            else if (big) v.Totals = new[] { Number("1,284,377", "EXP", "305,118 / hour", null), Number("−1,204,550", "profit", "−286,160 / hour", "negative"), Number("12,480,300", "mesos", "2,964,712 / hour", null), Number("38 min", "", "to next level", null) };
            else v.Totals = new[] { Number("21,083", "EXP", "48,212 / hour", null), set == "negative" ? Number("−5,450", "profit", "−12,480 / hour", "negative") : Number("+3,640", "profit", "+8,330 / hour", "positive"), Number("13,920", "mesos", "31,870 / hour", null), Number("2 h 46 min", "", "to next level", null) };
            v.Stop = "Pause";
            v.Start = v.State == "paused" ? "Continue" : "Start";
            v.Reset = "Reset";
            if (!idle)
            {
                // The drawings' three monsters with their drops.
                var mobs = new[] { new[] { "18", "Jr. Necki", big ? "3,340 kills" : "151 kills" }, new[] { "17", "Stirge", big ? "2,036 kills" : "92 kills" }, new[] { "32", "Jr. Wraith", big ? "1,042 kills" : "47 kills" } };
                var drops = big
                    ? new[] { new[] { "4000018:1,352", "2000001:88", "4020000:21", "4010002:9" }, new[] { "4000017:688", "2000003:41" }, new[] { "4000028:402", "4010000:19" } }
                    : new[] { new[] { "4000018:61", "2000001:4", "4020000:1" }, new[] { "4000017:31", "2000003:2" }, new[] { "4000028:19", "4010000:1" } };
                for (var i = 0; i < mobs.Length; i++)
                {
                    var m = new FloatMonster { Key = mobs[i][0], Name = mobs[i][1], Kills = mobs[i][2], Picture = art != null ? Known(images, art, "mobs/" + mobs[i][0] + ".png") : null };
                    foreach (var drop in drops[i])
                    {
                        var parts = drop.Split(':');
                        m.Drops.Add(new FloatHereItem { Icon = art != null ? Known(images, art, "items/" + parts[0] + ".png") : null, Count = parts[1], Name = "item" });
                    }
                    v.Monsters.Add(m);
                }
            }
            if (shop == "ask") v.Shop = new FloatShop { State = "ask", Title = "Put your shop on the Market?", Short = "Put your shop on the Market?", Note = "16 items · Channel 12 · Free Market <7>", Always = "Always", NotNow = "Not now" };
            else if (shop == "online") v.Shop = new FloatShop { State = "online", Title = "Your shop is online · 16 offers", Short = "Shop online · 16 offers", Note = "Channel 12 · Free Market <7>" };
            else if (shop == "open") v.Shop = new FloatShop { State = "open", Title = "Your shop is open · not online", Short = "Shop not online", Note = "Pick the channel or room in Tempo’s window" };
            // A shop outside the Free Market, on a map with a long name (once cut as "Channel 14 · Victoria Road: The Hill East of
            // H…"): the line goes on to a second one.
            else if (shop == "ask-long-map") v.Shop = new FloatShop { State = "ask", Title = "Put your shop on the Market?", Short = "Put your shop on the Market?", Note = "16 items · " + ShopMap, Always = "Always", NotNow = "Not now" };
            else if (shop == "online-long-map") v.Shop = new FloatShop { State = "online", Title = "Your shop is online · 16 offers", Short = "Shop online · 16 offers", Note = "Channel 14 · " + LongMap };
            // The own shop's window closed in the game: the page says so at once and takes it off the Market some 20 seconds later.
            else if (shop == "closing") v.Shop = new FloatShop { State = "open", Title = "Shop closed · going offline", Short = "Shop closed · going offline", Note = "" };
            // A question longer than the small float is wide (a language with longer words): on two lines, the buttons under them.
            // A shop that waits for the channel (the page's `wait`, pick 4 B of 25 November 2026): another player's shop Tempo
            // could not send, and the own shop; an orange dot.
            else if (shop == "wait-spotted") v.Shop = new FloatShop { State = "wait", Title = "Spotted, not on the Market", Short = "Spotted · channel unknown", Note = "Open Menu › Change Channel in the game: then the shops you open go to the Market." };
            else if (shop == "wait-own") v.Shop = new FloatShop { State = "wait", Title = "Your shop waits for your channel", Short = "Shop waits for channel", Note = "Open Menu › Change Channel in the game: then it goes online by itself." };
            else if (shop == "ask-long-question") v.Shop = new FloatShop { State = "ask", Title = "Put your own shop on the GameMargins Market now?", Short = "Put your own shop on the GameMargins Market now?", Note = "16 items · " + ShopMap, Always = "Always", NotNow = "Not now" };
            return v;
        }

        /// <summary>A map of middle length and a short one, as the page names them.</summary>
        private const string MiddleMap = "Victoria Road : The Hill East of Henesys", ShortMap = "Primeval Forest II";

        /// <summary>The map of a shop on Victoria Road, whose name the float once cut.</summary>
        private const string ShopMap = "Channel 14 · Victoria Road: The Hill East of Henesys";

        /// <summary>
        /// A set as a page in Dutch would send it: the words a Dutch page writes and Dutch numbers ("21.083"), so the float
        /// is seen with the longer words of another language. Made up here, for the pictures: the page's own words are the
        /// site's (its translations), only "– / uur" under a number without a rate and "vergrendeld" are the program's.
        /// </summary>
        private static FloatValues Dutch(FloatValues v)
        {
            Func<string, string> nl = delegate(string t)
            {
                if (string.IsNullOrEmpty(t)) return t;
                t = t.Replace(",", ".").Replace(" / hour", " / uur").Replace("to next level", "tot volgend level").Replace(" h ", " u ");
                switch (t)
                {
                    case "Not started": return "Niet gestart";
                    case "Paused": return "Gepauzeerd";
                    case "profit": return "winst";
                    case "Put your shop on the Market?": return "Je winkel op de Markt zetten?";
                    case "Your shop is online · 16 offers": return "Je winkel staat online · 16 aanbiedingen";
                    case "Shop online · 16 offers": return "Winkel online · 16 aanbiedingen";
                    case "Your shop is open · not online": return "Je winkel is open · niet online";
                    case "Shop not online": return "Winkel niet online";
                    case "Pick the channel or room in Tempo’s window": return "Kies het kanaal of de kamer in het venster van Tempo";
                    case "Shop closed · going offline": return "Winkel dicht · gaat offline";
                    // (The comma is a point here already: the numbers' commas became points above.)
                    case "Spotted. not on the Market": return "Gespot, niet op de Markt";
                    case "Spotted · channel unknown": return "Gespot · kanaal onbekend";
                    case "Open Menu › Change Channel in the game: then the shops you open go to the Market.": return "Open in het spel Menu › Change Channel: dan gaan de winkels die je opent naar de Markt.";
                    case "Your shop waits for your channel": return "Je winkel wacht op je kanaal";
                    case "Shop waits for channel": return "Winkel wacht op kanaal";
                    case "Open Menu › Change Channel in the game: then it goes online by itself.": return "Open in het spel Menu › Change Channel: dan gaat hij vanzelf online.";
                }
                return t.Replace("Channel ", "Kanaal ").Replace(" items", " spullen");
            };
            v.Place = nl(v.Place);
            foreach (var n in v.Totals)
            {
                n.Total = nl(n.Total);
                n.Unit = nl(n.Unit);
                n.Rate = nl(n.Rate);
            }
            foreach (var m in v.Monsters) m.Kills = nl(m.Kills).Replace("kills", "kills");
            v.Stop = "Pauzeren";
            v.Start = v.State == "paused" ? "Doorgaan" : "Start";
            v.Reset = "Opnieuw";
            v.SmallLabel = "Klein";
            v.LargeLabel = "Groot";
            v.BarLabel = "Balk";
            v.CloseLabel = "Sluiten";
            if (v.Shop != null)
            {
                v.Shop.Title = nl(v.Shop.Title);
                v.Shop.Short = nl(v.Shop.Short);
                v.Shop.Note = nl(v.Shop.Note);
                if (v.Shop.Always != null)
                {
                    v.Shop.Always = "Altijd";
                    v.Shop.NotNow = "Nu niet";
                }
            }
            return v;
        }

        private static readonly string[] TotalsSets =
        {
            "not-started", "positive", "negative", "big", "paused", "shop-ask", "shop-online", "shop-open", "shop-ask-long-map", "shop-online-long-map", "shop-ask-long-question", "shop-closing",
            // A shop that waits for the channel: spotted, and the own one (pick 4 B of 25 November 2026).
            "shop-wait-spotted", "shop-wait-own",
            // The map's own name in the head: long, middle and short, with the clock and before it runs.
            "map-long", "map-middle", "map-short", "map-long-first-minute", "map-middle-first-minute", "map-short-first-minute",
            // The float in Dutch, as a page in Dutch sends it.
            "nl-map-long", "nl-map-middle", "nl-map-short", "nl-map-long-first-minute",
            "nl-not-started", "nl-positive", "nl-big", "nl-paused", "nl-shop-ask-long-map", "nl-shop-online-long-map", "nl-shop-open", "nl-shop-closing",
            "nl-shop-wait-spotted", "nl-shop-wait-own",
        };

        /// <summary>
        /// The float with totals in its three sizes for every set ("float-small-totals-positive", "float-large-totals-…",
        /// "bar-totals-…"), and the bar with the own shop online beside a running session ("bar-totals-positive-shop-online").
        /// </summary>
        private static void TotalsShots(string folder, string tail, float scale, bool light)
        {
            var art = Environment.GetEnvironmentVariable("GM_SHOTS_ART");
            if (string.IsNullOrEmpty(art) || !Directory.Exists(art)) art = null;
            var kept = Texts.Language;
            foreach (var set in TotalsSets)
            {
                // A Dutch page goes with a program in Dutch: the program's own words ("– / uur", "vergrendeld") too.
                Texts.Language = set.StartsWith("nl-", StringComparison.Ordinal) ? "nl" : kept;
                var word = Texts.T("locked");
                var images = new FloatImages(null, null);
                var v = TotalsValues(set, images, art);
                foreach (var small in new[] { true, false })
                {
                    var size = FloatPainter.Measure(v, small, Size.Empty);
                    using (var bitmap = new Bitmap((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale), PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            var hits = FloatPainter.Paint(g, v, small, scale, null, 0, images, light, Size.Empty, false, null, word);
                            foreach (var p in hits.Problems) Problems.Add((small ? "small" : "large") + " float with totals, " + set + ": " + p);
                        }
                        bitmap.Save(Path.Combine(folder, "float-" + (small ? "small" : "large") + "-totals-" + set + tail), ImageFormat.Png);
                    }
                }
                TotalsBarShot(Path.Combine(folder, "bar-totals-" + set + tail), v, scale, light, word);
            }
            Texts.Language = kept;
            var word2 = Texts.T("locked");
            var withShop = TotalsValues("positive", new FloatImages(null, null), null);
            withShop.Shop = TotalsValues("shop-online", new FloatImages(null, null), null).Shop;
            TotalsBarShot(Path.Combine(folder, "bar-totals-positive-shop-online" + tail), withShop, scale, light, word2);
        }

        /// <summary>
        /// What the head must show of the map (the "map-…" sets): its own name, without the region before " : ", whole. In
        /// the large float at its usual width (unlocked) the long name stands under the clock, the others beside it; the
        /// small float shows none while the clock runs, and the map's own name in the clock's place before it runs.
        /// </summary>
        private static void MapInHead(string set, FloatValues v, bool small, Size user, FloatHits hits, string name, Action<string> problem)
        {
            var at = set.IndexOf("map-", StringComparison.Ordinal);
            if (at < 0) return;
            var first = set.EndsWith("-first-minute", StringComparison.Ordinal);
            var own = FloatPainter.OwnMapName(v.Place);
            var bar = name.EndsWith("(bar) ", StringComparison.Ordinal);
            if (small && !bar && !first)
            {
                if (hits.HeadMap != null) problem(name + "the small float shows a map beside its clock");
                return;
            }
            if (hits.HeadMap != own) problem(name + "the head shows \"" + hits.HeadMap + "\", not the map's own name \"" + own + "\"");
            if (bar || !user.IsEmpty || small || hits.Close.IsEmpty) return;
            var which = set.Substring(at + 4).Replace("-first-minute", "");
            if (!first && which == "long" && hits.HeadPlace != "under") problem(name + "the long name is not under the clock (" + hits.HeadPlace + ")");
            if (!first && which != "long" && hits.HeadPlace != "beside") problem(name + "the " + which + " name is not beside the clock (" + hits.HeadPlace + ")");
        }

        private static void TotalsBarShot(string file, FloatValues v, float scale, bool light, string word)
        {
            var size = FloatPainter.MeasureBar(v, false, word, scale);
            using (var bitmap = new Bitmap((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale), PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    var hits = FloatPainter.PaintBar(g, v, scale, null, light, false, word, size.Width);
                    foreach (var p in hits.Problems) Problems.Add("bar with totals, " + Path.GetFileNameWithoutExtension(file) + ": " + p);
                }
                bitmap.Save(file, ImageFormat.Png);
            }
        }

        /// <summary>
        /// The float with totals in every set, in six languages (the word for "locked"), at the scales Windows offers most
        /// (100% to 200%), locked and not: small, large (and dragged wider and taller) and the bar (at its width and wider).
        /// Nothing may be cut or overlap: a total and its word fit their cell at 13.5, 11.5 or 10.5 (shrinking is fine,
        /// cutting is not), the rate at 10.5 or 9.5, the shop's lines fit, its buttons lie inside the float and apart from
        /// each other and from the round buttons, and the small size's cells keep clear of its round button.
        /// </summary>
        private static void TotalsSweep()
        {
            var kept = Texts.Language;
            var seen = new HashSet<string>();
            Action<string> problem = delegate(string text) { if (seen.Add(text)) Problems.Add(text); };
            foreach (var language in Texts.Languages)
            {
                Texts.Language = language;
                var word = Texts.T("locked");
                foreach (var scale in new[] { 1f, 1.25f, 1.5f, 1.75f, 2f })
                {
                    foreach (var set in TotalsSets)
                    {
                        var v = TotalsValues(set, null, null);
                        foreach (var locked in new[] { false, true })
                        {
                            var where = set + (locked ? " locked" : "") + " (" + language + ") at " + (int)(scale * 100) + "%: ";
                            foreach (var small in new[] { true, false })
                            {
                                foreach (var user in small ? new[] { Size.Empty } : new[] { Size.Empty, new Size(340, 520), new Size(445, 620) })
                                {
                                    var size = FloatPainter.Measure(v, small, user, null, locked);
                                    var name = (small ? "small" : "large") + " float with totals" + (user.IsEmpty ? "" : " " + user.Width + "x" + user.Height) + ", " + where;
                                    if (small && size.Width != FloatPainter.SmallTotalsWidth) problem(name + "it is " + size.Width + " wide, not " + FloatPainter.SmallTotalsWidth);
                                    using (var bitmap = new Bitmap((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale), PixelFormat.Format32bppArgb))
                                    using (var g = Graphics.FromImage(bitmap))
                                    {
                                        var hits = FloatPainter.Paint(g, v, small, scale, null, 0, null, false, user, locked, null, word);
                                        // The small size with totals is 114 tall, and taller only by its own shop and by a head of more than one line.
                                        if (small && v.Shop == null && size.Height != FloatPainter.SmallTotalsHeight + (int)Math.Ceiling(hits.HeadExtra)) problem(name + "it is " + size.Height + " tall, not " + FloatPainter.SmallTotalsHeight + " and its head's " + hits.HeadExtra);
                                        foreach (var p in hits.Problems) problem(name + p);
                                        var z = hits.Zoom;
                                        var inside = new RectangleF(0, 0, size.Width / z + 0.01f, size.Height / z + 0.01f);
                                        var buttons = new[] { hits.Stop, hits.Start, hits.Reset, hits.ShopAlways, hits.ShopNotNow, hits.Size, hits.Close };
                                        for (var i = 0; i < buttons.Length; i++)
                                        {
                                            if (buttons[i].IsEmpty) continue;
                                            if (!inside.Contains(buttons[i])) problem(name + "a button lies outside it");
                                            for (var j = i + 1; j < buttons.Length; j++) if (!buttons[j].IsEmpty && RectangleF.Intersect(buttons[i], buttons[j]).Width > 0.01f && RectangleF.Intersect(buttons[i], buttons[j]).Height > 0.01f) problem(name + "two buttons lie over each other");
                                        }
                                        if (locked && (!hits.ShopAlways.IsEmpty || !hits.Stop.IsEmpty || !hits.Start.IsEmpty)) problem(name + "something can still be clicked");
                                        if (!locked && v.Shop != null && v.Shop.Asks && (hits.ShopAlways.IsEmpty || hits.ShopNotNow.IsEmpty)) problem(name + "the shop's question has no buttons");
                                        // The long map's name of the owner's test goes on to a second line at the usual width, whole.
                                        if (!small && user.IsEmpty && set.EndsWith("-long-map", StringComparison.Ordinal) && hits.Wrapped == 0) problem(name + "the long map's name stands on one line");
                                        if (small && set == "shop-ask-long-question" && hits.Wrapped == 0) problem(name + "the long question stands on one line");
                                        MapInHead(set, v, small, user, hits, name, problem);
                                        // The shop's buttons stand under its last line, inside the float.
                                        if (!locked && !hits.ShopAlways.IsEmpty && hits.ShopAlways.Bottom > size.Height / z - (small ? 0 : 28 + 10) + 0.01f) problem(name + "the shop's buttons pass the bottom");
                                    }
                                }
                            }
                            var bar = FloatPainter.MeasureBar(v, locked, word, scale);
                            foreach (var more in new[] { 0, 80 })
                            {
                                var name = "bar with totals" + (more > 0 ? " widened" : "") + ", " + where;
                                if (bar.Height != FloatPainter.BarHeight) problem(name + "it is " + bar.Height + " tall");
                                using (var bitmap = new Bitmap((int)Math.Round((bar.Width + more) * scale), (int)Math.Round(bar.Height * scale), PixelFormat.Format32bppArgb))
                                using (var g = Graphics.FromImage(bitmap))
                                {
                                    var hits = FloatPainter.PaintBar(g, v, scale, null, false, locked, word, bar.Width + more);
                                    foreach (var p in hits.Problems) problem(name + p);
                                    if (!hits.ShopAlways.IsEmpty) problem(name + "the bar asks the shop's question");
                                    MapInHead(set, v, false, Size.Empty, hits, name + "(bar) ", problem);
                                    // "Shop online · 16 offers" is a short tile: "Shop online" over "16 offers".
                                    if ((set.Contains("shop-online") || set.Contains("shop-closing")) && hits.Wrapped == 0) problem(name + "the shop's tile is one long line");
                                }
                            }
                        }
                    }
                }
            }
            Texts.Language = kept;
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
