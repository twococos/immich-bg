using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace ImmichBg.WindowsIntegration;

/// <summary>
/// Configures the built-in "Photos" screensaver (PhotoScreensaver.scr) to show a folder.
/// The folder is stored as a DPAPI-encrypted PIDL; this format is not officially documented.
/// </summary>
public static class Screensaver
{
    const uint SpiSetScreenSaveTimeout = 0x000F;
    const uint SpiSetScreenSaveActive = 0x0011;
    const uint SpifUpdateIniFileAndSendChange = 0x01 | 0x02;

    public static void Apply(string folder, int waitMinutes, bool shuffle)
    {
        var scr = Path.Combine(Environment.SystemDirectory, "PhotoScreensaver.scr");
        if (!File.Exists(scr)) throw new FileNotFoundException(scr);

        var encryptedPidl = ProtectedData.Protect(GetPidl(folder), null, DataProtectionScope.CurrentUser);
        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows Photo Viewer\Slideshow\Screensaver"))
        {
            key.SetValue("EncryptedPIDL", Convert.ToBase64String(encryptedPidl), RegistryValueKind.String);
            key.SetValue("Shuffle", shuffle ? 1 : 0, RegistryValueKind.DWord);
        }

        var seconds = Math.Max(1, waitMinutes) * 60;
        using (var desktop = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
        {
            desktop.SetValue("SCRNSAVE.EXE", scr, RegistryValueKind.String);
            desktop.SetValue("ScreenSaveActive", "1", RegistryValueKind.String);
            desktop.SetValue("ScreenSaveTimeOut", seconds.ToString(), RegistryValueKind.String);
        }
        SystemParametersInfo(SpiSetScreenSaveTimeout, (uint)seconds, IntPtr.Zero, SpifUpdateIniFileAndSendChange);
        SystemParametersInfo(SpiSetScreenSaveActive, 1, IntPtr.Zero, SpifUpdateIniFileAndSendChange);
    }

    /// <summary>Opens the classic screensaver dialog (fallback / to preview).</summary>
    public static void OpenDialog() =>
        Process.Start(new ProcessStartInfo("control.exe", "desk.cpl,,@screensaver") { UseShellExecute = true });

    static byte[] GetPidl(string folder)
    {
        Marshal.ThrowExceptionForHR(SHParseDisplayName(folder, IntPtr.Zero, out var pidl, 0, out _));
        try
        {
            var bytes = new byte[ILGetSize(pidl)];
            Marshal.Copy(pidl, bytes, 0, bytes.Length);
            return bytes;
        }
        finally
        {
            Marshal.FreeCoTaskMem(pidl);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHParseDisplayName(string name, IntPtr bindContext, out IntPtr pidl, uint sfgaoIn, out uint sfgaoOut);

    [DllImport("shell32.dll")]
    static extern uint ILGetSize(IntPtr pidl);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SystemParametersInfo(uint action, uint param, IntPtr vparam, uint winIni);
}
