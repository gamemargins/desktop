// GameMargins Desktop, the WoW: Forever desktop app (called GameMargins Uploader before 0.2.5): a small Windows program
// that watches the GameMargins addon's file, WTF\Account\<account>\SavedVariables\GameMargins.lua, in every WoW
// account of the player's install and uploads
// it to gamemargins.com the moment the game writes it (on logout or /reload), the way the big addons' desktop apps do;
// it also installs and keeps the addon up to date from the site's own zip, and itself from its GitHub release
// (Updater.cs). Once linked it lives by the clock (a left click opens the small window with the accounts, the addon,
// the last uploads and "Upload now"; a right click the menu); it is linked with a code on gamemargins.com/link (no
// password or mail code ever enters this program). Built with the C# compiler Windows ships (build.ps1): C# 5, .NET
// Framework 4.x, WinForms. BSD 2-Clause (LICENSE).
//
// Privacy: the account folders are Battle.net account names. They never leave this computer, are never written to a
// file by this program, and the window calls them "WoW account 1, 2, …". The only things sent are the addon's file,
// to the same upload the site's upload page uses, and the token the site gave, kept with Windows' own protection
// (DPAPI, this user only) in %LOCALAPPDATA%\GameMargins\Uploader\settings.json, never in plain text. The program's own
// update sends neither: it reads one small file from the site and downloads a new version from GitHub.

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
        public const string Version = "0.2.6";
        public const string DefaultSite = "https://gamemargins.com";

        [STAThread]
        private static void Main(string[] args)
        {
            var options = Options.Parse(args);

            // --test-toast: one notification, then a line in %TEMP%\GameMargins-toast-test.txt saying whether it was shown
            // and how many of the program's notifications the notification center holds (a check for a PC with Do not
            // disturb on). Before the single-copy check, so a running copy doesn't
            // take the start as a request for its window.
            if (options.TestToast)
            {
                TestToast();
                return;
            }

            // With "Keep the program up to date" ticked, an older copy started from elsewhere (an old download on the
            // desktop) hands over to the newer one the program installed when it updated itself, with the same --wow. Not a
            // test start against another site (--site), nor the copy an update just started.
            if (options.Site == null && options.UpdatedFrom == null && Settings.Load().KeepProgramUpdated)
            {
                var newer = Updater.NewestInstalled(Settings.Folder, Version, Application.ExecutablePath);
                if (newer != null && Updater.Start(newer, options.WowFolder != null ? "--wow " + Updater.Quote(options.WowFolder) : "")) return;
            }

            // One copy at a time: a second start tells the first to show its window, then leaves. The copy an update
            // started waits for the one it replaces to have gone (a minute at most).
            bool first;
            bool signalNew;
            using (var signal = new EventWaitHandle(false, EventResetMode.AutoReset, "GameMargins.Uploader.Show", out signalNew))
            using (var mutex = new Mutex(true, "GameMargins.Uploader.Single", out first))
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
            Toasts.EnsureShortcut();
            Toasts.RemoveOldShortcut();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // The site is gamemargins.com unless this start says otherwise (--site, for a local test server; never
            // remembered, so a plain start always talks to the real site). A token belongs to the site that made it:
            // one from another site is dropped, and the program asks to be linked again.
            var settings = Settings.Load();
            var site = options.Site ?? DefaultSite;
            if (settings.Site != site) settings.Token = null;
            settings.Site = site;
            if (options.WowFolder != null) settings.WowFolder = options.WowFolder;
            settings.Save();

            Texts.Language = Texts.Pick(settings.Locale);
            var form = new MainForm(settings, options.UpdatedFrom);
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
    /// a line in %TEMP%, then exit), --updated-from C:\…\GameMarginsDesktop-x.y.z.exe (given by the update to the new
    /// copy it starts: the exe it replaces). --tray is accepted for the autostart entry older builds wrote; a linked
    /// program starts by the clock in any case.
    /// </summary>
    internal sealed class Options
    {
        public string Site;
        public string WowFolder;
        public bool TestToast;
        public string UpdatedFrom;

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
            }
            return o;
        }
    }
}
