// Finding the WoW: Forever install and its accounts, and noticing when the game writes the addon's file. Per account
// one file, WTF\Account\<account>\SavedVariables\GameMargins.lua, read only when it changed and only that file:
// nothing else in the game folder is opened. The game writes the file on logout, exit and /reload; a watcher per
// folder hears it, a slow poll backs the watcher up (they can miss a write), and a short wait lets the game finish
// writing before the file is read.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    internal sealed class WowAccount
    {
        /// <summary>1, 2, …: the window never shows the folder's name (a Battle.net account name).</summary>
        public int Number;
        /// <summary>The account's SavedVariables folder.</summary>
        public string Folder;
        public string File { get { return Path.Combine(Folder, "GameMargins.lua"); } }
        public FileSystemWatcher Watcher;
        public DateTime LastWriteSeen;
        public bool Exists { get { return System.IO.File.Exists(File); } }
    }

    internal sealed class Scanner : IDisposable
    {
        public const string ForeverFolder = "_forever_";
        private static readonly TimeSpan Settle = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan PollEvery = TimeSpan.FromSeconds(60);

        private readonly System.Windows.Forms.Timer _settle = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _poll = new System.Windows.Forms.Timer();
        private readonly HashSet<string> _pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<WowAccount> _accounts = new List<WowAccount>();

        public string WowFolder { get; private set; }
        public IList<WowAccount> Accounts { get { return _accounts; } }

        /// <summary>Raised on the UI thread with the account whose file the game just wrote.</summary>
        public event Action<WowAccount> FileWritten;
        /// <summary>Raised on the UI thread when accounts appeared or went (a new account folder, a first file).</summary>
        public event Action AccountsChanged;

        public Scanner()
        {
            _settle.Interval = (int)Settle.TotalMilliseconds;
            _settle.Tick += OnSettled;
            _poll.Interval = (int)PollEvery.TotalMilliseconds;
            _poll.Tick += delegate { PollFiles(); };
        }

        /// <summary>The default install, if it has accounts; else null and the window asks once.</summary>
        public static string FindDefault()
        {
            var roots = new List<string>();
            var x86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var x64 = Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(x86)) roots.Add(Path.Combine(x86, "World of Warcraft"));
            if (!string.IsNullOrEmpty(x64)) roots.Add(Path.Combine(x64, "World of Warcraft"));
            foreach (var root in roots)
            {
                var candidate = Accept(root);
                if (candidate != null) return candidate;
            }
            return null;
        }

        /// <summary>A folder the player picked or the default: the _forever_ folder itself, or World of Warcraft with it inside; null if it has no WTF\Account.</summary>
        public static string Accept(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return null;
            var direct = Path.Combine(folder, Path.Combine("WTF", "Account"));
            if (Directory.Exists(direct)) return folder;
            var forever = Path.Combine(folder, ForeverFolder);
            if (Directory.Exists(Path.Combine(forever, Path.Combine("WTF", "Account")))) return forever;
            return null;
        }

        /// <summary>Starts watching an install. True if it has WTF\Account (accounts may still be none until the addon writes a file).</summary>
        public bool Start(string wowFolder)
        {
            Stop();
            var accepted = Accept(wowFolder);
            if (accepted == null) return false;
            WowFolder = accepted;
            Rescan();
            _poll.Start();
            return true;
        }

        public void Stop()
        {
            _poll.Stop();
            _settle.Stop();
            foreach (var a in _accounts) DisposeWatcher(a);
            _accounts.Clear();
            _pending.Clear();
        }

        /// <summary>Lists the account folders again (sorted, so the numbers stay put) and watches each SavedVariables folder.</summary>
        public void Rescan()
        {
            var accountsDir = Path.Combine(WowFolder, Path.Combine("WTF", "Account"));
            var folders = new List<string>();
            try
            {
                foreach (var dir in Directory.GetDirectories(accountsDir))
                {
                    // Each account's own folder; the client keeps no account called SavedVariables, but be safe.
                    if (string.Equals(Path.GetFileName(dir), "SavedVariables", StringComparison.OrdinalIgnoreCase)) continue;
                    folders.Add(dir);
                }
            }
            catch (Exception)
            {
                // Unreadable: no accounts for now; the next poll looks again.
            }
            folders.Sort(StringComparer.OrdinalIgnoreCase);
            var changed = folders.Count != _accounts.Count;
            for (var i = 0; i < folders.Count && !changed; i++) changed = !string.Equals(Path.GetDirectoryName(_accounts[i].Folder), folders[i], StringComparison.OrdinalIgnoreCase);
            if (!changed) return;
            foreach (var a in _accounts) DisposeWatcher(a);
            _accounts.Clear();
            for (var i = 0; i < folders.Count; i++)
            {
                var account = new WowAccount { Number = i + 1, Folder = Path.Combine(folders[i], "SavedVariables") };
                account.LastWriteSeen = account.Exists ? System.IO.File.GetLastWriteTimeUtc(account.File) : DateTime.MinValue;
                Watch(account);
                _accounts.Add(account);
            }
            if (AccountsChanged != null) AccountsChanged();
        }

        private void Watch(WowAccount account)
        {
            try
            {
                Directory.CreateDirectory(account.Folder);
                var w = new FileSystemWatcher(account.Folder, "GameMargins.lua")
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
                };
                FileSystemEventHandler handler = delegate { Touched(account); };
                w.Changed += handler;
                w.Created += handler;
                w.Renamed += delegate { Touched(account); };
                w.EnableRaisingEvents = true;
                account.Watcher = w;
            }
            catch (Exception)
            {
                // Without a watcher the poll still sees the file change, a minute late at most.
            }
        }

        private static void DisposeWatcher(WowAccount account)
        {
            if (account.Watcher == null) return;
            try { account.Watcher.EnableRaisingEvents = false; account.Watcher.Dispose(); } catch (Exception) { }
            account.Watcher = null;
        }

        /// <summary>A watcher fired (on its own thread): note the account and restart the settle timer on the UI thread.</summary>
        private void Touched(WowAccount account)
        {
            var form = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
            Action note = delegate
            {
                _pending.Add(account.Folder);
                _settle.Stop();
                _settle.Start();
            };
            if (form != null && form.IsHandleCreated && form.InvokeRequired) form.BeginInvoke(note);
            else note();
        }

        private void OnSettled(object sender, EventArgs e)
        {
            _settle.Stop();
            var folders = new List<string>(_pending);
            _pending.Clear();
            foreach (var folder in folders)
            {
                foreach (var a in _accounts)
                {
                    if (!string.Equals(a.Folder, folder, StringComparison.OrdinalIgnoreCase)) continue;
                    if (a.Exists) a.LastWriteSeen = System.IO.File.GetLastWriteTimeUtc(a.File);
                    if (FileWritten != null) FileWritten(a);
                }
            }
        }

        /// <summary>The slow poll: a file whose write time moved without a watcher event, and account folders that came or went.</summary>
        private void PollFiles()
        {
            Rescan();
            foreach (var a in _accounts)
            {
                if (!a.Exists) continue;
                var written = System.IO.File.GetLastWriteTimeUtc(a.File);
                if (written <= a.LastWriteSeen) continue;
                a.LastWriteSeen = written;
                if (FileWritten != null) FileWritten(a);
            }
        }

        /// <summary>
        /// The file's text once the game has finished writing it: a few tries a second apart, since the client may
        /// still hold it. Null when it can't be read.
        /// </summary>
        public static async Task<string> ReadWhenSettled(string path)
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    long before = new FileInfo(path).Length;
                    await Task.Delay(attempt == 0 ? 500 : 1000).ConfigureAwait(false);
                    if (new FileInfo(path).Length != before) continue;
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        return await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }
                catch (IOException)
                {
                    // Still being written, or locked: try again.
                }
                catch (UnauthorizedAccessException)
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// What of the file matters for an upload, hashed: the text with everything the site doesn't keep taken out of the
        /// `characters` table. The addon writes each character's `seen` and `money` at every logout and /reload (and its
        /// bags, its bank and the `at` of a recipe list when a window was opened), so the whole file changes every time
        /// while what the upload keeps does not; sending it again would only cost the player's quota. What the site keeps
        /// of a character since 0.2.2 stays in: the level, the class, the skills and
        /// the recipes, so a level-up or a skill-up does upload, and reaches the account. A file without that table, or
        /// one that can't be matched, is hashed whole.
        /// </summary>
        public static string Fingerprint(string text)
        {
            const string key = "[\"characters\"] = {";
            var at = text.IndexOf(key, StringComparison.Ordinal);
            if (at < 0) return Hash(text);
            var open = at + key.Length - 1;
            var close = MatchingBrace(text, open);
            if (close < 0) return Hash(text);
            return Hash(text.Substring(0, open + 1) + KeptOfCharacters(text.Substring(open + 1, close - open - 1)) + text.Substring(close));
        }

        /// <summary>The entries of a character the site never keeps: a change in one is no reason to upload.</summary>
        private static readonly string[] NotKept = { "seen", "money", "bags", "bagsAt", "bank", "bankAt", "realm", "region", "race", "faction", "at" };

        /// <summary>
        /// The inside of the characters table without the entries the site never keeps. The client writes one entry per
        /// line, `["seen"] = 1790000006,`, strings with their newlines escaped, and a nested table (`["skills"] = {`) on a
        /// line that ends in its brace; only a scalar entry under a name in NotKept is dropped, tables stay whole.
        /// </summary>
        public static string KeptOfCharacters(string table)
        {
            var lines = table.Split('\n');
            var sb = new StringBuilder(table.Length);
            for (var i = 0; i < lines.Length; i++)
            {
                if (IsNotKept(lines[i])) continue;
                if (sb.Length > 0 || i > 0) sb.Append('\n');
                sb.Append(lines[i]);
            }
            return sb.ToString();
        }

        private static bool IsNotKept(string line)
        {
            var t = line.Trim('\t', ' ', '\r');
            if (!t.StartsWith("[\"", StringComparison.Ordinal) || t.EndsWith("{", StringComparison.Ordinal)) return false;
            var end = t.IndexOf("\"] = ", 2, StringComparison.Ordinal);
            if (end < 0) return false;
            var name = t.Substring(2, end - 2);
            foreach (var n in NotKept) if (n == name) return true;
            return false;
        }

        /// <summary>The index of the brace that closes the one at `open`, skipping the client's double-quoted strings; -1 when none.</summary>
        private static int MatchingBrace(string s, int open)
        {
            var depth = 0;
            var inString = false;
            for (var i = open; i < s.Length; i++)
            {
                var c = s[i];
                if (inString)
                {
                    if (c == '\\') { i++; continue; }
                    if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return i;
            }
            return -1;
        }

        public static string Hash(string text)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public void Dispose()
        {
            Stop();
            _settle.Dispose();
            _poll.Dispose();
        }
    }
}
