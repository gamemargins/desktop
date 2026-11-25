// The screens connected to this computer, and what the program does when they change while it runs.
//
// Every check asks Windows itself which screens there are at that moment (EnumDisplayMonitors, GetMonitorInfo). .NET's
// Screen.AllScreens keeps the list it read until .NET hears of a change, so a message that comes late, early or not at
// all would leave it with screens that are gone; Windows' own answer can't be old.
//
// A screen that goes away (switched off, unplugged, the layout changed in Windows' display settings) is noticed in three
// ways: Windows' WM_DISPLAYCHANGE at the main window, .NET's SystemEvents.DisplaySettingsChanged, and a look every two
// seconds that compares the screens with the last ones it saw. After a change the windows are checked three times, half
// a second, two and five seconds later: Windows may still be arranging its screens, and a laptop whose own resolution
// changes with them may say so more than once. The Tempo window and the main window come to the middle of the main
// screen when their title bar stands on no screen (as when the program starts with its place on a screen that is gone);
// the float goes to its place at the game. A minimised window is left minimised: when it is brought up where no screen
// is any more, it comes to the main screen at once. The game's own window is never moved or sent anything.
//
// tempo.log says what happened: a line with the screens (their device names, places, work areas and scales; nothing
// else) at the start and at each change, with where the windows stand, and a line for every window the program moves.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GameMargins.Uploader
{
    /// <summary>One screen as Windows names it now: its device name ("\\.\DISPLAY1"), its place, its work area (without the taskbar), whether it is the main screen, and its scale (1 at 96 dpi).</summary>
    internal sealed class ScreenInfo
    {
        public string Name = "";
        public Rectangle Bounds;
        public Rectangle Work;
        public bool Main;
        public float Scale = 1f;
    }

    /// <summary>
    /// Whether a place shows on a screen that is connected now (a second screen may have been switched off or unplugged
    /// since): what the program puts back (the Tempo window, the float, the main window) is checked here first, and goes
    /// to the main screen when it would stand where nothing shows.
    /// </summary>
    internal static class ScreenPlace
    {
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MonitorInfoEx
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Device;
        }

        private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr dc, ref NativeRect rect, IntPtr data);
        [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorEnumProc proc, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);
        private const uint MONITORINFOF_PRIMARY = 1;

        /// <summary>The screens connected now, asked of Windows this moment; .NET's list only when Windows names none.</summary>
        public static ScreenInfo[] Now()
        {
            var found = new List<ScreenInfo>();
            MonitorEnumProc each = delegate(IntPtr monitor, IntPtr dc, ref NativeRect rect, IntPtr data)
            {
                var info = new MonitorInfoEx();
                info.Size = Marshal.SizeOf(typeof(MonitorInfoEx));
                if (GetMonitorInfo(monitor, ref info))
                {
                    found.Add(new ScreenInfo
                    {
                        Name = info.Device ?? "",
                        Bounds = Rectangle.FromLTRB(info.Monitor.Left, info.Monitor.Top, info.Monitor.Right, info.Monitor.Bottom),
                        Work = Rectangle.FromLTRB(info.Work.Left, info.Work.Top, info.Work.Right, info.Work.Bottom),
                        Main = (info.Flags & MONITORINFOF_PRIMARY) != 0,
                        Scale = MonitorScale.Of(monitor),
                    });
                }
                return true;
            };
            try
            {
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, each, IntPtr.Zero);
            }
            catch (Exception)
            {
                found.Clear();
            }
            GC.KeepAlive(each);
            if (found.Count > 0) return found.ToArray();
            var screens = Screen.AllScreens;
            var list = new ScreenInfo[screens.Length];
            for (var i = 0; i < screens.Length; i++)
            {
                var b = screens[i].Bounds;
                list[i] = new ScreenInfo { Name = screens[i].DeviceName ?? "", Bounds = b, Work = screens[i].WorkingArea, Main = screens[i].Primary, Scale = MonitorScale.At(new Point(b.Left + b.Width / 2, b.Top + b.Height / 2)) };
            }
            return list;
        }

        /// <summary>The work areas of the screens connected now.</summary>
        public static Rectangle[] Areas()
        {
            return WorkAreas(Now());
        }

        public static Rectangle[] WorkAreas(ScreenInfo[] screens)
        {
            var areas = new Rectangle[screens == null ? 0 : screens.Length];
            for (var i = 0; i < areas.Length; i++) areas[i] = screens[i].Work;
            return areas;
        }

        /// <summary>At least 100 × 50 pixels of `r` (all of it, when it is smaller) inside one of `areas`.</summary>
        public static bool Shows(Rectangle r, Rectangle[] areas)
        {
            if (r.Width <= 0 || r.Height <= 0) return false;
            foreach (var area in areas)
            {
                var seen = Rectangle.Intersect(area, r);
                if (seen.Width >= Math.Min(100, r.Width) && seen.Height >= Math.Min(50, r.Height)) return true;
            }
            return false;
        }

        /// <summary>
        /// A window with a title bar `title` high: at least 100 pixels of its title bar inside one of `areas` (so it can be
        /// taken and moved), and at least 100 × 50 of it in all.
        /// </summary>
        public static bool TitleShows(Rectangle r, int title, Rectangle[] areas)
        {
            if (!Shows(r, areas)) return false;
            var bar = new Rectangle(r.Left, r.Top, r.Width, Math.Max(1, Math.Min(title, r.Height)));
            foreach (var area in areas)
            {
                var seen = Rectangle.Intersect(area, bar);
                if (seen.Width >= Math.Min(100, r.Width) && seen.Height >= Math.Min(bar.Height, 20)) return true;
            }
            return false;
        }

        /// <summary>
        /// Whether a window at `r`, with a title bar `title` high, stands where it can't be seen or taken on `screens`, and
        /// so goes to the middle of the main screen. Right after the screens changed, and when a window comes into view
        /// again (`strict`): when its title bar does not show at least 100 pixels inside a screen's work area (TitleShows),
        /// as at the start. On the look every two seconds: only when nothing of its title bar is on any screen, so a window
        /// put partly past a screen's edge stays where it was put. With no screen named it stays: nothing is known.
        /// </summary>
        public static bool Stranded(Rectangle r, int title, ScreenInfo[] screens, bool strict)
        {
            if (screens == null || screens.Length == 0 || r.Width <= 0 || r.Height <= 0) return false;
            if (strict) return !TitleShows(r, title, WorkAreas(screens));
            var bar = new Rectangle(r.Left, r.Top, r.Width, Math.Max(1, Math.Min(title, r.Height)));
            foreach (var s in screens) if (s.Bounds.IntersectsWith(bar)) return false;
            return true;
        }

        /// <summary>The main screen among `screens` (the first one when none says it is the main one); null when there are none.</summary>
        public static ScreenInfo MainScreen(ScreenInfo[] screens)
        {
            if (screens == null || screens.Length == 0) return null;
            foreach (var s in screens) if (s.Main) return s;
            return screens[0];
        }

        /// <summary>The main screen's work area among `screens`; .NET's main screen when none is named.</summary>
        public static Rectangle MainArea(ScreenInfo[] screens)
        {
            var main = MainScreen(screens);
            return main != null ? main.Work : Screen.PrimaryScreen.WorkingArea;
        }

        /// <summary>`size` in the middle of `area` (the main screen's work area), no larger than it.</summary>
        public static Rectangle MiddleOf(Rectangle area, Size size)
        {
            var w = Math.Min(area.Width, size.Width);
            var h = Math.Min(area.Height, size.Height);
            return new Rectangle(area.Left + (area.Width - w) / 2, area.Top + (area.Height - h) / 2, w, h);
        }

        /// <summary>A place for tempo.log: "x,y,width,height".</summary>
        public static string Text(Rectangle r)
        {
            return r.X + "," + r.Y + "," + r.Width + "," + r.Height;
        }

        /// <summary>
        /// The screens for tempo.log, left to right: "\\.\DISPLAY1 0,0,2560,1440 (work 0,0,2560,1392) 100% main ·
        /// \\.\DISPLAY5 2560,5,2560,1440 (work 2560,5,2560,1392) 100%". Windows' device names and places, nothing more.
        /// </summary>
        public static string Describe(ScreenInfo[] screens)
        {
            if (screens == null || screens.Length == 0) return "no screen";
            var sorted = new List<ScreenInfo>(screens);
            sorted.Sort(delegate(ScreenInfo a, ScreenInfo b)
            {
                if (a.Bounds.X != b.Bounds.X) return a.Bounds.X.CompareTo(b.Bounds.X);
                if (a.Bounds.Y != b.Bounds.Y) return a.Bounds.Y.CompareTo(b.Bounds.Y);
                return string.CompareOrdinal(a.Name, b.Name);
            });
            var parts = new List<string>();
            foreach (var s in sorted) parts.Add(s.Name + " " + Text(s.Bounds) + " (work " + Text(s.Work) + ") " + (int)Math.Round(s.Scale * 100) + "%" + (s.Main ? " main" : ""));
            return string.Join(" · ", parts.ToArray());
        }
    }

    /// <summary>
    /// Watches the screens while the program runs (the top of this file says how). Lives on the main window's thread.
    /// `check` gets the screens and whether they changed just now (true: the checks after a change, with the strict rule
    /// and the float) or it is the look every two seconds (false); `windows` says where the windows stand, for the log.
    /// </summary>
    internal sealed class ScreenWatch : IDisposable
    {
        /// <summary>The checks after a change: this many milliseconds after Windows said so.</summary>
        internal static readonly int[] AfterChange = { 500, 2000, 5000 };
        /// <summary>The look between changes, every this many milliseconds.</summary>
        internal const int Every = 2000;

        private readonly Control _owner;
        private readonly Action<ScreenInfo[], bool> _check;
        private readonly Func<string> _windows;
        private readonly Timer _after = new Timer();
        private readonly Timer _every = new Timer();
        private int _step;
        /// <summary>The screens as tempo.log last said them, and when.</summary>
        private string _said;
        private DateTime _saidAt;
        private bool _disposed;

        public ScreenWatch(Control owner, Action<ScreenInfo[], bool> check, Func<string> windows)
        {
            _owner = owner;
            _check = check;
            _windows = windows;
            _after.Tick += delegate { AfterTick(); };
            _every.Interval = Every;
            _every.Tick += delegate { EveryTick(); };
            var screens = ScreenPlace.Now();
            Say("at the start", ScreenPlace.Describe(screens));
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            _every.Start();
        }

        /// <summary>.NET says the screens changed (on a thread of its own, or the window's).</summary>
        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            if (_disposed) return;
            try
            {
                _owner.BeginInvoke(new Action(delegate { Changed("DisplaySettingsChanged"); }));
            }
            catch (Exception) { }
        }

        /// <summary>Windows said the screens changed (`source`): a line in tempo.log, and the windows are checked half a second, two and five seconds from now.</summary>
        public void Changed(string source)
        {
            if (_disposed) return;
            var line = ScreenPlace.Describe(ScreenPlace.Now());
            // Windows' message and .NET's event come for one change within a moment of each other: one line for the two.
            if (line != _said || (DateTime.UtcNow - _saidAt).TotalMilliseconds > 1000) Say("Windows: " + source, line);
            Schedule();
        }

        private void Schedule()
        {
            _step = 0;
            _after.Stop();
            _after.Interval = AfterChange[0];
            _after.Start();
        }

        private void AfterTick()
        {
            _after.Stop();
            if (_disposed) return;
            var screens = ScreenPlace.Now();
            var line = ScreenPlace.Describe(screens);
            if (line != _said) Say(AfterChange[_step] + " ms after the change", line);
            Run(screens, true);
            _step++;
            if (_step >= AfterChange.Length) return;
            _after.Interval = AfterChange[_step] - AfterChange[_step - 1];
            _after.Start();
        }

        private void EveryTick()
        {
            if (_disposed) return;
            var screens = ScreenPlace.Now();
            var line = ScreenPlace.Describe(screens);
            if (line != _said)
            {
                // Changed without a word from Windows that reached the program (or before it): as if Windows had said so.
                Say("seen by the look every 2 s", line);
                Schedule();
            }
            Run(screens, false);
        }

        private void Run(ScreenInfo[] screens, bool afterChange)
        {
            try
            {
                _check(screens, afterChange);
            }
            catch (Exception e)
            {
                Problem("the check of the windows' places", e);
            }
        }

        private void Say(string why, string line)
        {
            _said = line;
            _saidAt = DateTime.UtcNow;
            string windows = null;
            try
            {
                if (_windows != null) windows = _windows();
            }
            catch (Exception) { }
            Tempo.Log("screens (" + why + "): " + line + (string.IsNullOrEmpty(windows) ? "" : "; " + windows));
        }

        private static readonly HashSet<string> Said = new HashSet<string>();

        /// <summary>Something in a check went wrong: said once in tempo.log for each kind, not every two seconds.</summary>
        internal static void Problem(string what, Exception e)
        {
            var key = what + ": " + e.GetType().Name;
            lock (Said)
            {
                if (!Said.Add(key)) return;
            }
            Tempo.Log(key + " (" + e.Message + ")");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; } catch (Exception) { }
            _after.Stop();
            _every.Stop();
            _after.Dispose();
            _every.Dispose();
        }
    }
}
