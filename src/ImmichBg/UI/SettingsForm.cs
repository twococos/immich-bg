using System.Diagnostics;
using ImmichBg.Config;
using ImmichBg.Immich;

namespace ImmichBg.UI;

internal sealed class SettingsForm : Form
{
    const int SidebarWidth = 236;

    readonly AppConfig config;
    readonly Action<AppConfig> onSave;
    readonly Dictionary<TargetKind, TargetPanel> panels = new();
    readonly Dictionary<TargetKind, NavItem> targetNav = new();
    readonly List<(NavItem Nav, Control Page, string Title, string Subtitle)> pages = [];

    readonly Label lblTitle = new() { AutoSize = true, Font = Theme.Title, ForeColor = Theme.Text, Margin = new Padding(0) };
    readonly Label lblSubtitle = new() { AutoSize = true, Font = Theme.Body, ForeColor = Theme.SubText, Margin = new Padding(2, 0, 0, 0) };
    readonly Panel pageHost = new() { Dock = DockStyle.Fill, BackColor = Theme.Window };

    readonly TextBox txtLocal = Ui.TextBox("http://192.168.1.200:2283");
    readonly TextBox txtRemote = Ui.TextBox("https://fotos.exemple.com");
    readonly TextBox txtKey = Ui.TextBox();
    readonly ModernButton btnTest = new(Loc.T("conn.test"), Glyph.Sync, primary: true);
    readonly FlowLayoutPanel connResults = Ui.Column();

    readonly NumericUpDown numInterval = Ui.Number(1, 1440);
    readonly ToggleSwitch tglStartup = new() { Text = Loc.T("general.startup") };
    readonly ComboBox cmbLanguage = Ui.Combo(220);

    readonly Label lblStatusDot = new() { Text = "●", AutoSize = true, Font = Theme.Body, ForeColor = Theme.Muted, Margin = new Padding(20, 2, 4, 0) };
    readonly Label lblStatus = new() { AutoSize = true, Font = Theme.Small, ForeColor = Theme.SubText, MaximumSize = new Size(SidebarWidth - 50, 0), Margin = new Padding(0, 3, 8, 8) };

    static readonly (string Code, string Name)[] Languages =
        [("", ""), ("ca", "Català"), ("es", "Español"), ("en", "English")];

    public SettingsForm(AppConfig config, Action<AppConfig> onSave, Action showLog, Action syncNow, int startPage = 0)
    {
        this.config = config;
        this.onSave = onSave;

        Text = "Immich BG";
        Icon = AppImages.AppIcon;
        StartPosition = FormStartPosition.CenterScreen;
        // Build inside Suspend/ResumeLayout like designer code, so WinForms scales everything for high-DPI screens.
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.Body;
        BackColor = Theme.Window;
        ClientSize = new Size(920, 720);
        MinimumSize = new Size(800, 560);

        // Pages
        AddPage(new NavItem(Glyph.Link, Loc.T("tab.connection")), BuildConnectionPage(), Loc.T("tab.connection"), Loc.T("page.connection"));
        foreach (var kind in Enum.GetValues<TargetKind>())
        {
            var panel = new TargetPanel(kind, config.Get(kind));
            panel.SyncToggled += (k, on) => targetNav[k].Dot = on ? Theme.Success : null;
            panels[kind] = panel;
            var glyph = kind switch { TargetKind.Wallpaper => Glyph.Monitor, TargetKind.Screensaver => Glyph.Moon, _ => Glyph.Lock };
            var nav = new NavItem(glyph, Loc.TargetName(kind)) { Dot = panel.IsEnabled ? Theme.Success : null };
            targetNav[kind] = nav;
            AddPage(nav, panel, Loc.TargetName(kind), Loc.T($"page.{kind}"));
        }
        AddPage(new NavItem(Glyph.Settings, Loc.T("tab.general")), BuildGeneralPage(), Loc.T("tab.general"), Loc.T("page.general"));

        // Content column: header, page, footer
        var titles = Ui.Column(lblTitle, lblSubtitle);
        titles.Location = new Point(28, 16);
        var header = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Theme.Window };
        header.Controls.Add(titles);

