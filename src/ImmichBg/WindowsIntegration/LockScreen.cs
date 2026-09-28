using System.Diagnostics;
using Windows.Storage;

namespace ImmichBg.WindowsIntegration;

/// <summary>
/// Windows 10 Home has no API to pick the lock screen slideshow folder, so either the user picks it
/// in Settings (we open the page and copy the path), or the app rotates a single lock screen image itself.
/// </summary>
public static class LockScreen
{
    static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp"];
    static string? lastImage;

    public static void OpenSettings(string folder)
    {
        Clipboard.SetText(folder);
        Process.Start(new ProcessStartInfo("ms-settings:lockscreen") { UseShellExecute = true });
    }

    /// <summary>Sets a random image of the folder (different from the previous one) as lock screen image.</summary>
    /// <returns>The chosen file, or null if the folder has no images.</returns>
    public static async Task<string?> RotateAsync(string folder)
    {
        if (!Directory.Exists(folder)) return null;
        var images = Directory.EnumerateFiles(folder)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();
        if (images.Count == 0) return null;
        if (images.Count > 1) images.Remove(lastImage ?? "");

        var chosen = images[Random.Shared.Next(images.Count)];
        var file = await StorageFile.GetFileFromPathAsync(chosen);
        await global::Windows.System.UserProfile.LockScreen.SetImageFileAsync(file);
        lastImage = chosen;
        return chosen;
    }
}
