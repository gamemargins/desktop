// GameMargins Desktop (called GameMargins Uploader before 0.2.5): a small Windows program for two games.
//
// For WoW: Forever it watches the GameMargins addon's file, WTF\Account\<account>\SavedVariables\GameMargins.lua, in
// every WoW account of the player's install and uploads it to gamemargins.com the moment the game writes it (on
// logout or /reload), the way the big addons' desktop apps do; it also installs and keeps the addon up to date from
// the site's own zip, and itself from its GitHub release (Updater.cs).
//
// For MapleStory Classic it holds Tempo, the site's tracker: a window of its own that shows gamemargins.com's Tempo
// page, fed with the picture of the game's window (Capture.cs says exactly what is looked at and what never), and a
// small always-on-top float (Tempo.cs, TempoWindow.cs, FloatWindow.cs).
//
// It lives by the clock: a left click on its icon opens the window (a list with Overview, a page per game and
// Settings, where every setting of the program is), a right click the short menu, which holds no settings. Each game
// is linked with a code on gamemargins.com/link, and no password or mail code ever enters this program. Built with the
// C# compiler Windows ships (build.ps1): C# 5, .NET Framework 4.x, WinForms. BSD 2-Clause (LICENSE).
//
// Privacy: the account folders are Battle.net account names. They never leave this computer, are never written to a
// file by this program, and the window calls them "WoW account 1, 2, …". For WoW the only things sent are the addon's
// file, to the same upload the site's upload page uses, and the token the site gave; Tempo has a token of its own,
// which goes to the site's Tempo routes and nowhere else, and no picture of the game ever leaves this computer. Both
// tokens are kept with Windows' own protection (DPAPI, this user only) in
// %LOCALAPPDATA%\GameMargins\Uploader\settings.json, never in plain text. The program's own update sends neither: it
// reads one small file from the site and downloads a new version from GitHub.

