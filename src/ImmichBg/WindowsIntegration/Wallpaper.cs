using System.Runtime.InteropServices;

namespace ImmichBg.WindowsIntegration;

/// <summary>Sets the desktop background to a slideshow of a folder via the documented IDesktopWallpaper COM API.</summary>
public static class Wallpaper
{
    const uint DsoShuffleImages = 0x1;
    const int DwposFill = 4;

    public static void ApplySlideshow(string folder, int minutes, bool shuffle)
    {
        var wallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
        IntPtr item = IntPtr.Zero, array = IntPtr.Zero;
        try
        {
            var iidShellItem = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
            var iidShellItemArray = new Guid("b63ea76d-1f85-456f-a19c-48159efa858b");
            Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(folder, IntPtr.Zero, ref iidShellItem, out item));
            Marshal.ThrowExceptionForHR(SHCreateShellItemArrayFromShellItem(item, ref iidShellItemArray, out array));

            wallpaper.SetSlideshow(array);
            wallpaper.SetSlideshowOptions(shuffle ? DsoShuffleImages : 0, (uint)Math.Max(1, minutes) * 60_000);
            wallpaper.SetPosition(DwposFill);
        }
        finally
        {
            if (array != IntPtr.Zero) Marshal.Release(array);
            if (item != IntPtr.Zero) Marshal.Release(item);
            Marshal.ReleaseComObject(wallpaper);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateItemFromParsingName(string path, IntPtr bindContext, ref Guid riid, out IntPtr item);

    [DllImport("shell32.dll")]
    static extern int SHCreateShellItemArrayFromShellItem(IntPtr item, ref Guid riid, out IntPtr array);

    [ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    class DesktopWallpaperClass;

    // Method order must match the native vtable exactly.
    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)] string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId);
        [return: MarshalAs(UnmanagedType.LPWStr)] string GetMonitorDevicePathAt(uint monitorIndex);
        uint GetMonitorDevicePathCount();
        Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorId);
        void SetBackgroundColor(uint color);
        uint GetBackgroundColor();
        void SetPosition(int position);
        int GetPosition();
        void SetSlideshow(IntPtr items);
        IntPtr GetSlideshow();
        void SetSlideshowOptions(uint options, uint slideshowTick);
        void GetSlideshowOptions(out uint options, out uint slideshowTick);
        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, int direction);
        int GetStatus();
        void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Rect { public int Left, Top, Right, Bottom; }
}