        var btnSave = new ModernButton(Loc.T("settings.save"), Glyph.Check, primary: true) { Margin = new Padding(8, 0, 0, 0) };
        var btnCancel = new ModernButton(Loc.T("settings.cancel")) { Margin = new Padding(8, 0, 0, 0) };
        btnSave.Click += (_, _) => Save();
        btnCancel.Click += (_, _) => Close();
        CancelButton = btnCancel;
        var buttons = Ui.Row(btnSave, btnCancel);
        buttons.FlowDirection = FlowDirection.RightToLeft;
        buttons.Dock = DockStyle.Right;
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Theme.Window, Padding = new Padding(28, 14, 28, 14) };
        footer.Controls.Add(buttons);
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Window };
        content.Controls.Add(pageHost);
        content.Controls.Add(header);
        content.Controls.Add(footer);

        Controls.Add(content);
        Controls.Add(BuildSidebar(showLog, syncNow));

        ResumeLayout(false);
        PerformLayout();

        SelectPage(pages[Math.Clamp(startPage, 0, pages.Count - 1)].Nav);
        ActiveControl = pages[Math.Clamp(startPage, 0, pages.Count - 1)].Nav;
        Shown += async (_, _) =>
        {
            if (config.HasConnection) await TestConnectionAsync();
        };
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Fit small screens.
        var area = Screen.FromControl(this).WorkingArea;
        if (Width > area.Width || Height > area.Height)
        {
            Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        }
    }

    public void SetStatus(SyncState state, string text)
    {
        lblStatusDot.ForeColor = Theme.For(state);
        lblStatus.Text = text;
    }

    void AddPage(NavItem nav, Control page, string title, string subtitle)
    {
        page.Dock = DockStyle.Fill;
        page.Visible = false;
        pageHost.Controls.Add(page);
        nav.Click += (_, _) => SelectPage(nav);
        pages.Add((nav, page, title, subtitle));
    }

    void SelectPage(NavItem nav)
    {
        foreach (var p in pages)
        {
            var selected = p.Nav == nav;
            p.Nav.Selected = selected;
            p.Page.Visible = selected;
            if (selected)
            {
                lblTitle.Text = p.Title;
                lblSubtitle.Text = p.Subtitle;
            }
        }
    }

    Panel BuildSidebar(Action showLog, Action syncNow)
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = SidebarWidth, BackColor = Theme.Sidebar };

        var logo = new PictureBox { Image = AppImages.LogoSmall, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(36, 36), Location = new Point(20, 21), BackColor = Theme.Sidebar };
        var name = new Label { Text = "Immich BG", Font = Theme.AppName, ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(0) };
        var tagline = new Label { Text = Loc.T("app.tagline"), Font = Theme.Small, ForeColor = Theme.SubText, AutoSize = true, Margin = new Padding(1, 0, 0, 0) };
        var names = Ui.Column(name, tagline);
        names.Location = new Point(64, 19);
        var brand = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Theme.Sidebar };
        brand.Controls.AddRange([logo, names]);

        var navSync = new NavItem(Glyph.Sync, Loc.T("menu.syncNow")) { Dock = DockStyle.None, Width = SidebarWidth, Margin = new Padding(0) };
        var navLog = new NavItem(Glyph.History, Loc.T("menu.log")) { Dock = DockStyle.None, Width = SidebarWidth, Margin = new Padding(0, 0, 0, 10) };
        navSync.Click += (_, _) => syncNow();
        navLog.Click += (_, _) => showLog();
        var bottom = Ui.Column(Ui.Row(lblStatusDot, lblStatus), navSync, navLog);
        bottom.Dock = DockStyle.Bottom;
        bottom.BackColor = Theme.Sidebar;

        sidebar.Controls.Add(bottom);
        // Dock=Top items stack in reverse order of adding.
        for (int i = pages.Count - 1; i >= 0; i--) sidebar.Controls.Add(pages[i].Nav);
        sidebar.Controls.Add(brand);
        return sidebar;
    }

    Control BuildConnectionPage()
    {
        txtLocal.Text = config.LocalServerUrl;
        txtRemote.Text = config.ServerUrl;
        txtKey.UseSystemPasswordChar = true;
        txtKey.PlaceholderText = config.ApiKeyProtected != null ? Loc.T("conn.keySaved") : "";
        btnTest.Click += async (_, _) => await TestConnectionAsync();

        var (page, stack) = Ui.Page();

        var addresses = new Card(Loc.T("card.addresses"), Glyph.Globe);
        addresses.AddRow(Loc.T("conn.local"), txtLocal);
        addresses.AddFull(Ui.Hint(Loc.T("conn.localHint")));
        addresses.AddRow(Loc.T("conn.remote"), txtRemote);
        addresses.AddFull(Ui.Hint(Loc.T("conn.remoteHint")));
        Ui.Add(stack, addresses);

        var key = new Card(Loc.T("card.apiKey"), Glyph.Key);
        key.AddRow(Loc.T("conn.key"), txtKey);
        key.AddFull(Ui.Hint(Loc.T("conn.help")));
        Ui.Add(stack, key);

        var test = new Card(Loc.T("card.connectionTest"), Glyph.Sync);
        btnTest.Margin = new Padding(0, 0, 0, 8);
        test.AddFull(btnTest);
        test.AddFull(connResults);
        Ui.Add(stack, test);

        return page;
    }

    Control BuildGeneralPage()
    {
        numInterval.Value = Math.Clamp(config.IntervalMinutes, 1, 1440);
        tglStartup.Checked = config.StartWithWindows;
        foreach (var (code, name) in Languages)
            cmbLanguage.Items.Add(code == "" ? Loc.T("general.langAuto") : name);
        cmbLanguage.SelectedIndex = Math.Max(0, Array.FindIndex(Languages, l => l.Code == config.Language));

        var (page, stack) = Ui.Page();

        var sync = new Card(Loc.T("card.sync"), Glyph.Sync);
        sync.AddRow(Loc.T("general.interval"), Ui.Row(numInterval, Ui.Inline(Loc.T("general.minutes"))));
        sync.AddFull(Ui.Hint(Loc.T("general.intervalHint")));
        Ui.Add(stack, sync);

        var system = new Card(Loc.T("card.system"), Glyph.Settings);
        system.AddFull(tglStartup);
        system.AddFull(Ui.Hint(Loc.T("general.startupHint")));
        Ui.Add(stack, system);

        var language = new Card(Loc.T("card.language"), Glyph.Globe);
        language.AddRow(Loc.T("general.language"), Ui.Row(cmbLanguage));
        language.AddFull(Ui.Hint(Loc.T("general.langHint")));
        Ui.Add(stack, language);

        var about = new Card(Loc.T("card.about"), Glyph.Info);
        var bigLogo = new PictureBox { Image = AppImages.Logo, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(96, 96), BackColor = Theme.Card, Margin = new Padding(0, 0, 16, 8) };
        var appName = new Label { Text = "Immich BG", Font = Theme.CardTitle, ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(0, 22, 0, 0) };
        var version = Ui.Hint($"{Loc.T("general.version")} {Application.ProductVersion}");
        about.AddFull(Ui.Row(bigLogo, Ui.Column(appName, version)));
        var btnConfig = new ModernButton(Loc.T("general.openConfig"), Glyph.OpenFolder);
        var btnLogs = new ModernButton(Loc.T("log.openFolder"), Glyph.OpenFolder);
        btnConfig.Click += (_, _) => OpenFolder(AppPaths.ConfigDir);
        btnLogs.Click += (_, _) => OpenFolder(AppPaths.LogDir);
        about.AddFull(Ui.Row(btnConfig, btnLogs));
        Ui.Add(stack, about);

        return page;
    }

    static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    string? EnteredApiKey() =>
        txtKey.Text.Trim() is { Length: > 0 } key ? key : ConfigStore.GetApiKey(config);

    void ShowResults(params Control[] lines)
    {
        if (IsDisposed) return;
        connResults.SuspendLayout();
        connResults.Controls.Clear();
        connResults.Controls.AddRange(lines);
        connResults.ResumeLayout();
    }

    async Task TestConnectionAsync()
    {
        var apiKey = EnteredApiKey();
        var candidates = new List<(string Label, string Url)>();
        if (txtLocal.Text.Trim().Length > 0) candidates.Add((Loc.T("conn.localShort"), txtLocal.Text.Trim()));
        if (txtRemote.Text.Trim().Length > 0) candidates.Add((Loc.T("conn.remoteShort"), txtRemote.Text.Trim()));
        if (candidates.Count == 0 || apiKey == null)
        {
            ShowResults(Ui.StatusLine(Glyph.Warning, Theme.Warning, Loc.T("conn.missing")));
            return;
        }

        btnTest.Enabled = false;
        ShowResults(Ui.StatusLine(Glyph.Sync, Theme.Accent, Loc.T("conn.testing")));

        var lines = new List<Control>();
        IReadOnlyList<AlbumInfo>? albums = null;
        bool anyNetworkFailure = false;
        foreach (var (label, url) in candidates)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                using var client = new ImmichClient(url, apiKey);
                var (version, user) = await client.TestConnectionAsync(cts.Token);
                albums ??= await client.GetAlbumsAsync(cts.Token);
                lines.Add(Ui.StatusLine(Glyph.Check, Theme.Success, $"{label}: {Loc.T("conn.ok", version, user)}"));
            }
            catch (Exception ex)
            {
                anyNetworkFailure |= ex is not ImmichApiException;
                lines.Add(Ui.StatusLine(Glyph.Error, Theme.Danger, $"{label}: {ImmichClient.Describe(ex)}"));
            }
        }

        if (albums != null)
        {
            foreach (var panel in panels.Values) panel.SetAlbums(albums);
            lines.Add(Ui.StatusLine(Glyph.Photo, Theme.Accent, Loc.T("conn.albums", albums.Count)));
        }
        if (anyNetworkFailure)
            lines.Add(Ui.StatusLine(Glyph.Info, Theme.Accent, Loc.T(albums != null ? "conn.hairpin" : "conn.hairpinMaybe")));

        ShowResults(lines.ToArray());
        if (!IsDisposed) btnTest.Enabled = true;
    }

    void Save()
    {
        var result = config.Clone();
        result.LocalServerUrl = txtLocal.Text.Trim();
        result.ServerUrl = txtRemote.Text.Trim();
        if (txtKey.Text.Trim().Length > 0) ConfigStore.SetApiKey(result, txtKey.Text);
        result.IntervalMinutes = (int)numInterval.Value;
        result.StartWithWindows = tglStartup.Checked;
        result.Language = Languages[Math.Max(0, cmbLanguage.SelectedIndex)].Code;

        var folders = new Dictionary<string, TargetKind>(StringComparer.OrdinalIgnoreCase);
        foreach (var (kind, panel) in panels)
        {
            var target = panel.Read();
            if (panel.ValidateTarget(target) is { } error)
            {
                SelectPage(targetNav[kind]);
                ShowError($"{Loc.TargetName(kind)}: {error}");
                return;
            }
            if (target.Enabled)
            {
                var full = Path.GetFullPath(target.Folder).TrimEnd('\\');
                if (folders.TryGetValue(full, out var other))
                {
                    ShowError(Loc.T("err.duplicateFolder", Loc.TargetName(other), Loc.TargetName(kind)));
                    return;
                }
                folders[full] = kind;
            }
            result.Set(kind, target);
        }

        onSave(result);
        Close();
    }

    void ShowError(string message) =>
        MessageBox.Show(this, message, "Immich BG", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
