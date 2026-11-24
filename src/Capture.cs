// How Tempo looks at the MapleStory window: the picture of that one window, as Windows composes it for the screen
// (Windows.Graphics.Capture, the same way streaming and recording programs look at a window), and nothing else.
//
// The promise, kept by the code in this file:
//   - Only the picture of the game's window is read. The mouse pointer is left out of it (Windows 10 before version
//     2004 can't leave it out: there the pointer shows in the picture while it is over the game).
//   - The game's window is found by asking Windows for the open windows' titles, classes, sizes and whether they are
//     shown or minimised (EnumWindows, GetWindowText, GetClassName, IsWindowVisible, IsIconic, IsWindow,
//     GetWindowRect, and GetWindowThreadProcessId to leave out this program's own windows). For another program's
//     window these read what the window manager keeps; none of them sends the game a message.
//   - Never: opening the game's process, reading its memory, its modules, its files or its network; sending it a
//     message, a key or a click (no SendMessage, no PostMessage, no PrintWindow, no SetWindowPos, no
//     SetForegroundWindow); hooks; anything drawn into or over the game.
//   - This program never looks inside the picture itself. The frames go, on this computer, to the Tempo page in the
//     program's own window, which reads the numbers; no picture leaves the computer.
//
// The Direct3D calls are made by hand through the interfaces' method tables, so the build needs nothing but the
// framework and the Windows Runtime's metadata. On Windows 11 the yellow border Windows draws around a watched window
// is switched off (IsBorderRequired); Windows 10 keeps it.
//
// Which Windows: looking at one window by its handle needs Windows 10 version 1903 (build 18362) or later; leaving the
// pointer out needs version 2004 (build 19041); no border needs Windows 11. Each of these is asked of Windows before it
// is used (CaptureSupport), and every call a Windows may lack stands in a method of its own, so a Windows without it
// fails there and nowhere else: Tempo then says it can't look on this Windows, and the rest of the program goes on.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace GameMargins.Uploader
{
    /// <summary>A top-level window as Windows describes it: nothing here was asked of the window itself.</summary>
    internal sealed class FoundWindow
    {
        public IntPtr Handle;
        public string Title;
        public string Class;
        public Rectangle Bounds;
        public bool Minimised;
    }

    /// <summary>Finding the game's window by what the window manager knows of it.</summary>
    internal static class GameWindow
    {
        /// <summary>The game's window title, exactly.</summary>
        public const string GameTitle = "MapleStory";

        /// <summary>Windows that can carry the same title and are not the game: a folder of that name in File Explorer.</summary>
        private static readonly string[] NotTheGame = { "CabinetWClass", "ExploreWClass" };

        private delegate bool EnumProc(IntPtr window, IntPtr l);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc proc, IntPtr l);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int max);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect rect);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentProcessId();
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int L, T, R, B; }

        /// <summary>
        /// The shown top-level window whose title is exactly `title` (the game's, or a test's own window), or null. A
        /// window of this program never counts for the game's title, nor does a File Explorer folder of that name.
        /// </summary>
        public static FoundWindow Find(string title, bool test)
        {
            FoundWindow found = null;
            var mine = GetCurrentProcessId();
            EnumWindows(delegate(IntPtr window, IntPtr l)
            {
                if (!IsWindowVisible(window)) return true;
                var text = new StringBuilder(260);
                GetWindowText(window, text, 260);
                if (!string.Equals(text.ToString(), title, StringComparison.Ordinal)) return true;
                var name = new StringBuilder(260);
                GetClassName(window, name, 260);
                if (!test)
                {
                    uint process;
                    GetWindowThreadProcessId(window, out process);
                    if (process == mine) return true;
                    foreach (var other in NotTheGame) if (name.ToString() == other) return true;
                }
                found = Describe(window, text.ToString(), name.ToString());
                return false;
            }, IntPtr.Zero);
            return found;
        }

        private static FoundWindow Describe(IntPtr window, string title, string name)
        {
            Rect r;
            GetWindowRect(window, out r);
            return new FoundWindow { Handle = window, Title = title, Class = name, Bounds = Rectangle.FromLTRB(r.L, r.T, r.R, r.B), Minimised = IsIconic(window) };
        }

        public static bool StillThere(IntPtr window)
        {
            return window != IntPtr.Zero && IsWindow(window);
        }

        public static bool IsMinimised(IntPtr window)
        {
            return IsIconic(window);
        }

        public static Rectangle BoundsOf(IntPtr window)
        {
            Rect r;
            return GetWindowRect(window, out r) ? Rectangle.FromLTRB(r.L, r.T, r.R, r.B) : Rectangle.Empty;
        }
    }

    /// <summary>
    /// What this Windows can do of looking at a window. Apart from Capture, which holds parts an older Windows 10 does
    /// not know: asking here loads none of them. Each question that touches the Windows Runtime is a method of its own
    /// (not inlined), so a Windows that lacks the type fails inside it and the answer is simply no.
    /// </summary>
    internal static class CaptureSupport
    {
        private const string Session = "Windows.Graphics.Capture.GraphicsCaptureSession";

        /// <summary>Whether Tempo can look at a window here: Windows 10 version 1903 or later, with a screen Windows can capture from.</summary>
        public static bool Works
        {
            get
            {
                try { return HasWindowCapture() && SessionSupported(); }
                catch (Exception) { return false; }
            }
        }

        /// <summary>Whether the pointer can be left out of the picture (Windows 10 version 2004 or later).</summary>
        public static bool CanHidePointer
        {
            get
            {
                try { return Has("IsCursorCaptureEnabled"); }
                catch (Exception) { return false; }
            }
        }

        /// <summary>Whether Windows' yellow border round a watched window can be switched off (Windows 11).</summary>
        public static bool CanDropBorder
        {
            get
            {
                try { return Has("IsBorderRequired"); }
                catch (Exception) { return false; }
            }
        }

        /// <summary>A window's picture by its handle came with version 1903, the Windows Runtime's contract 8.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool HasWindowCapture()
        {
            return Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8) && Windows.Foundation.Metadata.ApiInformation.IsTypePresent(Session);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SessionSupported()
        {
            return GraphicsCaptureSession.IsSupported();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool Has(string property)
        {
            return Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent(Session, property);
        }
    }

    /// <summary>
    /// Windows.Graphics.Capture for one window handle. Only the newest frame is kept (a frame that arrives replaces the
    /// one before it, on the capture's own thread, without a copy); `Grab` copies that frame to memory when asked.
    /// </summary>
    internal sealed class Capture : IDisposable
    {
        [ComImport, Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IGraphicsCaptureItemInterop
        {
            [PreserveSig] int CreateForWindow(IntPtr window, [In] ref Guid iid, out IntPtr result);
            [PreserveSig] int CreateForMonitor(IntPtr monitor, [In] ref Guid iid, out IntPtr result);
        }

        [ComImport, Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDirect3DDxgiInterfaceAccess
        {
            [PreserveSig] int GetInterface([In] ref Guid iid, out IntPtr p);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TexDesc { public uint Width, Height, MipLevels, ArraySize; public int Format; public uint SampleCount, SampleQuality; public int Usage; public uint BindFlags, CpuAccessFlags, MiscFlags; }
        [StructLayout(LayoutKind.Sequential)]
        private struct Mapped { public IntPtr Data; public uint RowPitch, DepthPitch; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int CreateTexture2DFn(IntPtr self, ref TexDesc desc, IntPtr init, out IntPtr texture);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void CopyResourceFn(IntPtr self, IntPtr dst, IntPtr src);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int MapFn(IntPtr self, IntPtr resource, uint sub, int type, uint flags, out Mapped mapped);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void UnmapFn(IntPtr self, IntPtr resource, uint sub);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void GetDescFn(IntPtr self, out TexDesc desc);

        [DllImport("d3d11.dll")] private static extern int D3D11CreateDevice(IntPtr adapter, int driverType, IntPtr software, uint flags, IntPtr levels, uint levelCount, uint sdkVersion, out IntPtr device, out int level, out IntPtr context);
        [DllImport("d3d11.dll")] private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")] private static extern void Move(IntPtr dest, IntPtr src, UIntPtr count);

        private static T Method<T>(IntPtr com, int slot)
        {
            return (T)(object)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(com), slot * IntPtr.Size), typeof(T));
        }

        private readonly object _gate = new object();
        private IntPtr _device, _context, _staging;
        private uint _stagingW, _stagingH;
        private IDirect3DDevice _winrtDevice;
        private Direct3D11CaptureFramePool _pool;
        private GraphicsCaptureSession _session;
        private GraphicsCaptureItem _item;
        private CreateTexture2DFn _createTexture;
        private CopyResourceFn _copyResource;
        private MapFn _map;
        private UnmapFn _unmap;
        private Direct3D11CaptureFrame _newest;
        private Windows.Graphics.SizeInt32 _poolSize;
        private bool _disposed;

        /// <summary>The window being watched.</summary>
        public readonly IntPtr Window;
        /// <summary>Frames Windows handed over so far.</summary>
        public int Arrived;
        /// <summary>Whether the yellow border could be switched off (Windows 11), and anything else worth a line in a report.</summary>
        public string Note = "";
        /// <summary>The window's size when the capture started, in pixels.</summary>
        public int Width, Height;
        /// <summary>Set once Windows says the watched window is gone.</summary>
        public volatile bool Closed;

        public Capture(IntPtr window)
        {
            Window = window;
            try
            {
                int level;
                // A hardware device with BGRA support, as the capture needs.
                Marshal.ThrowExceptionForHR(D3D11CreateDevice(IntPtr.Zero, 1, IntPtr.Zero, 0x20, IntPtr.Zero, 0, 7, out _device, out level, out _context));
                _createTexture = Method<CreateTexture2DFn>(_device, 5);
                _copyResource = Method<CopyResourceFn>(_context, 47);
                _map = Method<MapFn>(_context, 14);
                _unmap = Method<UnmapFn>(_context, 15);
                var dxgiDevice = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
                IntPtr dxgi, inspectable;
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(_device, ref dxgiDevice, out dxgi));
                try { Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgi, out inspectable)); }
                finally { Marshal.Release(dxgi); }
                _winrtDevice = (IDirect3DDevice)Marshal.GetObjectForIUnknown(inspectable);
                Marshal.Release(inspectable);

                var factory = (IGraphicsCaptureItemInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
                var itemId = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
                IntPtr raw;
                Marshal.ThrowExceptionForHR(factory.CreateForWindow(window, ref itemId, out raw));
                _item = (GraphicsCaptureItem)Marshal.GetObjectForIUnknown(raw);
                Marshal.Release(raw);
                _poolSize = _item.Size;
                Width = _poolSize.Width;
                Height = _poolSize.Height;
                _item.Closed += delegate { Closed = true; };
                _pool = Direct3D11CaptureFramePool.CreateFreeThreaded(_winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _poolSize);
                _pool.FrameArrived += OnFrame;
                _session = _pool.CreateCaptureSession(_item);
                var pointerOut = false;
                try
                {
                    if (CaptureSupport.CanHidePointer)
                    {
                        HidePointer(_session);
                        pointerOut = true;
                    }
                }
                catch (Exception) { }
                if (!pointerOut) Note += "the pointer stays in the picture; ";
                var borderless = false;
                try
                {
                    if (CaptureSupport.CanDropBorder) borderless = DropBorder(_session);
                }
                catch (Exception) { }
                Note += borderless ? "no border" : "Windows draws its border";
                _session.StartCapture();
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        /// <summary>The pointer out of the picture. A method of its own: Windows 10 before version 2004 does not know the property, and must not meet it anywhere else.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void HidePointer(GraphicsCaptureSession session)
        {
            session.IsCursorCaptureEnabled = false;
        }

        /// <summary>Windows' yellow border off; true when Windows agreed. A method of its own: only Windows 11 knows the property.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool DropBorder(GraphicsCaptureSession session)
        {
            session.IsBorderRequired = false;
            return !session.IsBorderRequired;
        }

        /// <summary>On the capture's own thread: the frame that just arrived replaces the one kept.</summary>
        private void OnFrame(Direct3D11CaptureFramePool sender, object args)
        {
            try
            {
                Direct3D11CaptureFrame frame;
                lock (_gate)
                {
                    if (_disposed) return;
                    while ((frame = sender.TryGetNextFrame()) != null)
                    {
                        if (_newest != null) _newest.Dispose();
                        _newest = frame;
                        Arrived++;
                    }
                }
            }
            catch (Exception)
            {
                // A frame that could not be taken: the next one will be.
            }
        }

        /// <summary>
        /// The newest frame, copied to `dest` as BGRA rows, top row first: its size and the length of a row in bytes.
        /// False when no frame arrived yet. `needed` is the room the frame takes; when that is more than `destSize`
        /// nothing is copied and the answer is false with `needed` set, so the caller can make room and ask again.
        /// </summary>
        public bool Grab(IntPtr dest, long destSize, out int width, out int height, out int stride, out long needed)
        {
            width = height = stride = 0;
            needed = 0;
            lock (_gate)
            {
                if (_disposed || _newest == null) return false;
                var frame = _newest;
                var content = frame.ContentSize;
                var textureId = new Guid("6F15AAF2-D208-4E89-9AB4-489535D34F9C");
                IntPtr texture;
                Marshal.ThrowExceptionForHR(((IDirect3DDxgiInterfaceAccess)(object)frame.Surface).GetInterface(ref textureId, out texture));
                try
                {
                    TexDesc d;
                    Method<GetDescFn>(texture, 10)(texture, out d);
                    if (_staging == IntPtr.Zero || d.Width != _stagingW || d.Height != _stagingH)
                    {
                        if (_staging != IntPtr.Zero) Marshal.Release(_staging);
                        _staging = IntPtr.Zero;
                        var s = d;
                        s.MipLevels = 1; s.ArraySize = 1; s.SampleCount = 1; s.SampleQuality = 0;
                        s.Usage = 3; s.BindFlags = 0; s.CpuAccessFlags = 0x20000; s.MiscFlags = 0;
                        Marshal.ThrowExceptionForHR(_createTexture(_device, ref s, IntPtr.Zero, out _staging));
                        _stagingW = d.Width;
                        _stagingH = d.Height;
                    }
                    _copyResource(_context, _staging, texture);
                    Mapped m;
                    Marshal.ThrowExceptionForHR(_map(_context, _staging, 0, 1, 0, out m));
                    try
                    {
                        // The window's own picture may be smaller than the texture it arrives in (the window was made smaller).
                        width = (int)Math.Min(d.Width, (uint)Math.Max(1, content.Width));
                        height = (int)Math.Min(d.Height, (uint)Math.Max(1, content.Height));
                        stride = (int)m.RowPitch;
                        needed = (long)stride * height;
                        if (dest == IntPtr.Zero || needed > destSize) return false;
                        Move(dest, m.Data, new UIntPtr((ulong)needed));
                    }
                    finally
                    {
                        _unmap(_context, _staging, 0);
                    }
                }
                finally
                {
                    Marshal.Release(texture);
                }
                // The window grew or shrank: later frames come at its new size.
                if (content.Width > 0 && content.Height > 0 && (content.Width != _poolSize.Width || content.Height != _poolSize.Height))
                {
                    _poolSize = content;
                    try { _pool.Recreate(_winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _poolSize); } catch (Exception) { }
                }
                return true;
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                try { if (_newest != null) _newest.Dispose(); } catch (Exception) { }
                _newest = null;
                try { if (_session != null) _session.Dispose(); } catch (Exception) { }
                try { if (_pool != null) _pool.Dispose(); } catch (Exception) { }
                _session = null;
                _pool = null;
                _item = null;
                _winrtDevice = null;
                if (_staging != IntPtr.Zero) Marshal.Release(_staging);
                if (_context != IntPtr.Zero) Marshal.Release(_context);
                if (_device != IntPtr.Zero) Marshal.Release(_device);
                _staging = _context = _device = IntPtr.Zero;
            }
        }
    }
}
