using System.Drawing.Drawing2D;

namespace ImmichBg.UI;

/// <summary>On/off switch in the Windows 10 settings style.</summary>
internal sealed class ToggleSwitch : CheckBox
{
    bool hover;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        AutoSize = true;
        Cursor = Cursors.Hand;
        Font = Theme.Body;
        ForeColor = Theme.Text;
        Margin = new Padding(0, 4, 12, 4);
    }

    int TrackWidth => LogicalToDeviceUnits(40);
    int TrackHeight => LogicalToDeviceUnits(20);
    int Gap => LogicalToDeviceUnits(10);

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(Text, Font);
        return new Size(TrackWidth + (text.Width > 0 ? Gap + text.Width : 0) + 2, Math.Max(TrackHeight, text.Height) + LogicalToDeviceUnits(6));
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.BackgroundBehind(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float y = (Height - TrackHeight) / 2f;
        var track = new RectangleF(0.5f, y, TrackWidth - 1, TrackHeight - 1);
        using var path = Theme.RoundedRect(track, track.Height / 2);
        var on = Checked;
        var fill = !Enabled ? (on ? Theme.Muted : Theme.Card)
            : on ? (hover ? Theme.AccentHover : Theme.Accent)
            : (hover ? Theme.Window : Theme.Card);
        using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
        if (!on)
        {
            using var pen = new Pen(Enabled ? Theme.SubText : Theme.Muted, 1f);
            g.DrawPath(pen, path);
        }

        float knob = TrackHeight - LogicalToDeviceUnits(9);
        float inset = LogicalToDeviceUnits(5);
        float kx = on ? track.Right - knob - inset : track.Left + inset;
        using (var brush = new SolidBrush(on ? Color.White : Enabled ? Theme.SubText : Theme.Muted))
            g.FillEllipse(brush, kx, y + (TrackHeight - knob) / 2f - 0.5f, knob, knob);

        if (Text.Length > 0)
        {
            var textRect = new Rectangle(TrackWidth + Gap, 0, Width - TrackWidth - Gap, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, Enabled ? ForeColor : Theme.Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(g, new Rectangle(TrackWidth + Gap - 2, 1, Width - TrackWidth - Gap + 1, Height - 2));
    }
}

/// <summary>
/// Flat rounded button. Primary = filled with the accent colour.
/// Derives from Control (not Button) so Windows never draws its own default-button frame.
/// </summary>
internal sealed class ModernButton : Control, IButtonControl
{
    bool hover, pressed;

    public ModernButton(string text, string? glyph = null, bool primary = false)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
        TabStop = true;
        Text = text;
        GlyphText = glyph;
        Primary = primary;
        Cursor = Cursors.Hand;
        Font = primary ? Theme.BodyBold : Theme.Body;
        AutoSize = true;
        Margin = new Padding(0, 4, 8, 4);
    }

    public bool Primary { get; }
    public string? GlyphText { get; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public DialogResult DialogResult { get; set; }

    public void NotifyDefault(bool value) { }

    public void PerformClick()
    {
        if (Enabled) OnClick(EventArgs.Empty);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter) PerformClick();
        base.OnKeyUp(e);
    }

    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = TextRenderer.MeasureText(Text, Font);
        var glyph = GlyphText == null ? 0 : TextRenderer.MeasureText(GlyphText, Theme.IconsSmall).Width + LogicalToDeviceUnits(2);
        return new Size(text.Width + glyph + LogicalToDeviceUnits(26), LogicalToDeviceUnits(34));
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.BackgroundBehind(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var path = Theme.RoundedRect(rect, LogicalToDeviceUnits(5));

        Color fill, fore, border;
        if (Primary)
        {
            fill = !Enabled ? Theme.Muted : pressed ? Theme.AccentPressed : hover ? Theme.AccentHover : Theme.Accent;
            fore = Color.White;
            border = fill;
        }
        else
        {
            fill = pressed ? Theme.SidebarSelected : hover ? Theme.AccentLight : Theme.Card;
            fore = Enabled ? Theme.Text : Theme.Muted;
            border = hover && Enabled ? Theme.Accent : Theme.Border;
        }
        using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
        using (var pen = new Pen(border)) g.DrawPath(pen, path);

        var textSize = TextRenderer.MeasureText(Text, Font);
        var glyphSize = GlyphText == null ? Size.Empty : TextRenderer.MeasureText(GlyphText, Theme.IconsSmall);
        var gap = GlyphText == null ? 0 : LogicalToDeviceUnits(2);
        var x = (Width - (textSize.Width + glyphSize.Width + gap)) / 2;
        const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        if (GlyphText != null)
        {
            TextRenderer.DrawText(g, GlyphText, Theme.IconsSmall, new Rectangle(x, 1, glyphSize.Width, Height), Primary ? fore : Enabled ? Theme.Accent : Theme.Muted, flags);
            x += glyphSize.Width + gap;
        }
        TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, textSize.Width + 4, Height), fore, flags);

        if (Focused && ShowFocusCues)
        {
            using var focusPath = Theme.RoundedRect(RectangleF.Inflate(rect, -2.5f, -2.5f), LogicalToDeviceUnits(3));
            using var pen = new Pen(Primary ? Color.White : Theme.Accent) { DashStyle = DashStyle.Dot };
            g.DrawPath(pen, focusPath);
        }
    }
}

