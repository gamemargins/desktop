// The words of every notification (Toasts.Show, through MainForm.Notify): a title and one line. Windows' notification
// center shows a notification folded until it is opened, and that is how a player finds it after a game (Do not
// disturb, on by itself while a game runs, keeps the banner away): the title on up to two lines and only the first
// line of the text, cut at a space without a "…". So the title says what happened, and the line is one short line that
// is seen whole in all six languages; the logic harness measures every one against the line a Windows 11 notification center showed on
// 10 November 2026, when 0.2.5's "Uploaded: 244,569 auctions · EU · Normal · Horde" was cut after "EU ·". The program's
// name stands above every notification already. The window keeps the longer texts.

namespace GameMargins.Uploader
{
    internal static class ToastTexts
    {
        /// <summary>A scan stored: "Uploaded: 244,569 auctions" above the auction house, "EU · Normal · Horde".</summary>
        public static string[] Uploaded(long auctions, string market)
        {
            return Pair(Texts.T("Uploaded: {0} auctions", auctions.ToString("N0", Texts.Culture)), MainForm.MarketLabel(market));
        }

        /// <summary>An upload the site refused, by its code: "Upload failed" above the reason in short (the window says it in full).</summary>
        public static string[] UploadFailed(string code)
        {
            switch (code ?? "")
            {
                case "too-big": return Failed(Texts.T("The file is over 12 MB."));
                case "addon-code": return Failed(Texts.T("That is the addon's code file."));
                case "old-format": return Failed(Texts.T("Update the addon first."));
                case "bad-request":
                case "unreadable":
                case "not-addon-file": return Failed(Texts.T("The site couldn't read the file."));
                case "no-region":
                case "no-ruleset":
                case "no-faction":
                case "region-not-on-site":
                case "unknown-market": return Failed(Texts.T("No auction house in the scan."));
                case "hardcore":
                case "neutral-ah": return Failed(Texts.T("No Hardcore or neutral houses."));
                case "rate-limited": return Pair(Texts.T("Too many uploads"), Texts.T("Next try in {0} min.", 1));
                default: return Failed(Texts.T("The site refused the file."));
            }
        }

        /// <summary>The site refused the token (unlinked on Account › Security).</summary>
        public static string[] Unlinked()
        {
            return Pair(Texts.T("Link the desktop app again"), Texts.T("It was unlinked on the site."));
        }

        /// <summary>The addon installed or updated in the game folder.</summary>
        public static string[] Addon(string version, bool wasUpdate)
        {
            return Pair(wasUpdate ? Texts.T("Addon updated to {0}", version) : Texts.T("Addon {0} installed", version), Texts.T("In your game folder."));
        }

        /// <summary>A newer version waits for a click ("Keep the program up to date" off): "Update to x.y.z" is on top of the shield's menu.</summary>
        public static string[] UpdateReady(string version)
        {
            return Pair(Texts.T("Version {0} is ready", version), Texts.T("Right-click the shield to update."));
        }

        /// <summary>The download did not match the site's SHA-256: nothing was kept, and the site is where to get it.</summary>
        public static string[] UpdateMismatch(string version, string host)
        {
            return Pair(Texts.T("Version {0} failed the checksum; nothing was kept", version), Texts.T("Get it from {0}.", host));
        }

        /// <summary>The new version could not be downloaded or started.</summary>
        public static string[] UpdateFailed(string version)
        {
            return Pair(Texts.T("Could not update to {0}", version), Texts.T("This version keeps running."));
        }

        /// <summary>The first start of a new version: by itself (the tick on), or after a click on "Update to".</summary>
        public static string[] Updated(string version, bool byItself)
        {
            return byItself
                ? Pair(Texts.T("Updated itself to {0}", version), Texts.T("Turn this off in its menu."))
                : Pair(Texts.T("Updated to {0}", version), Texts.T("Restarted in the new version."));
        }

        /// <summary>The window closed for the first time: the program goes on by the clock.</summary>
        public static string[] StillRunning()
        {
            return Pair(Texts.T("Still running by the clock; uploads continue"), Texts.T("Right-click the icon to quit."));
        }

        private static string[] Failed(string why)
        {
            return Pair(Texts.T("Upload failed"), why);
        }

        private static string[] Pair(string title, string line)
        {
            return new[] { title, line };
        }
    }
}
