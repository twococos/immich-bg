using System.Text.Json.Serialization;

namespace ImmichBg.Config;

public enum TargetKind { Wallpaper, Screensaver, LockScreen }

public enum SourceKind { Album, Favorites }

public enum OrientationFilter { Landscape, Portrait }

public sealed class TargetConfig
{
    public bool Enabled { get; set; }
    public string Folder { get; set; } = "";
    public SourceKind Source { get; set; } = SourceKind.Favorites;
    public string? AlbumId { get; set; }
    public string? AlbumName { get; set; }
    public bool LimitEnabled { get; set; }
    public int Limit { get; set; } = 50;
    public bool OrientationEnabled { get; set; }
    public OrientationFilter Orientation { get; set; } = OrientationFilter.Landscape;

    /// <summary>
    /// Wallpaper: slideshow interval. Screensaver: idle time before it starts.
    /// Lock screen: self-rotation interval.
    /// </summary>
    public int WindowsMinutes { get; set; } = 10;
    public bool Shuffle { get; set; } = true;

    /// <summary>Lock screen only: the app changes the lock screen image itself.</summary>
    public bool SelfRotate { get; set; }

    public TargetConfig Clone() => (TargetConfig)MemberwiseClone();
}

public sealed class AppConfig
{
    /// <summary>Remote (public) address, e.g. https://photos.example.com.</summary>
    public string ServerUrl { get; set; } = "";
    /// <summary>Optional address inside the home network, tried first (e.g. http://192.168.1.200:2283).</summary>
    public string LocalServerUrl { get; set; } = "";
    /// <summary>API key encrypted with DPAPI (current Windows user), base64.</summary>
    public string? ApiKeyProtected { get; set; }
    public int IntervalMinutes { get; set; } = 30;
    public bool StartWithWindows { get; set; } = true;
    /// <summary>"" = follow system, otherwise "ca", "es" or "en".</summary>
    public string Language { get; set; } = "";

    public TargetConfig Wallpaper { get; set; } = new();
    public TargetConfig Screensaver { get; set; } = new();
    public TargetConfig LockScreen { get; set; } = new() { WindowsMinutes = 30 };

    public TargetConfig Get(TargetKind kind) => kind switch
    {
        TargetKind.Wallpaper => Wallpaper,
        TargetKind.Screensaver => Screensaver,
        TargetKind.LockScreen => LockScreen,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public void Set(TargetKind kind, TargetConfig value)
    {
        switch (kind)
        {
            case TargetKind.Wallpaper: Wallpaper = value; break;
            case TargetKind.Screensaver: Screensaver = value; break;
            case TargetKind.LockScreen: LockScreen = value; break;
        }
    }

    /// <summary>Addresses to try, local first.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> ServerUrls =>
        new[] { LocalServerUrl, ServerUrl }.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToList();

    [JsonIgnore]
    public bool HasConnection => ServerUrls.Count > 0 && !string.IsNullOrEmpty(ApiKeyProtected);

    public AppConfig Clone()
    {
        var c = (AppConfig)MemberwiseClone();
        c.Wallpaper = Wallpaper.Clone();
        c.Screensaver = Screensaver.Clone();
        c.LockScreen = LockScreen.Clone();
        return c;
    }
}
