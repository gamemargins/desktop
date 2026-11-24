// Tempo's side of the program, for MapleStory Classic: linking it to the account with a code (its own token, apart from
// the WoW one), the Tempo window (TempoWindow.cs) and the float (FloatWindow.cs), "Start Tempo with MapleStory", and
// what the main window's pages and the menu by the clock show of it. The page in the Tempo window owns the session (start, stop,
// reset, the numbers); this class passes the buttons' presses on to it and shows what the page says.
//
// The program looks for the game's window by itself only while "Start Tempo with MapleStory" is on (a switch in Tempo's own Settings) or the Tempo
// window is open, every few seconds, by the window's title (Capture.cs). The switch is off until the player sets it.
//
// The site hands out the address of Tempo's page at every opening (Api.TempoStart). Should it refuse an account that
// is linked (403; Tempo is for every account, so this is not expected), that is remembered until the program closes
// or the site says yes again: MapleStory's page says so plainly and offers "Try again" (CheckAccess), and nothing of
// Tempo opens meanwhile.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    internal sealed class Tempo : IDisposable
    {
        private static readonly Regex TempoToken = new Regex("^gm_mt_[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant);
        /// <summary>The Tempo page's path on the site; the site's start route names the whole address.</summary>
        public const string PagePath = "/maplestory-classic/tempo-desktop";
        private const int CodeRenewalsUnseen = 2;

        private readonly Settings _settings;
        private readonly Api _api;
        private readonly Timer _poll = new Timer();
        private readonly Timer _look = new Timer();
        private TempoWindow _window;
        private FloatWindow _float;
        private FloatValues _floatValues;
        private bool _polling;
        private int _codeRenewals;
        private bool _opening;
        private bool _startWhenReady;
        private IntPtr _startedFor = IntPtr.Zero;
        private bool _disposed;
        private bool _asking;
        /// <summary>The site answered 403 to opening Tempo for this account (set by the start route's answer; a picture sets it too).</summary>
        public bool SiteRefused;

        /// <summary>Something the window or the menu shows changed.</summary>
        public Action Changed;
        /// <summary>The main window should come up (the program was unlinked on the site).</summary>
        public Action Attention;
        /// <summary>Whether the main window is in view, so a code nobody sees is not renewed for ever.</summary>
        public Func<bool> WindowVisible;
        /// <summary>The first link of the program: the caller switches autostart on, as for WoW.</summary>
        public Action FirstLink;

        /// <summary>The code being shown, or null.</summary>
        public LinkStart Link;
        public bool LinkPaused;
        /// <summary>The line next to the code ("Waiting…") and whether it is a warning or a failure ("", "warning", "negative").</summary>
        public string LinkLine = "";
        public string LinkTone = "";
        /// <summary>A line above the code, after the site unlinked the program.</summary>
        public string LinkNotice;
        /// <summary>A line in the window about opening Tempo that did not work, or null.</summary>
        public string Problem;
        public string ProblemTone = "";
        /// <summary>Windows has no WebView2 runtime: MapleStory's page offers Microsoft's download.</summary>
        public bool RuntimeMissing;
        /// <summary>The page's session: idle, reading or paused; and whether it holds numbers.</summary>
        public string SessionState = "idle";
        public bool Numbers;
        /// <summary>Whether the game's window was found at the last look; null while the program is not looking.</summary>
        public bool? GameFound;
        /// <summary>The session's seconds as the page said them last, and when it said so.</summary>
        public double SessionSeconds;
        public DateTime SessionSaid;
        public bool FloatOpen { get { return _float != null && _float.Open; } }
        /// <summary>The site is being asked again whether it opens Tempo for this account ("Try again" waits).</summary>
        public bool Asking { get { return _asking; } }
        /// <summary>What a pressed "Try again" could not do (the site out of reach), or null.</summary>
        public string CheckNote;
        /// <summary>This Windows can't hand over a window's picture (older than Windows 10 version 1903): Tempo can't look here.</summary>
        public bool CantLook;

        public Tempo(Settings settings, Api api)
        {
            _settings = settings;
            _api = api;
            _poll.Tick += async delegate { await PollOnce(); };
            _look.Interval = 3000;
            _look.Tick += delegate { Look(); };
            CantLook = !CaptureSupport.Works;
        }

        public bool Linked { get { return !string.IsNullOrEmpty(_settings.TempoToken); } }
        public bool Watching { get { return _window != null && _window.Watching; } }
        public bool WindowOpen { get { return _window != null; } }
        /// <summary>A session the player would lose: Tempo reads, or is paused with numbers. The window's cross only hides it then, and the program does not update itself.</summary>
        public bool SessionLive { get { return Watching || SessionState == "reading" || (SessionState == "paused" && Numbers); } }
        public bool Consent { get { return _settings.TempoConsent || Program.TestOpen; } }
        /// <summary>The site refused to open Tempo for this account just now. Not kept between starts: a new start asks afresh.</summary>
        public bool Refused { get { return Linked && SiteRefused; } }

        /// <summary>Whether `site` is a site on this computer (a test server): http or https on localhost or 127.0.0.1.</summary>
        public static bool IsLoopback(string site)
        {
            Uri u;
            if (string.IsNullOrEmpty(site) || !Uri.TryCreate(site, UriKind.Absolute, out u)) return false;
            return (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps) && (u.Host == "localhost" || u.Host == "127.0.0.1");
        }

        /// <summary>Tempo talks to gamemargins.com, or to a test server on this computer; to nothing else.</summary>
        public bool SiteAllowed
        {
            get { return _settings.Site == Program.DefaultSite || IsLoopback(_settings.Site); }
        }

        /// <summary>A line for %LOCALAPPDATA%\GameMargins\Uploader\tempo.log: what went wrong, never anything of a picture.</summary>
        public static void Log(string line)
        {
            try
            {
                var file = Path.Combine(Settings.Folder, "tempo.log");
                Directory.CreateDirectory(Settings.Folder);
                if (File.Exists(file) && new FileInfo(file).Length > 256 * 1024) File.Delete(file);
                File.AppendAllText(file, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + "\r\n");
            }
            catch (Exception) { }
        }

        private void Tell()
        {
            if (Changed != null && !_disposed) Changed();
        }

        // ------------------------------------------------------------------------------------------------ start and stop

        /// <summary>MapleStory was turned on (at start, or in "Which games do you play?").</summary>
        public async Task Begin()
        {
            if (!_settings.Maple || !SiteAllowed) return;
            _look.Start();
            if (!Linked)
            {
                await StartLinkFlow();
                return;
            }
            if (Program.TestOpen) await OpenTempo(true, true);
        }

        /// <summary>
        /// "Try again" after the site refused to open Tempo for this account: asks the start route once more. Yes or no
        /// is taken; a site out of reach changes nothing and is said under the card.
        /// </summary>
        public async Task CheckAccess()
        {
            if (!_settings.Maple || !Linked || !SiteAllowed || _asking || _disposed) return;
            _asking = true;
            CheckNote = null;
            Tell();
            try
            {
                var answer = await _api.TempoStart(_settings.TempoToken);
                if (_disposed || !Linked) return;
                if (answer.Status == "unlinked") OnUnauthorized();
                else if (answer.Status == "refused") SetRefused(true);
                else if (answer.Status == "ok") SetRefused(false);
                else CheckNote = Texts.T("Can't reach {0}; trying again later.", Host());
            }
            finally
            {
                _asking = false;
                Tell();
            }
        }

        /// <summary>Takes the site's yes or no. A no closes what of Tempo is open, unless a session runs (its numbers are the player's to keep first).</summary>
        private void SetRefused(bool refused)
        {
            if (SiteRefused == refused) return;
            SiteRefused = refused;
            Problem = null;
            if (refused && !SessionLive) CloseWindow();
            Look();
        }

        /// <summary>MapleStory was turned off: everything of Tempo closes; the link to the account stays.</summary>
        public void End()
        {
            _poll.Stop();
            _look.Stop();
            Link = null;
            GameFound = null;
            CloseWindow();
            Tell();
        }

        // ------------------------------------------------------------------------------------------------ linking

        public string LinkAddress(string host)
        {
            return host + "/link";
        }

        public async Task StartLinkFlow()
        {
            _poll.Stop();
            if (_codeRenewals >= CodeRenewalsUnseen && WindowVisible != null && !WindowVisible())
            {
                LinkPaused = true;
                Link = null;
                SetLinkLine(Texts.T("Open this window for a new code."), "");
                return;
            }
            LinkPaused = false;
            Link = await _api.StartLink(Api.TempoLink);
            if (Link == null || string.IsNullOrEmpty(Link.UserCode) || string.IsNullOrEmpty(Link.DeviceCode))
            {
                Link = null;
                SetLinkLine(Texts.T("Can't reach {0}; trying again later.", Host()), "negative");
                _poll.Interval = 15000;
                _poll.Start();
                return;
            }
            SetLinkLine(Texts.T("Waiting…"), "");
            _poll.Interval = Math.Max(2, Link.IntervalSeconds) * 1000;
            _poll.Start();
        }

        /// <summary>The main window came up: a fresh code when the last ones went unseen.</summary>
        public void WindowShown()
        {
            if (!LinkPaused || Linked || !_settings.Maple) return;
            _codeRenewals = 0;
            var flow = StartLinkFlow();
            GC.KeepAlive(flow);
        }

        private void SetLinkLine(string text, string tone)
        {
            LinkLine = text;
            LinkTone = tone;
            Tell();
        }

        private string Host()
        {
            try { return new Uri(_settings.Site).Host; } catch (Exception) { return _settings.Site; }
        }

        private async Task PollOnce()
        {
            if (_polling || Linked) return;
            _polling = true;
            try
            {
                if (Link == null || Link.ExpiresAt <= DateTime.UtcNow)
                {
                    if (Link != null) _codeRenewals++;
                    await StartLinkFlow();
                    if (Link != null && _codeRenewals > 0) SetLinkLine(Texts.T("The code expired; here is a new one."), "warning");
                    return;
                }
                var result = await _api.Poll(Link.DeviceCode, Api.TempoLink);
                switch (result.Status)
                {
                    case "ok":
                        if (result.Token == null || !TempoToken.IsMatch(result.Token))
                        {
                            // Not a token of ours: whatever answered, it isn't the site we linked to. Start over.
                            _codeRenewals++;
                            await StartLinkFlow();
                            return;
                        }
                        _poll.Stop();
                        _codeRenewals = 0;
                        _settings.TempoToken = result.Token;
                        if (string.IsNullOrEmpty(_settings.Locale) && result.Locale != null) _settings.Locale = result.Locale;
                        SiteRefused = false;
                        _settings.Save();
                        Link = null;
                        LinkNotice = null;
                        Problem = null;
                        if (FirstLink != null) FirstLink();
                        Tell();
                        if (Program.TestOpen) await OpenTempo(true, true);
                        break;
                    case "expired":
                    case "unknown":
                        _codeRenewals++;
                        await StartLinkFlow();
                        if (Link != null) SetLinkLine(Texts.T("The code expired; here is a new one."), "warning");
                        break;
                    case "offline":
                        SetLinkLine(Texts.T("Can't reach {0}; trying again later.", Host()), "negative");
                        break;
                    default:
                        if (LinkTone != "") SetLinkLine(Texts.T("Waiting…"), "");
                        break;
                }
            }
            finally
            {
                _polling = false;
            }
        }

        /// <summary>The site refused the token (unlinked on Account › Security): Tempo closes and the window shows the link step again.</summary>
        public void OnUnauthorized()
        {
            if (!Linked) return;
            _settings.TempoToken = null;
            SiteRefused = false;
            _settings.Save();
            LinkNotice = Texts.T("This program was unlinked on the site. Link it again with the code below.");
            // Not from inside the page's own event: the window goes on the next turn of the message loop.
            var timer = new Timer { Interval = 50 };
            timer.Tick += async delegate
            {
                timer.Stop();
                timer.Dispose();
                CloseWindow();
                if (Attention != null) Attention();
                await StartLinkFlow();
            };
            timer.Start();
        }

        /// <summary>"Unlink" in the window's Settings: this PC forgets Tempo's link, Tempo closes, and a new code is asked for.</summary>
        public void Unlink()
        {
            if (!Linked) return;
            _settings.TempoToken = null;
            SiteRefused = false;
            _settings.Save();
            LinkNotice = null;
            Problem = null;
            CheckNote = null;
            CloseWindow();
            Tell();
            if (_settings.Maple && SiteAllowed)
            {
                var flow = StartLinkFlow();
                GC.KeepAlive(flow);
            }
        }

        // ------------------------------------------------------------------------------------------------ the switches

        /// <summary>The player read what Tempo looks at and said yes.</summary>
        public void GiveConsent()
        {
            _settings.TempoConsent = true;
            _settings.TempoAsked = true;
            _settings.Save();
            Tell();
        }

        public void SetStartWithGame(bool on)
        {
            _settings.StartTempoWithGame = on;
            _settings.Save();
            // A game that is already running when the switch is set is not started for: the switch is about the next start of the game.
            if (on)
            {
                var found = Find();
                _startedFor = found != null ? found.Handle : IntPtr.Zero;
            }
            Look();
            SendState();
            Tell();
        }

        public void SetFloatShown(bool on)
        {
            ShowFloat(on, true);
        }

        public void SetFloatLocked(bool on)
        {
            _settings.FloatLocked = on;
            _settings.Save();
            if (_float != null) _float.SetLocked(on);
            SendState();
            Tell();
        }

        // ------------------------------------------------------------------------------------------------ the game's window

        private static FoundWindow Find()
        {
            var test = Program.TestWindow != null;
            return GameWindow.Find(test ? Program.TestWindow : GameWindow.GameTitle, test);
        }

        /// <summary>Every few seconds, and only while the switch is on or the Tempo window is open: is the game's window there?</summary>
        private void Look()
        {
            if (_disposed || !_settings.Maple || !Linked || Refused || !Consent || !(_settings.StartTempoWithGame || _window != null))
            {
                if (GameFound != null)
                {
                    GameFound = null;
                    Tell();
                }
                return;
            }
            FoundWindow found;
            try { found = Find(); }
            catch (Exception) { return; }
            var there = found != null;
            if (GameFound != there)
            {
                GameFound = there;
                SendState();
                Tell();
            }
            if (!there)
            {
                _startedFor = IntPtr.Zero;
                return;
            }
            // The game was started: Tempo opens (minimised, the game keeps the keyboard) and starts, once per game window.
            if (_settings.StartTempoWithGame && found.Handle != _startedFor && !found.Minimised && !SessionLive && !_opening)
            {
                _startedFor = found.Handle;
                var open = OpenTempo(true, true);
                GC.KeepAlive(open);
            }
        }

        // ------------------------------------------------------------------------------------------------ the Tempo window

        /// <summary>
        /// Opens the Tempo window (or brings it to the front). `start`: ask the page to start watching once it is there.
        /// `quiet`: minimised and without taking the keyboard.
        /// </summary>
        public async Task OpenTempo(bool start, bool quiet)
        {
            if (!_settings.Maple || !Linked || !Consent || !SiteAllowed || _opening) return;
            // After a refusal nothing opens: "Try again" (CheckAccess) is the way back.
            if (Refused && _window == null) return;
            if (_window != null)
            {
                if (!quiet) _window.ShowFront();
                if (start) Command("start");
                return;
            }
            _opening = true;
            try
            {
                Problem = null;
                RuntimeMissing = WebRuntime.Version() == null;
                if (RuntimeMissing)
                {
                    Tell();
                    return;
                }
                var answer = await _api.TempoStart(_settings.TempoToken);
                if (answer.Status == "unlinked")
                {
                    OnUnauthorized();
                    return;
                }
                Uri page = null;
                if (answer.Status == "ok" && !PageAllowed(answer.Url, out page)) answer.Status = "offline";
                if (answer.Status == "refused")
                {
                    // Not expected (Tempo is for every account): MapleStory's page says so plainly, with "Try again".
                    SetRefused(true);
                    Tell();
                    return;
                }
                if (answer.Status != "ok")
                {
                    Problem = Texts.T("Can't reach {0}; trying again later.", Host());
                    ProblemTone = "negative";
                    Tell();
                    return;
                }
                SetRefused(false);
                _startWhenReady = start;
                _window = new TempoWindow(this, _settings, page);
                var window = _window;
                window.FormClosed += delegate { WindowClosed(window); };
                window.Open(quiet);
                if (!await window.LoadPage())
                {
                    Problem = Texts.T("The Tempo window could not start.");
                    ProblemTone = "negative";
                    CloseWindow();
                }
                Look();
                Tell();
            }
            finally
            {
                _opening = false;
            }
        }

        /// <summary>The page to load must be the Tempo page on the site this program talks to.</summary>
        private bool PageAllowed(string url, out Uri page)
        {
            page = null;
            Uri u, site;
            if (!Uri.TryCreate(url, UriKind.Absolute, out u) || !Uri.TryCreate(_settings.Site, UriKind.Absolute, out site)) return false;
            if (Uri.Compare(u, site, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0) return false;
            if (!u.AbsolutePath.EndsWith(PagePath, StringComparison.Ordinal)) return false;
            page = u;
            return true;
        }

        private void WindowClosed(TempoWindow window)
        {
            if (!ReferenceEquals(_window, window)) return;
            _window = null;
            window.Dispose();
            HideFloat();
            SessionState = "idle";
            Numbers = false;
            _startWhenReady = false;
            Look();
            Tell();
        }

        private void CloseWindow()
        {
            var window = _window;
            if (window == null) return;
            try { window.Shut(); } catch (Exception) { }
            if (ReferenceEquals(_window, window)) WindowClosed(window);
        }

        /// <summary>A button's press goes to the page, which owns the session: "start", "stop" or "reset".</summary>
        public void Command(string action)
        {
            if (_window == null) return;
            if (!_window.Ready)
            {
                if (action == "start") _startWhenReady = true;
                return;
            }
            _window.Post(new Dictionary<string, object> { { "type", "command" }, { "action", action } });
        }

        /// <summary>The tray's "Stop watching".</summary>
        public void StopWatching()
        {
            Command("stop");
        }

        private void SendState()
        {
            if (_window == null || !_window.Ready) return;
            _window.Post(new Dictionary<string, object>
            {
                { "type", "state" },
                { "version", Program.Version },
                { "game", GameFound == true ? "found" : "missing" },
                { "watching", Watching },
                { "float", new Dictionary<string, object> { { "open", _float != null && _float.Open }, { "size", _float != null ? _float.SizeName : _settings.FloatSize }, { "locked", _settings.FloatLocked } } },
                { "startWithGame", _settings.StartTempoWithGame },
                { "settings", true },
            });
        }

        // ------------------------------------------------------------------------------------------------ what the page says

        public void OnReady()
        {
            Look();
            if (GameFound == null)
            {
                try { GameFound = Find() != null; } catch (Exception) { }
            }
            SendState();
            if (_settings.FloatShown) ShowFloat(true, true);
            if (_startWhenReady)
            {
                _startWhenReady = false;
                Command("start");
            }
            Tell();
        }

        /// <summary>The page's process ended and the page loads afresh: its session is gone.</summary>
        public void OnPageLost()
        {
            SessionState = "idle";
            Numbers = false;
            Tell();
        }

        public void OnWatching()
        {
            SendState();
            Tell();
        }

        public void OnSession(string state, bool numbers, double seconds)
        {
            var next = state == "reading" || state == "paused" ? state : "idle";
            // How long the session has counted, as the page said it just now: the menu by the clock writes the minutes from it.
            SessionSeconds = Math.Max(0, seconds);
            SessionSaid = DateTime.UtcNow;
            if (next == SessionState && numbers == Numbers) return;
            SessionState = next;
            Numbers = numbers;
            Tell();
        }

        /// <summary>The session's minutes now ("26 min", "1 h 4 min"): what the page said last, plus the time since while it reads; null without a session.</summary>
        public string SessionTime()
        {
            if (SessionState == "idle") return null;
            var seconds = SessionSeconds + (SessionState == "reading" && SessionSaid != default(DateTime) ? Math.Min(120, (DateTime.UtcNow - SessionSaid).TotalSeconds) : 0);
            var minutes = (int)Math.Floor(seconds / 60);
            return minutes >= 60 ? (minutes / 60) + " h " + (minutes % 60) + " min" : minutes + " min";
        }

        /// <summary>A switch of the page's own Settings list: kept here, acted on, and answered with the state.</summary>
        public void OnSetting(string name, bool value)
        {
            if (name == "floatLocked") SetFloatLocked(value);
            else if (name == "startWithGame") SetStartWithGame(value);
            else return;
            SendState();
        }

        /// <summary>Windows switched between light and dark: the float draws again in the new mode.</summary>
        public void LookChanged()
        {
            if (_float != null && !_float.IsDisposed) _float.Invalidate();
            if (_window != null && !_window.IsDisposed) _window.LookChanged();
        }

        /// <summary>"Tempo may look at the MapleStory window" was turned off in Settings: Tempo stops (the page keeps its session first) and closes.</summary>
        public void TakeConsent()
        {
            _settings.TempoConsent = false;
            _settings.TempoAsked = true;
            _settings.Save();
            if (_window != null)
            {
                Command("stop");
                var timer = new Timer { Interval = SessionLive ? 2500 : 50 };
                timer.Tick += delegate
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!_settings.TempoConsent) CloseWindow();
                };
                timer.Start();
            }
            Look();
            Tell();
        }

        public void OnFloat(string action, Dictionary<string, object> values)
        {
            switch (action ?? "")
            {
                case "open":
                    ShowFloat(true, false);
                    break;
                case "close":
                    ShowFloat(false, false);
                    break;
                case "update":
                    _floatValues = FloatValues.Parse(values);
                    if (_float != null) _float.SetValues(_floatValues);
                    break;
            }
        }

        // ------------------------------------------------------------------------------------------------ the float

        /// <summary>Shows or hides the float and remembers the choice. `tellPage`: the change did not come from the page, so the page hears of it.</summary>
        private void ShowFloat(bool on, bool tellPage)
        {
            if (_settings.FloatShown != on)
            {
                _settings.FloatShown = on;
                _settings.Save();
            }
            if (on && _window != null && _window.Ready)
            {
                if (_float == null)
                {
                    Uri site;
                    var origin = Uri.TryCreate(_settings.Site, UriKind.Absolute, out site) ? site.GetLeftPart(UriPartial.Authority) : null;
                    _float = new FloatWindow(_settings, origin);
                    _float.Command = Command;
                    _float.CloseAsked = delegate { ShowFloat(false, true); };
                    _float.SizeSwitched = delegate { SendState(); };
                    _float.GameWindowOf = GameHandle;
                    _float.SetLocked(_settings.FloatLocked);
                    if (_floatValues != null) _float.SetValues(_floatValues);
                }
                _float.ShowAt(_window.WatchedBounds);
            }
            else if (!on) HideFloat();
            if (tellPage && _window != null && _window.Ready)
            {
                _window.Post(new Dictionary<string, object> { { "type", "command" }, { "action", "float" }, { "open", on } });
            }
            SendState();
            Tell();
        }

        private IntPtr _gameSeen;
        private DateTime _gameLooked;

        /// <summary>The game's window for the float: the one Tempo watches, else the one found (looked for at most once a second).</summary>
        private IntPtr GameHandle()
        {
            var watched = _window != null ? _window.WatchedWindow : IntPtr.Zero;
            if (watched != IntPtr.Zero) return watched;
            if (_gameSeen != IntPtr.Zero && GameWindow.StillThere(_gameSeen)) return _gameSeen;
            if ((DateTime.UtcNow - _gameLooked).TotalSeconds < 1) return IntPtr.Zero;
            _gameLooked = DateTime.UtcNow;
            var found = Find();
            _gameSeen = found != null ? found.Handle : IntPtr.Zero;
            return _gameSeen;
        }

        private void HideFloat()
        {
            if (_float == null) return;
            var gone = _float;
            _float = null;
            // Closing a window that was shown also lets go of everything it holds.
            try { gone.Close(); } catch (Exception) { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _poll.Dispose();
            _look.Dispose();
            CloseWindow();
            HideFloat();
        }
    }
}