/// <summary>White rounded panel with an optional icon + title, holding a 2-column grid (label | control).</summary>
internal sealed class Card : Panel
{
    public TableLayoutPanel Body { get; }

    public Card(string? title = null, string? glyph = null)
    {
        DoubleBuffered = true;
        BackColor = Theme.Card;
        Padding = new Padding(20, 14, 20, 16);
        Margin = new Padding(0, 0, 0, 14);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Dock = DockStyle.Fill;

        Body = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            BackColor = Theme.Card,
        };
        Body.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(Body);

        if (title != null)
        {
            var header = Ui.Row();
            header.Margin = new Padding(0, 0, 0, 8);
            if (glyph != null)
                header.Controls.Add(new Label { Text = glyph, Font = Theme.Icons, ForeColor = Theme.Accent, AutoSize = true, Margin = new Padding(0, 4, 8, 0) });
            header.Controls.Add(new Label { Text = title, Font = Theme.CardTitle, ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(0, 1, 0, 0) });
            AddFull(header);
        }
    }

    public void AddRow(string label, Control content)
    {
        var row = NextRow();
        Body.Controls.Add(Ui.FieldLabel(label), 0, row);
        Body.Controls.Add(content, 1, row);
    }

    public void AddFull(Control content)
    {
        var row = NextRow();
        Body.Controls.Add(content, 0, row);
        Body.SetColumnSpan(content, 2);
    }

    int NextRow()
    {
        Body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        return Body.RowCount++;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.BackgroundBehind(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theme.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), LogicalToDeviceUnits(8));
        using (var brush = new SolidBrush(BackColor)) g.FillPath(brush, path);
        using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, path);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }
}

/// <summary>Sidebar entry: icon + text, highlighted when selected, optional status dot.</summary>
internal sealed class NavItem : Control
{
    bool selected, hover;
    Color? dot;

    public NavItem(string glyph, string text)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        GlyphText = glyph;
        Text = text;
        Font = Theme.Body;
        Height = 42;
        Dock = DockStyle.Top;
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public string GlyphText { get; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => selected;
        set { selected = value; Invalidate(); }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color? Dot
    {
        get => dot;
        set { dot = value; Invalidate(); }
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space) OnClick(EventArgs.Empty);
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.BackgroundBehind(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var r = new Rectangle(LogicalToDeviceUnits(8), LogicalToDeviceUnits(2), Width - LogicalToDeviceUnits(16), Height - LogicalToDeviceUnits(4));
        if (selected || hover || Focused)
        {
            using var path = Theme.RoundedRect(r, LogicalToDeviceUnits(6));
            using var brush = new SolidBrush(selected ? Theme.SidebarSelected : Theme.SidebarHover);
            g.FillPath(brush, path);
        }
        if (selected)
        {
            var barHeight = LogicalToDeviceUnits(18);
            using var bar = Theme.RoundedRect(new RectangleF(r.Left, r.Top + (r.Height - barHeight) / 2f, LogicalToDeviceUnits(3), barHeight), 1.5f);
            using var brush = new SolidBrush(Theme.Accent);
            g.FillPath(brush, bar);
        }

        const TextFormatFlags center = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding;
        TextRenderer.DrawText(g, GlyphText, Theme.Icons, new Rectangle(r.Left + LogicalToDeviceUnits(10), r.Top, LogicalToDeviceUnits(26), r.Height),
            selected ? Theme.Accent : Theme.Text, center);

        var textLeft = r.Left + LogicalToDeviceUnits(46);
        var dotSpace = dot.HasValue ? LogicalToDeviceUnits(22) : 0;
        TextRenderer.DrawText(g, Text, selected ? Theme.BodyBold : Theme.Body, new Rectangle(textLeft, r.Top, r.Right - textLeft - dotSpace, r.Height),
            Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        if (dot is { } color)
        {
            var d = LogicalToDeviceUnits(8);
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, r.Right - LogicalToDeviceUnits(14) - d / 2f, r.Top + (r.Height - d) / 2f, d, d);
        }
    }
}
