using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using TelaRec.Core;

namespace TelaRec.Native;

/// <summary>Miniatura barata de um monitor (usada só para ajudar a identificar a tela correta).</summary>
internal static class ScreenThumbnail
{
    public static BitmapSource? Capture(MonitorInfo m, int maxWidth = 480)
    {
        IntPtr screen = IntPtr.Zero, mem = IntPtr.Zero, bmp = IntPtr.Zero, old = IntPtr.Zero;
        try
        {
            if (m.Width <= 0 || m.Height <= 0) return null;
            int w = maxWidth;
            int h = Math.Max(1, (int)((double)m.Height * w / m.Width));
            screen = NativeMethods.GetDC(IntPtr.Zero);
            mem = NativeMethods.CreateCompatibleDC(screen);
            bmp = NativeMethods.CreateCompatibleBitmap(screen, w, h);
            old = NativeMethods.SelectObject(mem, bmp);
            NativeMethods.SetStretchBltMode(mem, 4); // HALFTONE
            NativeMethods.StretchBlt(mem, 0, 0, w, h, screen, m.X, m.Y, m.Width, m.Height, 0x00CC0020); // SRCCOPY
            var src = Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            src.Freeze();
            return src;
        }
        catch { return null; }
        finally
        {
            if (old != IntPtr.Zero) NativeMethods.SelectObject(mem, old);
            if (bmp != IntPtr.Zero) NativeMethods.DeleteObject(bmp);
            if (mem != IntPtr.Zero) NativeMethods.DeleteDC(mem);
            if (screen != IntPtr.Zero) NativeMethods.ReleaseDC(IntPtr.Zero, screen);
        }
    }
}
