// Tempo's side of the program, for MapleStory Classic: linking it to the account with a code (its own token, apart from
// the WoW one), the Tempo window (TempoWindow.cs) and the float (FloatWindow.cs), and what the main window's pages and
// the menu by the clock show of it. The page in the Tempo window reads the game and owns the farm session (Start, Pause,
// Continue, Reset, the numbers); this class passes the buttons' presses on to it and shows what the page says.
//
// Tempo reads whenever the program runs with MapleStory open, once it is linked and the player said Tempo may look:
// every few seconds the program looks for the game's window by its title (Capture.cs), and while it is there the Tempo
// page runs in a window that is not shown until the player opens it ("Open Tempo"), and is asked to read. The farm
// session is apart from that and the player's alone. Looking stops when the game closes, when "Tempo may look at the
// MapleStory window" is turned off on Settings, and at Quit. Before the program closes or updates itself the page is
// asked to send what it read so far ("flush"), and given five seconds at most.
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
        /// <summary>How long the program waits for the page's "flushed" before it closes or updates all the same.</summary>
        public const int FlushMs = 5000;
        /// <summary>A page loaded unseen that did not say it is there in this long is closed and tried again later.</summary>
        private const int UnseenLoadMs = 120000;
        /// <summary>After an unseen load that could not be done (the site out of reach), the next try waits this long.</summary>
        private const int UnseenRetryMs = 60000;

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
        private bool _disposed;
        private bool _asking;
        /// <summary>Not before this time is the page loaded unseen again, after a try that failed.</summary>
        private DateTime _unseenAgain = DateTime.MinValue;
        /// <summary>A "flush" the page has not answered yet: its answer, or null.</summary>
        private TaskCompletionSource<bool> _flushing;
        /// <summary>"Open Tempo" was pressed while the window was on its way unseen: it comes to the front.</summary>
        private bool _frontWhenOpen;
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
        /// <summary>The page's farm session: idle, reading or paused; and whether it holds numbers.</summary>
        public string SessionState = "idle";
        public bool Numbers;
        /// <summary>The page said it reads the game right now, session or not.</summary>
        public bool PageReading;
        /// <summary>Whether the game's window was found at the last look; null while the program is not looking.</summary>
        public bool? GameFound;
        /// <summary>A picture's made-up state (Shots.cs): Tempo watches the game. Never set by the program itself.</summary>
        public bool PictureWatching;
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
        /// <summary>The program hands the page the game's window right now: Tempo reads (the green dot by the clock).</summary>
        public bool Watching { get { return PictureWatching || (_window != null && _window.Watching); } }
        public bool WindowOpen { get { return _window != null; } }
        /// <summary>A farm session the player would lose: it runs, or it is paused with numbers. The window's cross only hides it then.</summary>
        public bool SessionLive { get { return SessionState == "reading" || (SessionState == "paused" && Numbers); } }
        /// <summary>The farm session runs: the program does not update itself meanwhile (a paused one is kept on the account already).</summary>
        public bool SessionRunning { get { return SessionState == "reading"; } }
        /// <summary>The Tempo window stays (hidden by its cross) while the game is open, Tempo reads, or a session would be lost.</summary>
        public bool KeepsWindow { get { return GameFound == true || Watching || SessionLive; } }
        public bool Consent { get { return _settings.TempoConsent || Program.TestOpen; } }
        /// <summary>Looking is off because the player turned it off himself (not "No, not now", not never asked).</summary>
        public bool TurnedOff { get { return !Consent && _settings.TempoTurnedOff; } }
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

        /// <summary>MapleStory was turned off: everything of Tempo closes, after the page sent what it read; the link to the account stays.</summary>
        public void End()
        {
            _poll.Stop();
            _look.Stop();
            Link = null;
            GameFound = null;
            Retire(false);
            Tell();
        }

        /// <summary>
        /// Tempo stops: the page stops looking at once (a running session pauses and is kept), sends what it read, and
        /// then the window closes. `consent`: only while the player has not said yes again meanwhile.
        /// </summary>
        private async void Retire(bool consent)
        {
            var window = _window;
            if (window == null) return;
            Command("stop");
            window.EndWatching();
            await Flush();
            if (consent && _settings.TempoConsent) return;
            if (!ReferenceEquals(_window, window)) return;
            await window.LeavePage();
            if (ReferenceEquals(_window, window)) CloseWindow();
        }

        /// <summary>The page leaves before the window closes (Quit, an update), so what it sends as it goes still carries the token.</summary>
        public async Task Leave()
        {
            var window = _window;
            if (window != null) await window.LeavePage();
        }

        /// <summary>
        /// Asks the page to send what it read so far (the program is about to close or update itself) and waits for its
        /// "flushed", FlushMs at most: without an answer the program goes on all the same.
        /// </summary>
        public async Task Flush()
        {
            var window = _window;
            if (window == null || !window.Ready) return;
            var waiting = _flushing;
            if (waiting == null)
            {
                waiting = new TaskCompletionSource<bool>();
                _flushing = waiting;
                window.Post(new Dictionary<string, object> { { "type", "command" }, { "action", "flush" } });
            }
            await Task.WhenAny(waiting.Task, Task.Delay(FlushMs));
            if (ReferenceEquals(_flushing, waiting)) _flushing = null;
        }

        /// <summary>The page answered "flush": what it had to send went.</summary>
        public void OnFlushed()
        {
            var waiting = _flushing;
            _flushing = null;
            if (waiting != null) waiting.TrySetResult(true);
        }

        /// <summary>Quit: the Tempo window and the float go out of sight at once; the page runs on until it is flushed and closed.</summary>
        public void HideAll()
        {
            HideFloat();
            if (_window != null) _window.HideNow();
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
            _settings.TempoTurnedOff = false;
            _settings.Save();
            Tell();
        }

        /// <summary>"Put my shop online by itself": kept for the page, which acts on it.</summary>
        public void SetShopAuto(bool on)
        {
            _settings.ShopAuto = on;
            _settings.Save();
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

        /// <summary>
        /// Every few seconds, once linked and with the player's yes: is the game's window there? While it is (and not
        /// minimised) the Tempo page reads it: loaded in a window that is not shown, the first time, and asked to read
        /// whenever it does not watch.
        /// </summary>
        private void Look()
        {
            if (_disposed || !_settings.Maple || !SiteAllowed || !Linked || Refused || !Consent || CantLook)
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
            if (!there || found.Minimised) return;
            if (_window == null)
            {
                if (!_opening && !RuntimeMissing && DateTime.UtcNow >= _unseenAgain)
                {
                    var open = OpenTempo(false, Opening.Unseen);
                    GC.KeepAlive(open);
                }
                return;
            }
            if (!_window.Ready)
            {
                // A page loaded unseen that never said it is there (the site out of reach): closed, and tried again later.
                if (!_window.Visible && (DateTime.UtcNow - _window.MadeAt).TotalMilliseconds > UnseenLoadMs)
                {
                    Tempo.Log("the Tempo page did not load unseen; trying again later");
                    _unseenAgain = DateTime.UtcNow.AddMilliseconds(UnseenRetryMs);
                    CloseWindow();
                }
                return;
            }
            if (!_window.Watching) Command("read");
        }

        // ------------------------------------------------------------------------------------------------ the Tempo window

        /// <summary>How the Tempo window comes: in front; minimised without taking the keyboard (a test start); or not shown at all (Tempo reads, nobody asked for the window).</summary>
        internal enum Opening { Front, Quiet, Unseen }

        /// <summary>
        /// Opens the Tempo window (or brings it to the front). `start`: ask the page to start the farm session once it is
        /// there. `quiet`: minimised and without taking the keyboard.
        /// </summary>
        public Task OpenTempo(bool start, bool quiet)
        {
            return OpenTempo(start, quiet ? Opening.Quiet : Opening.Front);
        }

        internal async Task OpenTempo(bool start, Opening how)
        {
            if (!_settings.Maple || !Linked || !Consent || !SiteAllowed) return;
            // Already on its way (perhaps unseen): it comes as the player asks now.
            if (_opening)
            {
                if (how == Opening.Front) _frontWhenOpen = true;
                if (start) _startWhenReady = true;
                return;
            }
            // After a refusal nothing opens: "Try again" (CheckAccess) is the way back.
            if (Refused && _window == null) return;
            if (_window != null)
            {
                if (how == Opening.Front) _window.ShowFront();
                if (start) Command("start");
                return;
            }
            _opening = true;
            var unseen = how == Opening.Unseen;
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
                    if (unseen) _unseenAgain = DateTime.UtcNow.AddMilliseconds(UnseenRetryMs);
                    Tell();
                    return;
                }
                SetRefused(false);
                _startWhenReady = _startWhenReady || start;
                // In the program's language: the site's address for it ("/nl/maplestory-classic/tempo-desktop").
                page = InLanguage(page, Texts.Language);
                _window = new TempoWindow(this, _settings, page);
                var window = _window;
                window.FormClosed += delegate { WindowClosed(window); };
                window.Open(_frontWhenOpen ? Opening.Front : how);
                _frontWhenOpen = false;
                if (!await window.LoadPage())
                {
                    Problem = Texts.T("The Tempo window could not start.");
                    ProblemTone = "negative";
                    if (unseen) _unseenAgain = DateTime.UtcNow.AddMilliseconds(UnseenRetryMs);
                    CloseWindow();
                }
                Look();
                Tell();
            }
            finally
            {
                _opening = false;
                // What was asked on the way is for the window that came; none came, so nothing waits.
                _frontWhenOpen = false;
                if (_window == null) _startWhenReady = false;
            }
        }

        /// <summary>
        /// The Tempo page's address in `language`, as the site writes its languages: "/nl/maplestory-classic/tempo-desktop"
        /// (and de, fr, es, pt-br); English without one. Whatever language the address had goes first; its query stays.
        /// </summary>
        internal static Uri InLanguage(Uri page, string language)
        {
            var path = TempoWindow.WithoutLanguage(page.AbsolutePath);
            var known = Array.IndexOf(Texts.Languages, language) >= 0;
            var b = new UriBuilder(page) { Path = known && language != "en" ? "/" + language + path : path };
            return b.Uri;
        }

        /// <summary>The page's language is due to change once no session would be lost (the program's language changed meanwhile).</summary>
        private bool _languageWaits;

        /// <summary>
        /// The program's language changed (Settings › Language, or the account's language when the player picked none): the
        /// Tempo page loads again in it, after it sent what it read (flush), and only when no farm session would be lost
        /// (a running one, or a paused one with numbers, is not kept across a load): then as soon as it is gone (after
        /// Reset). A window that is not there opens in the new language anyway.
        /// </summary>
        public void LanguageChanged()
        {
            if (_window == null || _disposed) return;
            if (_window.Page.AbsolutePath == InLanguage(_window.Page, Texts.Language).AbsolutePath)
            {
                _languageWaits = false;
                return;
            }
            if (SessionLive)
            {
                _languageWaits = true;
                return;
            }
            ReloadInLanguage();
        }

        private async void ReloadInLanguage()
        {
            _languageWaits = false;
            var window = _window;
            if (window == null) return;
            // Still on its way: once it says it is there.
            if (!window.Ready)
            {
                _languageWaits = true;
                return;
            }
            await Flush();
            if (!ReferenceEquals(_window, window) || window.IsDisposed || SessionLive) return;
            Tempo.Log("the Tempo page loads again in " + Texts.Language);
            window.LoadAgain(InLanguage(window.Page, Texts.Language));
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
            PageReading = false;
            _startWhenReady = false;
            OnFlushed();
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

        /// <summary>
        /// A button's press goes to the page, which owns the farm session: "start" (start or continue it), "stop" (pause
        /// it), "reset"; the float's answers about the player's own shop, "shop-always" and "shop-not-now"; and "read"
        /// (the game's window is there to be looked at).
        /// </summary>
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

        /// <summary>The menu's "Start session" and "Continue session": the page starts or continues the farm session, loading unseen first when it is not there yet.</summary>
        public void StartSession()
        {
            if (_window != null)
            {
                Command("start");
                return;
            }
            var open = OpenTempo(true, Opening.Unseen);
            GC.KeepAlive(open);
        }

        /// <summary>The menu's "Pause session". Tempo goes on reading; only the farm session pauses.</summary>
        public void PauseSession()
        {
            Command("stop");
        }

        /// <summary>
        /// What the program is, for the page: `always`, it reads whenever it runs with the game open (the page asks to
        /// watch as soon as `game` is "found", never asks to stop for a pause, and Start, Pause, Continue and Reset are the
        /// farm session's alone); `totals`, the float draws its numbers as totals with their words; and its switches.
        /// </summary>
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
                { "always", true },
                { "totals", true },
                { "shopAuto", _settings.ShopAuto },
                // Tempo's own Settings may turn "Tempo may look at the MapleStory window" off (setting consent): always on
                // while the page runs, as without it the program closes the page.
                { "consent", true },
            });
        }

        // ------------------------------------------------------------------------------------------------ what the page says

        public void OnReady()
        {
            if (GameFound == null)
            {
                try { GameFound = Find() != null; } catch (Exception) { }
            }
            // The state first: the page reads from it (with the game there) and then hears "read" from the looks.
            SendState();
            Look();
            // The float comes as it was the last time: shown only when it was, and only with the game (without it, it
            // waits out of sight and comes when the game does).
            if (_settings.FloatShown) ShowFloat(true, true, true);
            if (_startWhenReady)
            {
                _startWhenReady = false;
                Command("start");
            }
            // The program's language changed while the page was on its way.
            if (_languageWaits) LanguageChanged();
            Tell();
        }

        /// <summary>The page's process ended and the page loads afresh: its session is gone.</summary>
        public void OnPageLost()
        {
            SessionState = "idle";
            Numbers = false;
            PageReading = false;
            OnFlushed();
            Tell();
        }

        public void OnWatching()
        {
            SendState();
            Tell();
        }

        /// <summary>The page's farm session (`state`, `numbers`, `seconds`), and whether the page reads the game right now (`reading`).</summary>
        public void OnSession(string state, bool numbers, double seconds, bool reading)
        {
            var next = state == "reading" || state == "paused" ? state : "idle";
            // How long the session has counted, as the page said it just now: the menu by the clock writes the minutes from it.
            SessionSeconds = Math.Max(0, seconds);
            SessionSaid = DateTime.UtcNow;
            if (next == SessionState && numbers == Numbers && reading == PageReading) return;
            SessionState = next;
            Numbers = numbers;
            PageReading = reading;
            // The language changed during a session: the page loads in it now that no session would be lost.
            if (_languageWaits && !SessionLive) LanguageChanged();
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
            if (name == "consent")
            {
                // "Tempo may look at the MapleStory window" off in Tempo's own Settings: as the switch in the program's
                // Settings does (TakeConsent). Not from inside the page's own event: the window goes on the next turn.
                // On asks nothing (the page runs only with it on); turning it on again is the program's question.
                if (value)
                {
                    SendState();
                    return;
                }
                var timer = new Timer { Interval = 50 };
                timer.Tick += delegate
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!_disposed && _settings.TempoConsent) TakeConsent();
                };
                timer.Start();
                return;
            }
            if (name == "floatLocked") SetFloatLocked(value);
            else if (name == "shopAuto") SetShopAuto(value);
            else if (name == "startWithGame")
            {
                // A page from before Tempo read always may still send it: kept as it is said, and nothing else.
                _settings.StartTempoWithGame = value;
                _settings.Save();
            }
            else return;
            SendState();
        }

        /// <summary>
        /// A screen came or went, or the screens were arranged anew (`afterChange`), or it is the look every two seconds
        /// (Screens.cs): the Tempo window goes where it shows; after a change the float goes to its place at the game too.
        /// </summary>
        public void ScreensChanged(ScreenInfo[] screens, bool afterChange)
        {
            if (_window != null && !_window.IsDisposed) _window.KeepOnScreen(screens, afterChange);
            if (afterChange && _float != null && !_float.IsDisposed) _float.ScreensChanged();
        }

        /// <summary>Where the Tempo window stands, for tempo.log's line about the screens.</summary>
        public string WindowText()
        {
            if (_window == null || _window.IsDisposed) return "the Tempo window: not open";
            return "the Tempo window: " + _window.StateText();
        }

        /// <summary>Windows switched between light and dark: the float draws again in the new mode.</summary>
        public void LookChanged()
        {
            if (_float != null && !_float.IsDisposed) _float.Invalidate();
            if (_window != null && !_window.IsDisposed) _window.LookChanged();
        }

        /// <summary>
        /// "Tempo may look at the MapleStory window" was turned off in Settings: Tempo stops looking at once (a running
        /// session pauses and is kept), the page sends what it read, and the window closes.
        /// </summary>
        public void TakeConsent()
        {
            _settings.TempoConsent = false;
            _settings.TempoAsked = true;
            // The player turned it off himself: the window says so and offers to turn it back on (pick 3 A of 25 November 2026).
            _settings.TempoTurnedOff = true;
            _settings.Save();
            Retire(true);
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
            ShowFloat(on, tellPage, false);
        }

        /// <summary>`withGameOnly`: the float comes back as it was (not asked for just now), so it shows only while the game's window is there and waits for it otherwise.</summary>
        private void ShowFloat(bool on, bool tellPage, bool withGameOnly)
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
                _float.ShowAt(_window.WatchedBounds, withGameOnly);
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
