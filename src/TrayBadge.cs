// The tray icon after an upload that stored a scan: the site's mark with a small green check in its corner for ten
// minutes, green being "done" in the site's colours (its positive colour, #1baf7a), with a tooltip that says what was
// uploaded when (MainForm.RefreshTray).
// Drawn here from the normal icon at the tray's size, so GameMargins.ico stays the one picture of the program.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GameMargins.Uploader
{
    internal static class TrayBadge
    {
        private static readonly Color Green = Color.FromArgb(0x1b, 0xaf, 0x7a);
        private static readonly Color Rim = Color.FromArgb(0x12, 0x14, 0x18);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>`normal` with a green disc and a white check over its bottom-right corner, `size` pixels square.</summary>
        public static Icon WithCheck(Icon normal, int size)
        {
            using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawIcon(normal, new Rectangle(0, 0, size, size));
                    // The disc takes a good half of the icon: at 16 px that is 9 px, the least a check still reads at.
                    var d = Math.Max(9f, size * 0.58f);
                    var x = size - d;
                    var y = size - d;
                    var disc = new RectangleF(x, y, d, d);
                    // A thin dark rim first, so the disc stands off the mark behind it.
                    using (var rim = new SolidBrush(Rim)) g.FillEllipse(rim, RectangleF.Inflate(disc, 1f, 1f));
                    using (var fill = new SolidBrush(Green)) g.FillEllipse(fill, disc);
                    using (var pen = new Pen(Color.White, Math.Max(1.5f, d / 6f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    {
                        g.DrawLines(pen, new[] { new PointF(x + d * 0.26f, y + d * 0.52f), new PointF(x + d * 0.44f, y + d * 0.70f), new PointF(x + d * 0.76f, y + d * 0.33f) });
                    }
                }
                var handle = bitmap.GetHicon();
                try
                {
                    // Clone copies the picture into an icon of its own; the handle GetHicon gave is ours to destroy.
                    return (Icon)Icon.FromHandle(handle).Clone();
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }
    }
}
