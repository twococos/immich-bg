using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ImmichBg.UI;

public enum SyncState { Idle, Syncing, Ok, Error }

/// <summary>Colours and fonts of the app. Accent is Immich's indigo.</summary>
internal static class Theme
{
    public static readonly Color Window = Color.FromArgb(244, 244, 248);
    public static readonly Color Sidebar = Color.FromArgb(234, 235, 242);
    public static readonly Color SidebarHover = Color.FromArgb(223, 224, 235);
    public static readonly Color SidebarSelected = Color.FromArgb(215, 218, 240);
    public static readonly Color Card = Color.White;
    public static readonly Color Border = Color.FromArgb(225, 226, 234);
    public static readonly Color Text = Color.FromArgb(28, 28, 34);
    public static readonly Color SubText = Color.FromArgb(98, 100, 112);
    public static readonly Color Muted = Color.FromArgb(160, 162, 172);
    public static readonly Color Accent = Color.FromArgb(66, 80, 175);
    public static readonly Color AccentHover = Color.FromArgb(55, 67, 152);
    public static readonly Color AccentPressed = Color.FromArgb(45, 55, 128);
    public static readonly Color AccentLight = Color.FromArgb(234, 236, 251);
    public static readonly Color Success = Color.FromArgb(22, 128, 60);
    public static readonly Color Danger = Color.FromArgb(200, 45, 35);
    public static readonly Color Warning = Color.FromArgb(185, 115, 0);

    public static readonly Font Body = new("Segoe UI", 9.75f);
    public static readonly Font BodyBold = new("Segoe UI Semibold", 9.75f);
    public static readonly Font Small = new("Segoe UI", 9f);
    public static readonly Font Title = new("Segoe UI Semibold", 17f);
    public static readonly Font CardTitle = new("Segoe UI Semibold", 11f);
    public static readonly Font AppName = new("Segoe UI Semibold", 12.5f);
    public static readonly Font Icons = new("Segoe MDL2 Assets", 12f);
    public static readonly Font IconsSmall = new("Segoe MDL2 Assets", 10f);

    /// <summary>The first opaque background behind a control (layout panels are transparent).</summary>
    public static Color BackgroundBehind(Control control)
    {
        for (var c = control.Parent; c != null; c = c.Parent)
            if (c.BackColor.A == 255) return c.BackColor;
        return Window;
    }

    public static Color For(SyncState state) => state switch
    {
        SyncState.Ok => Success,
        SyncState.Error => Danger,
        SyncState.Syncing => Accent,
        _ => Muted,
    };

    public static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0)
        {
            path.AddRectangle(r);
            return path;
        }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Bitmap DotBitmap(Color color, int size = 16)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, size * 0.25f, size * 0.25f, size * 0.5f, size * 0.5f);
        return bmp;
    }

    /// <summary>A Segoe MDL2 glyph as a small bitmap (for menus).</summary>
    public static Bitmap GlyphBitmap(string glyph, Color color, int size = 16)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe MDL2 Assets", size * 0.62f, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(glyph, font, brush, new RectangleF(0, 0, size, size), format);
        return bmp;
    }
}

/// <summary>Segoe MDL2 Assets code points (the icon font built into Windows 10).</summary>
internal static class Glyph
{
    public const string Link = "";
    public const string Globe = "";
    public const string Monitor = "";
    public const string Moon = "";
    public const string Lock = "";
    public const string Settings = "";
    public const string Sync = "";
    public const string History = "";
    public const string Folder = "";
    public const string OpenFolder = "";
    public const string Photo = "";
    public const string Key = "";
    public const string Check = "";
    public const string Error = "";
    public const string Warning = "";
    public const string Info = "";
    public const string Copy = "";
    public const string Exit = "";
    public const string Play = "";
}
