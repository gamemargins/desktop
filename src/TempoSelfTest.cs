// Two checks of Tempo's side that need no game and no site, run from the command line:
//
//   GameMarginsDesktop.exe --tempo-selftest
//       Opens a small window of the program's own (four coloured quarters; shown without taking the keyboard), finds
//       it by its title the way the game's window is found, watches it through Capture.cs and compares the picture
//       with what was drawn. The result is the exit code (0: every pixel as drawn) and a few lines in
//       %TEMP%\GameMargins-tempo-selftest.txt.
//   GameMarginsDesktop.exe --tempo-test-target <seconds>
//       Shows only that coloured window, titled "GameMargins Desktop capture self-test", for that long: something for a
//       test start to watch in place of a game (--tempo-test-window with that title).
//
// The pictures of everything the program draws are made by --shots <folder> (Shots.cs), without any window.
//
// Neither looks for, or at, any other program's window, and neither reads or writes the settings.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GameMargins.Uploader
{
    internal static class TempoSelfTest
    {
        private const string Title = "GameMargins Desktop capture self-test";
        private static readonly Color[] Quarters = { Color.FromArgb(200, 40, 40), Color.FromArgb(40, 180, 60), Color.FromArgb(50, 90, 220), Color.FromArgb(240, 240, 240) };

        private sealed class Target : Form
        {
            private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
            private int _n;

            protected override bool ShowWithoutActivation { get { return true; } }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= 0x08000000 | 0x80;
                    return cp;
                }
            }

            public Target()
            {
                Text = Title;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                AutoScaleMode = AutoScaleMode.None;
                ShowInTaskbar = false;
                DoubleBuffered = true;
                // The screen the mouse is not on, when there is one: whoever sits at this computer is busy on the other.
                var pick = Screen.PrimaryScreen;
                foreach (var s in Screen.AllScreens) if (!s.Bounds.Contains(Cursor.Position)) { pick = s; break; }
                Bounds = new Rectangle(pick.WorkingArea.Left + 40, pick.WorkingArea.Top + 40, 640, 400);
                // A picture that changes, so frames keep arriving as they do from a game.
                _timer.Interval = 30;
                _timer.Tick += delegate { _n++; Invalidate(new Rectangle(0, 190, 640, 20)); };
                _timer.Start();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                for (var i = 0; i < 4; i++)
                {
                    using (var b = new SolidBrush(Quarters[i])) g.FillRectangle(b, (i % 2) * 320, (i / 2) * 200, 320, 200);
                }
                using (var b = new SolidBrush(Color.Black)) g.FillRectangle(b, (_n * 7) % 600, 195, 40, 10);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _timer.Dispose();
                base.Dispose(disposing);
            }
        }

        private static void Pump(int ms)
        {
            var until = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < until)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
        }

        /// <summary>--tempo-test-target <seconds>: only the coloured window, for that long, as something for a test start to watch (--tempo-test-window).</summary>
        public static int ShowTarget(int seconds)
        {
            Application.EnableVisualStyles();
            using (var target = new Target())
            {
                target.Show();
                Pump(Math.Max(1, Math.Min(600, seconds)) * 1000);
                target.Close();
            }
            return 0;
        }

        public const string TargetTitle = Title;

        /// <summary>--tempo-window-shots <folder>: the main window's views as PNGs, drawn off the screen in a folder of its own for the settings.</summary>
        public static int Capture()
        {
            var report = new StringBuilder();
            var ok = false;
            try
            {
                report.AppendLine("GameMargins Desktop " + Program.Version + ", capture self-test, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                report.AppendLine("Windows " + Environment.OSVersion.Version + "; capture supported: " + CaptureSupport.Works);
                Application.EnableVisualStyles();
                using (var target = new Target())
                {
                    target.Show();
                    Pump(400);
                    var found = GameWindow.Find(Title, true);
                    report.AppendLine("the window found by its title: " + (found != null ? "yes, class \"" + found.Class + "\", " + found.Bounds.Width + " x " + found.Bounds.Height : "NO"));
                    if (found != null && found.Handle == target.Handle)
                    {
                        using (var capture = new Capture(found.Handle))
                        {
                            Pump(1500);
                            var size = 1024 * 768 * 4 * 4;
                            var memory = Marshal.AllocHGlobal(size);
                            try
                            {
                                int width, height, stride;
                                long needed;
                                var watch = System.Diagnostics.Stopwatch.StartNew();
                                var got = capture.Grab(memory, size, out width, out height, out stride, out needed);
                                watch.Stop();
                                report.AppendLine("capture: " + capture.Note + "; frames arrived " + capture.Arrived + "; frame " + (got ? width + " x " + height + ", row " + stride + " bytes, taken in " + watch.Elapsed.TotalMilliseconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ms" : "NONE"));
                                if (got)
                                {
                                    var wrong = 0;
                                    for (var i = 0; i < 4; i++)
                                    {
                                        // The middle of each quarter, away from the window's rounded corners and the moving bar.
                                        var x = (i % 2) * (width / 2) + width / 4;
                                        var y = (i / 2) * (height / 2) + height / 4;
                                        var at = y * stride + x * 4;
                                        var b = Marshal.ReadByte(memory, at);
                                        var g = Marshal.ReadByte(memory, at + 1);
                                        var r = Marshal.ReadByte(memory, at + 2);
                                        var same = r == Quarters[i].R && g == Quarters[i].G && b == Quarters[i].B;
                                        if (!same) wrong++;
                                        report.AppendLine("quarter " + (i + 1) + " at " + x + "," + y + ": drawn " + Quarters[i].R + "," + Quarters[i].G + "," + Quarters[i].B + ", captured " + r + "," + g + "," + b + (same ? "" : "  DIFFERENT"));
                                    }
                                    ok = wrong == 0 && width == 640 && height == 400;
                                    // A second look a moment later: frames keep coming.
                                    var before = capture.Arrived;
                                    Pump(500);
                                    report.AppendLine("frames in the next half second: " + (capture.Arrived - before));
                                    ok = ok && capture.Arrived > before;
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(memory);
                            }
                        }
                    }
                    target.Close();
                }
            }
            catch (Exception e)
            {
                report.AppendLine("FAILED: " + e);
            }
            report.AppendLine(ok ? "RESULT: ok" : "RESULT: failed");
            try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "GameMargins-tempo-selftest.txt"), report.ToString()); } catch (Exception) { }
            return ok ? 0 : 1;
        }
    }
}
