// Installing and updating the GameMargins addon in the player's game folder, so it is in place without unpacking a zip
// by hand. The site's status names the zip it serves with its version, size and
// SHA-256; the program downloads it from our site only, refuses anything whose hash or size differs, unpacks it next
// to AddOns (never inside), and only then, with WoW closed, moves the old GameMargins folder to a backup outside
// AddOns and the new one in. Nothing else under Interface\AddOns is read or written. The tray's "Keep the addon up to
// date" (on by default) switches all of it off for players who let CurseForge or Wago do it.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameMargins.Uploader
{
    internal enum AddonMark
    {
        /// <summary>Plain text: installed, up to date.</summary>
        Plain,
        /// <summary>Blue: something will happen (an install or update waits for WoW to close, or runs now).</summary>
        Info,
        /// <summary>Grey: the player keeps the addon up to date themselves.</summary>
        Muted,
        /// <summary>Red: a download or install failed; nothing was changed.</summary>
        Failed,
    }

    internal sealed class AddonStatus
    {
        public AddonMark Mark;
        public string Text;
        /// <summary>Set when this status is a fresh install or update, for the notification: the version put in place.</summary>
        public string JustInstalled;
        public bool WasUpdate;
    }

    internal sealed class AddonException : Exception
    {
        public AddonException(string message) : base(message) { }
    }

    internal sealed class Addon
    {
        public const string Folder = "GameMargins";
        private const int BackupsKept = 3;

        private readonly Api _api;
        private bool _busy;

        /// <summary>What the site offers, from the last status answer; null before one, or when the site has no zip.</summary>
        public AddonInfo Site;
        public bool KeepUpdated = true;

        /// <summary>Raised on the calling (UI) thread with every change the window should show.</summary>
        public event Action<AddonStatus> Changed;

        public Addon(Api api)
        {
            _api = api;
        }

        /// <summary>Whether a look at the addon (a download or an install) is under way; the program's own update waits for it.</summary>
        public bool Busy
        {
            get { return _busy; }
        }

        private void Report(AddonMark mark, string text)
        {
            if (Changed != null) Changed(new AddonStatus { Mark = mark, Text = text });
        }

        /// <summary>Whether a World of Warcraft client runs: its folder must not be changed under it.</summary>
        public static bool WowRunning()
        {
            foreach (var p in Process.GetProcesses())
            {
                string name;
                try { name = p.ProcessName; }
                catch (Exception) { continue; }
                if (name.StartsWith("Wow", StringComparison.OrdinalIgnoreCase) || name.StartsWith("World of Warcraft", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>The version the installed addon's TOC declares; "?" for a folder without one; null when the addon isn't there.</summary>
        public static string InstalledVersion(string wowFolder)
        {
            var toc = Path.Combine(TargetFolder(wowFolder), Folder + ".toc");
            if (!Directory.Exists(TargetFolder(wowFolder))) return null;
            if (!File.Exists(toc)) return "?";
            return TocVersion(toc) ?? "?";
        }

        private static string TocVersion(string toc)
        {
            try
            {
                foreach (var line in File.ReadAllLines(toc, Encoding.UTF8))
                {
                    if (!line.StartsWith("## Version:", StringComparison.OrdinalIgnoreCase)) continue;
                    var v = line.Substring("## Version:".Length).Trim();
                    return v.Length > 0 ? v : null;
                }
            }
            catch (Exception) { }
            return null;
        }

        private static string TargetFolder(string wowFolder)
        {
            return Path.Combine(wowFolder, Path.Combine("Interface", Path.Combine("AddOns", Folder)));
        }

        /// <summary>
        /// True when the site's version is newer than the installed one, or the addon is missing or has no version line.
        /// Versions compare by their leading numbers ("0.6.0-beta" is 0.6.0); a version that has none is never replaced,
        /// so a player's own test build can't be swapped for an older release.
        /// </summary>
        public static bool Newer(string site, string installed)
        {
            if (installed == null || installed == "?") return true;
            var a = Numeric(site);
            var b = Numeric(installed);
            return a != null && b != null && a > b;
        }

        private static Version Numeric(string v)
        {
            var m = Regex.Match(v ?? "", @"^\s*v?(\d+(?:\.\d+){1,3})");
            Version r;
            return m.Success && Version.TryParse(m.Groups[1].Value, out r) ? r : null;
        }

        /// <summary>
        /// Looks at the installed addon against what the site offers and installs or updates when it should and may:
        /// the tick on, the site known, something to do, WoW closed. Reports every state to the window.
        /// </summary>
        public async Task Maintain(string wowFolder)
        {
            if (_busy || wowFolder == null) return;
            _busy = true;
            try
            {
                var installed = InstalledVersion(wowFolder);
                if (!KeepUpdated)
                {
                    Report(AddonMark.Muted, installed == null ? Texts.T("not installed · you install it yourself") : Texts.T("{0} · you keep it up to date yourself", installed));
                    return;
                }
                if (Site == null)
                {
                    Report(installed == null ? AddonMark.Info : AddonMark.Plain, installed == null ? Texts.T("not installed · installs when WoW is closed") : Texts.T("{0} · up to date", installed));
                    return;
                }
                if (!Newer(Site.Version, installed))
                {
                    Report(AddonMark.Plain, Texts.T("{0} · up to date", installed));
                    return;
                }
                if (WowRunning())
                {
                    Report(AddonMark.Info, installed == null ? Texts.T("not installed · installs when WoW is closed") : Texts.T("{0} · {1} available · updates when WoW is closed", installed, Site.Version));
                    return;
                }
                Report(AddonMark.Info, Texts.T("installing {0}…", Site.Version));
                var site = Site;
                var bytes = await _api.Download(site.Url);
                if (bytes == null)
                {
                    Report(AddonMark.Failed, Texts.T("update failed: {0}", Texts.T("Can't reach {0}; trying again later.", HostOf(site.Url))));
                    return;
                }
                if (bytes.LongLength != site.Size || !string.Equals(Sha256Hex(bytes), site.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    Report(AddonMark.Failed, Texts.T("update failed: {0}", Texts.T("The download did not match the site's checksum; nothing was changed.")));
                    return;
                }
                try
                {
                    await Task.Run(delegate { Install(wowFolder, bytes, site.Version, installed); });
                }
                catch (AddonException e)
                {
                    Report(AddonMark.Failed, Texts.T("update failed: {0}", e.Message));
                    return;
                }
                catch (Exception e)
                {
                    Report(AddonMark.Failed, Texts.T("update failed: {0}", e.Message));
                    return;
                }
                if (Changed != null) Changed(new AddonStatus { Mark = AddonMark.Plain, Text = Texts.T("{0} · installed just now", site.Version), JustInstalled = site.Version, WasUpdate = installed != null });
            }
            finally
            {
                _busy = false;
            }
        }

        private static string HostOf(string url)
        {
            try { return new Uri(url).Host; } catch (Exception) { return url; }
        }

        public static string Sha256Hex(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Unpacks the verified zip into Interface\GameMargins-staging (outside AddOns), checks it holds exactly the
        /// GameMargins folder of the expected version, then swaps: the installed folder moves to
        /// Interface\GameMargins-backup\GameMargins-&lt;old version&gt;, the new one into AddOns. A failure after the
        /// first move puts the old folder back. The last three backups are kept.
        /// </summary>
        private static void Install(string wowFolder, byte[] zip, string version, string installed)
        {
            var addons = Path.Combine(wowFolder, Path.Combine("Interface", "AddOns"));
            var target = TargetFolder(wowFolder);
            var staging = Path.Combine(wowFolder, Path.Combine("Interface", "GameMargins-staging"));
            var staged = Path.Combine(staging, Folder);
            var backupRoot = Path.Combine(wowFolder, Path.Combine("Interface", "GameMargins-backup"));

            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            Directory.CreateDirectory(staging);
            try
            {
                var files = 0;
                using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var name = entry.FullName.Replace('\\', '/');
                        if (name.EndsWith("/", StringComparison.Ordinal)) continue;
                        var parts = name.Split('/');
                        if (parts.Length < 2 || parts[0] != Folder) throw new AddonException(Texts.T("The zip holds files outside the GameMargins folder; nothing was changed."));
                        foreach (var part in parts)
                        {
                            if (part.Length == 0 || part == "." || part == ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new AddonException(Texts.T("The zip holds files outside the GameMargins folder; nothing was changed."));
                        }
                        var dest = Path.Combine(staging, Path.Combine(parts));
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        using (var src = entry.Open())
                        using (var dst = File.Create(dest))
                        {
                            src.CopyTo(dst);
                        }
                        files++;
                    }
                }
                if (files == 0 || !Directory.Exists(staged)) throw new AddonException(Texts.T("The zip holds files outside the GameMargins folder; nothing was changed."));
                var stagedVersion = TocVersion(Path.Combine(staged, Folder + ".toc"));
                if (stagedVersion != version) throw new AddonException(Texts.T("The download is not version {0}; nothing was changed.", version));

                // The last look before anything in the game folder changes.
                if (WowRunning()) throw new AddonException(Texts.T("not installed · installs when WoW is closed"));

                Directory.CreateDirectory(addons);
                string backup = null;
                if (Directory.Exists(target))
                {
                    Directory.CreateDirectory(backupRoot);
                    backup = Path.Combine(backupRoot, Folder + "-" + SafeName(installed ?? "unknown"));
                    if (Directory.Exists(backup)) Directory.Delete(backup, true);
                    Directory.Move(target, backup);
                }
                try
                {
                    Directory.Move(staged, target);
                }
                catch (Exception)
                {
                    if (backup != null && !Directory.Exists(target) && Directory.Exists(backup)) Directory.Move(backup, target);
                    throw;
                }
                PruneBackups(backupRoot);
            }
            finally
            {
                try { if (Directory.Exists(staging)) Directory.Delete(staging, true); } catch (Exception) { }
            }
        }

        private static string SafeName(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s) sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
            return sb.Length > 0 ? sb.ToString() : "unknown";
        }

        private static void PruneBackups(string backupRoot)
        {
            try
            {
                if (!Directory.Exists(backupRoot)) return;
                var dirs = new List<DirectoryInfo>();
                foreach (var d in new DirectoryInfo(backupRoot).GetDirectories(Folder + "-*")) dirs.Add(d);
                dirs.Sort(delegate(DirectoryInfo a, DirectoryInfo b) { return b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc); });
                for (var i = BackupsKept; i < dirs.Count; i++) dirs[i].Delete(true);
            }
            catch (Exception)
            {
                // Old backups that can't be removed stay; nothing else depends on them.
            }
        }
    }
}
