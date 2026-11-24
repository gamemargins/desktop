// What the program remembers between starts, in %LOCALAPPDATA%\GameMargins\Uploader\settings.json: the site the
// tokens belong to, the WoW token (protected with DPAPI for this Windows user: another user or another computer can't
// read it), the WoW folder, the language, the switches of the window's Settings (Start with Windows, Keep the program
// up to date, Keep the addon up to date, Appearance), the newest program version a notification already announced,
// the version the program last updated itself to and one that did not stay up, the hashes of the files already
// uploaded (so a restart doesn't send them again) and the last uploads the window lists. Never an account folder
// name, never the file itself. For Tempo: which games the program is used for, Tempo's own token (protected the same
// way), whether the player read what Tempo looks at and said yes, Tempo's own switches (Start Tempo with MapleStory,
// Show the float, Lock the float), and where its window and its float stood.

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
        /// <summary>Settings' "Start with Windows": on from the first link unless turned off.</summary>
        public bool StartWithWindows;
        /// <summary>Whether the first link already switched the autostart on, so a player's later "off" stays off.</summary>
        public bool AutostartDecided;
        /// <summary>Settings' "Keep the addon up to date": on by default; off for players who use CurseForge or Wago.</summary>
        public bool KeepAddonUpdated = true;
        /// <summary>Settings' "Keep the program up to date": on by default; off, a newer version waits for a click ("Update now" on Settings, "Update to x.y.z" in the menu).</summary>
        public bool KeepProgramUpdated = true;
        /// <summary>The newer version the "Version x.y.z is ready" notification announced, so it comes once per version.</summary>
        public string UpdateNoted;
        /// <summary>The token in the clear while the program runs; stored protected.</summary>
        public string Token;
        public List<string> Uploaded = new List<string>();
        public List<UploadRow> Uploads = new List<UploadRow>();

        /// <summary>Whether "Which games do you play?" was answered. A settings file from before 0.4.0 counts as answered: WoW.</summary>
        public bool GamesAsked;
        public bool Wow;
        public bool Maple;
        /// <summary>Tempo's token (scope ms-tempo), in the clear while the program runs; stored protected, apart from the WoW token.</summary>
        public string TempoToken;
        /// <summary>The player read what Tempo looks at and said yes; asked once, before Tempo first watches.</summary>
        public bool TempoConsent;
        /// <summary>"Start Tempo with MapleStory": off unless the player ticks it. Only while on does the program look for the game's window by itself.</summary>
        public bool StartTempoWithGame;
        /// <summary>"Show the float": the small always-on-top window, shown while Tempo is open.</summary>
        public bool FloatShown = true;
        /// <summary>"Lock the float (click through)".</summary>
        public bool FloatLocked;
        /// <summary>The float's size: "large", "small" or "bar" (the bar from 0.4.1).</summary>
        public string FloatSize = "large";
        /// <summary>The size the player dragged the large float to ("320,460", pixels at 96 dpi); null until they do.</summary>
        public string FloatLarge;
        /// <summary>Light or dark: "system" (as Windows is), "light" or "dark".</summary>
        public string Appearance = "system";
        /// <summary>The first start's language step was gone through (a copy from before it is not asked).</summary>
        public bool LanguageAsked;
        /// <summary>This copy's first start had the language step: the step bars count it.</summary>
        public bool LanguageStep;
        /// <summary>Where the float's top right corner stood, "x,y", per arrangement of screens.</summary>
        public Dictionary<string, string> FloatPlaces = new Dictionary<string, string>();
        /// <summary>Where the bar stood, "x,y,side" (x its left, its right or its middle: the side it sticks to), per arrangement of screens; apart from the float's.</summary>
        public Dictionary<string, string> FloatBarPlaces = new Dictionary<string, string>();
        /// <summary>The Tempo window's place and size, "x,y,w,h"; null until it was moved.</summary>
        public string TempoBounds;
        /// <summary>The first start's steps were gone through once: after that a step that comes back (a link the site dropped) shows without the step bars.</summary>
        public bool SetupDone;
        /// <summary>The question "May Tempo look at your MapleStory window?" was answered, with yes or with "No, not now".</summary>
        public bool TempoAsked;
        /// <summary>The language was picked in Settings: the account's language on the site no longer changes it.</summary>
        public bool LocalePicked;
        /// <summary>The version the program last updated itself to and when, for the window's line "Updated to x last night".</summary>
        public string UpdatedTo;
        public DateTime UpdatedAt;
        /// <summary>A version that did not stay up after an update: not installed again, and never started in place of this one.</summary>
        public string UpdateBad;

        /// <summary>Another folder for everything the program keeps (--data, for a test next to an installed copy); null: the real one.</summary>
        public static string FolderOverride;

        public static string Folder
        {
            get { return FolderOverride ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.Combine("GameMargins", "Uploader")); }
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
                // A file from before 0.4.0 has no games in it: that copy was used for WoW, and is not asked.
                s.GamesAsked = !Json.Has(json, "gamesAsked") || Json.Bool(json, "gamesAsked");
                s.Wow = Json.Has(json, "wow") ? Json.Bool(json, "wow") : !Json.Has(json, "gamesAsked");
                s.Maple = Json.Bool(json, "maple");
                s.TempoToken = Unprotect(Json.Str(json, "tempoToken"));
                s.TempoConsent = Json.Bool(json, "tempoConsent");
                s.StartTempoWithGame = Json.Bool(json, "startTempoWithGame");
                s.FloatShown = !Json.Has(json, "floatShown") || Json.Bool(json, "floatShown");
                s.FloatLocked = Json.Bool(json, "floatLocked");
                var floatSize = Json.Str(json, "floatSize");
                s.FloatSize = floatSize == "small" || floatSize == "bar" ? floatSize : "large";
                s.FloatLarge = Json.Str(json, "floatLarge");
                var appearance = Json.Str(json, "appearance");
                s.Appearance = appearance == "light" || appearance == "dark" ? appearance : "system";
                // A settings file from before the language step is a copy in use: its language stays as it is.
                s.LanguageAsked = !Json.Has(json, "languageAsked") || Json.Bool(json, "languageAsked");
                s.LanguageStep = Json.Bool(json, "languageStep");
                s.TempoBounds = Json.Str(json, "tempoBounds");
                s.SetupDone = Json.Bool(json, "setupDone");
                s.TempoAsked = Json.Bool(json, "tempoAsked") || s.TempoConsent;
                s.LocalePicked = Json.Bool(json, "localePicked");
                s.UpdatedTo = Json.Str(json, "updatedTo");
                DateTime updatedAt;
                if (DateTime.TryParse(Json.Str(json, "updatedAt"), null, System.Globalization.DateTimeStyles.RoundtripKind, out updatedAt)) s.UpdatedAt = updatedAt;
                s.UpdateBad = Json.Str(json, "updateBad");
                var places = Json.Object(json, "floatPlaces");
                if (places != null) foreach (var place in places) if (place.Value is string) s.FloatPlaces[place.Key] = (string)place.Value;
                var barPlaces = Json.Object(json, "floatBarPlaces");
                if (barPlaces != null) foreach (var place in barPlaces) if (place.Value is string) s.FloatBarPlaces[place.Key] = (string)place.Value;
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
                    { "gamesAsked", GamesAsked },
                    { "wow", Wow },
                    { "maple", Maple },
                    { "tempoToken", Protect(TempoToken) },
                    { "tempoConsent", TempoConsent },
                    { "startTempoWithGame", StartTempoWithGame },
                    { "floatShown", FloatShown },
                    { "floatLocked", FloatLocked },
                    { "floatSize", FloatSize },
                    { "floatLarge", FloatLarge },
                    { "appearance", Appearance },
                    { "languageAsked", LanguageAsked },
                    { "languageStep", LanguageStep },
                    { "floatPlaces", FloatPlaces },
                    { "floatBarPlaces", FloatBarPlaces },
                    { "tempoBounds", TempoBounds },
                    { "setupDone", SetupDone },
                    { "tempoAsked", TempoAsked },
                    { "localePicked", LocalePicked },
                    { "updatedTo", UpdatedTo },
                    { "updatedAt", UpdatedAt == default(DateTime) ? null : UpdatedAt.ToUniversalTime().ToString("o") },
                    { "updateBad", UpdateBad },
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

        public static double Number(Dictionary<string, object> d, string key)
        {
            object v;
            if (d == null || !d.TryGetValue(key, out v) || v == null) return 0;
            try { return Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture); }
            catch (Exception) { return 0; }
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
