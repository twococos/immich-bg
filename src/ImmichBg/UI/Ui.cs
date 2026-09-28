namespace ImmichBg.UI;

/// <summary>Small helpers to build forms in code (no designer files).</summary>
internal static class Ui
{
    public const int HintWidth = 560;

    public static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent,
        };
        row.Controls.AddRange(controls);
        return row;
    }

    public static FlowLayoutPanel Column(params Control[] controls)
    {
        var col = Row(controls);
        col.FlowDirection = FlowDirection.TopDown;
        return col;
    }

    /// <summary>Label on the left column of a card.</summary>
    public static Label FieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Body,
        ForeColor = Theme.Text,
        Margin = new Padding(0, 9, 18, 3),
    };

    /// <summary>Text next to a control in a row.</summary>
    public static Label Inline(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Body,
        ForeColor = Theme.SubText,
        Margin = new Padding(2, 9, 8, 3),
    };

    /// <summary>Grey explanatory text that wraps.</summary>
    public static Label Hint(string text, int maxWidth = HintWidth) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Small,
        ForeColor = Theme.SubText,
        Margin = new Padding(0, 2, 0, 8),
        MaximumSize = new Size(maxWidth, 0),
    };

    public static TextBox TextBox(string placeholder = "") => new()
    {
        Font = Theme.Body,
        Dock = DockStyle.Fill,
        PlaceholderText = placeholder,
        Margin = new Padding(0, 5, 0, 5),
    };

    public static ComboBox Combo(int width) => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.System,
        Font = Theme.Body,
        Width = width,
        Margin = new Padding(0, 5, 8, 5),
    };

    public static NumericUpDown Number(int min, int max, int width = 76) => new()
    {
        Minimum = min,
        Maximum = max,
        Width = width,
        Font = Theme.Body,
        Margin = new Padding(0, 5, 4, 5),
    };

    public static RadioButton Radio(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Body,
        ForeColor = Theme.Text,
        Margin = new Padding(0, 7, 12, 3),
        Cursor = Cursors.Hand,
    };

    /// <summary>A coloured icon followed by wrapping text (connection results, "apply" feedback).</summary>
    public static FlowLayoutPanel StatusLine(string glyph, Color color, string text, int maxWidth = HintWidth - 30)
    {
        var icon = new Label { Text = glyph, Font = Theme.IconsSmall, ForeColor = color, AutoSize = true, Margin = new Padding(0, 4, 6, 0) };
        var label = new Label { Text = text, Font = Theme.Body, ForeColor = Theme.Text, AutoSize = true, MaximumSize = new Size(maxWidth, 0), Margin = new Padding(0, 2, 0, 4) };
        return Row(icon, label);
    }

    /// <summary>Scrollable page body: cards stacked vertically, filling the width.</summary>
    public static (Panel Page, TableLayoutPanel Stack) Page()
    {
        var page = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Window, Padding = new Padding(28, 4, 28, 12) };
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = Theme.Window,
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.Controls.Add(stack);
        return (page, stack);
    }

    public static void Add(TableLayoutPanel stack, Control card)
    {
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.Controls.Add(card, 0, stack.RowCount++);
    }
}
