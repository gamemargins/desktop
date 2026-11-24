// The three files of Microsoft's WebView2 SDK that travel inside the exe (lib\webview2, with Microsoft's licence), so
// the program stays one file with nothing to install: the two managed libraries are loaded straight from the exe's
// resources when the Tempo window first needs them, and the small native loader is written once to
// %LOCALAPPDATA%\GameMargins\Uploader\webview2\<SDK version>\ after its SHA-256 was checked. The WebView2 runtime
// itself is part of Windows 11 and of every updated Windows 10 (it is Microsoft Edge's engine); a player who only
// uses the WoW side never starts any of this.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace GameMargins.Uploader
{
    internal static class Embedded
    {
        /// <summary>The pinned SDK: the NuGet package Microsoft.Web.WebView2 of this version.</summary>
        public const string SdkVersion = "1.0.4258.31";
        /// <summary>SHA-256 of that package's runtimes\win-x64\native\WebView2Loader.dll.</summary>
        private const string LoaderSha256 = "3426dcc55fdfb8b5e7ac623cf33b4ccf283fbf68a0c5a65bddfdb4d749a8abee";
        private const string Prefix = "GameMargins.Uploader.";

        private static readonly Dictionary<string, Assembly> Loaded = new Dictionary<string, Assembly>();

        /// <summary>Call first thing in Main: from then on the managed SDK libraries are found inside the exe.</summary>
        public static void Register()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        }

        public static byte[] Resource(string name)
        {
            using (var s = typeof(Embedded).Assembly.GetManifestResourceStream(Prefix + name))
            {
                if (s == null) return null;
                var bytes = new byte[s.Length];
                var at = 0;
                while (at < bytes.Length)
                {
                    var n = s.Read(bytes, at, bytes.Length - at);
                    if (n <= 0) break;
                    at += n;
                }
                return bytes;
            }
        }

        private static Assembly Resolve(object sender, ResolveEventArgs e)
        {
            var name = new AssemblyName(e.Name).Name;
            var resource = name == "Microsoft.Web.WebView2.Core" ? "wv2.Core.dll" : name == "Microsoft.Web.WebView2.WinForms" ? "wv2.WinForms.dll" : null;
            if (resource == null) return null;
            lock (Loaded)
            {
                Assembly known;
                if (Loaded.TryGetValue(resource, out known)) return known;
                var bytes = Resource(resource);
                if (bytes == null) return null;
                known = Assembly.Load(bytes);
                Loaded[resource] = known;
                return known;
            }
        }

        public static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// The folder that holds the native loader, written from the exe when it is not there yet or is not the file
        /// this build carries. Null when it can't be written or the copy inside the exe is not the pinned one.
        /// </summary>
        public static string EnsureLoader()
        {
            try
            {
                var folder = Path.Combine(Path.Combine(Settings.Folder, "webview2"), SdkVersion);
                var file = Path.Combine(folder, "WebView2Loader.dll");
                if (File.Exists(file) && Sha256(File.ReadAllBytes(file)) == LoaderSha256) return folder;
                var bytes = Resource("wv2.Loader.x64.dll");
                if (bytes == null || Sha256(bytes) != LoaderSha256) return null;
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(file, bytes);
                return folder;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
