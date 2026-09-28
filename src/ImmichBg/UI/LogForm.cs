using System.Diagnostics;
using ImmichBg.Logging;

namespace ImmichBg.UI;

/// <summary>Live log viewer: dark console with colours per level.</summary>
internal sealed class LogForm : Form
{
    static readonly Color ConsoleBack = Color.FromArgb(28, 29, 36);
    static readonly Color TimeColor = Color.FromArgb(120, 124, 140);
    static readonly Color TextColor = Color.FromArgb(222, 224, 232);
    static readonly Color InfoColor = Color.FromArgb(130, 150, 255);
    static readonly Color WarnColor = Color.FromArgb(240, 180, 60);
    static readonly Color ErrorColor = Color.FromArgb(250, 100, 90);

    readonly RichTextBox box = new()
    {
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9.75f),
        BackColor = ConsoleBack,
        ForeColor = TextColor,
        WordWrap = false,
        DetectUrls = false,
    };

    public LogForm()
    {
        Text = "Immich BG – " + Loc.T("log.title");
        Icon = AppImages.AppIcon;
        StartPosition = FormStartPosition.CenterScreen;
        // Build inside Suspend/ResumeLayout like designer code, so WinForms scales everything for high-DPI screens.
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.Body;
        BackColor = Theme.Window;
        ClientSize = new Size(920, 500);

        var title = new Label { Text = Loc.T("log.title"), Font = Theme.CardTitle, ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(0, 6, 16, 0) };
        var hint = new Label { Text = Loc.T("log.hint"), Font = Theme.Small, ForeColor = Theme.SubText, AutoSize = true, Margin = new Padding(0, 9, 16, 0) };
        var btnCopy = new ModernButton(Loc.T("log.copy"), Glyph.Copy);
        var btnFolder = new ModernButton(Loc.T("log.openFolder"), Glyph.OpenFolder);
        btnCopy.Click += (_, _) => { if (box.TextLength > 0) Clipboard.SetText(box.Text); };
        btnFolder.Click += (_, _) =>
        {
            Directory.CreateDirectory(AppPaths.LogDir);
            Process.Start(new ProcessStartInfo(AppPaths.LogDir) { UseShellExecute = true });
        };
        var buttons = Ui.Row(btnFolder, btnCopy);
        buttons.FlowDirection = FlowDirection.RightToLeft;
        buttons.Dock = DockStyle.Right;
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(16, 10, 12, 8), BackColor = Theme.Window };
        var left = Ui.Row(title, hint);
        left.Dock = DockStyle.Left;
        toolbar.Controls.Add(buttons);
        toolbar.Controls.Add(left);

        var console = new Panel { Dock = DockStyle.Fill, BackColor = ConsoleBack, Padding = new Padding(14, 10, 4, 10) };
        console.Controls.Add(box);

        Controls.Add(console);
        Controls.Add(toolbar);

        ResumeLayout(false);
        PerformLayout();

        ActiveControl = btnFolder;
        // Colours need the native control, so fill it once the window exists.
        Shown += (_, _) =>
        {
            foreach (var line in AppLog.Snapshot()) Append(line);
            AppLog.LineAdded += OnLineAdded;
            ScrollToEnd();
        };
        FormClosed += (_, _) => AppLog.LineAdded -= OnLineAdded;
    }

    void OnLineAdded(string line)
    {
        if (!IsHandleCreated || IsDisposed) return;
        BeginInvoke(() =>
        {
            Append(line);
            ScrollToEnd();
        });
    }

    /// <summary>Lines look like "2024-01-31 18:30:05 [INFO] message".</summary>
    void Append(string line)
    {
        var levelStart = line.IndexOf('[');
        var levelEnd = line.IndexOf(']');
        if (levelStart < 0 || levelEnd < levelStart)
        {
            Write(line + "\n", TextColor);
            return;
        }
        var level = line[(levelStart + 1)..levelEnd];
        var levelColor = level switch { "ERROR" => ErrorColor, "WARN" => WarnColor, _ => InfoColor };
        Write(line[..levelStart], TimeColor);
        Write(level.PadRight(5) + "  ", levelColor);
        Write(line[(levelEnd + 1)..].TrimStart() + "\n", level == "ERROR" ? ErrorColor : TextColor);
    }

    void Write(string text, Color color)
    {
        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionColor = color;
        box.AppendText(text);
    }

    void ScrollToEnd()
    {
        box.SelectionStart = box.TextLength;
        box.ScrollToCaret();
    }
}
