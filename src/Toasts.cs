// Notifications that stay: Windows toast notifications, which land in the notification center and stay readable after
// playing, also under Do not disturb. Windows switches Do not disturb on by itself while a game runs, and it drops the
// old balloon tips (NotifyIcon.ShowBalloonTip) without a trace, not even behind the bell. A desktop program needs two
// things for toasts: an AppUserModelID
// (GameMargins.Uploader, set on the process at start) and a Start-menu shortcut that carries that id, which is how
// Windows knows the program's name and icon for the notification center and what to start when a notification is
// clicked. The shortcut, "GameMargins Desktop.lnk" under the user's own Start menu, is made at every start when it
// is missing and rewritten when the exe moved, and the program's own update points it at the new exe. The shortcut
// that versions before 0.2.5 made under the old name, "GameMargins Uploader.lnk", is removed at start (only that file,
// only when it points at a GameMargins exe), so Windows knows the program by one name. The toast itself goes through the
// Windows Runtime
// (Windows.UI.Notifications), which the .NET Framework reaches through the WinMD references in build.ps1; the
// methods that touch it are kept apart (NoInlining), so a Windows without it fails inside them and the caller falls
// back to the balloon (MainForm.Notify).

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows.Forms;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace GameMargins.Uploader
{
    internal static class Toasts
    {
        /// <summary>The program's AppUserModelID: on the process, on the shortcut, and the notifier's name.</summary>
        public const string AppId = "GameMargins.Uploader";
        public const string ShortcutName = "GameMargins Desktop.lnk";
        /// <summary>The shortcut's name before 0.2.5, when the program was called GameMargins Uploader.</summary>
        public const string OldShortcutName = "GameMargins Uploader.lnk";
        private static readonly System.Text.RegularExpressions.Regex OurExe = new System.Text.RegularExpressions.Regex("^GameMargins(Uploader|Desktop)(-[0-9]+\\.[0-9]+\\.[0-9]+)?\\.exe$", System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

        /// <summary>Tells Windows which app this process is, before any window exists: its taskbar button and its toasts belong to the shortcut's id.</summary>
        public static void SetAppId()
        {
            try { SetCurrentProcessExplicitAppUserModelID(AppId); } catch (Exception) { }
        }

        /// <summary>%APPDATA%\Microsoft\Windows\Start Menu\Programs\GameMargins Desktop.lnk</summary>
        public static string ShortcutPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), ShortcutName); }
        }

        /// <summary>
        /// Removes the shortcut of the program's old name, "GameMargins Uploader.lnk", from the user's own Start menu: that
        /// exact file only, and only when it points at a GameMargins exe (GameMarginsUploader…exe or GameMarginsDesktop…exe).
        /// </summary>
        public static void RemoveOldShortcut()
        {
            RemoveOldShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Programs));
        }

        /// <summary>The same in another Start-menu Programs folder (a test's). True when a shortcut was removed.</summary>
        public static bool RemoveOldShortcut(string programsFolder)
        {
            try
            {
                if (string.IsNullOrEmpty(programsFolder)) return false;
                var path = Path.Combine(programsFolder, OldShortcutName);
                if (!File.Exists(path)) return false;
                var target = ShortcutTarget(path);
                if (string.IsNullOrEmpty(target) || !OurExe.IsMatch(Path.GetFileName(target))) return false;
                File.Delete(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// The Start-menu shortcut with the AppUserModelID, for the running exe: made when it is missing, rewritten when it
        /// points at another exe (the program moved) or lacks the id. True when it is in place.
        /// </summary>
        public static bool EnsureShortcut()
        {
            return EnsureShortcut(Application.ExecutablePath);
        }

        /// <summary>The same for another exe: the program's own update points the shortcut at the new exe before starting it.</summary>
        public static bool EnsureShortcut(string exe)
        {
            try
            {
                var path = ShortcutPath;
                if (File.Exists(path) && ShortcutIsCurrent(path, exe)) return true;
                WriteShortcut(path, exe);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// A notification: `title` above `line`, with the site's mark. Throws where the Windows Runtime isn't there; the
        /// caller falls back. The notification center shows a notification folded until it is opened: the title on up to
        /// two lines and only the first line of `line`, cut at a space without a "…". What must be read there goes in the
        /// title, and `line` is one short line (ToastTexts).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Show(string title, string line, string tag)
        {
            var logo = EnsureLogo();
            var xml = new XmlDocument();
            xml.LoadXml(Xml(title, line, logo != null ? new Uri(logo).AbsoluteUri : null));
            var toast = new ToastNotification(xml);
            // A tag and group let the notification center keep each kind apart (a tag is at most 16 characters on older Windows 10).
            if (!string.IsNullOrEmpty(tag)) toast.Tag = tag.Length > 16 ? tag.Substring(0, 16) : tag;
            toast.Group = "uploader";
            ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
        }

        /// <summary>The toast's XML for Show: the picture at `logoUri` (a file: address) when there is one, then `title` and `line` as plain text, escaped.</summary>
        internal static string Xml(string title, string line, string logoUri)
        {
            var sb = new StringBuilder();
            sb.Append("<toast><visual><binding template=\"ToastGeneric\">");
            if (logoUri != null) sb.Append("<image placement=\"appLogoOverride\" src=\"").Append(SecurityElement.Escape(logoUri)).Append("\"/>");
            sb.Append("<text>").Append(SecurityElement.Escape(title)).Append("</text>");
            sb.Append("<text>").Append(SecurityElement.Escape(line)).Append("</text>");
            sb.Append("</binding></visual></toast>");
            return sb.ToString();
        }

        /// <summary>How many of the program's notifications the notification center holds (the --test-toast check).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int HistoryCount()
        {
            return ToastNotificationManager.History.GetHistory(AppId).Count;
        }

        // ------------------------------------------------------------------------------------------------ the logo

        private static string LogoPath
        {
            get { return Path.Combine(Settings.Folder, "logo.png"); }
        }

        /// <summary>
        /// The site's mark as a PNG next to the settings, for the toast's picture (a toast takes a file, not a resource);
        /// written once, and again after a new exe. Null when it can't be made: the toast then shows without a picture.
        /// </summary>
        private static string EnsureLogo()
        {
            try
            {
                var path = LogoPath;
                var exe = Application.ExecutablePath;
                if (File.Exists(path) && new FileInfo(path).Length > 0 && File.GetLastWriteTimeUtc(path) >= File.GetLastWriteTimeUtc(exe)) return path;
                using (var stream = typeof(Toasts).Assembly.GetManifestResourceStream("GameMargins.Uploader.GameMargins.ico"))
                {
                    if (stream == null) return null;
                    Bitmap bitmap = null;
                    try
                    {
                        try
                        {
                            using (var icon = new Icon(stream, 256, 256)) bitmap = icon.ToBitmap();
                        }
                        catch (Exception)
                        {
                            stream.Position = 0;
                            using (var icon = new Icon(stream, 48, 48)) bitmap = icon.ToBitmap();
                        }
                        Directory.CreateDirectory(Settings.Folder);
                        bitmap.Save(path, ImageFormat.Png);
                    }
                    finally
                    {
                        if (bitmap != null) bitmap.Dispose();
                    }
                }
                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------------------------------------ the shortcut (COM)

        private static readonly Guid AppUserModelFormat = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
        /// <summary>PKEY_AppUserModel_ID.</summary>
        private static PropertyKey AppIdKey
        {
            get { return new PropertyKey { fmtid = AppUserModelFormat, pid = 5 }; }
        }
        private const ushort VT_LPWSTR = 31;

        private static bool ShortcutIsCurrent(string path, string exe)
        {
            var link = (IShellLinkW)new CShellLink();
            try
            {
                ((IPersistFile)link).Load(path, 0);
                var target = new StringBuilder(1024);
                link.GetPath(target, target.Capacity, IntPtr.Zero, 0);
                if (!string.Equals(target.ToString(), exe, StringComparison.OrdinalIgnoreCase)) return false;
                var key = AppIdKey;
                PropVariant value;
                ((IPropertyStore)link).GetValue(ref key, out value);
                try
                {
                    return value.vt == VT_LPWSTR && value.p != IntPtr.Zero && Marshal.PtrToStringUni(value.p) == AppId;
                }
                finally
                {
                    PropVariantClear(ref value);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(link);
            }
        }

        /// <summary>The exe a shortcut points at, as it stores it; null when the file can't be read as a shortcut.</summary>
        internal static string ShortcutTarget(string path)
        {
            var link = (IShellLinkW)new CShellLink();
            try
            {
                ((IPersistFile)link).Load(path, 0);
                var target = new StringBuilder(1024);
                link.GetPath(target, target.Capacity, IntPtr.Zero, 0);
                return target.ToString();
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                Marshal.ReleaseComObject(link);
            }
        }

        internal static void WriteShortcut(string path, string exe)
        {
            var link = (IShellLinkW)new CShellLink();
            try
            {
                link.SetPath(exe);
                link.SetWorkingDirectory(Path.GetDirectoryName(exe));
                link.SetArguments("");
                link.SetDescription("GameMargins Desktop");
                link.SetIconLocation(exe, 0);
                var key = AppIdKey;
                var value = new PropVariant { vt = VT_LPWSTR, p = Marshal.StringToCoTaskMemUni(AppId) };
                try
                {
                    var store = (IPropertyStore)link;
                    store.SetValue(ref key, ref value);
                    store.Commit();
                }
                finally
                {
                    PropVariantClear(ref value);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                ((IPersistFile)link).Save(path, true);
            }
            finally
            {
                Marshal.ReleaseComObject(link);
            }
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant pvar);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;
        }

        /// <summary>PROPVARIANT, as far as a string needs it: the type and the pointer (16 bytes on x86, 24 on x64).</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct PropVariant
        {
            public ushort vt;
            public ushort reserved1;
            public ushort reserved2;
            public ushort reserved3;
            public IntPtr p;
            public IntPtr p2;
        }

        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class CShellLink
        {
        }

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport, Guid("0000010b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            [PreserveSig]
            int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out uint cProps);
            void GetAt(uint iProp, out PropertyKey pkey);
            void GetValue(ref PropertyKey key, out PropVariant pv);
            void SetValue(ref PropertyKey key, ref PropVariant pv);
            void Commit();
        }
    }
}
