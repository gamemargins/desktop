// One look for everything this program draws: the window, the float, the menu by the clock. It follows Windows'
// own "app mode" (Settings > Personalisation > Colours: light or dark), live, and uses the site's colours with the
// site's meanings, in both modes: gold only for the one main button, links and the brand; green = works; orange =
// watch out; red = can't, or delete; blue = explanation and a measure; what is on or selected has the text's own
// colour (white in dark, near black in light). One font (Segoe UI, Windows' own), one button height, one scale of
// spacing (4, 8, 12, 16, 24). The values are the colours of gamemargins.com, so the Tempo page inside the
// Tempo window and the program around it are one family.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace GameMargins.Uploader
{
    /// <summary>The colours of one mode.</summary>
    internal sealed class Palette
    {
        public Color Background, Surface, Raised, Border, Edge, Foreground, Muted;
        public Color Positive, Negative, Warning, Info;
        /// <summary>Gold as text (a link, the brand); a gold button's face is AccentFill with OnAccent on it.</summary>
        public Color Accent, AccentFill, OnAccent;
        /// <summary>What is on or selected: the text's own colour, and the row it stands on.</summary>
        public Color Selected, SelectedSurface;
        /// <summary>A bar that measures (the level): the chart's blue, not a sign of good or bad.</summary>
        public Color Measure;
        /// <summary>The float's own face and edge: it stands over a game, not in a window.</summary>
        public Color FloatFace, FloatEdge;
        /// <summary>The menu by the clock.</summary>
        public Color MenuFace, MenuEdge, MenuHot;
        /// <summary>The soft faces behind a pill: works, watch out, can't.</summary>
        public Color PositiveSoft, WarningSoft, NegativeSoft, PositiveEdge, WarningEdge, NegativeEdge;
    }

    internal static class Look
    {
        private static Color C(int rgb)
        {
            return Color.FromArgb(0xff, (rgb >> 16) & 0xff, (rgb >> 8) & 0xff, rgb & 0xff);
        }

        public static readonly Palette Dark = new Palette
        {
            Background = C(0x0c0e12), Surface = C(0x14171d), Raised = C(0x1b1f27), Border = C(0x262b35), Edge = C(0x3a4150), Foreground = C(0xe6e8ec), Muted = C(0x8b93a3),
            Positive = C(0x4ade80), Negative = C(0xf87171), Warning = C(0xfb923c), Info = C(0x60a5fa),
            Accent = C(0xf2b544), AccentFill = C(0xf2b544), OnAccent = C(0x0c0e12),
            Selected = C(0xe6e8ec), SelectedSurface = C(0x232934), Measure = C(0x3987e5),
            FloatFace = C(0x12151b), FloatEdge = C(0x2c323d),
            MenuFace = C(0x1b1f27), MenuEdge = C(0x3a4150), MenuHot = C(0x2a303b),
            PositiveSoft = C(0x0f2a1a), WarningSoft = C(0x2a1a0c), NegativeSoft = C(0x2a1212), PositiveEdge = C(0x1f6b3d), WarningEdge = C(0x7a4a1c), NegativeEdge = C(0x5b2a2a),
        };

        public static readonly Palette Light = new Palette
        {
            Background = C(0xf4f5f7), Surface = C(0xffffff), Raised = C(0xeceef2), Border = C(0xd3d8e0), Edge = C(0xc5cad4), Foreground = C(0x14171d), Muted = C(0x5a6372),
            Positive = C(0x137236), Negative = C(0xbe2626), Warning = C(0xa5470a), Info = C(0x1d5dcc),
            Accent = C(0x895900), AccentFill = C(0xf2b544), OnAccent = C(0x1a1405),
            Selected = C(0x14171d), SelectedSurface = C(0xe3e6eb), Measure = C(0x3987e5),
            FloatFace = C(0xffffff), FloatEdge = C(0xc5cad4),
            MenuFace = C(0xffffff), MenuEdge = C(0xc5cad4), MenuHot = C(0xeceef2),
            PositiveSoft = C(0xe7f6ec), WarningSoft = C(0xfbefe4), NegativeSoft = C(0xfbe9e9), PositiveEdge = C(0x9bd3ad), WarningEdge = C(0xe5b48f), NegativeEdge = C(0xe3b1b1),
        };

        /// <summary>One height for every button, in pixels at 96 dpi; and the corners.</summary>
        public const float ButtonHeight = 30f;
        public const float Radius = 6f;
        public const float CardRadius = 8f;
        public const string FontName = "Segoe UI";

        private static bool _light;
        private static bool _forced;
        private static bool _watching;

        /// <summary>Windows is in light app mode (or a picture is being made of it).</summary>
        public static bool IsLight { get { return _light; } }
        public static Palette Now { get { return _light ? Light : Dark; } }
        public static Palette Of(bool light) { return light ? Light : Dark; }

        /// <summary>Windows switched between light and dark: everything draws again.</summary>
        public static event Action Changed;

        /// <summary>Windows' app mode: light unless the value says dark; dark where the value is missing is wrong for Windows 10 before 1809, which had no dark apps, so missing means light.</summary>
        public static bool SystemIsLight()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key == null) return true;
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int) return (int)value != 0;
                }
            }
            catch (Exception) { }
            return true;
        }

        private static string _mode = "system";

        /// <summary>What Settings' "Appearance" says: "system" (as Windows is), "light" or "dark".</summary>
        public static string Mode { get { return _mode; } }

        /// <summary>The player's choice, before Windows' own: "light" or "dark" stay whatever Windows does; "system" follows it again. Everything draws again when the mode changes.</summary>
        public static void Choose(string mode)
        {
            _mode = mode == "light" || mode == "dark" ? mode : "system";
            if (_forced) return;
            var light = _mode == "light" || (_mode == "system" && SystemIsLight());
            var same = light == _light;
            _light = light;
            var changed = Changed;
            // Also when the colours stay as they are: the Tempo window's page has to hear "follow Windows" or "always dark".
            if (changed != null && (!same || _watching)) changed();
        }

        /// <summary>Reads Windows' mode and keeps following it. Called once, before the first window.</summary>
        public static void Watch()
        {
            if (!_forced) _light = _mode == "light" || (_mode == "system" && SystemIsLight());
            if (_watching) return;
            _watching = true;
            try
            {
                SystemEvents.UserPreferenceChanged += delegate(object s, UserPreferenceChangedEventArgs e)
                {
                    if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color || e.Category == UserPreferenceCategory.VisualStyle) Refresh();
                };
            }
            catch (Exception) { }
        }

        /// <summary>Looks again (a window heard WM_SETTINGCHANGE "ImmersiveColorSet"); tells everyone when the mode changed.</summary>
        public static void Refresh()
        {
            if (_forced || _mode != "system") return;
            var light = SystemIsLight();
            if (light == _light) return;
            _light = light;
            var changed = Changed;
            if (changed != null) changed();
        }

        /// <summary>For the pictures (--shots): one mode, whatever Windows says.</summary>
        public static void Force(bool light)
        {
            _forced = true;
            if (_light == light) return;
            _light = light;
            var changed = Changed;
            if (changed != null) changed();
        }

        // ------------------------------------------------------------------------------------------------ fonts

        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        /// <summary>Segoe UI at `px` pixels (at 96 dpi; the drawing is scaled as a whole). `weight`: 0 regular, 1 semibold, 2 bold.</summary>
        public static Font Font(float px, int weight)
        {
            var key = px.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "/" + weight;
            Font font;
            if (Fonts.TryGetValue(key, out font)) return font;
            font = weight == 1 ? new Font("Segoe UI Semibold", px, FontStyle.Regular, GraphicsUnit.Pixel) : new Font(FontName, px, weight == 2 ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
            Fonts[key] = font;
            return font;
        }

        public static Font Mono(float px)
        {
            var key = "mono/" + px.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            Font font;
            if (Fonts.TryGetValue(key, out font)) return font;
            font = new Font("Consolas", px, FontStyle.Bold, GraphicsUnit.Pixel);
            Fonts[key] = font;
            return font;
        }

        // ------------------------------------------------------------------------------------------------ shapes

        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            var d = Math.Max(0.1f, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color Mix(Color a, Color b, double part)
        {
            return Color.FromArgb(0xff, (int)Math.Round(a.R + (b.R - a.R) * part), (int)Math.Round(a.G + (b.G - a.G) * part), (int)Math.Round(a.B + (b.B - a.B) * part));
        }

        // ------------------------------------------------------------------------------------------------ Windows

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        /// <summary>The window's title bar in the mode of the rest: dark on Windows 10 (1809 and later) and 11 in dark mode; nothing happens where the call is unknown.</summary>
        public static void TitleBar(IntPtr handle)
        {
            try
            {
                var on = _light ? 0 : 1;
                if (DwmSetWindowAttribute(handle, 20, ref on, sizeof(int)) != 0) DwmSetWindowAttribute(handle, 19, ref on, sizeof(int));
            }
            catch (Exception)
            {
                // Older Windows: its own title bar.
            }
        }

        /// <summary>Whether a window message says Windows' colours changed (WM_SETTINGCHANGE with "ImmersiveColorSet").</summary>
        public static bool IsColourChange(ref System.Windows.Forms.Message m)
        {
            if (m.Msg != 0x001A || m.LParam == IntPtr.Zero) return false;
            try { return Marshal.PtrToStringUni(m.LParam) == "ImmersiveColorSet"; } catch (Exception) { return false; }
        }
    }
}
