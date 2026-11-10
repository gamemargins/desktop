// The site's routes the program talks to (/api/wow/uploader/…): the device flow that links it
// (link, link/poll), the status check (the token still good, the account's language, the versions the site offers,
// the addon zip with its hash), the upload itself with "Authorization: Bearer gm_wu_…", and the plain download of
// the addon zip. Nothing else on the site takes that token.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GameMargins.Uploader
{
    internal sealed class LinkStart
    {
        public string DeviceCode;
        public string UserCode;
        public string VerifyUrl;
        public int IntervalSeconds;
        public DateTime ExpiresAt;
    }

    internal sealed class PollResult
    {
        /// <summary>pending, ok, expired, unknown or offline.</summary>
        public string Status;
        public string Token;
        public string Locale;
    }

    /// <summary>The addon zip as the site serves it: what to install, and what the download must hash to.</summary>
    internal sealed class AddonInfo
    {
        public string Version;
        public long Size;
        /// <summary>Lower-case hex SHA-256.</summary>
        public string Sha256;
        public string Url;
    }

    internal sealed class StatusResult
    {
        /// <summary>ok, unlinked (401) or offline.</summary>
        public string Status;
        public string Locale;
        public string Latest;
        public string Download;
        /// <summary>Null when the site has no zip to offer (a dev server before its build).</summary>
        public AddonInfo Addon;
    }

    internal sealed class ScanResult
    {
        public string Status;
        public string Market;
        public long Auctions;
        /// <summary>For a skipped scan: the site's English words, and its code (the program speaks the code in the player's language).</summary>
        public string Why;
        public string Code;
    }

    internal sealed class UploadResult
    {
        /// <summary>ok, unlinked (401), refused (the site said why: Error and Code) or offline.</summary>
        public string Status;
        public string Error;
        public string Code;
        /// <summary>Seconds to wait when the site answered 429 (its Retry-After); 0 otherwise.</summary>
        public int RetryAfter;
        public bool Stored;
        public List<ScanResult> Results = new List<ScanResult>();
        public int Trainers;
        public int Vendors;
        /// <summary>The characters the file names, now kept in the account (level, class, professions and recipes; 0.2.2).</summary>
        public int Characters;
    }

    internal sealed class Api
    {
        private readonly HttpClient _http;

        public Api(string site)
        {
            _http = new HttpClient { BaseAddress = new Uri(site + "/"), Timeout = TimeSpan.FromSeconds(90) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("GameMarginsUploader/" + Program.Version + " (Windows)");
        }

        public async Task<LinkStart> StartLink()
        {
            try
            {
                var res = await _http.PostAsync("api/wow/uploader/link", new StringContent("", Encoding.UTF8, "application/json")).ConfigureAwait(false);
                if (!res.IsSuccessStatusCode) return null;
                var json = Json.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
                if (json == null) return null;
                var interval = (int)Json.Long(json, "interval");
                var expiresIn = (int)Json.Long(json, "expiresIn");
                return new LinkStart
                {
                    DeviceCode = Json.Str(json, "deviceCode"),
                    UserCode = Json.Str(json, "userCode"),
                    VerifyUrl = Json.Str(json, "verifyUrl"),
                    IntervalSeconds = interval > 0 ? interval : 5,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn > 0 ? expiresIn : 600),
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<PollResult> Poll(string deviceCode)
        {
            try
            {
                var body = Json.Write(new Dictionary<string, object> { { "deviceCode", deviceCode } });
                var res = await _http.PostAsync("api/wow/uploader/link/poll", new StringContent(body, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                var json = Json.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
                if (res.StatusCode == HttpStatusCode.Gone) return new PollResult { Status = "expired" };
                if (res.StatusCode == HttpStatusCode.NotFound) return new PollResult { Status = "unknown" };
                if (!res.IsSuccessStatusCode || json == null) return new PollResult { Status = "offline" };
                return new PollResult { Status = Json.Str(json, "status") ?? "pending", Token = Json.Str(json, "token"), Locale = Json.Str(json, "locale") };
            }
            catch (Exception)
            {
                return new PollResult { Status = "offline" };
            }
        }

        public async Task<StatusResult> Status(string token)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, "api/wow/uploader/status");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var res = await _http.SendAsync(req).ConfigureAwait(false);
                if (res.StatusCode == HttpStatusCode.Unauthorized) return new StatusResult { Status = "unlinked" };
                var json = Json.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
                if (!res.IsSuccessStatusCode || json == null) return new StatusResult { Status = "offline" };
                var result = new StatusResult { Status = "ok", Locale = Json.Str(json, "locale"), Latest = Json.Str(json, "latest"), Download = Json.Str(json, "download") };
                var addon = Json.Object(json, "addon");
                if (addon != null)
                {
                    var info = new AddonInfo { Version = Json.Str(addon, "version"), Size = Json.Long(addon, "size"), Sha256 = (Json.Str(addon, "sha256") ?? "").ToLowerInvariant(), Url = Json.Str(addon, "url") };
                    if (!string.IsNullOrEmpty(info.Version) && info.Size > 0 && info.Sha256.Length == 64 && !string.IsNullOrEmpty(info.Url)) result.Addon = info;
                }
                return result;
            }
            catch (Exception)
            {
                return new StatusResult { Status = "offline" };
            }
        }

        /// <summary>Uploads the text of one GameMargins.lua, exactly what the upload page sends.</summary>
        public async Task<UploadResult> Upload(string token, string file)
        {
            try
            {
                var body = Json.Write(new Dictionary<string, object> { { "file", file } });
                var req = new HttpRequestMessage(HttpMethod.Post, "api/wow/uploader/scans") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var res = await _http.SendAsync(req).ConfigureAwait(false);
                if (res.StatusCode == HttpStatusCode.Unauthorized) return new UploadResult { Status = "unlinked" };
                var json = Json.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
                if (!res.IsSuccessStatusCode)
                {
                    var error = Json.Str(json, "error");
                    if (res.StatusCode == (HttpStatusCode)429)
                    {
                        var seconds = 60;
                        if (res.Headers.RetryAfter != null && res.Headers.RetryAfter.Delta.HasValue) seconds = Math.Max(1, (int)res.Headers.RetryAfter.Delta.Value.TotalSeconds);
                        return new UploadResult { Status = "refused", Code = "rate-limited", Error = error, RetryAfter = seconds };
                    }
                    return new UploadResult { Status = error != null ? "refused" : "offline", Error = error, Code = Json.Str(json, "code") };
                }
                if (json == null) return new UploadResult { Status = "offline" };
                var result = new UploadResult { Status = "ok", Stored = Json.Bool(json, "stored"), Trainers = (int)Json.Long(json, "trainers"), Vendors = (int)Json.Long(json, "vendors"), Characters = (int)Json.Long(json, "characters") };
                foreach (var r in Json.List(json, "results"))
                {
                    var d = r as Dictionary<string, object>;
                    if (d == null) continue;
                    result.Results.Add(new ScanResult { Status = Json.Str(d, "status") ?? "skipped", Market = Json.Str(d, "market"), Auctions = Json.Long(d, "auctions"), Why = Json.Str(d, "why"), Code = Json.Str(d, "code") });
                }
                return result;
            }
            catch (Exception)
            {
                return new UploadResult { Status = "offline" };
            }
        }

        /// <summary>The path of `url` on the site at `site` (its host, if any, left out); null if that can't be made.</summary>
        public static Uri OnOwnSite(Uri site, string url)
        {
            if (site == null || string.IsNullOrEmpty(url)) return null;
            Uri parsed;
            var path = Uri.TryCreate(url, UriKind.Absolute, out parsed) ? parsed.PathAndQuery : url;
            Uri target;
            if (!Uri.TryCreate(site, path.TrimStart('/', '\\'), out target)) return null;
            return Uri.Compare(target, site, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0 ? target : null;
        }

        /// <summary>
        /// The addon zip's bytes, or null when it can't be fetched. Capped at 20 MB. Only the path of the address the status
        /// named is used, on the site this program is linked to: a status that named another host still downloads from
        /// our own site.
        /// </summary>
        public async Task<byte[]> Download(string url)
        {
            try
            {
                var target = OnOwnSite(_http.BaseAddress, url);
                if (target == null) return null;
                var res = await _http.GetAsync(target, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (!res.IsSuccessStatusCode) return null;
                if (res.Content.Headers.ContentLength.HasValue && res.Content.Headers.ContentLength.Value > 20L * 1024 * 1024) return null;
                var bytes = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                return bytes.LongLength > 20L * 1024 * 1024 ? null : bytes;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
