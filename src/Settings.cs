// What the program remembers between starts, in %LOCALAPPDATA%\GameMargins\Uploader\settings.json: the site the
// token belongs to, the token (protected with DPAPI for this Windows user: another user or another computer can't read
// it), the WoW folder, the account's language, the three ticks of the tray menu, the newest program version a
// notification already announced, the hashes of the files already uploaded (so a restart doesn't send them again) and
// the last uploads the window lists. Never an account folder name, never the file itself.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace GameMargins.Uploader
{
    internal sealed class UploadRow
    {
        public DateTime At;
        /// <summary>"eu-normal-horde" as the site names the market, or null for an upload that stored no scan.</summary>
        public string Market;
        public long Auctions;
        /// <summary>stored, duplicate or skipped.</summary>
        public string Status;
    }

    internal sealed class Settings
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("GameMargins.Uploader.token.v1");
        private const int UploadedKept = 24;
        private const int RowsKept = 8;

        public string Site;
        public string WowFolder;
        public string Locale;
        /// <summary>The tray's "Start with Windows": on from the first link unless turned off.</summary>
        public bool StartWithWindows;
        /// <summary>Whether the first link already switched the autostart on, so a player's later "off" stays off.</summary>
        public bool AutostartDecided;
        /// <summary>The tray's "Keep the addon up to date": on by default; off for players who use CurseForge or Wago.</summary>
        public bool KeepAddonUpdated = true;
        /// <summary>The tray's "Keep the program up to date": on by default; off, the menu offers "Update to x.y.z" instead.</summary>
        public bool KeepProgramUpdated = true;
        /// <summary>The newer version the "Version x.y.z is ready" notification announced, so it comes once per version.</summary>
        public string UpdateNoted;
        /// <summary>The token in the clear while the program runs; stored protected.</summary>
        public string Token;
        public List<string> Uploaded = new List<string>();
        public List<UploadRow> Uploads = new List<UploadRow>();

        public static string Folder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.Combine("GameMargins", "Uploader")); }
        }

        private static string File
        {
            get { return Path.Combine(Folder, "settings.json"); }
        }

        public static Settings Load()
        {
            var s = new Settings();
            try
            {
                if (!System.IO.File.Exists(File)) return s;
                var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(System.IO.File.ReadAllText(File, Encoding.UTF8));
                if (json == null) return s;
                s.Site = Json.Str(json, "site");
                s.WowFolder = Json.Str(json, "wowFolder");
                s.Locale = Json.Str(json, "locale");
                s.StartWithWindows = Json.Bool(json, "startWithWindows");
                s.AutostartDecided = Json.Bool(json, "autostartDecided");
                s.KeepAddonUpdated = !Json.Has(json, "keepAddonUpdated") || Json.Bool(json, "keepAddonUpdated");
                s.KeepProgramUpdated = !Json.Has(json, "keepProgramUpdated") || Json.Bool(json, "keepProgramUpdated");
                s.UpdateNoted = Json.Str(json, "updateNoted");
                s.Token = Unprotect(Json.Str(json, "token"));
                foreach (var h in Json.List(json, "uploaded"))
                {
                    var hash = h as string;
                    if (hash != null) s.Uploaded.Add(hash);
                }
                foreach (var r in Json.List(json, "uploads"))
                {
                    var row = r as Dictionary<string, object>;
                    if (row == null) continue;
                    DateTime at;
                    if (!DateTime.TryParse(Json.Str(row, "at"), null, System.Globalization.DateTimeStyles.RoundtripKind, out at)) continue;
                    s.Uploads.Add(new UploadRow { At = at, Market = Json.Str(row, "market"), Auctions = Json.Long(row, "auctions"), Status = Json.Str(row, "status") ?? "stored" });
                }
            }
            catch (Exception)
            {
                // A damaged file starts over: the program asks to be linked again, nothing worse.
            }
            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                var uploads = new List<Dictionary<string, object>>();
                foreach (var r in Uploads)
                {
                    uploads.Add(new Dictionary<string, object> { { "at", r.At.ToUniversalTime().ToString("o") }, { "market", r.Market }, { "auctions", r.Auctions }, { "status", r.Status } });
                }
                var json = new Dictionary<string, object>
                {
                    { "site", Site },
                    { "wowFolder", WowFolder },
                    { "locale", Locale },
                    { "startWithWindows", StartWithWindows },
                    { "autostartDecided", AutostartDecided },
                    { "keepAddonUpdated", KeepAddonUpdated },
                    { "keepProgramUpdated", KeepProgramUpdated },
                    { "updateNoted", UpdateNoted },
                    { "token", Protect(Token) },
                    { "uploaded", Uploaded },
                    { "uploads", uploads },
                };
                var text = new JavaScriptSerializer().Serialize(json);
                var tmp = File + ".tmp";
                System.IO.File.WriteAllText(tmp, text, new UTF8Encoding(false));
                if (System.IO.File.Exists(File)) System.IO.File.Replace(tmp, File, null);
                else System.IO.File.Move(tmp, File);
            }
            catch (Exception)
            {
                // Nothing to do but try again on the next change; the window keeps working from memory.
            }
        }

        /// <summary>Remembers a file as uploaded by the hash of its text.</summary>
        public void MarkUploaded(string hash)
        {
            Uploaded.Remove(hash);
            Uploaded.Insert(0, hash);
            while (Uploaded.Count > UploadedKept) Uploaded.RemoveAt(Uploaded.Count - 1);
        }

        public void AddUpload(UploadRow row)
        {
            Uploads.Insert(0, row);
            while (Uploads.Count > RowsKept) Uploads.RemoveAt(Uploads.Count - 1);
        }

        private static string Protect(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(bytes);
        }

        private static string Unprotect(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return null;
            try
            {
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(stored), Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                // Another user's or another computer's file: not ours to read. Link again.
                return null;
            }
        }
    }

    /// <summary>Reading the loose dictionaries JavaScriptSerializer gives, without a dependency on a JSON library.</summary>
    internal static class Json
    {
        public static bool Has(Dictionary<string, object> d, string key)
        {
            return d != null && d.ContainsKey(key);
        }

        public static string Str(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) && v != null ? Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) : null;
        }

        public static bool Bool(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) && v is bool && (bool)v;
        }

        public static long Long(Dictionary<string, object> d, string key)
        {
            object v;
            if (d == null || !d.TryGetValue(key, out v) || v == null) return 0;
            try { return Convert.ToInt64(v, System.Globalization.CultureInfo.InvariantCulture); }
            catch (Exception) { return 0; }
        }

        /// <summary>A JSON array as a list. JavaScriptSerializer hands nested arrays over as a non-generic ArrayList, so anything enumerable but a string counts.</summary>
        public static IList<object> List(Dictionary<string, object> d, string key)
        {
            object v;
            var result = new List<object>();
            if (d == null || !d.TryGetValue(key, out v) || v == null || v is string) return result;
            var items = v as System.Collections.IEnumerable;
            if (items == null) return result;
            foreach (var item in items) result.Add(item);
            return result;
        }

        public static Dictionary<string, object> Object(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v as Dictionary<string, object> : null;
        }

        public static Dictionary<string, object> Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                var s = new JavaScriptSerializer();
                s.MaxJsonLength = int.MaxValue;
                return s.Deserialize<Dictionary<string, object>>(text);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string Write(Dictionary<string, object> d)
        {
            var s = new JavaScriptSerializer();
            s.MaxJsonLength = int.MaxValue;
            return s.Serialize(d);
        }
    }
}
