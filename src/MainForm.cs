// The program's window and its life by the clock. Once set up the program lives by the clock with the site's mark as
// its icon: a left click opens the window, a right click the short menu (TrayMenu.cs). The window
// is a list at the left like Tempo's: Overview, MapleStory Classic, WoW: Forever, Settings, with
// the page that is picked at the right (MainViews.cs draws them; a game the program is not used for has no line). A
// first start is one thing a screen: your language, your games, the link (one gold button that opens the site with the
// code filled in), and the question "May Tempo look at your MapleStory window?"; with WoW: Forever too, its link is a
// step of its own in between. A notification comes only when the player has to do something (ToastTexts.cs); an upload
// that went well shows on the icon (a green check for ten minutes, TrayBadge.cs) and in the window. The program keeps
// itself up to date (Updater.cs) and says so afterwards in one quiet line on Overview. This file holds what the
// program does: linking, uploading, the addon, the update, the icon and its menu. Colours as the site's rules have
// them, in Windows' light or dark mode (Look.cs).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GameMargins.Uploader
{
    internal sealed partial class MainForm : Form
    {
        private const string Title = "GameMargins Desktop";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        /// <summary>The Run value's name, kept from the program's old name (GameMargins Uploader) so autostart stays one entry.</summary>
        private const string RunValue = "GameMarginsUploader";
        private static readonly Regex UploadToken = new Regex("^gm_wu_[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant);
        /// <summary>Codes whose fresh link the program asks for by itself before it waits for the window to be opened.</summary>
        private const int CodeRenewalsUnseen = 2;
        /// <summary>How long the tray icon shows the green check after an upload that stored a scan.</summary>
        private const int BadgeMinutes = 10;
        /// <summary>NotifyIcon.Text holds no more than this.</summary>
        private const int TrayTextMax = 63;
        /// <summary>How long Overview says "Updated to x" after the program updated itself.</summary>
        private const int UpdatedNoteDays = 3;
        /// <summary>The window's inside, in pixels at 96 dpi; it is not resized, so nothing is ever cut off or scrolls sideways.</summary>
        internal const int ViewWidth = 660, ViewHeight = 440;

        private readonly Settings _settings;
        private readonly Api _api;
        private readonly Scanner _scanner = new Scanner();
        private readonly Addon _addon;
        private readonly NotifyIcon _tray;
        private readonly Icon _trayIcon;
        /// <summary>The same icon with the green check (TrayBadge), for the ten minutes after a stored scan.</summary>
        private readonly Icon _trayIconOk;
        /// <summary>The tray icon with a green dot, while Tempo watches the game's window.</summary>
        private readonly Icon _trayIconDot;
        private readonly System.Windows.Forms.Timer _pollTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _clock = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _maintain = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _afterUpload = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _retry = new System.Windows.Forms.Timer();
        /// <summary>The program's own update check: shortly after the start, then once a day.</summary>
        private readonly System.Windows.Forms.Timer _updateCheck = new System.Windows.Forms.Timer();
        /// <summary>Waits until nothing is under way before handing over to a new exe.</summary>
        private readonly System.Windows.Forms.Timer _handOver = new System.Windows.Forms.Timer();
        private readonly Updater _updater;
        /// <summary>The exe this copy replaced, when an update started it (--updated-from); null otherwise.</summary>
        private readonly string _updatedFrom;
        /// <summary>Made for a picture (Shots.cs): no icon by the clock, nothing asked of the site, no window shown.</summary>
        private readonly bool _shots;

        private readonly UiCanvas _canvas = new UiCanvas();
        /// <summary>Tempo's side (MapleStory Classic): its link, its window, its float.</summary>
        private readonly Tempo _tempo;

        // What the window shows, kept as plain words and tones ("", "muted", "warning", "negative", "info"): the pages are drawn from these.
        private string _page = "overview";
        private bool _gamesEdit;
        private bool _tickMaple, _tickWow;
        private bool _askAgain;
        private bool _unlinkAsk;
        private string _linkNotice;
        private string _waitText = "";
        private string _waitTone = "";
        private string _result = "";
        private string _resultTone = "muted";
        private string _addonText;
        private string _addonTone = "";
        private readonly Dictionary<int, string[]> _accountStatus = new Dictionary<int, string[]>();
        private DateTime _copiedAt = DateTime.MinValue;
        private bool _lastUploadFailed;
        private bool _wowBegun;

        private bool _allowVisible;
        private bool _quitting;
        private LinkStart _link;
        private bool _polling;
        private int _codeRenewals;
        private bool _linkPaused;
        private DateTime _retryAfterUtc = DateTime.MinValue;
        /// <summary>Uploads in a row that could not reach the site: the next try waits 1, 2, 4, 8, then 15 minutes.</summary>
        private int _offlineTries;
        private readonly HashSet<int> _inFlight = new HashSet<int>();
        private readonly HashSet<int> _again = new HashSet<int>();
        private int _uploadsRunning;
        /// <summary>A folder picker is open: the hand-over to a new exe waits for it.</summary>
        private bool _dialogOpen;
        private bool _tempoDirty;

        /// <summary>A newer version waiting for the player's click ("Keep the program up to date" off); null when there is none.</summary>
        private ProgramRelease _offered;
        private bool _updating;
        /// <summary>A version whose download did not match or whose exe would not start: not tried again until the next start.</summary>
        private string _skipVersion;
        /// <summary>The checked new exe this copy hands over to once nothing is under way, and for which version; null before.</summary>
        private string _handOverTo;
        private ProgramRelease _handOverRelease;
        /// <summary>Whether that update came from a click on "Update now", which doesn't wait for the window to be closed.</summary>
        private bool _handOverClicked;
        /// <summary>The site was asked for its newest version and answered: the list's foot may say "up to date".</summary>
        private bool _checked;

        public MainForm(Settings settings, string updatedFrom) : this(settings, updatedFrom, false)
        {
        }

        public MainForm(Settings settings, string updatedFrom, bool shots)
        {
            _settings = settings;
            _updatedFrom = updatedFrom;
            _shots = shots;
            _api = new Api(settings.Site);
            _addon = new Addon(_api) { KeepUpdated = settings.KeepAddonUpdated };
            _addon.Changed += OnAddonChanged;
            _updater = new Updater(new HttpUpdateWeb(), Settings.Folder, Program.Version);
            _tempo = new Tempo(settings, _api);
            _tempo.WindowVisible = delegate { return Visible; };
            _tempo.Changed = OnTempoChanged;
            // Unlinked on the site: the one thing of Tempo the player has to do something about, so it is said by the clock too.
            _tempo.Attention = delegate
            {
                Notify(ToastTexts.Unlinked(), "unlinked", ToolTipIcon.Warning, 8000);
                ShowWindow();
            };
            _tempo.FirstLink = FirstLink;
            _tickMaple = settings.Maple;
            _tickWow = settings.Wow && settings.GamesAsked;

            Text = Title;
            BackColor = Look.Now.Background;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(ViewWidth, ViewHeight);
            Icon = LoadIcon(32);
            _canvas.Dock = DockStyle.Fill;
            _canvas.Render = Draw;
            Controls.Add(_canvas);
            if (shots) return;

            Look.Changed += OnLookChanged;
            var traySize = SystemInformation.SmallIconSize.Width;
            _trayIcon = LoadIcon(traySize);
            try { _trayIconOk = TrayBadge.WithCheck(_trayIcon, traySize); } catch (Exception) { _trayIconOk = _trayIcon; }
            try { _trayIconDot = TrayBadge.WithDot(_trayIcon, traySize); } catch (Exception) { _trayIconDot = _trayIcon; }
            _tray = new NotifyIcon { Icon = _trayIcon, Text = Title, Visible = true };
            _tray.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) ShowWindow(); };
            _tray.DoubleClick += delegate { ShowWindow(); };
            _tray.BalloonTipClicked += delegate { ShowWindow(); };
            _tray.ContextMenuStrip = TrayMenu.Make(TrayLines);
            RefreshTray();

            _pollTimer.Tick += async delegate { await PollOnce(); };
            _clock.Interval = 60000;
            _clock.Tick += delegate
            {
                Repaint();
                RefreshTray();
            };
            _clock.Start();
            _maintain.Interval = 10 * 60000;
            _maintain.Tick += async delegate { await _addon.Maintain(_scanner.WowFolder); };
            _afterUpload.Interval = 20000;
            _afterUpload.Tick += async delegate
            {
                _afterUpload.Stop();
                await _addon.Maintain(_scanner.WowFolder);
            };
            _retry.Tick += async delegate
            {
                _retry.Stop();
                await UploadAll(false);
            };
            // The program's own update: twenty seconds after the start, then once a day. Only for our own site: a test
            // start against another one (--site) never updates itself.
            _updateCheck.Interval = 20000;
            _updateCheck.Tick += async delegate
            {
                _updateCheck.Interval = 24 * 60 * 60 * 1000;
                await CheckForUpdate();
            };
            if (SelfUpdating) _updateCheck.Start();
            _handOver.Interval = 5000;
            _handOver.Tick += delegate { HandOverWhenIdle(); };

            _scanner.FileWritten += async delegate(WowAccount account) { await UploadAccount(account, false); };
            _scanner.AccountsChanged += delegate { Repaint(); };

            FormClosing += OnClosing;

            // The window exists from the start (timers, Invoke) but shows only when something asks for it: a program that
            // is set up starts by the clock, one that is not shows the step that is open.
            CreateHandle();
            BeginInvoke(new Action(async delegate { await Begin(); }));
        }

        /// <summary>Hidden until ShowWindow: Application.Run's own Show() is held back this way.</summary>
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(_allowVisible && value);
        }

        /// <summary>A test start that opens Tempo by itself shows its windows without taking the keyboard.</summary>
        protected override bool ShowWithoutActivation
        {
            get { return Program.TestOpen; }
        }

        // ------------------------------------------------------------------------------------------------ the window's size and mode

        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Look.TitleBar(Handle);
            var zoom = 1f;
            try { zoom = Math.Max(1f, GetDpiForWindow(Handle) / 96f); } catch (Exception) { }
            Zoom(zoom);
        }

        /// <summary>The window at the screen's scale: the same page, every size times `zoom`.</summary>
        private void Zoom(float zoom)
        {
            _canvas.Zoom = zoom;
            ClientSize = new Size((int)Math.Round(ViewWidth * zoom), (int)Math.Round(ViewHeight * zoom));
            _canvas.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (Look.IsColourChange(ref m)) Look.Refresh();
            // Moved to a screen with another scale: Windows names the new scale; the window keeps its place and takes the size that fits its page.
            if (m.Msg == 0x02E0)
            {
                Zoom(Math.Max(1f, (m.WParam.ToInt64() & 0xFFFF) / 96f));
                return;
            }
            base.WndProc(ref m);
        }

        /// <summary>Windows switched between light and dark: the window, the float and the menu follow at once.</summary>
        private void OnLookChanged()
        {
            if (_quitting || IsDisposed) return;
            // Windows may say so on a thread of its own: the window is touched on its own thread only.
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(OnLookChanged)); } catch (Exception) { }
                return;
            }
            try
            {
                BackColor = Look.Now.Background;
                if (IsHandleCreated) Look.TitleBar(Handle);
                _canvas.Invalidate();
                _tempo.LookChanged();
            }
            catch (Exception) { }
        }

        private void Repaint()
        {
            if (_quitting || IsDisposed) return;
            Settle();
            _canvas.Invalidate();
        }

        /// <summary>A second start of the exe sets this signal; the running copy answers by showing its window.</summary>
        public void ListenForSecondStart(EventWaitHandle signal)
        {
            var thread = new Thread(delegate()
            {
                while (!_quitting)
                {
                    try
                    {
                        if (!signal.WaitOne(1000)) continue;
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    try { BeginInvoke(new Action(ShowWindow)); } catch (Exception) { }
                }
            }) { IsBackground = true, Name = "second start" };
            thread.Start();
        }

        private static Icon LoadIcon(int size)
        {
            try
            {
                using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("GameMargins.Uploader.GameMargins.ico"))
                {
                    if (stream != null) return new Icon(stream, size, size);
                }
            }
            catch (Exception) { }
            try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { return SystemIcons.Application; }
        }

        // ------------------------------------------------------------------------------------------------ which screen

        /// <summary>
        /// What the window shows: "language" (a first start's first step: which language?), "games" (which games?), "link-maple" or "link-wow" (a code to link with), "ask" (may
        /// Tempo look?), or "main" (the list and its pages). A step shows as long as it is open; "No, not now" leaves
        /// the question for later (MapleStory's page and Settings lead back to it). A Windows that can't look at a
        /// window is never asked the question.
        /// </summary>
        private string Stage()
        {
            if (!_settings.LanguageAsked && !_settings.GamesAsked && !_gamesEdit) return "language";
            if (!_settings.GamesAsked || _gamesEdit) return "games";
            if (_settings.Maple && _tempo.SiteAllowed && !_tempo.Linked) return "link-maple";
            if (_settings.Wow && string.IsNullOrEmpty(_settings.Token)) return "link-wow";
            if (_settings.Maple && _tempo.Linked && !TempoShut && !_tempo.Consent && (!_settings.TempoAsked || _askAgain)) return "ask";
            return "main";
        }

        /// <summary>Tempo can't be used here for now: the site just refused to open it for this account, or this Windows can't look at a window. No gold button leads to it then.</summary>
        private bool TempoShut
        {
            get { return _tempo.Refused || _tempo.CantLook; }
        }

        /// <summary>The first start's steps for the games ticked: the language, games, MapleStory's link, WoW's link, the question.</summary>
        private List<string> Steps()
        {
            var maple = _settings.GamesAsked ? _settings.Maple : _tickMaple || !_tickWow;
            var wow = _settings.GamesAsked ? _settings.Wow : _tickWow;
            var steps = new List<string>();
            if (!_settings.LanguageAsked || _settings.LanguageStep) steps.Add("language");
            steps.Add("games");
            if (maple) steps.Add("link-maple");
            if (wow) steps.Add("link-wow");
            if (maple) steps.Add("ask");
            return steps;
        }

        /// <summary>Once every step is done the first start is over: a step that opens again later shows without the step bars.</summary>
        private void Settle()
        {
            if (_settings.SetupDone || Stage() != "main") return;
            _settings.SetupDone = true;
            _settings.Save();
        }

        private void Go(string page)
        {
            _page = page;
            _unlinkAsk = false;
            _canvas.Scroll = 0;
            Repaint();
        }

        // ------------------------------------------------------------------------------------------------ notifications and the tray

        /// <summary>
        /// A notification that stays: a Windows toast in the notification center (Toasts.Show; also under Do not disturb,
        /// which hides the banner but keeps the notification), and the old balloon where the toast can't be made. Only
        /// for what the player has to do something about (ToastTexts.cs).
        /// </summary>
        private void Notify(string[] toast, string tag, ToolTipIcon fallbackIcon, int balloonMs)
        {
            if (_shots) return;
            try
            {
                Toasts.Show(toast[0], toast[1], tag);
            }
            catch (Exception)
            {
                _tray.ShowBalloonTip(balloonMs, toast[0], toast[1], fallbackIcon);
            }
        }

        /// <summary>The newest upload that stored a scan, from the window's list (which lives in the settings, so a restart knows it too).</summary>
        private UploadRow LastStored()
        {
            foreach (var row in _settings.Uploads) if (row.Status == "stored") return row;
            return null;
        }

        /// <summary>
        /// The tray icon and its tooltip. While Tempo watches the game the icon carries a green dot; for BadgeMinutes
        /// after a stored scan the green check and "Uploaded 3 min ago · 240,910 auctions"; then the plain icon and
        /// "Last upload: 14:23". Called after an upload, when Tempo's state changes, and by the minute clock.
        /// </summary>
        private void RefreshTray()
        {
            if (_tray == null) return;
            if (_settings.Maple && _tempo.Watching)
            {
                if (!ReferenceEquals(_tray.Icon, _trayIconDot)) _tray.Icon = _trayIconDot;
                SetTrayText(Texts.T("Tempo is watching MapleStory"));
                return;
            }
            var last = _settings.Wow ? LastStored() : null;
            var age = last == null ? TimeSpan.MaxValue : DateTime.UtcNow - last.At;
            var recent = last != null && age.TotalMinutes < BadgeMinutes;
            var icon = recent ? _trayIconOk : _trayIcon;
            if (!ReferenceEquals(_tray.Icon, icon)) _tray.Icon = icon;
            string line = null;
            if (recent)
            {
                var auctions = last.Auctions.ToString("N0", Texts.Culture);
                var minutes = (int)Math.Max(0, age.TotalMinutes);
                line = minutes < 1 ? Texts.T("Uploaded just now · {0} auctions", auctions) : Texts.T("Uploaded {0} min ago · {1} auctions", minutes, auctions);
            }
            else if (last != null) line = Texts.T("Last upload: {0}", WhenText(last.At));
            SetTrayText(line);
        }

        /// <summary>
        /// The title with the program's version and `line` as the tooltip, within NotifyIcon's 63 characters: the version
        /// goes first when they don't fit, then the title, then the line is cut.
        /// </summary>
        private void SetTrayText(string line)
        {
            var titled = Title + " " + Program.Version;
            var text = string.IsNullOrEmpty(line) ? titled : titled + "\n" + line;
            if (text.Length > TrayTextMax) text = Title + "\n" + line;
            if (text.Length > TrayTextMax) text = line;
            if (text.Length > TrayTextMax) text = text.Substring(0, TrayTextMax - 1) + "…";
            try
            {
                if (_tray.Text != text) _tray.Text = text;
            }
            catch (ArgumentException)
            {
                _tray.Text = Title;
            }
        }

        /// <summary>How Tempo is doing, in one line, with its dot's tone ("ok", "warn", "bad", ""): the same words on Overview, MapleStory's page and the menu.</summary>
        internal string[] TempoStatus()
        {
            if (!_tempo.SiteAllowed) return new[] { Texts.T("Tempo works with gamemargins.com only."), "warn" };
            if (!_tempo.Linked) return new[] { Texts.T("Tempo is not linked to your account yet"), "warn" };
            if (_tempo.Refused) return new[] { Texts.T("The site did not open Tempo for this account"), "bad" };
            if (_tempo.CantLook) return new[] { Texts.T("Tempo can't look at the game on this Windows"), "bad" };
            if (!_tempo.Consent) return new[] { Texts.T("Tempo is off: it may not look at the game yet"), "" };
            if (_tempo.RuntimeMissing) return new[] { Texts.T("Tempo needs Microsoft's WebView2"), "warn" };
            var time = _tempo.SessionTime();
            if (_tempo.SessionState == "reading") return new[] { Texts.T("Tempo is reading · {0}", time), "ok" };
            if (_tempo.SessionState == "paused") return new[] { Texts.T("Tempo is paused · {0}", time), "" };
            if (_tempo.Problem != null) return new[] { _tempo.Problem, _tempo.ProblemTone == "negative" ? "bad" : "warn" };
            if (_tempo.GameFound == true) return new[] { Texts.T("MapleStory is running · Tempo is not started"), "" };
            return new[] { Texts.T("Tempo is not started"), "" };
        }

        /// <summary>How WoW's side is doing, in one line, with its dot's tone.</summary>
        internal string[] WowStatus()
        {
            if (string.IsNullOrEmpty(_settings.Token)) return new[] { Texts.T("Not linked to your account yet"), "warn" };
            if (WowFolderNow == null) return new[] { Texts.T("WoW folder not found."), "warn" };
            if (_uploadsRunning > 0) return new[] { Texts.T("Uploading…"), "ok" };
            if (_lastUploadFailed) return new[] { Texts.T("The last upload failed"), "bad" };
            var last = LastStored();
            if (last == null) return new[] { Texts.T("No uploads yet"), "" };
            return new[] { Texts.T("Uploaded {0}", WhenText(last.At)), "ok" };
        }

        /// <summary>The menu's lines, made each time it opens (TrayMenu.Make), so the minutes are right.</summary>
        private List<TrayLine> TrayLines()
        {
            var maple = _settings.Maple && _settings.GamesAsked;
            var wow = _settings.Wow && _settings.GamesAsked;
            var tempo = TempoStatus();
            var wowStatus = WowStatus();
            var shut = maple && TempoShut;
            var state = new TrayMenuState
            {
                TempoShut = shut,
                Maple = maple,
                Wow = wow,
                TempoLine = maple ? tempo[0] : null,
                TempoTone = tempo[1],
                WowLine = wow ? "WoW: " + LowerFirst(wowStatus[0]) : null,
                WowTone = wowStatus[1],
                TempoReady = maple && !shut && _tempo.Linked && _tempo.Consent,
                Watching = _tempo.SessionState == "reading" || _tempo.Watching,
                WowReady = wow && !string.IsNullOrEmpty(_settings.Token),
                UpdateTo = !_settings.KeepProgramUpdated && _offered != null ? _offered.Version : null,
            };
            var actions = new TrayMenuActions
            {
                Update = UpdateNow,
                UploadNow = async delegate { await UploadAll(true); },
                Quit = Quit,
                OpenTempo = OpenTempoOrWindow,
                StopWatching = delegate { _tempo.StopWatching(); },
                StartWatching = delegate
                {
                    var open = _tempo.OpenTempo(true, true);
                    GC.KeepAlive(open);
                },
                OpenWindow = ShowWindow,
                OpenSettings = delegate
                {
                    if (Stage() == "main") Go("settings");
                    ShowWindow();
                },
            };
            return TrayMenu.Lines(state, actions);
        }

        private static string LowerFirst(string text)
        {
            if (string.IsNullOrEmpty(text) || text.StartsWith("WoW", StringComparison.Ordinal)) return text;
            return char.ToLower(text[0], Texts.Culture) + text.Substring(1);
        }

        // ------------------------------------------------------------------------------------------------ start-up

        private async Task Begin()
        {
            if (_updatedFrom != null) AfterUpdate();
            EnsureAutostart();
            // A first start: which games? A copy that was in use before 0.4.0 is not asked: it is used for WoW.
            if (!_settings.GamesAsked)
            {
                // A test start that opens Tempo by itself answers the question itself: MapleStory.
                if (Program.TestOpen)
                {
                    _settings.LanguageAsked = true;
                    await ApplyGames(false, true);
                    return;
                }
                ShowWindow();
                return;
            }
            if (_settings.Maple)
            {
                var tempo = _tempo.Begin();
                GC.KeepAlive(tempo);
            }
            // A step still open (a link, the question): the window says so.
            if (Stage() != "main") ShowWindow();
            if (_settings.Wow) await BeginWow();
            Repaint();
        }

        /// <summary>WoW's side at the start, or once WoW is turned on: the folder, the link, the first upload, the addon.</summary>
        private async Task BeginWow()
        {
            _wowBegun = true;
            FindWowFolder();
            if (string.IsNullOrEmpty(_settings.Token))
            {
                ShowWindow();
                await StartLinkFlow();
                return;
            }
            var status = await _api.Status(_settings.Token);
            if (status.Status == "unlinked")
            {
                await Unlinked();
                return;
            }
            if (status.Status == "ok")
            {
                ApplyLocale(status.Locale);
                _addon.Site = status.Addon;
            }
            else SetResult(Texts.T("Can't reach {0}; trying again later.", SiteHost()), "negative");
            await UploadAll(false);
            await _addon.Maintain(_scanner.WowFolder);
            _maintain.Start();
        }

        /// <summary>The install Settings knows, else the default one. Not found: WoW's page says so and has "Choose folder…" (no dialog opens by itself).</summary>
        private void FindWowFolder()
        {
            var folder = Scanner.Accept(_settings.WowFolder) ?? Scanner.FindDefault();
            if (folder == null) return;
            _settings.WowFolder = folder;
            _settings.Save();
            _scanner.Start(folder);
        }

        /// <summary>"Change" or "Choose folder…" on WoW's page: the player picks the folder.</summary>
        private async void PickWowFolder()
        {
            string picked;
            _dialogOpen = true;
            try
            {
                using (var dialog = new FolderBrowserDialog { Description = Texts.T("Pick your World of Warcraft folder (the one that holds _forever_), or the _forever_ folder itself."), ShowNewFolderButton = false })
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    picked = Scanner.Accept(dialog.SelectedPath);
                }
            }
            finally
            {
                _dialogOpen = false;
            }
            if (picked == null)
            {
                SetResult(Texts.T("That folder holds no World of Warcraft: pick the one that holds _forever_."), "warning");
                return;
            }
            _settings.WowFolder = picked;
            _settings.Save();
            _scanner.Start(picked);
            SetResult("", "muted");
            Repaint();
            await UploadAll(false);
            await _addon.Maintain(_scanner.WowFolder);
        }

        private void ApplyLocale(string locale)
        {
            // A language picked in Settings stays; else the account's language on the site.
            if (_settings.LocalePicked) return;
            var language = Texts.Pick(locale);
            if (locale != null && _settings.Locale != locale)
            {
                _settings.Locale = locale;
                _settings.Save();
            }
            if (language == Texts.Language) return;
            Texts.Language = language;
            Repaint();
        }

        /// <summary>The first start's language step: a click switches the program's language at once (the step draws again in it).</summary>
        private void TryLanguage(string language)
        {
            Texts.Language = Texts.Pick(language);
            Repaint();
        }

        /// <summary>"Continue" on the language step: the language that shows is kept, as if picked in Settings, and the games come next.</summary>
        private void KeepLanguage()
        {
            _settings.Locale = Texts.Language;
            _settings.LocalePicked = true;
            _settings.LanguageAsked = true;
            _settings.LanguageStep = true;
            _settings.Save();
            Repaint();
            RefreshTray();
        }

        /// <summary>"Appearance" in Settings: follow Windows, or always light or dark; the window, the float, the menu and Tempo's window follow at once.</summary>
        private void PickAppearance(string mode)
        {
            _settings.Appearance = mode;
            _settings.Save();
            Look.Choose(mode);
            Repaint();
        }

        /// <summary>"+ Add a game" in the list, and "Add or remove a game" in Settings: the question of the first start again.</summary>
        private void EditGames()
        {
            _tickMaple = _settings.Maple;
            _tickWow = _settings.Wow;
            _gamesEdit = true;
            Repaint();
        }

        /// <summary>The language picked in Settings.</summary>
        private void PickLanguage(string language)
        {
            _settings.Locale = language;
            _settings.LocalePicked = true;
            _settings.Save();
            Texts.Language = Texts.Pick(language);
            Repaint();
            RefreshTray();
        }

        private string SiteHost()
        {
            try { return new Uri(_settings.Site).Host; } catch (Exception) { return _settings.Site; }
        }

        private string LinkAddress()
        {
            return SiteHost() + "/link";
        }

        /// <summary>Opens an address of our own site in the browser; nothing a server answer wrote goes to the shell.</summary>
        private void OpenSite(string path)
        {
            Uri url;
            if (!Uri.TryCreate(_settings.Site + path, UriKind.Absolute, out url)) return;
            if (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp) return;
            try { Process.Start(url.AbsoluteUri); } catch (Exception) { }
        }

        /// <summary>The code of the link step that shows: MapleStory's or WoW's.</summary>
        private LinkStart CodeOf(string stage)
        {
            return stage == "link-maple" ? _tempo.Link : _link;
        }

        /// <summary>The gold button: the site's link page in the player's browser, the code already filled in. Built here from our own site and the code: nothing the server sent is run.</summary>
        private void OpenLink(string stage)
        {
            var link = CodeOf(stage);
            if (link != null) OpenSite("/link?code=" + Uri.EscapeDataString(link.UserCode));
        }

        /// <summary>The same address on the clipboard, for a browser the program does not open by itself; "Copied" on the button a moment.</summary>
        private void CopyLink(string stage)
        {
            var link = CodeOf(stage);
            if (link == null) return;
            Uri url;
            if (!Uri.TryCreate(_settings.Site + "/link?code=" + Uri.EscapeDataString(link.UserCode), UriKind.Absolute, out url)) return;
            try { Clipboard.SetText(url.AbsoluteUri); }
            catch (Exception) { return; }
            _copiedAt = DateTime.UtcNow;
            var timer = new System.Windows.Forms.Timer { Interval = 2600 };
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();
                Repaint();
            };
            timer.Start();
            Repaint();
        }

        // ------------------------------------------------------------------------------------------------ linking

        private async Task StartLinkFlow()
        {
            _pollTimer.Stop();
            // A code nobody picked up twice over: no third one until the player opens the window (then it starts afresh).
            if (_codeRenewals >= CodeRenewalsUnseen && !Visible)
            {
                _linkPaused = true;
                _link = null;
                SetWaiting(Texts.T("Open this window for a new code."), "muted");
                return;
            }
            _linkPaused = false;
            _link = await _api.StartLink();
            if (_link == null || string.IsNullOrEmpty(_link.UserCode) || string.IsNullOrEmpty(_link.DeviceCode))
            {
                _link = null;
                SetWaiting(Texts.T("Can't reach {0}; trying again later.", SiteHost()), "negative");
                _pollTimer.Interval = 15000;
                _pollTimer.Start();
                return;
            }
            SetWaiting("", "");
            _pollTimer.Interval = Math.Max(2, _link.IntervalSeconds) * 1000;
            _pollTimer.Start();
        }

        private async Task PollOnce()
        {
            if (_polling) return;
            _polling = true;
            try
            {
                if (_link == null || _link.ExpiresAt <= DateTime.UtcNow)
                {
                    var expired = _link != null;
                    if (expired) _codeRenewals++;
                    await StartLinkFlow();
                    if (expired && _link != null) SetWaiting(Texts.T("The code expired; here is a new one."), "warning");
                    return;
                }
                var result = await _api.Poll(_link.DeviceCode);
                switch (result.Status)
                {
                    case "ok":
                        if (result.Token == null || !UploadToken.IsMatch(result.Token))
                        {
                            // Not a token of ours: whatever answered, it isn't the site we linked to. Start over.
                            _codeRenewals++;
                            await StartLinkFlow();
                            if (_link != null) SetWaiting(Texts.T("The code expired; here is a new one."), "warning");
                            return;
                        }
                        _pollTimer.Stop();
                        _codeRenewals = 0;
                        _settings.Token = result.Token;
                        _settings.Save();
                        _link = null;
                        _linkNotice = null;
                        ApplyLocale(result.Locale);
                        FirstLink();
                        await Linked();
                        break;
                    case "expired":
                    case "unknown":
                        _codeRenewals++;
                        await StartLinkFlow();
                        if (_link != null) SetWaiting(Texts.T("The code expired; here is a new one."), "warning");
                        break;
                    case "offline":
                        SetWaiting(Texts.T("Can't reach {0}; trying again later.", SiteHost()), "negative");
                        break;
                    default:
                        if (_waitTone != "") SetWaiting("", "");
                        break;
                }
            }
            finally
            {
                _polling = false;
            }
        }

        /// <summary>The program's first link, for either game: "Start with Windows" goes on; Settings turns it off.</summary>
        private void FirstLink()
        {
            if (_settings.AutostartDecided) return;
            _settings.AutostartDecided = true;
            SetStartWithWindows(true);
        }

        /// <summary>Just linked: the site's versions, the first upload, the addon. Without a WoW folder the window opens on WoW's page, which asks for it.</summary>
        private async Task Linked()
        {
            if (_scanner.WowFolder == null) _page = "wow";
            Repaint();
            var status = await _api.Status(_settings.Token);
            if (status.Status == "ok") _addon.Site = status.Addon;
            await UploadAll(false);
            await _addon.Maintain(_scanner.WowFolder);
            _maintain.Start();
        }

        /// <summary>The site refused the token (unlinked on Account › Security): back to the link step, and say so even by the clock.</summary>
        private async Task Unlinked()
        {
            _settings.Token = null;
            _settings.Save();
            _maintain.Stop();
            _retry.Stop();
            _linkNotice = Texts.T("This program was unlinked on the site. Link it again with the code below.");
            Notify(ToastTexts.Unlinked(), "unlinked", ToolTipIcon.Warning, 8000);
            ShowWindow();
            await StartLinkFlow();
        }

        /// <summary>"Unlink" in Settings, after its one question: this PC forgets both links; the first step that opens again asks for a new code.</summary>
        private async void UnlinkHere()
        {
            _unlinkAsk = false;
            _maintain.Stop();
            _retry.Stop();
            _settings.Token = null;
            _settings.Save();
            _linkNotice = null;
            _tempo.Unlink();
            _page = "overview";
            Repaint();
            RefreshTray();
            if (_settings.Wow) await StartLinkFlow();
        }

        private void SetWaiting(string text, string tone)
        {
            _waitText = text;
            _waitTone = tone;
            Repaint();
        }

        // ------------------------------------------------------------------------------------------------ uploading

        /// <summary>
        /// "Trying again later" made true: an upload that could not reach the site (no network, the site down) tries again
        /// by itself, after 1, 2, 4, 8 and then every 15 minutes, until one goes through. A file write or Upload now still
        /// goes at once; a 429's wait (_retryAfterUtc) stays in charge when there is one.
        /// </summary>
        private void RetryWhenOnline()
        {
            if (_retryAfterUtc > DateTime.UtcNow) return;
            _offlineTries = Math.Min(_offlineTries + 1, 5);
            var minutes = _offlineTries >= 5 ? 15 : 1 << (_offlineTries - 1);
            _retry.Stop();
            _retry.Interval = minutes * 60 * 1000;
            _retry.Start();
        }

        private async Task UploadAll(bool force)
        {
            foreach (var account in new List<WowAccount>(_scanner.Accounts)) await UploadAccount(account, force);
        }

        private async Task UploadAccount(WowAccount account, bool force)
        {
            if (!_settings.Wow || string.IsNullOrEmpty(_settings.Token) || !account.Exists) return;
            if (_inFlight.Contains(account.Number))
            {
                // The game wrote the file while its last write is still being sent: once more after this one.
                _again.Add(account.Number);
                return;
            }
            if (!force && _retryAfterUtc > DateTime.UtcNow)
            {
                SetResult(Texts.T("Too many uploads; the next try is in {0} min.", Math.Max(1, (int)Math.Ceiling((_retryAfterUtc - DateTime.UtcNow).TotalMinutes))), "info");
                ScheduleRetry();
                return;
            }
            _inFlight.Add(account.Number);
            _uploadsRunning++;
            SetStatus(account, Texts.T("uploading…"), "muted");
            try
            {
                var text = await Scanner.ReadWhenSettled(account.File);
                if (text == null)
                {
                    SetStatus(account, Texts.T("upload failed"), "negative");
                    SetResult(Texts.T("Upload failed: {0}", Texts.T("Could not read the file; the game may still be writing it.")), "negative");
                    return;
                }
                var fingerprint = Scanner.Fingerprint(text);
                if (!force && _settings.Uploaded.Contains(fingerprint))
                {
                    SetStatus(account, Texts.T("uploaded · watching"), "muted");
                    SetResult(Texts.T("Nothing new in the file: these scans were uploaded before."), "muted");
                    return;
                }
                var result = await _api.Upload(_settings.Token, text);
                switch (result.Status)
                {
                    case "ok":
                        _offlineTries = 0;
                        _lastUploadFailed = false;
                        _settings.MarkUploaded(fingerprint);
                        ScanResult first = null;
                        ScanResult skipped = null;
                        foreach (var r in result.Results)
                        {
                            if (r.Status == "stored")
                            {
                                _settings.AddUpload(new UploadRow { At = DateTime.UtcNow, Market = r.Market, Auctions = r.Auctions, Status = "stored" });
                                if (first == null) first = r;
                            }
                            else if (r.Status != "duplicate" && skipped == null) skipped = r;
                        }
                        _settings.Save();
                        SetStatus(account, Texts.T("uploaded · watching"), "muted");
                        // The green check and the tooltip for the next ten minutes: an upload that went well shows there, not as a notification.
                        RefreshTray();
                        if (first != null) SetResult(Texts.T("Uploaded: {0} auctions · {1}", first.Auctions.ToString("N0", Texts.Culture), MarketLabel(first.Market)), "muted");
                        else if (skipped != null) SetResult(Texts.T("Skipped: {0}", RefusalText(skipped.Code, skipped.Why)), "warning");
                        // No scan stored, nothing skipped: the file went for its characters (a level-up, a skill-up), or for nothing.
                        else if (result.Characters > 0) SetResult(Texts.T("Characters up to date on the site: {0}.", result.Characters), "muted");
                        else SetResult(Texts.T("Nothing new in the file: these scans were uploaded before."), "muted");
                        break;
                    case "unlinked":
                        await Unlinked();
                        break;
                    case "refused":
                        if (result.RetryAfter > 0)
                        {
                            _retryAfterUtc = DateTime.UtcNow.AddSeconds(result.RetryAfter);
                            SetStatus(account, Texts.T("file found · watching"), "muted");
                            SetResult(Texts.T("Too many uploads; the next try is in {0} min.", Math.Max(1, (int)Math.Ceiling(result.RetryAfter / 60.0))), "info");
                            ScheduleRetry();
                        }
                        else
                        {
                            _lastUploadFailed = true;
                            SetStatus(account, Texts.T("upload failed"), "negative");
                            SetResult(Texts.T("Upload failed: {0}", RefusalText(result.Code, result.Error)), "negative");
                            Notify(ToastTexts.UploadFailed(result.Code), "failed", ToolTipIcon.Error, 5000);
                        }
                        break;
                    default:
                        SetStatus(account, Texts.T("upload failed"), "negative");
                        SetResult(Texts.T("Can't reach {0}; trying again later.", SiteHost()), "negative");
                        RetryWhenOnline();
                        break;
                }
            }
            finally
            {
                _inFlight.Remove(account.Number);
                _uploadsRunning--;
                Repaint();
                if (!string.IsNullOrEmpty(_settings.Token))
                {
                    _afterUpload.Stop();
                    _afterUpload.Start();
                }
                if (_again.Remove(account.Number) && !string.IsNullOrEmpty(_settings.Token))
                {
                    var again = UploadAccount(account, false);
                    GC.KeepAlive(again);
                }
            }
        }

        private void ScheduleRetry()
        {
            var wait = _retryAfterUtc - DateTime.UtcNow;
            _retry.Stop();
            _retry.Interval = (int)Math.Min(int.MaxValue, Math.Max(1000, wait.TotalMilliseconds + 500));
            _retry.Start();
        }

        /// <summary>The site's refusal in the player's language, by its code; the site's English only when the code is unknown.</summary>
        private static string RefusalText(string code, string fallback)
        {
            switch (code ?? "")
            {
                case "too-big": return Texts.T("The file is bigger than the site allows (12 MB).");
                case "addon-code": return Texts.T("This file is the addon's code, not its scan file.");
                case "old-format": return Texts.T("The file was written by another version of the addon; update it.");
                case "bad-request":
                case "unreadable":
                case "not-addon-file": return Texts.T("The site could not read the file.");
                case "no-region":
                case "no-ruleset":
                case "no-faction":
                case "region-not-on-site":
                case "unknown-market": return Texts.T("The scan does not say which auction house it is from: upload this file once on the site's upload page and pick the ruleset or region there.");
                case "hardcore":
                case "neutral-ah": return Texts.T("Hardcore realms and neutral auction houses are not on the site.");
                case "rate-limited": return Texts.T("Too many uploads; the next try is in {0} min.", 1);
                default: return string.IsNullOrEmpty(fallback) ? Texts.T("The site refused the file.") : fallback;
            }
        }

        /// <summary>"EU · Normal · Horde" from "eu-normal-horde": the names the game itself uses, in every language (the site's glossaries keep them English).</summary>
        internal static string MarketLabel(string market)
        {
            if (string.IsNullOrEmpty(market)) return "?";
            var parts = market.Split('-');
            if (parts.Length != 3) return market;
            string ruleset;
            switch (parts[1])
            {
                case "normal": ruleset = "Normal"; break;
                case "pvp": ruleset = "PvP"; break;
                case "rp": ruleset = "Roleplay"; break;
                case "rppvp": ruleset = "Roleplay PvP"; break;
                default: ruleset = parts[1]; break;
            }
            var faction = parts[2].Length > 0 ? char.ToUpperInvariant(parts[2][0]) + parts[2].Substring(1) : parts[2];
            return parts[0].ToUpperInvariant() + " · " + ruleset + " · " + faction;
        }

        internal static string WhenText(DateTime atUtc)
        {
            return WhenText(atUtc, ShotNow ?? DateTime.Now);
        }

        internal static string WhenText(DateTime atUtc, DateTime now)
        {
            var at = atUtc.ToLocalTime();
            var age = now - at;
            var culture = Texts.Culture;
            if (age.TotalMinutes < 1) return Texts.T("just now");
            if (age.TotalMinutes < 60) return Texts.T("{0} min ago", (int)age.TotalMinutes);
            var time = at.ToString("t", culture);
            if (at.Date == now.Date) return time;
            if (at.Date == now.Date.AddDays(-1)) return Texts.T("Yesterday {0}", time);
            if (age.TotalDays < 6) return at.ToString("ddd", culture) + " " + time;
            return at.ToString("d", culture);
        }

        private void SetStatus(WowAccount account, string text, string tone)
        {
            _accountStatus[account.Number] = new[] { text, tone };
            Repaint();
        }

        private void SetResult(string text, string tone)
        {
            _result = text ?? "";
            _resultTone = tone;
            Repaint();
        }

        // ------------------------------------------------------------------------------------------------ the addon

        private void OnAddonChanged(AddonStatus status)
        {
            _addonText = status.Text;
            _addonTone = status.Mark == AddonMark.Failed ? "negative" : status.Mark == AddonMark.Info ? "info" : status.Mark == AddonMark.Muted ? "muted" : "";
            // An addon that was installed or updated says so on WoW's page, not in a notification.
            Repaint();
        }

        // ------------------------------------------------------------------------------------------------ the program's own update

        /// <summary>Only a start that talks to our own site updates itself; a test start against another one (--site) never does.</summary>
        private bool SelfUpdating
        {
            get { return _settings.Site == Program.DefaultSite; }
        }

        /// <summary>
        /// Asks the site for its newest version (Updater.Check). A newer one is installed by itself with "Keep the program up
        /// to date" on, else offered in Settings and the menu as "Update to x.y.z". A site that can't be reached: the next
        /// check. A version that did not stay up after an earlier update is never taken again.
        /// </summary>
        private async Task CheckForUpdate()
        {
            if (!SelfUpdating || _updating || _handOverTo != null || _quitting) return;
            _updating = true;
            ProgramRelease found;
            try
            {
                found = await _updater.Check();
            }
            finally
            {
                _updating = false;
            }
            if (_updater.Reached) _checked = true;
            Repaint();
            if (found == null || found.Version == _skipVersion || found.Version == _settings.UpdateBad) return;
            if (_settings.KeepProgramUpdated) await InstallUpdate(found, false);
            else Offer(found);
        }

        /// <summary>"Keep the program up to date" is off: the version waits in Settings ("Update now") and in the menu, and one notification says so, once per version.</summary>
        private void Offer(ProgramRelease release)
        {
            _offered = release;
            Repaint();
            if (_settings.UpdateNoted == release.Version) return;
            _settings.UpdateNoted = release.Version;
            _settings.Save();
            Notify(ToastTexts.UpdateReady(release.Version), "update", ToolTipIcon.Info, 6000);
        }

        private void DropOffer()
        {
            _offered = null;
            Repaint();
        }

        private async void UpdateNow()
        {
            var release = _offered;
            if (release != null) await InstallUpdate(release, true);
        }

        /// <summary>
        /// Downloads and checks `release` (Updater.Fetch), then hands over as soon as nothing is under way. A download that
        /// doesn't match keeps nothing and says so once; one that can't be made says so only after a click.
        /// </summary>
        private async Task InstallUpdate(ProgramRelease release, bool clicked)
        {
            if (!SelfUpdating || _updating || _handOverTo != null || _quitting) return;
            _updating = true;
            UpdateResult result;
            try
            {
                result = await _updater.Fetch(release);
            }
            finally
            {
                _updating = false;
            }
            if (result.Outcome == UpdateOutcome.Mismatch)
            {
                _skipVersion = release.Version;
                DropOffer();
                Notify(ToastTexts.UpdateMismatch(release.Version, SiteHost()), "update", ToolTipIcon.Warning, 6000);
                return;
            }
            if (result.Outcome != UpdateOutcome.Ready)
            {
                if (clicked) Notify(ToastTexts.UpdateFailed(release.Version), "update", ToolTipIcon.Warning, 6000);
                return;
            }
            _handOverTo = result.Exe;
            _handOverRelease = release;
            _handOverClicked = clicked;
            Repaint();
            HandOverWhenIdle();
        }

        /// <summary>
        /// Hands over to the checked new exe once nothing is under way: no upload, no addon install, no folder dialog, no
        /// Tempo session (Tempo reading, or paused with numbers: the update waits until the session is stopped and kept,
        /// or reset), and for an update by itself no open window either (a click on "Update now" doesn't wait for that).
        /// Autostart (while on) and the Start-menu shortcut point at the new exe, it starts with --updated-from naming this
        /// one, and this copy closes its window and icon. This exe stays where it is as the way back: Program.Main
        /// stands by for half a minute, and when the new copy does not stay up it points everything back here, marks the
        /// version as bad and starts this one again (Updater.StandBy).
        /// </summary>
        private void HandOverWhenIdle()
        {
            _handOver.Stop();
            if (_handOverTo == null || _quitting) return;
            if (!Updater.MayHandOver(_uploadsRunning, _addon.Busy, _dialogOpen, Visible, _handOverClicked, _tempo.SessionLive))
            {
                _handOver.Start();
                return;
            }
            var exe = _handOverTo;
            PointAt(exe, _settings.StartWithWindows);
            var started = Updater.StartProcess(exe, "--updated-from " + Updater.Quote(Application.ExecutablePath));
            if (started != null)
            {
                Program.HandedOver = started;
                Program.HandedOverVersion = _handOverRelease.Version;
                Quit();
                return;
            }
            PointAt(Application.ExecutablePath, _settings.StartWithWindows);
            _skipVersion = _handOverRelease.Version;
            _handOverTo = null;
            DropOffer();
            Notify(ToastTexts.UpdateFailed(_handOverRelease.Version), "update", ToolTipIcon.Warning, 6000);
        }

        /// <summary>Autostart (only while "Start with Windows" is on, as at every start) and the Start-menu shortcut follow `exe`.</summary>
        internal static void PointAt(string exe, bool startWithWindows)
        {
            if (startWithWindows) WriteRunValue(exe);
            if (!Program.Isolated) Toasts.EnsureShortcut(exe);
        }

        /// <summary>
        /// This copy was started by the update of an older one (--updated-from). No notification: Overview says "Updated
        /// to x" for a few days, with a link to what is new. The exe it replaced stays as the way back; copies older than
        /// that one, installed next to the settings, go.
        /// </summary>
        private void AfterUpdate()
        {
            _settings.UpdatedTo = Program.Version;
            _settings.UpdatedAt = DateTime.UtcNow;
            _settings.Save();
            var removal = Updater.RemoveOlder(Settings.Folder, _updatedFrom, Application.ExecutablePath);
            GC.KeepAlive(removal);
        }

        /// <summary>Overview's quiet line after the program updated itself: "Updated to 0.4.1 last night"; null once it is old news.</summary>
        internal string UpdatedNote()
        {
            return UpdatedNote(ShotNow ?? DateTime.Now);
        }

        internal string UpdatedNote(DateTime now)
        {
            if (string.IsNullOrEmpty(_settings.UpdatedTo) || _settings.UpdatedTo != Program.Version || _settings.UpdatedAt == default(DateTime)) return null;
            var at = _settings.UpdatedAt.ToLocalTime();
            if ((now - at).TotalDays > UpdatedNoteDays) return null;
            string when;
            if (at.Date == now.Date) when = at.Hour < 6 ? Texts.T("last night") : Texts.T("today");
            else if (at.Date == now.Date.AddDays(-1)) when = at.Hour >= 18 ? Texts.T("last night") : Texts.T("yesterday");
            else when = Texts.T("on {0}", at.ToString("dddd", Texts.Culture));
            return Texts.T("Updated to {0} {1}", Program.Version, when);
        }

        /// <summary>Under the list: "Version 0.4.0 · up to date", or what is on its way.</summary>
        internal string VersionLine()
        {
            var version = Texts.T("Version {0}", Program.Version);
            if (_handOverTo != null && _handOverRelease != null) return version + " · " + Texts.T("{0} installs soon", _handOverRelease.Version);
            if (_offered != null) return version + " · " + Texts.T("{0} is ready", _offered.Version);
            if (_checked) return version + " · " + Texts.T("up to date");
            return version;
        }

        /// <summary>This copy took over again after version `version` did not stay up (Program.StandBy): said once, by the clock.</summary>
        public void SayUpdateFailed(string version)
        {
            _skipVersion = version;
            BeginInvoke(new Action(delegate { Notify(ToastTexts.UpdateFailed(version), "update", ToolTipIcon.Warning, 6000); }));
        }

        // ------------------------------------------------------------------------------------------------ the games

        /// <summary>The answer to "Which games do you play?": each game is turned on or off, and the next step that is open shows.</summary>
        private async Task ApplyGames(bool wow, bool maple)
        {
            var hadWow = _settings.Wow && _settings.GamesAsked;
            var hadMaple = _settings.Maple && _settings.GamesAsked;
            _settings.GamesAsked = true;
            _settings.Wow = wow;
            _settings.Maple = maple;
            _settings.Save();
            _gamesEdit = false;
            if (_page == "maple" && !maple) _page = "overview";
            if (_page == "wow" && !wow) _page = "overview";
            if (!maple && hadMaple) _tempo.End();
            if (!wow && hadWow)
            {
                _pollTimer.Stop();
                _maintain.Stop();
                _retry.Stop();
                _link = null;
            }
            if (maple && !hadMaple)
            {
                var tempo = _tempo.Begin();
                GC.KeepAlive(tempo);
            }
            Repaint();
            RefreshTray();
            if (wow && (!hadWow || !_wowBegun)) await BeginWow();
            Repaint();
        }

        // ------------------------------------------------------------------------------------------------ Tempo

        /// <summary>Tempo's state changed: the window and the icon follow.</summary>
        private void OnTempoChanged()
        {
            if (_quitting || IsDisposed || _tempoDirty || _shots) return;
            // On the next turn, and once for changes that come together.
            _tempoDirty = true;
            BeginInvoke(new Action(delegate
            {
                _tempoDirty = false;
                if (_quitting || IsDisposed) return;
                Repaint();
                RefreshTray();
            }));
        }

        private void OpenTempoOrWindow()
        {
            // Nothing to open for an account the site just refused (or on a Windows that can't look): MapleStory's page says why.
            if (TempoShut)
            {
                if (Stage() == "main") Go("maple");
                ShowWindow();
                return;
            }
            if (!_tempo.Linked || !_tempo.Consent)
            {
                if (_tempo.Linked) _askAgain = true;
                Repaint();
                ShowWindow();
                return;
            }
            var open = _tempo.OpenTempo(false, false);
            GC.KeepAlive(open);
        }

        /// <summary>"Try again" on the page of an account the site refused Tempo: the site is asked now.</summary>
        private void CheckTempoAgain()
        {
            var check = _tempo.CheckAccess();
            GC.KeepAlive(check);
        }

        /// <summary>"Yes, Tempo may look": the player's yes is kept, and Tempo opens and starts.</summary>
        private void SayYes()
        {
            _askAgain = false;
            _tempo.GiveConsent();
            _page = "maple";
            Repaint();
            var open = _tempo.OpenTempo(true, false);
            GC.KeepAlive(open);
        }

        /// <summary>"No, not now": nothing is looked at; MapleStory's page and Settings lead back to the question.</summary>
        private void SayNo()
        {
            _askAgain = false;
            _settings.TempoAsked = true;
            _settings.Save();
            if (_settings.Maple) _page = "maple";
            Repaint();
        }

        /// <summary>The switch "Tempo may look at the MapleStory window" in Settings: off stops Tempo; on is the question again.</summary>
        private void ToggleConsent()
        {
            if (_tempo.Consent) _tempo.TakeConsent();
            else _askAgain = true;
            Repaint();
        }

        // ------------------------------------------------------------------------------------------------ settings

        private void ToggleStartWithWindows()
        {
            _settings.AutostartDecided = true;
            SetStartWithWindows(!_settings.StartWithWindows);
            Repaint();
        }

        private async void ToggleKeepAddon()
        {
            var on = !_settings.KeepAddonUpdated;
            _settings.KeepAddonUpdated = on;
            _settings.Save();
            _addon.KeepUpdated = on;
            Repaint();
            if (!string.IsNullOrEmpty(_settings.Token)) await _addon.Maintain(_scanner.WowFolder);
        }

        /// <summary>
        /// "Keep the program up to date". On: a version on offer goes in now, else the site is asked. Off while an update
        /// by itself waits to hand over: it waits for a click instead (the checked exe stays, so the click needs no second
        /// download).
        /// </summary>
        private async void ToggleKeepProgram()
        {
            var on = !_settings.KeepProgramUpdated;
            _settings.KeepProgramUpdated = on;
            _settings.Save();
            var offered = _offered;
            if (on) _offered = null;
            else if (_handOverTo != null && !_handOverClicked)
            {
                _handOver.Stop();
                _offered = _handOverRelease;
                _handOverTo = null;
            }
            Repaint();
            if (!on) return;
            if (offered != null) await InstallUpdate(offered, false);
            else await CheckForUpdate();
        }

        private void SetStartWithWindows(bool on)
        {
            _settings.StartWithWindows = on;
            _settings.Save();
            if (on)
            {
                WriteRunValue(Application.ExecutablePath);
                return;
            }
            if (Program.Isolated) return;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key != null) key.DeleteValue(RunValue, false);
                }
            }
            catch (Exception)
            {
                // No registry access: the switch stays, Windows just won't start it.
            }
        }

        /// <summary>
        /// At every start, the way Toasts.EnsureShortcut keeps the Start-menu shortcut: with "Start with Windows" on, the
        /// Run value is written again when it doesn't start this exe (every version has its own file name, so a new file
        /// would leave autostart on the old one; the program's own update points it at the new exe before starting it,
        /// PointAt). With it off nothing is written, and a test start against another site (--site) leaves it alone too.
        /// </summary>
        private void EnsureAutostart()
        {
            if (!_settings.StartWithWindows || _settings.Site != Program.DefaultSite) return;
            WriteRunValue(Application.ExecutablePath);
        }

        /// <summary>The Run value pointed at `exe`, written only when it says something else.</summary>
        private static void WriteRunValue(string exe)
        {
            // A test start in a folder of its own never touches autostart.
            if (Program.Isolated) return;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return;
                    var command = "\"" + exe + "\"";
                    if (string.Equals(key.GetValue(RunValue) as string, command, StringComparison.OrdinalIgnoreCase)) return;
                    key.SetValue(RunValue, command);
                }
            }
            catch (Exception)
            {
                // No registry access: the switch stays, Windows just won't start it.
            }
        }

        /// <summary>The languages' own names, for the list in Settings.</summary>
        internal static string LanguageName(string code)
        {
            switch (code)
            {
                case "nl": return "Nederlands";
                case "de": return "Deutsch";
                case "fr": return "Français";
                case "es": return "Español";
                case "pt-br": return "Português (Brasil)";
                default: return "English";
            }
        }

        /// <summary>The list of languages, opened under its button in Settings, in the menu's look.</summary>
        private void ShowLanguages(RectangleF button)
        {
            var lines = new List<TrayLine>();
            foreach (var code in Texts.Languages)
            {
                var picked = code;
                lines.Add(new TrayLine { Kind = code == Texts.Language ? "main" : "action", Name = code, Text = LanguageName(code), Do = delegate { PickLanguage(picked); } });
            }
            var menu = new ContextMenuStrip { ShowImageMargin = false, ShowCheckMargin = false, Renderer = new TrayRenderer() };
            TrayMenu.Fill(menu, lines);
            menu.Opened += delegate { TrayPaint.RoundCorners(menu.Handle); };
            menu.Closed += delegate { BeginInvoke(new Action(menu.Dispose)); };
            var zoom = _canvas.Zoom;
            menu.Show(_canvas, new Point((int)(button.Left * zoom), (int)((button.Bottom + 4) * zoom)));
        }

        // ------------------------------------------------------------------------------------------------ show, hide, quit

        private void ShowWindow()
        {
            if (_shots) return;
            _allowVisible = true;
            Show();
            WindowState = FormWindowState.Normal;
            if (!Program.TestOpen) Activate();
            _tempo.WindowShown();
            if (_linkPaused)
            {
                // The player is here: a fresh code, and the count starts over.
                _codeRenewals = 0;
                var flow = StartLinkFlow();
                GC.KeepAlive(flow);
            }
            Repaint();
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (_quitting || e.CloseReason != CloseReason.UserClosing) return;
            // The cross hides the window; the program goes on by the clock (Quit is in its menu).
            e.Cancel = true;
            _gamesEdit = false;
            _askAgain = false;
            _unlinkAsk = false;
            Hide();
        }

        private void Quit()
        {
            _quitting = true;
            if (_tray != null) _tray.Visible = false;
            _tempo.Dispose();
            _scanner.Dispose();
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Look.Changed -= OnLookChanged;
                if (_tray != null)
                {
                    var menu = _tray.ContextMenuStrip;
                    _tray.Dispose();
                    if (menu != null) menu.Dispose();
                    if (!ReferenceEquals(_trayIconOk, _trayIcon)) _trayIconOk.Dispose();
                    if (!ReferenceEquals(_trayIconDot, _trayIcon)) _trayIconDot.Dispose();
                    _trayIcon.Dispose();
                }
                _pollTimer.Dispose();
                _clock.Dispose();
                _maintain.Dispose();
                _afterUpload.Dispose();
                _retry.Dispose();
                _updateCheck.Dispose();
                _handOver.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
