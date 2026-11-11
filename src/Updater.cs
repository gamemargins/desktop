// The program keeping itself up to date: "Keep the program up to date" in the tray menu, on by default; with it off the
// menu offers "Update to x.y.z" instead and one click does the same. At start and once a day the program reads one small
// file from our site, https://gamemargins.com/wow-forever/uploader.json, which names the newest version, its exe on the
// public repository's GitHub release and that exe's SHA-256. The request carries nothing but the program's user agent:
// no token, nothing about the account or the game. Only a newer version is taken (never an older one, nor one whose
// number can't be read), only from https://github.com/gamemargins/desktop/releases/download/ (over HTTPS, through
// GitHub's redirect to its own download host, at most 10 MB), and only when the download's SHA-256 is the one the site
// named: a mismatch keeps nothing. The new exe goes next to the settings, as
// %LOCALAPPDATA%\GameMargins\Uploader\GameMarginsDesktop-<version>.exe, and must carry that version in its file data
// before it is used. MainForm then points autostart and the Start-menu shortcut at it, starts it with
// --updated-from <this exe> and quits, once no upload is under way; the new copy waits for the old one to have gone.
// A copy started from elsewhere, such as an old download on the desktop, that finds a newer one installed there starts
// that one instead (Program.Main).

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameMargins.Uploader
{
    /// <summary>A version the site offers: its number, its exe on the GitHub release and that exe's SHA-256 (lower-case hex).</summary>
    internal sealed class ProgramRelease
    {
        public string Version;
        public Uri Url;
        public string Sha256;
    }

    internal enum UpdateOutcome
    {
        /// <summary>The new exe is in place and checked: UpdateResult.Exe names it.</summary>
        Ready,
        /// <summary>GitHub could not be reached, or answered with an error or a redirect elsewhere: nothing was written.</summary>
        Unreachable,
        /// <summary>The download was not the file the site named (its SHA-256, the size cap, its version): nothing was kept.</summary>
        Mismatch,
        /// <summary>The file could not be written next to the settings: nothing was kept.</summary>
        Failed,
    }

    internal sealed class UpdateResult
    {
        public UpdateOutcome Outcome;
        public string Exe;
    }

    /// <summary>One answer from the web, as far as the updater needs it.</summary>
    internal sealed class WebAnswer
    {
        public int Status;
        /// <summary>A redirect's target, made absolute; null when the answer named none.</summary>
        public Uri Location;
        /// <summary>The body of a 2xx answer; null for any other answer and for a body over the cap.</summary>
        public byte[] Body;
        public bool TooBig;
    }

    /// <summary>The updater's one way to the network: a plain GET that follows no redirect by itself and sends no token.</summary>
    internal interface IUpdateWeb
    {
        Task<WebAnswer> Get(Uri url, long maxBytes);
    }

    internal sealed class HttpUpdateWeb : IUpdateWeb
    {
        private readonly HttpClient _http;

        public HttpUpdateWeb()
        {
            // Redirects are followed by Updater, which checks each one; no cookies, no Authorization header, ever.
            _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { Timeout = TimeSpan.FromSeconds(120) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("GameMarginsDesktop/" + Program.Version + " (Windows)");
        }

        public async Task<WebAnswer> Get(Uri url, long maxBytes)
        {
            using (var res = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                var answer = new WebAnswer { Status = (int)res.StatusCode };
                var location = res.Headers.Location;
                if (location != null) answer.Location = location.IsAbsoluteUri ? location : new Uri(url, location);
                if (!res.IsSuccessStatusCode) return answer;
                var length = res.Content.Headers.ContentLength;
                if (length.HasValue && length.Value > maxBytes)
                {
                    answer.TooBig = true;
                    return answer;
                }
                using (var stream = await res.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var buffer = new MemoryStream())
                {
                    var chunk = new byte[81920];
                    int read;
                    while ((read = await stream.ReadAsync(chunk, 0, chunk.Length).ConfigureAwait(false)) > 0)
                    {
                        if (buffer.Length + read > maxBytes)
                        {
                            answer.TooBig = true;
                            return answer;
                        }
                        buffer.Write(chunk, 0, read);
                    }
                    answer.Body = buffer.ToArray();
                }
                return answer;
            }
        }
    }

    internal sealed class Updater
    {
        /// <summary>The one address the program asks which version is newest: our own site, this path only.</summary>
        public static readonly Uri VersionFile = new Uri("https://gamemargins.com/wow-forever/uploader.json");
        /// <summary>The one place a new exe comes from: the public repository's GitHub releases.</summary>
        public const string ReleasePrefix = "https://github.com/gamemargins/desktop/releases/download/";
        public const long MaxExeBytes = 10L * 1024 * 1024;
        private const long MaxVersionFileBytes = 16 * 1024;
        private const int MaxRedirects = 5;
        private const string ExePrefix = "GameMarginsDesktop-";
        private const string ProductName = "GameMargins Desktop";
        private static readonly Regex Number = new Regex("^[0-9]{1,5}\\.[0-9]{1,5}\\.[0-9]{1,5}$", RegexOptions.CultureInvariant);
        private static readonly Regex ReleaseFile = new Regex("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\\.exe$", RegexOptions.CultureInvariant);
        private static readonly Regex InstalledName = new Regex("^GameMarginsDesktop-([0-9]{1,5}\\.[0-9]{1,5}\\.[0-9]{1,5})\\.exe$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex Hex = new Regex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant);

        private readonly IUpdateWeb _web;
        private readonly string _folder;
        private readonly string _current;

        /// <param name="web">The network (HttpUpdateWeb; a fake one in tests).</param>
        /// <param name="folder">Where new versions go: the settings' folder.</param>
        /// <param name="current">The running version, Program.Version.</param>
        public Updater(IUpdateWeb web, string folder, string current)
        {
            _web = web;
            _folder = folder;
            _current = current;
        }

        // ------------------------------------------------------------------------------------------------ the rules

        /// <summary>"x.y.z" with numbers only, as Program.Version is written; null for anything else ("0.3", "0.2.6-beta", "latest").</summary>
        public static Version ParseVersion(string v)
        {
            if (v == null || !Number.IsMatch(v)) return null;
            Version parsed;
            return Version.TryParse(v, out parsed) ? parsed : null;
        }

        /// <summary>True only when both versions can be read and `candidate` is the newer: never a downgrade, never a guess.</summary>
        public static bool IsNewer(string candidate, string current)
        {
            var a = ParseVersion(candidate);
            var b = ParseVersion(current);
            return a != null && b != null && a > b;
        }

        /// <summary>
        /// The address as a Uri when it is a file of the public repository's releases
        /// (https://github.com/gamemargins/desktop/releases/download/&lt;tag&gt;/&lt;name&gt;.exe, plain letters, digits,
        /// dots, dashes and underscores, nothing after it); null for anything else.
        /// </summary>
        public static Uri AllowedDownload(string url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith(ReleasePrefix, StringComparison.Ordinal)) return null;
            var rest = url.Substring(ReleasePrefix.Length);
            if (!ReleaseFile.IsMatch(rest)) return null;
            foreach (var part in rest.Split('/')) if (part == "." || part == "..") return null;
            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed)) return null;
            if (parsed.Scheme != Uri.UriSchemeHttps || parsed.Host != "github.com" || !parsed.IsDefaultPort || parsed.UserInfo.Length > 0) return null;
            if (parsed.Query.Length > 0 || parsed.Fragment.Length > 0) return null;
            return parsed.AbsoluteUri == url ? parsed : null;
        }

        /// <summary>
        /// Where a release download may be sent on: GitHub's own download hosts (*.githubusercontent.com), or the releases
        /// themselves; HTTPS on the standard port only.
        /// </summary>
        public static bool AllowedRedirect(Uri to)
        {
            if (to == null || !to.IsAbsoluteUri || to.Scheme != Uri.UriSchemeHttps || !to.IsDefaultPort || to.UserInfo.Length > 0) return false;
            var host = to.Host.ToLowerInvariant();
            if (host.EndsWith(".githubusercontent.com", StringComparison.Ordinal)) return true;
            return host == "github.com" && to.AbsolutePath.StartsWith(new Uri(ReleasePrefix).AbsolutePath, StringComparison.Ordinal);
        }

        /// <summary>The site's version file, { "version", "url", "sha256" }, when every part of it is acceptable; null otherwise.</summary>
        public static ProgramRelease ReadVersionFile(string text)
        {
            var json = Json.Parse(text);
            if (json == null) return null;
            var version = Json.Str(json, "version");
            if (ParseVersion(version) == null) return null;
            var url = AllowedDownload(Json.Str(json, "url"));
            if (url == null) return null;
            var sha = (Json.Str(json, "sha256") ?? "").Trim().ToLowerInvariant();
            if (!Hex.IsMatch(sha)) return null;
            return new ProgramRelease { Version = version, Url = url, Sha256 = sha };
        }

        /// <summary>Where version `version` lives once the program installed it: GameMarginsDesktop-&lt;version&gt;.exe in `folder`.</summary>
        public static string InstalledPath(string folder, string version)
        {
            return Path.Combine(folder, ExePrefix + version + ".exe");
        }

        /// <summary>Whether the file at `path` is this program in version `version`, by its file data (read from the file, never by running it).</summary>
        public static bool FileIs(string path, string version)
        {
            try
            {
                if (!File.Exists(path)) return false;
                var info = FileVersionInfo.GetVersionInfo(path);
                return info.ProductName == ProductName && info.ProductVersion == version && info.FileVersion == version + ".0";
            }
            catch (Exception)
            {
                return false;
            }
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

        private static bool SamePath(string a, string b)
        {
            try
            {
                return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Whether `exe` is a copy the program installed itself: GameMarginsDesktop-x.y.z.exe directly in `folder`.</summary>
        public static bool IsInstalledCopy(string folder, string exe)
        {
            try
            {
                if (string.IsNullOrEmpty(exe) || string.IsNullOrEmpty(folder)) return false;
                var dir = Path.GetDirectoryName(Path.GetFullPath(exe));
                return dir != null && SamePath(dir, folder) && InstalledName.IsMatch(Path.GetFileName(exe));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// The newest copy in `folder` that is newer than `current` and is what its name says (its file data), other than
        /// `running`; null when there is none. An older copy started from elsewhere hands over to it.
        /// </summary>
        public static string NewestInstalled(string folder, string current, string running)
        {
            try
            {
                if (!Directory.Exists(folder)) return null;
                string best = null;
                var bestVersion = ParseVersion(current);
                if (bestVersion == null) return null;
                foreach (var file in Directory.GetFiles(folder, ExePrefix + "*.exe"))
                {
                    var m = InstalledName.Match(Path.GetFileName(file));
                    if (!m.Success || SamePath(file, running)) continue;
                    var version = ParseVersion(m.Groups[1].Value);
                    if (version == null || version <= bestVersion || !FileIs(file, m.Groups[1].Value)) continue;
                    best = file;
                    bestVersion = version;
                }
                return best;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Whether the hand-over to a new exe may happen now: no upload and no addon install under way, no dialog open, and
        /// for an update by itself no open window either (after a click on "Update to" the player is expecting it).
        /// </summary>
        public static bool MayHandOver(int uploadsRunning, bool addonBusy, bool dialogOpen, bool windowOpen, bool clicked)
        {
            return uploadsRunning == 0 && !addonBusy && !dialogOpen && (clicked || !windowOpen);
        }

        /// <summary>A path as one command-line argument.</summary>
        public static string Quote(string path)
        {
            return "\"" + path.TrimEnd('\\') + "\"";
        }

        /// <summary>Starts `exe` with `arguments`; false when Windows would not start it.</summary>
        public static bool Start(string exe, string arguments)
        {
            try
            {
                using (var process = Process.Start(new ProcessStartInfo(exe, arguments) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe) }))
                {
                    return process != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ------------------------------------------------------------------------------------------------ the network

        /// <summary>The site's newest version when it is newer than this one; null when it isn't, can't be read, or the site can't be reached.</summary>
        public async Task<ProgramRelease> Check()
        {
            WebAnswer answer;
            try
            {
                answer = await _web.Get(VersionFile, MaxVersionFileBytes).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return null;
            }
            if (answer == null || answer.Status != 200 || answer.Body == null) return null;
            string text;
            try { text = new UTF8Encoding(false, true).GetString(answer.Body); }
            catch (Exception) { return null; }
            var release = ReadVersionFile(text);
            return release != null && IsNewer(release.Version, _current) ? release : null;
        }

        /// <summary>
        /// Downloads `release` from GitHub and puts it next to the settings when its SHA-256 is the site's and its file data
        /// names that version; a copy already there that passes the same checks is used as it is. Nothing is kept otherwise.
        /// </summary>
        public async Task<UpdateResult> Fetch(ProgramRelease release)
        {
            if (release == null || release.Url == null || AllowedDownload(release.Url.AbsoluteUri) == null || !IsNewer(release.Version, _current) || !Hex.IsMatch(release.Sha256 ?? ""))
            {
                return new UpdateResult { Outcome = UpdateOutcome.Mismatch };
            }
            var target = InstalledPath(_folder, release.Version);
            if (FileIs(target, release.Version) && HashOfFile(target) == release.Sha256) return new UpdateResult { Outcome = UpdateOutcome.Ready, Exe = target };

            WebAnswer answer;
            try
            {
                answer = await Follow(release.Url).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return new UpdateResult { Outcome = UpdateOutcome.Unreachable };
            }
            if (answer == null) return new UpdateResult { Outcome = UpdateOutcome.Unreachable };
            if (answer.TooBig) return new UpdateResult { Outcome = UpdateOutcome.Mismatch };
            if (answer.Status != 200 || answer.Body == null) return new UpdateResult { Outcome = UpdateOutcome.Unreachable };
            if (Sha256Hex(answer.Body) != release.Sha256) return new UpdateResult { Outcome = UpdateOutcome.Mismatch };

            var part = target + ".part";
            try
            {
                Directory.CreateDirectory(_folder);
                File.WriteAllBytes(part, answer.Body);
                if (!FileIs(part, release.Version))
                {
                    Delete(part);
                    return new UpdateResult { Outcome = UpdateOutcome.Mismatch };
                }
                if (File.Exists(target)) File.Delete(target);
                File.Move(part, target);
            }
            catch (Exception)
            {
                Delete(part);
                return new UpdateResult { Outcome = UpdateOutcome.Failed };
            }
            return new UpdateResult { Outcome = UpdateOutcome.Ready, Exe = target };
        }

        /// <summary>The release's file, following GitHub's redirects only where AllowedRedirect lets them go; null when one leads elsewhere.</summary>
        private async Task<WebAnswer> Follow(Uri url)
        {
            var current = url;
            for (var hop = 0; hop <= MaxRedirects; hop++)
            {
                var answer = await _web.Get(current, MaxExeBytes).ConfigureAwait(false);
                if (answer == null) return null;
                var redirect = answer.Status == 301 || answer.Status == 302 || answer.Status == 303 || answer.Status == 307 || answer.Status == 308;
                if (!redirect) return answer;
                if (!AllowedRedirect(answer.Location)) return null;
                current = answer.Location;
            }
            return null;
        }

        /// <summary>
        /// After an update: the copy it replaced, when that one was installed in `folder` too (a copy elsewhere, such as a
        /// download on the desktop, is the player's and stays). Tried for a few seconds while the old process ends.
        /// </summary>
        public static async Task RemoveReplaced(string folder, string replaced, string running)
        {
            if (!IsInstalledCopy(folder, replaced) || SamePath(replaced, running)) return;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    if (File.Exists(replaced)) File.Delete(replaced);
                    return;
                }
                catch (Exception)
                {
                    // Still in use while the old process ends: once more in a second.
                }
                await Task.Delay(1000);
            }
        }

        private static string HashOfFile(string path)
        {
            try { return Sha256Hex(File.ReadAllBytes(path)); }
            catch (Exception) { return null; }
        }

        private static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception) { }
        }
    }
}
