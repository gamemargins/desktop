// The program's face: once linked it lives by the clock with the site's mark as its icon; a left click opens the small
// window (per WoW account the status, the addon, the last uploads, "Upload now", the line about the next upload), a
// right click the menu (TrayMenu.cs: Upload now, Open gamemargins.com, Start with Windows, Keep the addon up to date,
// Keep the program up to date, Quit, and "Update to x.y.z" on top while a newer version waits for a click); a
// notification follows each upload, a Windows toast that stays in the notification center (Toasts.cs; the balloon tips
// were lost to Do not disturb), and for ten minutes after a stored scan the tray icon carries a green check with
// "Uploaded 3 min ago · 240,910 auctions" as its tooltip (TrayBadge.cs), which names the program's version otherwise
// ("GameMargins Desktop 0.2.5"). The program's own update (Updater.cs) is driven from here: the checks, the menu's
// offer, and the hand-over to the new exe once no upload is under way. Before it is linked, the window shows the link card (a
// code, "Open gamemargins.com/link", waiting). Colours as the site's rules have them: gold only for the button and the
// brand, orange for watch out (a skipped scan, no folder), red for a failure, blue for information, grey for
// structure; "uploaded" is plain text in the window, and the check is green because green means done.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GameMargins.Uploader
{
    internal sealed class MainForm : Form
    {
        private static readonly Color Background = Color.FromArgb(0x12, 0x14, 0x18);
        private static readonly Color Surface = Color.FromArgb(0x1a, 0x1d, 0x23);
        private static readonly Color Border = Color.FromArgb(0x2a, 0x2e, 0x36);
        private static readonly Color Foreground = Color.FromArgb(0xe8, 0xea, 0xf0);
        private static readonly Color Muted = Color.FromArgb(0x9a, 0xa3, 0xb2);
        private static readonly Color Accent = Color.FromArgb(0xf5, 0xb8, 0x3d);
        private static readonly Color OnAccent = Color.FromArgb(0x14, 0x14, 0x14);
        private static readonly Color Warning = Color.FromArgb(0xf0, 0x8a, 0x24);
        private static readonly Color Negative = Color.FromArgb(0xe5, 0x48, 0x4d);
        private static readonly Color Info = Color.FromArgb(0x39, 0x87, 0xe5);

        private static readonly Font Body = new Font("Segoe UI", 10F);
        private static readonly Font Small = new Font("Segoe UI", 9F);
        private static readonly Font Heading = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        private static readonly Font Strong = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        private static readonly Font Code = new Font("Consolas", 26F);

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

        private readonly Settings _settings;
        private readonly Api _api;
        private readonly Scanner _scanner = new Scanner();
        private readonly Addon _addon;
        private readonly NotifyIcon _tray;
        private readonly Icon _trayIcon;
        /// <summary>The same icon with the green check (TrayBadge), for the ten minutes after a stored scan.</summary>
        private readonly Icon _trayIconOk;
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

        private Panel _root;
        private FlowLayoutPanel _accountsPanel;
        private Label _addonLabel;
        private TableLayoutPanel _uploadsTable;
        private Label _resultLabel;
        private Label _updateLabel;
        private Label _folderLabel;
        private Button _uploadButton;
        private Label _codeLabel;
        private Label _waitingLabel;
        private Button _openLinkButton;
        private Button _copyLinkButton;
        private readonly Dictionary<int, Label> _statusLabels = new Dictionary<int, Label>();

        private bool _allowVisible;
        private bool _quitting;
        private bool _hiddenOnce;
        private bool _mainView;
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
        /// <summary>A folder picker or message box is open: the hand-over to a new exe waits for it.</summary>
        private bool _dialogOpen;

        /// <summary>A newer version waiting for the player's click ("Keep the program up to date" off); null when there is none.</summary>
        private ProgramRelease _offered;
        private bool _updating;
        /// <summary>A version whose download did not match or whose exe would not start: not tried again until the next start.</summary>
        private string _skipVersion;
        /// <summary>The checked new exe this copy hands over to once nothing is under way, and for which version; null before.</summary>
        private string _handOverTo;
        private ProgramRelease _handOverRelease;
        /// <summary>Whether that update came from a click on "Update to x.y.z", which doesn't wait for an open window.</summary>
        private bool _handOverClicked;

        public MainForm(Settings settings, string updatedFrom)
        {
            _settings = settings;
            _updatedFrom = updatedFrom;
            _api = new Api(settings.Site);
            _addon = new Addon(_api) { KeepUpdated = settings.KeepAddonUpdated };
            _addon.Changed += OnAddonChanged;
            _updater = new Updater(new HttpUpdateWeb(), Settings.Folder, Program.Version);

            Text = Title;
            BackColor = Background;
            ForeColor = Foreground;
            Font = Body;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            ClientSize = new Size(620, 600);
            Icon = LoadIcon(32);

            var traySize = SystemInformation.SmallIconSize.Width;
            _trayIcon = LoadIcon(traySize);
            try { _trayIconOk = TrayBadge.WithCheck(_trayIcon, traySize); } catch (Exception) { _trayIconOk = _trayIcon; }
            _tray = new NotifyIcon { Icon = _trayIcon, Text = Title, Visible = true };
            _tray.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) ShowWindow(); };
            _tray.DoubleClick += delegate { ShowWindow(); };
            _tray.BalloonTipClicked += delegate { ShowWindow(); };
            BuildTrayMenu();
            RefreshTray();

            _pollTimer.Tick += async delegate { await PollOnce(); };
            _clock.Interval = 60000;
            _clock.Tick += delegate
            {
                RenderUploads();
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
            _scanner.AccountsChanged += delegate { RenderAccounts(); };

            FormClosing += OnClosing;

            // The window exists from the start (timers, Invoke) but shows only when something asks for it: a linked program
            // starts by the clock, an unlinked one shows the link card.
            CreateHandle();
            BeginInvoke(new Action(async delegate { await Begin(); }));
        }

        /// <summary>Hidden until ShowWindow: Application.Run's own Show() is held back this way.</summary>
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(_allowVisible && value);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DarkTitleBar();
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

        // ------------------------------------------------------------------------------------------------ notifications and the tray

        /// <summary>
        /// A notification that stays: a Windows toast in the notification center (Toasts.Show; also under Do not disturb,
        /// which hides the banner but keeps the notification), and the old balloon where the toast can't be made (a
        /// Windows without the runtime, a shortcut that could not be written). `toast` is its title and line, from
        /// ToastTexts (short enough to be read whole where the notification center shows it folded); `tag` names the kind
        /// for the center.
        /// </summary>
        private void Notify(string[] toast, string tag, ToolTipIcon fallbackIcon, int balloonMs)
        {
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
        /// The tray icon and its tooltip: for BadgeMinutes after a stored scan
        /// the icon with the green check and "Uploaded 3 min ago · 240,910 auctions"; then the plain icon and "Last
        /// upload: 14:23" (WhenText: "35 min ago" within the hour, "Yesterday 14:23", a date). Called after an upload and
        /// by the minute clock.
        /// </summary>
        private void RefreshTray()
        {
            var last = LastStored();
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

        // ------------------------------------------------------------------------------------------------ start-up

        private async Task Begin()
        {
            if (_updatedFrom != null) AfterUpdate();
            EnsureAutostart();
            EnsureWowFolder();
            if (string.IsNullOrEmpty(_settings.Token))
            {
                ShowLinkView(null);
                ShowWindow();
                await StartLinkFlow();
                return;
            }
            ShowMainView();
            var status = await _api.Status(_settings.Token);
            if (status.Status == "unlinked")
            {
                await Unlinked();
                return;
            }
            if (status.Status == "ok")
            {
                ApplyLocale(status.Locale);
                NoteVersion(status.Latest);
                _addon.Site = status.Addon;
            }
            else SetResult(Texts.T("Can't reach {0}; trying again later.", SiteHost()), Negative);
            await UploadAll(false);
            await _addon.Maintain(_scanner.WowFolder);
            _maintain.Start();
        }

        /// <summary>The install Settings knows, else the default one, else the player's pick (asked once; "Choose folder…" asks again).</summary>
        private void EnsureWowFolder()
        {
            var folder = Scanner.Accept(_settings.WowFolder) ?? Scanner.FindDefault();
            if (folder == null)
            {
                ShowWindow();
                folder = AskWowFolder();
            }
            if (folder == null) return;
            _settings.WowFolder = folder;
            _settings.Save();
            _scanner.Start(folder);
        }

        private string AskWowFolder()
        {
            _dialogOpen = true;
            try
            {
                using (var dialog = new FolderBrowserDialog { Description = Texts.T("Pick your World of Warcraft folder (the one that holds _forever_), or the _forever_ folder itself."), ShowNewFolderButton = false })
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return null;
                    var accepted = Scanner.Accept(dialog.SelectedPath);
                    if (accepted == null) MessageBox.Show(this, Texts.T("WoW folder not found."), Texts.T("Where is World of Warcraft?"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return accepted;
                }
            }
            finally
            {
                _dialogOpen = false;
            }
        }

        private void ApplyLocale(string locale)
        {
            var language = Texts.Pick(locale);
            if (locale != null && _settings.Locale != locale)
            {
                _settings.Locale = locale;
                _settings.Save();
            }
            if (language == Texts.Language) return;
            Texts.Language = language;
            BuildTrayMenu();
            if (_root == null) return;
            if (_mainView) ShowMainView();
            else ShowLinkView(null);
        }

        private void NoteVersion(string latest)
        {
            if (_updateLabel == null || string.IsNullOrEmpty(latest) || latest == Program.Version) return;
            Version mine, theirs;
            if (!Version.TryParse(Program.Version, out mine) || !Version.TryParse(latest, out theirs) || theirs <= mine) return;
            _updateLabel.Text = Texts.T("A newer version is available ({0}).", latest);
            _updateLabel.Visible = true;
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

        /// <summary>The link page with the code on the clipboard, built as Open builds it; "Copied" on the button a moment.</summary>
        private void CopyLink()
        {
            if (_link == null || _copyLinkButton == null) return;
            Uri url;
            if (!Uri.TryCreate(_settings.Site + "/link?code=" + Uri.EscapeDataString(_link.UserCode), UriKind.Absolute, out url)) return;
            try { Clipboard.SetText(url.AbsoluteUri); }
            catch (Exception) { return; }
            var button = _copyLinkButton;
            button.Text = Texts.T("Copied");
            var timer = new System.Windows.Forms.Timer { Interval = 2500 };
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();
                if (!button.IsDisposed) button.Text = Texts.T("Copy link");
            };
            timer.Start();
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
                if (_codeLabel != null) _codeLabel.Text = "· · ·";
                if (_openLinkButton != null) _openLinkButton.Enabled = false;
                if (_copyLinkButton != null) _copyLinkButton.Enabled = false;
                SetWaiting(Texts.T("Open this window for a new code."), Muted);
                return;
            }
            _linkPaused = false;
            _link = await _api.StartLink();
            if (_link == null || string.IsNullOrEmpty(_link.UserCode) || string.IsNullOrEmpty(_link.DeviceCode))
            {
                _link = null;
                if (_codeLabel != null) _codeLabel.Text = "· · ·";
                if (_openLinkButton != null) _openLinkButton.Enabled = false;
                if (_copyLinkButton != null) _copyLinkButton.Enabled = false;
                SetWaiting(Texts.T("Can't reach {0}; trying again later.", SiteHost()), Negative);
                _pollTimer.Interval = 15000;
                _pollTimer.Start();
                return;
            }
            if (_codeLabel != null) _codeLabel.Text = _link.UserCode;
            if (_openLinkButton != null) _openLinkButton.Enabled = true;
            if (_copyLinkButton != null) _copyLinkButton.Enabled = true;
            SetWaiting(Texts.T("Waiting…"), Muted);
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
                    if (_link != null)
                    {
                        _codeRenewals++;
                        SetWaiting(Texts.T("The code expired; here is a new one."), Warning);
                    }
                    await StartLinkFlow();
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
                            SetWaiting(Texts.T("The code expired; here is a new one."), Warning);
                            await StartLinkFlow();
                            return;
                        }
                        _pollTimer.Stop();
                        _codeRenewals = 0;
                        _settings.Token = result.Token;
                        _settings.Save();
                        _link = null;
                        ApplyLocale(result.Locale);
                        if (!_settings.AutostartDecided)
                        {
                            // On from the first link; the menu turns it off.
                            _settings.AutostartDecided = true;
                            SetStartWithWindows(true);
                            BuildTrayMenu();
                        }
                        await Linked();
                        break;
                    case "expired":
                    case "unknown":
                        _codeRenewals++;
                        SetWaiting(Texts.T("The code expired; here is a new one."), Warning);
                        await StartLinkFlow();
                        break;
                    case "offline":
                        SetWaiting(Texts.T("Can't reach {0}; trying again later.", SiteHost()), Negative);
                        break;
                    default:
                        SetWaiting(Texts.T("Waiting…"), Muted);
                        break;
                }
            }
            finally
            {
                _polling = false;
            }
        }

        /// <summary>Just linked: the main view, the site's versions, the first upload, the addon.</summary>
        private async Task Linked()
        {
            ShowMainView();
            var status = await _api.Status(_settings.Token);
            if (status.Status == "ok")
            {
                NoteVersion(status.Latest);
                _addon.Site = status.Addon;
            }
            await UploadAll(false);
            await _addon.Maintain(_scanner.WowFolder);
            _maintain.Start();
        }

        /// <summary>The site refused the token (unlinked on Account › Security): back to the link card, and say so even by the clock.</summary>
        private async Task Unlinked()
        {
            _settings.Token = null;
            _settings.Save();
            _maintain.Stop();
            _retry.Stop();
            ShowLinkView(Texts.T("This program was unlinked on the site. Link it again with the code below."));
            Notify(ToastTexts.Unlinked(), "unlinked", ToolTipIcon.Warning, 8000);
            ShowWindow();
            await StartLinkFlow();
        }

        private void SetWaiting(string text, Color colour)
        {
            if (_waitingLabel == null) return;
            _waitingLabel.Text = text;
            _waitingLabel.ForeColor = colour;
        }

        // ------------------------------------------------------------------------------------------------ uploading

        /// <summary>
        /// "Trying again later" made true: an upload that could not reach the site (no network, the site down) tries again
        /// by itself, after 1, 2, 4, 8 and then every 15 minutes, until one goes through (before 0.2.3 it waited for the
        /// next file write). A file write or Upload now still goes
        /// at once; a 429's wait (_retryAfterUtc) stays in charge when there is one.
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
            if (string.IsNullOrEmpty(_settings.Token) || !account.Exists) return;
            if (_inFlight.Contains(account.Number))
            {
                // The game wrote the file while its last write is still being sent: once more after this one.
                _again.Add(account.Number);
                return;
            }
            if (!force && _retryAfterUtc > DateTime.UtcNow)
            {
                SetResult(Texts.T("Too many uploads; the next try is in {0} min.", Math.Max(1, (int)Math.Ceiling((_retryAfterUtc - DateTime.UtcNow).TotalMinutes))), Info);
                ScheduleRetry();
                return;
            }
            _inFlight.Add(account.Number);
            _uploadsRunning++;
            if (_uploadButton != null) _uploadButton.Enabled = false;
            SetStatus(account, Texts.T("uploading…"), Muted);
            try
            {
                var text = await Scanner.ReadWhenSettled(account.File);
                if (text == null)
                {
                    SetStatus(account, Texts.T("upload failed"), Negative);
                    SetResult(Texts.T("Upload failed: {0}", Texts.T("Could not read the file; the game may still be writing it.")), Negative);
                    return;
                }
                var fingerprint = Scanner.Fingerprint(text);
                if (!force && _settings.Uploaded.Contains(fingerprint))
                {
                    SetStatus(account, Texts.T("uploaded · watching"), Muted);
                    SetResult(Texts.T("Nothing new in the file: these scans were uploaded before."), Muted);
                    return;
                }
                var result = await _api.Upload(_settings.Token, text);
                switch (result.Status)
                {
                    case "ok":
                        _offlineTries = 0;
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
                        SetStatus(account, Texts.T("uploaded · watching"), Muted);
                        RenderUploads();
                        // The green check and the tooltip for the next ten minutes.
                        RefreshTray();
                        if (first != null)
                        {
                            SetResult(Texts.T("Uploaded: {0} auctions · {1}", first.Auctions.ToString("N0", Texts.Culture), MarketLabel(first.Market)), Foreground);
                            Notify(ToastTexts.Uploaded(first.Auctions, first.Market), "upload", ToolTipIcon.None, 4000);
                        }
                        else if (skipped != null) SetResult(Texts.T("Skipped: {0}", RefusalText(skipped.Code, skipped.Why)), Warning);
                        // No scan stored, nothing skipped: the file went for its characters (a level-up, a skill-up), or for nothing.
                        else if (result.Characters > 0) SetResult(Texts.T("Characters up to date on the site: {0}.", result.Characters), Muted);
                        else SetResult(Texts.T("Nothing new in the file: these scans were uploaded before."), Muted);
                        break;
                    case "unlinked":
                        await Unlinked();
                        break;
                    case "refused":
                        if (result.RetryAfter > 0)
                        {
                            _retryAfterUtc = DateTime.UtcNow.AddSeconds(result.RetryAfter);
                            SetStatus(account, Texts.T("file found · watching"), Muted);
                            SetResult(Texts.T("Too many uploads; the next try is in {0} min.", Math.Max(1, (int)Math.Ceiling(result.RetryAfter / 60.0))), Info);
                            ScheduleRetry();
                        }
                        else
                        {
                            var why = Texts.T("Upload failed: {0}", RefusalText(result.Code, result.Error));
                            SetStatus(account, Texts.T("upload failed"), Negative);
                            SetResult(why, Negative);
                            Notify(ToastTexts.UploadFailed(result.Code), "failed", ToolTipIcon.Error, 5000);
                        }
                        break;
                    default:
                        SetStatus(account, Texts.T("upload failed"), Negative);
                        SetResult(Texts.T("Can't reach {0}; trying again later.", SiteHost()), Negative);
                        RetryWhenOnline();
                        break;
                }
            }
            finally
            {
                _inFlight.Remove(account.Number);
                _uploadsRunning--;
                if (_uploadButton != null) _uploadButton.Enabled = _uploadsRunning == 0;
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

        private string WhenText(DateTime atUtc)
        {
            var at = atUtc.ToLocalTime();
            var now = DateTime.Now;
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

        // ------------------------------------------------------------------------------------------------ the addon

        private void OnAddonChanged(AddonStatus status)
        {
            if (_addonLabel != null)
            {
                _addonLabel.Text = status.Text;
                _addonLabel.ForeColor = status.Mark == AddonMark.Failed ? Negative : status.Mark == AddonMark.Info ? Info : status.Mark == AddonMark.Muted ? Muted : Foreground;
            }
            if (status.JustInstalled != null)
            {
                Notify(ToastTexts.Addon(status.JustInstalled, status.WasUpdate), "addon", ToolTipIcon.None, 5000);
            }
        }

        // ------------------------------------------------------------------------------------------------ the program's own update

        /// <summary>Only a start that talks to our own site updates itself; a test start against another one (--site) never does.</summary>
        private bool SelfUpdating
        {
            get { return _settings.Site == Program.DefaultSite; }
        }

        /// <summary>
        /// Asks the site for its newest version (Updater.Check). A newer one is installed by itself with "Keep the program up
        /// to date" ticked, else offered in the menu as "Update to x.y.z". A site that can't be reached: the next check.
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
            if (found == null || found.Version == _skipVersion) return;
            if (_settings.KeepProgramUpdated) await InstallUpdate(found, false);
            else Offer(found);
        }

        /// <summary>"Update to x.y.z" in bold on top of the menu, and once per version the notification that says where it is.</summary>
        private void Offer(ProgramRelease release)
        {
            var changed = _offered == null || _offered.Version != release.Version;
            _offered = release;
            if (changed) BuildTrayMenu();
            if (_settings.UpdateNoted == release.Version) return;
            _settings.UpdateNoted = release.Version;
            _settings.Save();
            Notify(ToastTexts.UpdateReady(release.Version), "update", ToolTipIcon.Info, 6000);
        }

        private void DropOffer()
        {
            if (_offered == null) return;
            _offered = null;
            BuildTrayMenu();
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
            HandOverWhenIdle();
        }

        /// <summary>
        /// Hands over to the checked new exe once nothing is under way: no upload, no addon install, no folder dialog, and
        /// for an update by itself no open window either (a click on "Update to" doesn't wait for that). Autostart (while
        /// ticked) and the Start-menu shortcut point at the new exe, it starts with --updated-from naming this one, and
        /// this copy quits. When Windows won't start it, both point back here and this copy keeps running.
        /// </summary>
        private void HandOverWhenIdle()
        {
            _handOver.Stop();
            if (_handOverTo == null || _quitting) return;
            if (!Updater.MayHandOver(_uploadsRunning, _addon.Busy, _dialogOpen, Visible, _handOverClicked))
            {
                _handOver.Start();
                return;
            }
            var exe = _handOverTo;
            PointAt(exe);
            if (Updater.Start(exe, "--updated-from " + Updater.Quote(Application.ExecutablePath)))
            {
                Quit();
                return;
            }
            PointAt(Application.ExecutablePath);
            _skipVersion = _handOverRelease.Version;
            _handOverTo = null;
            DropOffer();
            Notify(ToastTexts.UpdateFailed(_handOverRelease.Version), "update", ToolTipIcon.Warning, 6000);
        }

        /// <summary>Autostart (only while "Start with Windows" is ticked, as at every start) and the Start-menu shortcut follow `exe`.</summary>
        private void PointAt(string exe)
        {
            if (_settings.StartWithWindows) WriteRunValue(exe);
            Toasts.EnsureShortcut(exe);
        }

        /// <summary>
        /// This copy was started by the update of an older one (--updated-from): it says so once, and removes the old exe
        /// when that one was installed next to the settings too (a copy elsewhere is the player's and stays).
        /// </summary>
        private void AfterUpdate()
        {
            Notify(ToastTexts.Updated(Program.Version, _settings.KeepProgramUpdated), "updated", ToolTipIcon.None, 6000);
            var removal = Updater.RemoveReplaced(Settings.Folder, _updatedFrom, Application.ExecutablePath);
            GC.KeepAlive(removal);
        }

        // ------------------------------------------------------------------------------------------------ the views

        private void ResetRoot()
        {
            SuspendLayout();
            Controls.Clear();
            _accountsPanel = null;
            _addonLabel = null;
            _uploadsTable = null;
            _resultLabel = null;
            _updateLabel = null;
            _folderLabel = null;
            _uploadButton = null;
            _codeLabel = null;
            _waitingLabel = null;
            _openLinkButton = null;
            _copyLinkButton = null;
            _statusLabels.Clear();
            _root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 20), BackColor = Background, AutoScroll = true };
            Controls.Add(_root);
            ResumeLayout();
        }

        private static Label HeadingLabel(string text)
        {
            return new Label { Text = text.ToUpperInvariant(), Font = Heading, ForeColor = Muted, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        }

        private void ShowLinkView(string notice)
        {
            _mainView = false;
            ResetRoot();
            var column = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = Background };
            column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            column.Controls.Add(HeadingLabel(Texts.T("Link this program to your account")));

            if (notice != null) column.Controls.Add(new Label { Text = notice, ForeColor = Warning, AutoSize = true, MaximumSize = new Size(560, 0), Margin = new Padding(0, 0, 0, 12) });

            var box = new Panel { Height = 96, Dock = DockStyle.Top, BackColor = Background, Margin = new Padding(0, 4, 0, 16) };
            box.Paint += delegate(object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Muted, 1F) { DashStyle = DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, box.Width - 1, box.Height - 1);
                }
            };
            _codeLabel = new Label { Text = _link != null ? _link.UserCode : "· · ·", Font = Code, ForeColor = Foreground, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            box.Controls.Add(_codeLabel);
            column.Controls.Add(box);

            column.Controls.Add(new Label
            {
                Text = Texts.T("Go to {0}, log in as you always do and enter this code. The code works for 10 minutes; no password or mail code goes into this program.", LinkAddress()),
                ForeColor = Muted,
                AutoSize = true,
                MaximumSize = new Size(560, 0),
                Margin = new Padding(0, 0, 0, 16),
            });

            var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Background, Margin = new Padding(0) };
            _openLinkButton = GreyButton(Texts.T("Open {0}", LinkAddress()));
            _openLinkButton.Enabled = _link != null;
            _openLinkButton.Click += delegate
            {
                // Built here from our own site and the code: nothing the server sent is run.
                if (_link != null) OpenSite("/link?code=" + Uri.EscapeDataString(_link.UserCode));
            };
            row.Controls.Add(_openLinkButton);
            // The same link for the clipboard, for a browser the program does not open by itself.
            _copyLinkButton = GreyButton(Texts.T("Copy link"));
            _copyLinkButton.Enabled = _link != null;
            _copyLinkButton.Margin = new Padding(8, 0, 0, 0);
            _copyLinkButton.Click += delegate { CopyLink(); };
            row.Controls.Add(_copyLinkButton);
            _waitingLabel = new Label { Text = _linkPaused ? Texts.T("Open this window for a new code.") : Texts.T("Waiting…"), ForeColor = Muted, AutoSize = true, Margin = new Padding(12, 10, 0, 0) };
            row.Controls.Add(_waitingLabel);
            column.Controls.Add(row);

            _root.Controls.Add(column);
        }

        private void ShowMainView()
        {
            _mainView = true;
            ResetRoot();
            var column = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = Background };
            column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            column.Controls.Add(HeadingLabel(Texts.T("WoW accounts")));
            _accountsPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Top, BackColor = Background, Margin = new Padding(0, 0, 0, 4) };
            column.Controls.Add(_accountsPanel);
            _folderLabel = new Label { ForeColor = Warning, AutoSize = true, Visible = false, Margin = new Padding(0, 0, 0, 12) };
            column.Controls.Add(_folderLabel);

            // The addon's row, in the accounts' shape: what is installed and what the program will do about it.
            var installed = _scanner.WowFolder != null ? Addon.InstalledVersion(_scanner.WowFolder) : null;
            var addonRow = Row(Texts.T("GameMargins addon"), installed ?? Texts.T("no scans yet"), Muted, out _addonLabel);
            if (installed == null) _addonLabel.Text = _settings.KeepAddonUpdated ? Texts.T("not installed · installs when WoW is closed") : Texts.T("not installed · you install it yourself");
            else if (!_settings.KeepAddonUpdated) _addonLabel.Text = Texts.T("{0} · you keep it up to date yourself", installed);
            else _addonLabel.Text = installed;
            addonRow.Margin = new Padding(0, 0, 0, 16);
            column.Controls.Add(addonRow);

            column.Controls.Add(HeadingLabel(Texts.T("Last uploads")));
            _uploadsTable = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 3, BackColor = Background, Margin = new Padding(0, 0, 0, 12) };
            _uploadsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
            _uploadsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
            _uploadsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            column.Controls.Add(_uploadsTable);

            _resultLabel = new Label { ForeColor = Muted, AutoSize = true, MaximumSize = new Size(560, 0), Margin = new Padding(0, 0, 0, 12), Text = "" };
            column.Controls.Add(_resultLabel);

            var bottom = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, BackColor = Background, Margin = new Padding(0, 4, 0, 0) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _uploadButton = new Button
            {
                Text = Texts.T("Upload now"),
                Font = Strong,
                BackColor = Accent,
                ForeColor = OnAccent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(170, 56),
                Margin = new Padding(0, 0, 16, 0),
                Cursor = Cursors.Hand,
            };
            _uploadButton.FlatAppearance.BorderSize = 0;
            _uploadButton.Enabled = _uploadsRunning == 0;
            _uploadButton.Click += async delegate { await UploadAll(true); };
            bottom.Controls.Add(_uploadButton, 0, 0);
            bottom.Controls.Add(new Label { Text = Texts.T("Next: as soon as the game writes the file (log out or /reload)."), ForeColor = Muted, AutoSize = true, MaximumSize = new Size(360, 0), Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 0, 0) }, 1, 0);
            column.Controls.Add(bottom);

            _updateLabel = new Label { ForeColor = Info, AutoSize = true, Visible = false, Margin = new Padding(0, 16, 0, 0) };
            column.Controls.Add(_updateLabel);

            _root.Controls.Add(column);
            RenderAccounts();
            RenderUploads();
        }

        /// <summary>A bordered row with a name on the left and a status on the right, as the accounts and the addon are shown.</summary>
        private static Panel Row(string name, string status, Color statusColour, out Label statusLabel)
        {
            var row = new Panel { Width = 560, Height = 46, BackColor = Surface, Margin = new Padding(0, 0, 0, 8) };
            row.Paint += delegate(object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Border)) e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
            };
            row.Controls.Add(new Label { Text = name, ForeColor = Foreground, AutoSize = true, Location = new Point(14, 12) });
            statusLabel = new Label
            {
                Text = status,
                ForeColor = statusColour,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Size = new Size(360, 46),
                Location = new Point(560 - 360 - 14, 0),
            };
            row.Controls.Add(statusLabel);
            return row;
        }

        private void RenderAccounts()
        {
            if (_accountsPanel == null) return;
            _accountsPanel.SuspendLayout();
            _accountsPanel.Controls.Clear();
            _statusLabels.Clear();
            if (_scanner.WowFolder == null)
            {
                _folderLabel.Text = Texts.T("WoW folder not found.");
                _folderLabel.Visible = true;
                var choose = GreyButton(Texts.T("Choose folder…"));
                choose.Click += async delegate
                {
                    var picked = AskWowFolder();
                    if (picked == null) return;
                    _settings.WowFolder = picked;
                    _settings.Save();
                    _scanner.Start(picked);
                    RenderAccounts();
                    await UploadAll(false);
                    await _addon.Maintain(_scanner.WowFolder);
                };
                _accountsPanel.Controls.Add(choose);
            }
            else
            {
                _folderLabel.Visible = false;
                foreach (var account in _scanner.Accounts)
                {
                    Label status;
                    var row = Row(Texts.T("WoW account {0}", account.Number) + " · Forever", account.Exists ? Texts.T("file found · watching") : Texts.T("no scans yet"), Muted, out status);
                    _statusLabels[account.Number] = status;
                    _accountsPanel.Controls.Add(row);
                }
                if (_scanner.Accounts.Count == 0) _accountsPanel.Controls.Add(new Label { Text = Texts.T("no scans yet"), ForeColor = Muted, AutoSize = true, Margin = new Padding(0, 0, 0, 8) });
            }
            _accountsPanel.ResumeLayout();
        }

        private void SetStatus(WowAccount account, string text, Color colour)
        {
            Label label;
            if (!_statusLabels.TryGetValue(account.Number, out label)) return;
            label.Text = text;
            label.ForeColor = colour;
        }

        private void SetResult(string text, Color colour)
        {
            if (_resultLabel == null) return;
            _resultLabel.Text = text;
            _resultLabel.ForeColor = colour;
        }

        private void RenderUploads()
        {
            if (_uploadsTable == null) return;
            _uploadsTable.SuspendLayout();
            _uploadsTable.Controls.Clear();
            _uploadsTable.RowStyles.Clear();
            _uploadsTable.RowCount = 0;
            var header = new[] { Texts.T("When"), Texts.T("Auction house"), Texts.T("Auctions") };
            for (var c = 0; c < 3; c++)
            {
                _uploadsTable.Controls.Add(new Label { Text = header[c], ForeColor = Muted, Font = Small, AutoSize = true, Dock = DockStyle.Fill, TextAlign = c == 2 ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft, Margin = new Padding(4, 0, 4, 6) }, c, 0);
            }
            if (_settings.Uploads.Count == 0)
            {
                var none = new Label { Text = Texts.T("No uploads yet. Scan the auction house in the game, then log out or type /reload: the upload starts by itself."), ForeColor = Muted, AutoSize = true, MaximumSize = new Size(540, 0), Margin = new Padding(4, 4, 4, 4) };
                _uploadsTable.Controls.Add(none, 0, 1);
                _uploadsTable.SetColumnSpan(none, 3);
            }
            else
            {
                var r = 1;
                foreach (var row in _settings.Uploads)
                {
                    var cells = new[] { WhenText(row.At), MarketLabel(row.Market), row.Auctions.ToString("N0", Texts.Culture) };
                    for (var c = 0; c < 3; c++)
                    {
                        var cell = new Label { Text = cells[c], ForeColor = Foreground, AutoSize = true, Dock = DockStyle.Fill, TextAlign = c == 2 ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft, Margin = new Padding(4, 6, 4, 6) };
                        if (r > 1) cell.Paint += delegate(object s, PaintEventArgs e) { using (var pen = new Pen(Border)) e.Graphics.DrawLine(pen, 0, 0, cell.Width, 0); };
                        _uploadsTable.Controls.Add(cell, c, r);
                    }
                    r++;
                }
            }
            _uploadsTable.ResumeLayout();
        }

        private static Button GreyButton(string text)
        {
            var b = new Button { Text = text, ForeColor = Foreground, BackColor = Surface, FlatStyle = FlatStyle.Flat, AutoSize = true, Padding = new Padding(10, 6, 10, 6), Cursor = Cursors.Hand, Margin = new Padding(0) };
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Border;
            return b;
        }

        // ------------------------------------------------------------------------------------------------ the tray

        /// <summary>The menu (TrayMenu.cs) from the settings and the version on offer; again whenever one of them changes.</summary>
        private void BuildTrayMenu()
        {
            var state = new TrayMenuState
            {
                Host = SiteHost(),
                StartWithWindows = _settings.StartWithWindows,
                KeepAddonUpdated = _settings.KeepAddonUpdated,
                KeepProgramUpdated = _settings.KeepProgramUpdated,
                UpdateTo = _offered != null ? _offered.Version : null,
            };
            var actions = new TrayMenuActions
            {
                Update = async delegate
                {
                    var release = _offered;
                    if (release != null) await InstallUpdate(release, true);
                },
                UploadNow = async delegate { await UploadAll(true); },
                OpenSite = delegate { OpenSite("/wow-forever"); },
                Quit = Quit,
                StartWithWindows = delegate(bool on)
                {
                    _settings.AutostartDecided = true;
                    SetStartWithWindows(on);
                },
                KeepAddonUpdated = async delegate(bool on)
                {
                    _settings.KeepAddonUpdated = on;
                    _settings.Save();
                    _addon.KeepUpdated = on;
                    if (!string.IsNullOrEmpty(_settings.Token)) await _addon.Maintain(_scanner.WowFolder);
                },
                KeepProgramUpdated = async delegate(bool on)
                {
                    _settings.KeepProgramUpdated = on;
                    _settings.Save();
                    // Ticked: a version on offer goes in now, else the site is asked; the "Update to" line leaves the menu.
                    // Unticked while an update by itself waits to hand over: it waits for a click instead (the checked
                    // exe stays, so the click needs no second download). The menu is built again once this click is done.
                    var offered = _offered;
                    if (on) _offered = null;
                    else if (_handOverTo != null && !_handOverClicked)
                    {
                        _handOver.Stop();
                        _offered = _handOverRelease;
                        _handOverTo = null;
                    }
                    BeginInvoke(new Action(BuildTrayMenu));
                    if (!on) return;
                    if (offered != null) await InstallUpdate(offered, false);
                    else await CheckForUpdate();
                },
            };
            var menu = TrayMenu.Build(state, actions);
            var old = _tray.ContextMenuStrip;
            _tray.ContextMenuStrip = menu;
            if (old != null) old.Dispose();
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
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key != null) key.DeleteValue(RunValue, false);
                }
            }
            catch (Exception)
            {
                // No registry access: the tick stays, Windows just won't start it.
            }
        }

        /// <summary>
        /// At every start, the way Toasts.EnsureShortcut keeps the Start-menu shortcut: with "Start with Windows" on, the
        /// Run value is written again when it doesn't start this exe (every version has its own file name,
        /// GameMarginsDesktop-0.2.5.exe, so a new file would leave autostart on the old one; the program's own update
        /// points it at the new exe before starting it, PointAt). With it off nothing is written, and a test start against
        /// another site (--site) leaves it alone too.
        /// </summary>
        private void EnsureAutostart()
        {
            if (!_settings.StartWithWindows || _settings.Site != Program.DefaultSite) return;
            WriteRunValue(Application.ExecutablePath);
        }

        /// <summary>The Run value pointed at `exe`, written only when it says something else.</summary>
        private static void WriteRunValue(string exe)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return;
                    var command = RunCommand(exe);
                    if (string.Equals(key.GetValue(RunValue) as string, command, StringComparison.OrdinalIgnoreCase)) return;
                    key.SetValue(RunValue, command);
                }
            }
            catch (Exception)
            {
                // No registry access: the tick stays, Windows just won't start it.
            }
        }

        /// <summary>What the Run value holds: the exe, quoted.</summary>
        private static string RunCommand(string exe)
        {
            return "\"" + exe + "\"";
        }

        private void ShowWindow()
        {
            _allowVisible = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            if (_linkPaused)
            {
                // The player is here: a fresh code, and the count starts over.
                _codeRenewals = 0;
                var flow = StartLinkFlow();
                GC.KeepAlive(flow);
            }
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (_quitting || e.CloseReason != CloseReason.UserClosing) return;
            e.Cancel = true;
            Hide();
            if (_hiddenOnce) return;
            _hiddenOnce = true;
            Notify(ToastTexts.StillRunning(), "clock", ToolTipIcon.None, 5000);
        }

        private void Quit()
        {
            _quitting = true;
            _tray.Visible = false;
            _scanner.Dispose();
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tray.Dispose();
                if (!ReferenceEquals(_trayIconOk, _trayIcon)) _trayIconOk.Dispose();
                _trayIcon.Dispose();
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

        // ------------------------------------------------------------------------------------------------ Windows

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        /// <summary>A dark title bar on Windows 10 (1809 and later) and 11, like the rest of the window; nothing happens where the call is unknown.</summary>
        private void DarkTitleBar()
        {
            try
            {
                var on = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref on, sizeof(int)) != 0) DwmSetWindowAttribute(Handle, 19, ref on, sizeof(int));
            }
            catch (Exception)
            {
                // Older Windows: a light title bar.
            }
        }
    }
}
