using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

// Diagnostic-only native UI access. This '~' folder is not imported by Unity.
public static class MetaLinkWindow
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }
    private delegate bool EnumProc(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr window, EnumProc callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);

    private static Rect Validate(IntPtr window, bool allowMirror = false)
    {
        var title = new StringBuilder(256);
        GetWindowText(window, title, title.Capacity);
        Rect bounds;
        bool validTitle = title.ToString() == "Meta Horizon Link" || (allowMirror && title.ToString() == "Oculus Mirror");
        if (!IsWindow(window) || !validTitle || !GetWindowRect(window, out bounds))
            throw new InvalidOperationException("The observed Meta Horizon Link window is unavailable.");
        return bounds;
    }

    public static void Click(long windowHandle, int x, int y)
    {
        var window = new IntPtr(windowHandle);
        var bounds = Validate(window);
        if (x < 0 || y < 0 || x >= bounds.Right - bounds.Left || y >= bounds.Bottom - bounds.Top)
            throw new ArgumentOutOfRangeException("Click outside application bounds.");
        var render = IntPtr.Zero;
        EnumChildWindows(window, delegate(IntPtr child, IntPtr unused) {
            var name = new StringBuilder(256);
            GetClassName(child, name, name.Capacity);
            if (name.ToString() == "Chrome_RenderWidgetHostHWND" && GetAncestor(child, 2) == window)
            {
                render = child;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        if (render == IntPtr.Zero) throw new InvalidOperationException("Link render window not found; no click sent.");
        var position = new IntPtr((y << 16) | x);
        if (!PostMessage(render, 0x200, IntPtr.Zero, position) ||
            !PostMessage(render, 0x201, new IntPtr(1), position) ||
            !PostMessage(render, 0x202, IntPtr.Zero, position))
            throw new InvalidOperationException("Could not send application input.");
    }

    public static void Capture(long windowHandle, string path)
    {
        var window = new IntPtr(windowHandle);
        var bounds = Validate(window, true);
        using (var image = new Bitmap(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top))
        using (var graphics = Graphics.FromImage(image))
        {
            var context = graphics.GetHdc();
            try { if (!PrintWindow(window, context, 2)) throw new InvalidOperationException("Capture failed."); }
            finally { graphics.ReleaseHdc(context); }
            image.Save(path, ImageFormat.Png);
        }
    }
}
