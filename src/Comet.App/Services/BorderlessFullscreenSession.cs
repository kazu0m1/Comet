using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Comet.App.Services;

/// <summary>
/// Changes a WPF window into a borderless monitor-sized window without ever
/// visibly restoring a maximized window to Normal during entry.
/// </summary>
internal sealed class BorderlessFullscreenSession : IDisposable
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsOverlappedWindow = 0x00CF0000;
    private const long WsExTopmost = 0x00000008;
    private const uint MonitorDefaultToNearest = 2;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpNoOwnerZOrder = 0x0200;

    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNoTopmost = new(-2);

    private readonly IntPtr _hwnd;
    private readonly IntPtr _originalStyle;
    private readonly WINDOWPLACEMENT _originalPlacement;
    private readonly bool _wasTopmost;
    private bool _disposed;

    private BorderlessFullscreenSession(IntPtr hwnd, IntPtr originalStyle,
        WINDOWPLACEMENT originalPlacement, bool wasTopmost)
    {
        _hwnd = hwnd;
        _originalStyle = originalStyle;
        _originalPlacement = originalPlacement;
        _wasTopmost = wasTopmost;
    }

    public static BorderlessFullscreenSession Enter(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Fullscreen requires an initialized window.");

        var placement = new WINDOWPLACEMENT
        {
            Length = Marshal.SizeOf<WINDOWPLACEMENT>()
        };
        if (!GetWindowPlacement(hwnd, ref placement))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var style = GetWindowLongPtr(hwnd, GwlStyle);
        var exStyle = GetWindowLongPtr(hwnd, GwlExStyle);

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var info = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var session = new BorderlessFullscreenSession(
            hwnd, style, placement, (exStyle.ToInt64() & WsExTopmost) != 0);

        // Preserve the native maximized/normal state and saved placement.
        // Unlike WindowState = Normal then Maximized, this does not play
        // Windows' restore/maximize animation.
        SetWindowLongPtr(hwnd, GwlStyle, new IntPtr(style.ToInt64() & ~WsOverlappedWindow));
        if (!SetWindowPos(hwnd, HwndTopmost,
            info.Monitor.Left, info.Monitor.Top,
            info.Monitor.Right - info.Monitor.Left,
            info.Monitor.Bottom - info.Monitor.Top,
            SwpFrameChanged | SwpNoOwnerZOrder))
        {
            session.Dispose();
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return session;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        SetWindowLongPtr(_hwnd, GwlStyle, _originalStyle);
        if (!SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged | SwpNoOwnerZOrder))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var placement = _originalPlacement;
        if (!SetWindowPlacement(_hwnd, ref placement))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        if (!SetWindowPos(_hwnd, _wasTopmost ? HwndTopmost : HwndNoTopmost,
            0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpNoOwnerZOrder))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public int Length;
        public int Flags;
        public int ShowCmd;
        public POINT MinPosition;
        public POINT MaxPosition;
        public RECT NormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int Size;
        public RECT Monitor;
        public RECT Work;
        public uint Flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowPlacement(IntPtr hwnd, ref WINDOWPLACEMENT placement);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPlacement(IntPtr hwnd, ref WINDOWPLACEMENT placement);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter,
        int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
}
