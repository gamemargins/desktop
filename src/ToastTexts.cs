// The words of every notification (Toasts.Show, through MainForm.Notify): a title and one line. A notification
// comes only when the player has to do something: an upload the site refused,
// the program unlinked on the site, an update that could not be installed (or, with "Keep the program up to date" off,
// one that waits for a click). What went well shows on the icon by the clock (a green check after an upload) and in
// the window, never as a notification; an update that installed itself is one quiet line in the window afterwards.
// Windows' notification center shows a notification folded until it is opened: the title on up to two lines and only
// the first line of the text, cut at a space without a "…". So the title says what happened and the line is one short
// line that is seen whole in all six languages. The program's name stands above every notification already.

namespace GameMargins.Uploader
{
    internal static class ToastTexts
    {
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

        /// <summary>A newer version waits for a click ("Keep the program up to date" off): Settings in the window has "Update now", and the menu by the clock "Update to x.y.z".</summary>
        public static string[] UpdateReady(string version)
        {
            return Pair(Texts.T("Version {0} is ready", version), Texts.T("Open Settings to update."));
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
