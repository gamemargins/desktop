// The Tempo window: a window of this program that shows gamemargins.com's Tempo page (in WebView2, Microsoft Edge's
// engine, which Windows has built in) and hands that page the picture of the game's window (Capture.cs). The page reads
// the numbers; this program never looks inside a frame.
//
// What the window allows, and nothing more:
//   - It shows the Tempo page only (also with one of the site's languages before its path: /nl/…). Any other link
//     opens in the player's own browser; the Tempo page itself never does.
//   - The page can't share a screen or a window by itself, nor use the camera, the microphone or the location.
//   - Tempo's token never enters the page: the program adds it to the page's requests to the site's Tempo routes
//     (/api/maplestory/tempo-…) and to nothing else.
//   - Frames travel in memory both programs share on this computer, one frame per request of the page. Nothing of a
//     frame is written to disk or sent anywhere by this program.
//
// The page and the program talk in small JSON messages (protocol 1):
//   page → program   { type: "ready" } · { type: "capture", action: "start" | "stop" } · { type: "frame" } ·
//                    { type: "float", action: "open" | "close" | "update", values } ·
//                    { type: "session", state, numbers } · { type: "open", url }
//   program → page   { type: "state", … } · { type: "capture", ok, … } · { type: "frame", width, height, stride, at } ·
//                    { type: "captureEnded", reason } · { type: "command", action: "start" | "stop" | "reset" | "float" }

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace GameMargins.Uploader
{
    /// <summary>The WebView2 runtime on this computer (part of Windows 11 and of an updated Windows 10).</summary>
    internal static class WebRuntime
    {
        /// <summary>Where Microsoft offers the runtime for a Windows that lacks it.</summary>
        public const string Download = "https://developer.microsoft.com/microsoft-edge/webview2/";
        private static bool _loader;

        /// <summary>Points the SDK at the loader written from the exe; false when that could not be done.</summary>
        public static bool Prepare()
        {
            if (_loader) return true;
            var folder = Embedded.EnsureLoader();
            if (folder == null) return false;
            try
            {
                CoreWebView2Environment.SetLoaderDllFolderPath(folder);
                _loader = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>The runtime's version, or null when Windows has none (or the loader could not be prepared).</summary>
        public static string Version()
        {
            try
            {
                if (!Prepare()) return null;
                var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return string.IsNullOrEmpty(version) ? null : version;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    internal sealed class TempoWindow : Form
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [DllImport("dwmapi.dll", PreserveSig = true)] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int how);

        private readonly Tempo _tempo;
        private readonly Settings _settings;
        private readonly Uri _page;
        private readonly string _origin;
        private readonly bool _local;
        private readonly WebView2 _web = new WebView2();
        private readonly Timer _watchdog = new Timer();
        private CoreWebView2Environment _env;
        private CoreWebView2 _core;
        private Capture _capture;
        private CoreWebView2SharedBuffer _shared;
        private long _sharedSize;
        private bool _closing;
        private bool _quiet;

        /// <summary>The page said it is there and listening.</summary>
        public bool Ready;
        /// <summary>The watched window's place on the screen, or empty.</summary>
        public Rectangle WatchedBounds
        {
            get { return _capture != null ? GameWindow.BoundsOf(_capture.Window) : Rectangle.Empty; }
        }

        public bool Watching { get { return _capture != null; } }

        /// <summary>The watched window, or IntPtr.Zero.</summary>
        public IntPtr WatchedWindow { get { return _capture != null ? _capture.Window : IntPtr.Zero; } }

        protected override bool ShowWithoutActivation { get { return _quiet; } }

        public TempoWindow(Tempo tempo, Settings settings, Uri page)
        {
            _tempo = tempo;
            _settings = settings;
            _page = page;
            _origin = page.GetLeftPart(UriPartial.Authority);
            _local = Tempo.IsLoopback(_origin);

            // The window's name is the product's, as the float's.
            Text = "Tempo";
            BackColor = Look.Now.Background;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            MinimumSize = new Size(480, 360);
            try
            {
                using (var stream = typeof(TempoWindow).Assembly.GetManifestResourceStream("GameMargins.Uploader.GameMargins.ico"))
                {
                    if (stream != null) Icon = new Icon(stream, 32, 32);
                }
            }
            catch (Exception) { }
            Bounds = StartBounds();
            _web.Dock = DockStyle.Fill;
            _web.DefaultBackgroundColor = Look.Now.Background;
            Controls.Add(_web);
            _watchdog.Interval = 1000;
            _watchdog.Tick += delegate { Watchdog(); };
            FormClosing += OnClosing;
        }

        /// <summary>Where the window stood last time when that is still on a screen, else 1100 × 720 in the middle of the main screen.</summary>
        private Rectangle StartBounds()
        {
            var parts = (_settings.TempoBounds ?? "").Split(',');
            int x, y, w, h;
            if (parts.Length == 4 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y) && int.TryParse(parts[2], out w) && int.TryParse(parts[3], out h) && w >= 480 && h >= 360)
            {
                var kept = new Rectangle(x, y, w, h);
                foreach (var screen in Screen.AllScreens)
                {
                    var seen = Rectangle.Intersect(screen.WorkingArea, kept);
                    if (seen.Width >= 200 && seen.Height >= 120) return kept;
                }
            }
            var area = Screen.PrimaryScreen.WorkingArea;
            var scale = 1f;
            try { using (var g = CreateGraphics()) scale = g.DpiX / 96f; } catch (Exception) { }
            var width = Math.Min(area.Width, (int)(1116 * scale));
            var height = Math.Min(area.Height, (int)(759 * scale));
            return new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Look.TitleBar(Handle);
        }

        /// <summary>Windows switched between light and dark: the title bar and the colour behind the page follow; the page itself follows Windows through WebView2 (its colour scheme is the system's).</summary>
        public void LookChanged()
        {
            try
            {
                BackColor = Look.Now.Background;
                _web.DefaultBackgroundColor = Look.Now.Background;
                if (IsHandleCreated) Look.TitleBar(Handle);
                ApplyScheme();
            }
            catch (Exception) { }
        }

        private void ApplyScheme()
        {
            try
            {
                if (_core == null) return;
                // "Follow Windows" is PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto; Light and Dark hold the page to one.
                var scheme = Look.Mode == "light" ? CoreWebView2PreferredColorScheme.Light : Look.Mode == "dark" ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Auto;
                _core.Profile.PreferredColorScheme = scheme;
            }
            catch (Exception) { }
        }

        protected override void WndProc(ref Message m)
        {
            if (Look.IsColourChange(ref m)) Look.Refresh();
            base.WndProc(ref m);
        }

        /// <summary>Shows the window; `quiet`: minimised and without taking the keyboard (Tempo started with the game).</summary>
        public void Open(bool quiet)
        {
            _quiet = quiet;
            if (quiet)
            {
                // Minimised and not activated, asked of Windows directly (the form's own Show would activate a minimised
                // window): the game keeps the keyboard and the screen.
                WindowState = FormWindowState.Minimized;
                _shownByWindows = true;
                ShowWindow(Handle, 7);
            }
            else Show();
            _quiet = false;
        }

        /// <summary>The window was shown minimised by Windows directly, and the form has not been told yet that it shows.</summary>
        private bool _shownByWindows;

        /// <summary>The player brought the quietly opened window up from the taskbar: from here on it is a shown form like any other.</summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!_shownByWindows || WindowState == FormWindowState.Minimized || _closing) return;
            _shownByWindows = false;
            Show();
        }

        public void ShowFront()
        {
            if (_shownByWindows)
            {
                _shownByWindows = false;
                ShowWindow(Handle, 9);
            }
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
        }

        /// <summary>Starts WebView2 and loads the page. False when that failed (the caller closes the window and says so).</summary>
        public async Task<bool> LoadPage()
        {
            try
            {
                if (!WebRuntime.Prepare()) return false;
                _env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Settings.Folder, "webview2-data"), new CoreWebView2EnvironmentOptions());
                await _web.EnsureCoreWebView2Async(_env);
                _core = _web.CoreWebView2;
                // Light or dark as the program is: the page follows the colour scheme it is given (Windows' own, or the one picked in Settings).
                ApplyScheme();
                var s = _core.Settings;
                s.UserAgent = s.UserAgent + " GameMarginsDesktop/" + Program.Version;
                s.IsStatusBarEnabled = false;
                s.IsPasswordAutosaveEnabled = false;
                s.IsGeneralAutofillEnabled = false;
                s.IsSwipeNavigationEnabled = false;
                // Reload and the browser's own menu would end a session by accident; a test against a site on this computer keeps them.
                s.AreDevToolsEnabled = _local;
                s.AreDefaultContextMenusEnabled = _local;
                s.AreBrowserAcceleratorKeysEnabled = _local;
                await _core.AddScriptToExecuteOnDocumentCreatedAsync("window.__gmDesktop = Object.freeze({ version: \"" + Program.Version + "\", protocol: 1 });");

                _core.NavigationStarting += OnNavigationStarting;
                _core.NewWindowRequested += OnNewWindow;
                _core.ScreenCaptureStarting += delegate(object sender, CoreWebView2ScreenCaptureStartingEventArgs e) { e.Cancel = true; };
                _core.PermissionRequested += OnPermission;
                _core.WebMessageReceived += OnMessage;
                _core.ProcessFailed += OnProcessFailed;
                // Tempo's token goes with the page's requests to the site's Tempo routes, and with nothing else.
                _core.AddWebResourceRequestedFilter(_origin + "/api/maplestory/tempo-*", CoreWebView2WebResourceContext.All);
                _core.WebResourceRequested += OnResourceRequested;
                _core.WebResourceResponseReceived += OnResourceResponse;
                _core.Navigate(_page.AbsoluteUri);
                return true;
            }
            catch (Exception e)
            {
                Tempo.Log("the Tempo window could not start: " + e.GetType().Name + " " + e.Message);
                return false;
            }
        }

        // ------------------------------------------------------------------------------------------------ what the page may do

        /// <summary>Tempo's page for this program, as the site's start route names it (without a language before it).</summary>
        internal const string DesktopPath = "/maplestory-classic/tempo-desktop";
        /// <summary>The site's languages, as they stand before a page's path ("/nl/maplestory-classic/tempo-desktop").</summary>
        private static readonly string[] Languages = { "en", "nl", "de", "fr", "es", "pt-br" };

        /// <summary>A path without the site's language before it: "/nl/maplestory-classic/tempo-desktop" is "/maplestory-classic/tempo-desktop".</summary>
        internal static string WithoutLanguage(string path)
        {
            path = path ?? "";
            foreach (var language in Languages)
            {
                var prefix = "/" + language + "/";
                if (path.StartsWith(prefix, StringComparison.Ordinal)) return path.Substring(prefix.Length - 1);
            }
            return path;
        }

        /// <summary>
        /// Whether `address` is this window's page: on the site this start talks to (`origin`), at the page's path, also with
        /// one of the site's languages before it: should the site send /maplestory-classic/tempo-desktop on to
        /// /nl/maplestory-classic/tempo-desktop (a Windows in Dutch), the window shows it rather than staying empty.
        /// </summary>
        internal static bool IsPageAddress(string address, string origin, string pagePath)
        {
            Uri u;
            if (!Uri.TryCreate(address, UriKind.Absolute, out u)) return false;
            return string.Equals(u.GetLeftPart(UriPartial.Authority), origin, StringComparison.OrdinalIgnoreCase) && WithoutLanguage(u.AbsolutePath) == WithoutLanguage(pagePath);
        }

        /// <summary>
        /// Whether `address` is the program's own Tempo page, on any site, with or without a language and a slash at its end.
        /// That page belongs in this window and never in a browser.
        /// </summary>
        internal static bool IsDesktopPage(string address, string pagePath)
        {
            Uri u;
            if (!Uri.TryCreate(address, UriKind.Absolute, out u)) return false;
            var path = WithoutLanguage(u.AbsolutePath).TrimEnd('/');
            return string.Equals(path, DesktopPath, StringComparison.OrdinalIgnoreCase) || (pagePath != null && string.Equals(path, WithoutLanguage(pagePath).TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
        }

        private bool IsPage(string address)
        {
            return IsPageAddress(address, _origin, _page.AbsolutePath);
        }

        private bool IsTempoRoute(string address)
        {
            Uri u;
            if (!Uri.TryCreate(address, UriKind.Absolute, out u)) return false;
            return string.Equals(u.GetLeftPart(UriPartial.Authority), _origin, StringComparison.OrdinalIgnoreCase) && u.AbsolutePath.StartsWith("/api/maplestory/tempo-", StringComparison.Ordinal);
        }

        /// <summary>
        /// An address in the player's own browser: https, or the site this start talks to. Nothing else goes to Windows, and
        /// never Tempo's own page: that one is said in the log and opened nowhere.
        /// </summary>
        private void OpenOutside(string address)
        {
            Uri u;
            if (!Uri.TryCreate(address, UriKind.Absolute, out u)) return;
            if (IsDesktopPage(address, _page != null ? _page.AbsolutePath : null))
            {
                Tempo.Log("Tempo's own page was not opened in the browser: " + u.GetLeftPart(UriPartial.Authority) + u.AbsolutePath);
                return;
            }
            var own = string.Equals(u.GetLeftPart(UriPartial.Authority), _origin, StringComparison.OrdinalIgnoreCase);
            if (u.Scheme != Uri.UriSchemeHttps && !own) return;
            try { Process.Start(u.AbsoluteUri); } catch (Exception) { }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (IsPage(e.Uri)) return;
            e.Cancel = true;
            if (e.IsUserInitiated) OpenOutside(e.Uri);
        }

        private void OnNewWindow(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            OpenOutside(e.Uri);
        }

        private static void OnPermission(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            switch (e.PermissionKind)
            {
                case CoreWebView2PermissionKind.Camera:
                case CoreWebView2PermissionKind.Microphone:
                case CoreWebView2PermissionKind.Geolocation:
                case CoreWebView2PermissionKind.Notifications:
                case CoreWebView2PermissionKind.OtherSensors:
                    e.State = CoreWebView2PermissionState.Deny;
                    break;
            }
        }

        private void OnResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            try
            {
                var token = _settings.TempoToken;
                if (string.IsNullOrEmpty(token) || !IsTempoRoute(e.Request.Uri)) return;
                e.Request.Headers.SetHeader("Authorization", "Bearer " + token);
            }
            catch (Exception) { }
        }

        private void OnResourceResponse(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            try
            {
                // Unlinked on the site: the routes answer 401 from then on.
                if (e.Response != null && e.Response.StatusCode == 401 && IsTempoRoute(e.Request.Uri) && !string.IsNullOrEmpty(_settings.TempoToken)) _tempo.OnUnauthorized();
            }
            catch (Exception) { }
        }

        private void OnProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            Tempo.Log("the page's process failed: " + e.ProcessFailedKind);
            if (e.ProcessFailedKind != CoreWebView2ProcessFailedKind.RenderProcessExited && e.ProcessFailedKind != CoreWebView2ProcessFailedKind.RenderProcessUnresponsive) return;
            // The page is gone and its session with it: stop looking, and load the page afresh.
            StopCapture();
            Ready = false;
            _tempo.OnPageLost();
            try { _core.Reload(); } catch (Exception) { }
        }

        // ------------------------------------------------------------------------------------------------ messages

        public void Post(Dictionary<string, object> message)
        {
            if (_core == null || _closing) return;
            try { _core.PostWebMessageAsJson(Json.Write(message)); } catch (Exception) { }
        }

        private void OnMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            Dictionary<string, object> m;
            try
            {
                // Only the Tempo page talks to the program.
                if (!IsPage(e.Source)) return;
                m = Json.Parse(e.WebMessageAsJson);
            }
            catch (Exception)
            {
                return;
            }
            if (m == null) return;
            try
            {
                switch (Json.Str(m, "type") ?? "")
                {
                    case "ready":
                        Ready = true;
                        _tempo.OnReady();
                        break;
                    case "capture":
                        if (Json.Str(m, "action") == "start") StartCapture();
                        else if (Json.Str(m, "action") == "stop")
                        {
                            StopCapture();
                            Post(new Dictionary<string, object> { { "type", "capture" }, { "ok", true }, { "stopped", true } });
                        }
                        break;
                    case "frame":
                        SendFrame();
                        break;
                    case "float":
                        _tempo.OnFloat(Json.Str(m, "action"), Json.Object(m, "values"));
                        break;
                    case "session":
                        _tempo.OnSession(Json.Str(m, "state"), Json.Bool(m, "numbers"), Json.Number(m, "seconds"));
                        break;
                    case "setting":
                        _tempo.OnSetting(Json.Str(m, "name"), Json.Bool(m, "value"));
                        break;
                    case "open":
                        OpenOutside(Json.Str(m, "url"));
                        break;
                }
            }
            catch (Exception error)
            {
                Tempo.Log("a message of the page failed: " + error.GetType().Name + " " + error.Message);
            }
        }

        // ------------------------------------------------------------------------------------------------ the picture

        private void Refuse(string reason, string detail)
        {
            var answer = new Dictionary<string, object> { { "type", "capture" }, { "ok", false }, { "reason", reason } };
            if (detail != null) answer["detail"] = detail;
            Post(answer);
        }

        private void StartCapture()
        {
            StopCapture();
            if (!_settings.TempoConsent && !Program.TestOpen)
            {
                Refuse("consent", null);
                return;
            }
            if (!CaptureSupport.Works)
            {
                Refuse("unsupported", null);
                return;
            }
            var test = Program.TestWindow != null;
            var found = GameWindow.Find(test ? Program.TestWindow : GameWindow.GameTitle, test);
            if (found == null)
            {
                Refuse("no-game", null);
                return;
            }
            if (found.Minimised)
            {
                Refuse("minimized", null);
                return;
            }
            try
            {
                _capture = new Capture(found.Handle);
            }
            catch (Exception e)
            {
                _capture = null;
                Tempo.Log("the capture could not start: " + e.GetType().Name + " " + e.Message);
                Refuse("error", e.GetType().Name);
                return;
            }
            Tempo.Log("watching a window of class \"" + found.Class + "\", " + _capture.Width + " x " + _capture.Height + "; " + _capture.Note);
            try
            {
                MakeRoom((long)_capture.Width * _capture.Height * 4 + 1024 * 1024);
            }
            catch (Exception e)
            {
                StopCapture();
                Refuse("error", e.GetType().Name);
                return;
            }
            _watchdog.Start();
            Post(new Dictionary<string, object> { { "type", "capture" }, { "ok", true }, { "width", _capture.Width }, { "height", _capture.Height } });
            _tempo.OnWatching();
        }

        /// <summary>One block of memory the page can read, big enough for a frame; a bigger frame gets a new block.</summary>
        private void MakeRoom(long bytes)
        {
            if (_shared != null && _sharedSize >= bytes) return;
            var old = _shared;
            _shared = _env.CreateSharedBuffer((ulong)bytes);
            _sharedSize = bytes;
            _core.PostSharedBufferToScript(_shared, CoreWebView2SharedBufferAccess.ReadOnly, "{\"type\":\"buffer\",\"bytes\":" + bytes + "}");
            if (old == null) return;
            // The page may still be reading the block before this one: it goes a little later.
            var later = new Timer { Interval = 5000 };
            later.Tick += delegate
            {
                later.Stop();
                later.Dispose();
                try { old.Dispose(); } catch (Exception) { }
            };
            later.Start();
        }

        private void SendFrame()
        {
            if (_capture == null)
            {
                Post(new Dictionary<string, object> { { "type", "frame" }, { "none", true }, { "reason", "no-frame" } });
                return;
            }
            try
            {
                if (_capture.Closed || !GameWindow.StillThere(_capture.Window))
                {
                    EndCapture("closed", null);
                    return;
                }
                if (GameWindow.IsMinimised(_capture.Window))
                {
                    Post(new Dictionary<string, object> { { "type", "frame" }, { "none", true }, { "reason", "minimized" } });
                    return;
                }
                int width, height, stride;
                long needed;
                var got = _capture.Grab(_shared.Buffer, _sharedSize, out width, out height, out stride, out needed);
                if (!got && needed > _sharedSize)
                {
                    MakeRoom(needed + 1024 * 1024);
                    got = _capture.Grab(_shared.Buffer, _sharedSize, out width, out height, out stride, out needed);
                }
                if (!got)
                {
                    Post(new Dictionary<string, object> { { "type", "frame" }, { "none", true }, { "reason", "no-frame" } });
                    return;
                }
                var at = Math.Floor((DateTime.UtcNow - Epoch).TotalMilliseconds);
                Post(new Dictionary<string, object> { { "type", "frame" }, { "width", width }, { "height", height }, { "stride", stride }, { "at", at } });
            }
            catch (Exception e)
            {
                // A failure of the graphics calls ends the watching and is told to the page; the rest of the program goes on.
                Tempo.Log("a frame could not be taken: " + e.GetType().Name + " " + e.Message);
                EndCapture("error", e.GetType().Name);
            }
        }

        /// <summary>Once a second while watching: a game that was closed is noticed also when the page asks for no frame.</summary>
        private void Watchdog()
        {
            if (_capture == null)
            {
                _watchdog.Stop();
                return;
            }
            try
            {
                if (_capture.Closed || !GameWindow.StillThere(_capture.Window)) EndCapture("closed", null);
            }
            catch (Exception) { }
        }

        private void EndCapture(string reason, string detail)
        {
            StopCapture();
            var message = new Dictionary<string, object> { { "type", "captureEnded" }, { "reason", reason } };
            if (detail != null) message["detail"] = detail;
            Post(message);
        }

        public void StopCapture()
        {
            _watchdog.Stop();
            if (_capture == null) return;
            try { _capture.Dispose(); } catch (Exception) { }
            _capture = null;
            _tempo.OnWatching();
        }

        // ------------------------------------------------------------------------------------------------ closing

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                _settings.TempoBounds = Left + "," + Top + "," + Width + "," + Height;
                _settings.Save();
            }
            // While a session runs the cross only hides the window: Tempo goes on, and the tray icon and the float show it.
            if (!_closing && e.CloseReason == CloseReason.UserClosing && _tempo.SessionLive)
            {
                e.Cancel = true;
                Hide();
                // Also when the form itself was never told it shows (a quiet start).
                ShowWindow(Handle, 0);
                return;
            }
            _closing = true;
            StopCapture();
        }

        /// <summary>Closes for good, whatever runs.</summary>
        public void Shut()
        {
            _closing = true;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _watchdog.Dispose();
                try { if (_capture != null) _capture.Dispose(); } catch (Exception) { }
                _capture = null;
                try { if (_shared != null) _shared.Dispose(); } catch (Exception) { }
                _shared = null;
                try { _web.Dispose(); } catch (Exception) { }
            }
            base.Dispose(disposing);
        }
    }
}