using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    internal static class Program
    {
        /// <summary>
        /// The program's version, written here only: AssemblyInfo.cs stamps it into the exe's file data, and build.ps1
        /// reads it for the download's name (GameMarginsDesktop-x.y.z.exe). The site's version file names the newest
        /// version (Updater.cs); a newer one is installed by itself, or with a click in the menu.
        /// </summary>
        public const string Version = "0.4.0";
        public const string DefaultSite = "https://gamemargins.com";

        /// <summary>
        /// A test start that keeps everything in a folder of its own (--data): it may run next to an installed copy, and
        /// writes no Start-menu shortcut and no autostart entry.
        /// </summary>
        public static bool Isolated;
        /// <summary>A test start's own window to watch in place of the game's (--tempo-test-window "exact title"); null otherwise.</summary>
        public static string TestWindow;
        /// <summary>A test start opens Tempo and asks it to start by itself (--tempo-open; only with --data and a site on this computer).</summary>
        public static bool TestOpen;
        /// <summary>The new copy this one handed over to (an update), and its version: Main stands by to see it stay up.</summary>
        public static System.Diagnostics.Process HandedOver;
        public static string HandedOverVersion;

        [STAThread]
        private static void Main(string[] args)
        {
            // The WebView2 SDK's managed libraries are inside the exe (Embedded.cs).
            Embedded.Register();
            var options = Options.Parse(args);
            if (options.DataFolder != null)
            {
                Settings.FolderOverride = options.DataFolder;
                Isolated = true;
            }
            TestWindow = options.TestWindow;
            TestOpen = options.TestOpen && Isolated && Tempo.IsLoopback(options.Site);

            // --tempo-selftest, --tempo-test-target <seconds> and --shots <folder>: checks of the capture and pictures
            // of everything the program draws (TempoSelfTest.cs, Shots.cs); they touch no settings and no other program.
            if (options.SelfTest)
            {
                Environment.ExitCode = TempoSelfTest.Capture();
                return;
            }
            if (options.TestTarget > 0)
            {
                Environment.ExitCode = TempoSelfTest.ShowTarget(options.TestTarget);
                return;
            }
            if (options.Shots != null)
            {
                Environment.ExitCode = Shots.All(options.Shots);
                return;
            }

            // --test-toast: one notification, then a line in %TEMP%\GameMargins-toast-test.txt saying whether it was shown
            // and how many of the program's notifications the notification center holds (a check for a PC with Do not
            // disturb on). Before the single-copy check, so a running copy doesn't
            // take the start as a request for its window.
            if (options.TestToast)
            {
                TestToast();
                return;
            }

            // With "Keep the program up to date" on, an older copy started from elsewhere (an old download on the
            // desktop) hands over to the newer one the program installed when it updated itself, with the same --wow. Not a
            // test start against another site (--site), nor the copy an update just started.
            if (options.Site == null && !Isolated && options.UpdatedFrom == null && Settings.Load().KeepProgramUpdated)
            {
                var newer = Updater.NewestInstalled(Settings.Folder, Version, Application.ExecutablePath);
                // Never a version that did not stay up after an update.
                var bad = Settings.Load().UpdateBad;
                if (newer != null && !string.IsNullOrEmpty(bad) && string.Equals(newer, Updater.InstalledPath(Settings.Folder, bad), StringComparison.OrdinalIgnoreCase)) newer = null;
                if (newer != null && Updater.Start(newer, options.WowFolder != null ? "--wow " + Updater.Quote(options.WowFolder) : "")) return;
            }

            // One copy at a time: a second start tells the first to show its window, then leaves. The copy an update
            // started waits for the one it replaces to have gone (a minute at most).
            bool first;
            bool signalNew;
            // A test start (--data) is one copy per data folder: two tests with folders of their own never take each
            // other for a second start (with one name for all of them, a second test would only bring the first one's
            // window up).
            var suffix = Isolated ? ".Test." + FolderMark(options.DataFolder) : "";
            using (var signal = new EventWaitHandle(false, EventResetMode.AutoReset, "GameMargins.Uploader.Show" + suffix, out signalNew))
            using (var mutex = new Mutex(true, "GameMargins.Uploader.Single" + suffix, out first))
            {
                if (!first && options.UpdatedFrom != null) first = WaitFor(mutex, TimeSpan.FromMinutes(1));
                if (!first)
                {
                    try { signal.Set(); } catch (Exception) { }
                    return;
                }
                try
                {
                    Run(options, signal);
                }
                finally
                {
                    // Let go at once: the copy an update starts waits for it.
                    try { mutex.ReleaseMutex(); } catch (Exception) { }
                }
            }
            StandBy(options);
        }

        /// <summary>
        /// After handing over to a new version: this copy stays for half a minute, without a window or an icon. When the
        /// new copy does not stay up (it ends with an error), the version is marked as bad (never installed or started
        /// again), autostart and the Start-menu shortcut point back at this exe, and this version starts again and says
        /// so. The old exe is never removed by the update that replaced it, so there is always a way back.
        /// </summary>
        private static void StandBy(Options options)
        {
            var started = HandedOver;
            if (started == null) return;
            try
            {
                if (!Updater.FellOver(started, Updater.StandBySeconds)) return;
                var settings = Settings.Load();
                settings.UpdateBad = HandedOverVersion;
                settings.Save();
                MainForm.PointAt(Application.ExecutablePath, settings.StartWithWindows);
                Updater.Start(Application.ExecutablePath, "--update-failed " + HandedOverVersion + (options.WowFolder != null ? " --wow " + Updater.Quote(options.WowFolder) : ""));
            }
            catch (Exception) { }
            finally
            {
                try { started.Dispose(); } catch (Exception) { }
            }
        }

        /// <summary>A short mark for a folder's path (its full path, lower case, hashed), for the names a test start shares with nothing else.</summary>
        private static string FolderMark(string folder)
        {
            try
            {
                var full = System.IO.Path.GetFullPath(folder).TrimEnd('\\').ToLowerInvariant();
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(full)), 0, 6).Replace("-", "");
                }
            }
            catch (Exception)
            {
                return "x";
            }
        }

        /// <summary>The single-copy mutex, once its owner let go or ended (an ended owner abandons it: ours all the same).</summary>
        private static bool WaitFor(Mutex mutex, TimeSpan timeout)
        {
            try
            {
                return mutex.WaitOne(timeout);
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
        }

        /// <summary>The program, once this is the one copy that runs.</summary>
        private static void Run(Options options, EventWaitHandle signal)
        {
            // The exe has no .config of its own, so the framework would start on the oldest TLS; gamemargins.com speaks TLS 1.2 and 1.3.
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)12288;
            }
            catch (NotSupportedException)
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            }

            // Who this process is to Windows (the toasts' and the taskbar's AppUserModelID), before any window exists, and
            // the Start-menu shortcut that carries the same id, which the notification center needs (Toasts.cs).
            Toasts.SetAppId();
            if (!Isolated)
            {
                Toasts.EnsureShortcut();
                Toasts.RemoveOldShortcut();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // The site is gamemargins.com unless this start says otherwise (--site, for a local test server; never
            // remembered, so a plain start always talks to the real site). A token belongs to the site that made it:
            // one from another site is dropped, and the program asks to be linked again.
            var settings = Settings.Load();
            var site = options.Site ?? DefaultSite;
            if (settings.Site != site)
            {
                settings.Token = null;
                settings.TempoToken = null;
            }
            settings.Site = site;
            if (options.WowFolder != null) settings.WowFolder = options.WowFolder;
            settings.Save();

            Texts.Language = Texts.Pick(settings.Locale);
            Look.Choose(settings.Appearance);
            Look.Watch();
            var form = new MainForm(settings, options.UpdatedFrom);
            if (options.UpdateFailed != null && Updater.ParseVersion(options.UpdateFailed) != null) form.SayUpdateFailed(options.UpdateFailed);
            form.ListenForSecondStart(signal);
            Application.Run(form);
        }

        private static void TestToast()
        {
            Toasts.SetAppId();
            var shortcut = Toasts.EnsureShortcut();
            string result;
            try
            {
                Toasts.Show("GameMargins Desktop", "Test notification: the program's notifications land here, also while Do not disturb is on.", "test");
                Thread.Sleep(1500);
                result = "toast shown; shortcut " + (shortcut ? "in place" : "NOT written") + "; notifications of this program in the notification center: " + Toasts.HistoryCount();
            }
            catch (Exception e)
            {
                result = "toast failed (" + e.GetType().Name + "): " + e.Message;
            }
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameMargins-toast-test.txt"), result); } catch (Exception) { }
        }
    }

    /// <summary>
    /// The command line: --site https://… (another site, for a local test server; for this start only), --wow
    /// C:\…\_forever_ (another install; also the one Settings keeps afterwards), --test-toast (one test notification and
    /// a line in %TEMP%, then exit), --data <folder> (a test's own folder for everything the program keeps, so it can
    /// run next to an installed copy), --tempo-test-window "title" (Tempo watches the window with exactly this title
    /// in place of the game's), --tempo-open (with --data and a site on this computer: open Tempo and start),
    /// --tempo-selftest and --tempo-test-target <seconds> (TempoSelfTest.cs), --shots <folder> (Shots.cs),
    /// --updated-from C:\…\GameMarginsDesktop-x.y.z.exe (given by the update to the new copy it starts: the exe it
    /// replaces), --update-failed x.y.z (given by a copy to itself when the update to that version did not stay up).
    /// --tray is accepted for the autostart entry older builds wrote; a linked
    /// program starts by the clock in any case.
    /// </summary>
    internal sealed class Options
    {
        public string Site;
        public string WowFolder;
        public bool TestToast;
        public string UpdatedFrom;
        public string DataFolder;
        public string TestWindow;
        public bool TestOpen;
        public bool SelfTest;
        public int TestTarget;
        public string Shots;
        public string UpdateFailed;

        public static Options Parse(string[] args)
        {
            var o = new Options();
            for (var i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a == "--site" && i + 1 < args.Length) o.Site = args[++i].TrimEnd('/');
                else if (a == "--wow" && i + 1 < args.Length) o.WowFolder = args[++i];
                else if (a == "--test-toast") o.TestToast = true;
                else if (a == "--updated-from" && i + 1 < args.Length) o.UpdatedFrom = args[++i];
                else if (a == "--data" && i + 1 < args.Length) o.DataFolder = args[++i];
                else if (a == "--tempo-test-window" && i + 1 < args.Length) o.TestWindow = args[++i];
                else if (a == "--tempo-open") o.TestOpen = true;
                else if (a == "--tempo-selftest") o.SelfTest = true;
                else if (a == "--shots" && i + 1 < args.Length) o.Shots = args[++i];
                else if (a == "--update-failed" && i + 1 < args.Length) o.UpdateFailed = args[++i];
                else if (a == "--tempo-test-target" && i + 1 < args.Length) int.TryParse(args[++i], out o.TestTarget);
            }
            // GAMEMARGINS_SITE does what --site does, for a start that can't be given arguments.
            if (o.Site == null)
            {
                var site = Environment.GetEnvironmentVariable("GAMEMARGINS_SITE");
                if (!string.IsNullOrEmpty(site)) o.Site = site.TrimEnd('/');
            }
            return o;
        }
    }
}
