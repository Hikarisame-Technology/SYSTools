using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell;

namespace SYSTools.Helpers
{
    public static class IconHelper
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static ImageSource LoadIcon(string exePath, int size = 0)
        {
            try
            {
                var shellFile = ShellFile.FromFilePath(exePath);
                var bitmapSource = shellFile.Thumbnail.LargeBitmapSource;
                if (bitmapSource != null)
                {
                    if (size > 0 && bitmapSource.PixelWidth > 0 && bitmapSource.PixelHeight > 0)
                    {
                        double scaleX = (double)size / bitmapSource.PixelWidth;
                        double scaleY = (double)size / bitmapSource.PixelHeight;
                        var transformed = new TransformedBitmap(bitmapSource, new ScaleTransform(scaleX, scaleY));
                        transformed.Freeze();
                        return transformed;
                    }
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
            }
            catch { }

            try
            {
                uint count = ExtractIconEx(exePath, -1, null, null, 0);
                if (count > 0)
                {
                    IntPtr[] icons = new IntPtr[count];
                    ExtractIconEx(exePath, 0, icons, null, count);
                    Icon? bestIcon = null;
                    int bestSize = 0;
                    foreach (var h in icons)
                    {
                        if (h != IntPtr.Zero)
                        {
                            using (Icon ico = Icon.FromHandle(h))
                            {
                                if (ico.Width > bestSize)
                                {
                                    bestIcon?.Dispose();
                                    bestIcon = (Icon)ico.Clone();
                                    bestSize = ico.Width;
                                }
                            }
                            DestroyIcon(h);
                        }
                    }
                    if (bestIcon != null)
                    {
                        var rect = new Int32Rect(0, 0, bestIcon.Width, bestIcon.Height);
                        var options = size > 0
                            ? BitmapSizeOptions.FromWidthAndHeight(size, size)
                            : BitmapSizeOptions.FromEmptyOptions();
                        var bmp = Imaging.CreateBitmapSourceFromHIcon(bestIcon.Handle, rect, options);
                        bmp.Freeze();
                        bestIcon.Dispose();
                        return bmp;
                    }
                }
            }
            catch { }
            try
            {
                using (Icon icon = Icon.ExtractAssociatedIcon(exePath))
                {
                    if (icon != null)
                    {
                        var rect = new Int32Rect(0, 0, icon.Width, icon.Height);
                        var options = size > 0
                            ? BitmapSizeOptions.FromWidthAndHeight(size, size)
                            : BitmapSizeOptions.FromEmptyOptions();
                        var bmp = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, rect, options);
                        bmp.Freeze();
                        return bmp;
                    }
                }
            }
            catch { }
            return null;
        }
    }
} 