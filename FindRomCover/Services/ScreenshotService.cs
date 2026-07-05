using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace FindRomCover.Services;

public static class ScreenshotService
{
    private static readonly string ScreenshotFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshot");

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const int DwmwaExtendedFrameBounds = 9;

    public static string? CaptureActiveWindow()
    {
        try
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
            {
                LogService.Warning("Screenshot: No foreground window found.");
                return null;
            }

            if (!TryGetWindowBounds(hWnd, out var rect))
            {
                LogService.Warning("Screenshot: Failed to get window bounds.");
                return null;
            }

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
            {
                LogService.Warning("Screenshot: Window has invalid dimensions.");
                return null;
            }

            if (!Directory.Exists(ScreenshotFolder))
            {
                Directory.CreateDirectory(ScreenshotFolder);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var filePath = Path.Combine(ScreenshotFolder, $"Screenshot_{timestamp}.png");

            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));

            bitmap.Save(filePath, ImageFormat.Png);

            LogService.Information($"Screenshot: Saved to '{filePath}'.");
            return filePath;
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Error capturing screenshot.");
            return null;
        }
    }

    private static bool TryGetWindowBounds(IntPtr hWnd, out Rect rect)
    {
        if (DwmGetWindowAttribute(hWnd, DwmwaExtendedFrameBounds, out rect, Marshal.SizeOf<Rect>()) == 0)
        {
            return true;
        }

        return GetWindowRect(hWnd, out rect);
    }
}
