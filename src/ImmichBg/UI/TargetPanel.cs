using System.Text.RegularExpressions;
using ImmichBg.Config;
using ImmichBg.Immich;
using ImmichBg.Logging;
using ImmichBg.WindowsIntegration;

namespace ImmichBg.UI;

/// <summary>The settings page of one destination (wallpaper, screensaver or lock screen). Same layout for the three.</summary>
internal sealed partial class TargetPanel : UserControl
{
    readonly TargetKind kind;
    readonly TargetConfig original;
    readonly List<Card> dependentCards = [];

    readonly ToggleSwitch tglEnabled = new() { Font = Theme.BodyBold };
    readonly TextBox txtFolder = Ui.TextBox();
    readonly ModernButton btnBrowse = new(Loc.T("target.browse"), Glyph.OpenFolder);
    readonly RadioButton rbAlbum = Ui.Radio(Loc.T("target.album"));
    readonly ComboBox cmbAlbum = Ui.Combo(280);
    readonly RadioButton rbFavorites = Ui.Radio(Loc.T("target.favorites"));
    readonly RadioButton rbAll = Ui.Radio(Loc.T("target.all"));
    readonly RadioButton rbLast = Ui.Radio(Loc.T("target.last"));
    readonly NumericUpDown numLimit = Ui.Number(1, 100_000);
    readonly ToggleSwitch tglOrientation = new();
    readonly ComboBox cmbOrientation = Ui.Combo(150);
    readonly NumericUpDown numMinutes = Ui.Number(1, 1440, 64);
    readonly ToggleSwitch tglShuffle = new() { Text = Loc.T("win.shuffle") };
    readonly ToggleSwitch tglSelfRotate = new() { Text = Loc.T("win.lock.selfRotate") };
    readonly ModernButton btnApply = new(Loc.T("win.apply"), Glyph.Play, primary: true);
    readonly FlowLayoutPanel applyResult = Ui.Column();

    [GeneratedRegex(@"^\d{8}_\d{6}_[0-9a-f]{8}\.\w+$", RegexOptions.IgnoreCase)]
    private static partial Regex OwnFileRegex();

    /// <summary>Raised when the main switch changes (the sidebar shows a dot for enabled targets).</summary>
    public event Action<TargetKind, bool>? SyncToggled;

    public TargetPanel(TargetKind kind, TargetConfig config)
    {
        this.kind = kind;
        original = config.Clone();
        Dock = DockStyle.Fill;
        BackColor = Theme.Window;

        tglEnabled.Text = Loc.T("target.enabledFor", Loc.TargetName(kind).ToLowerInvariant());
        cmbOrientation.Items.AddRange([Loc.T("target.landscape"), Loc.T("target.portrait")]);

        var (page, stack) = Ui.Page();

        var main = new Card();
        main.AddFull(tglEnabled);
        main.AddFull(Ui.Hint(Loc.T("target.enabledHint")));
        Ui.Add(stack, main);

        var folderCard = new Card(Loc.T("card.folder"), Glyph.Folder);
        var folderRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0), BackColor = Theme.Card };
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderRow.Controls.Add(txtFolder, 0, 0);
        btnBrowse.Margin = new Padding(8, 3, 0, 3);
        folderRow.Controls.Add(btnBrowse, 1, 0);
        folderCard.AddFull(folderRow);
        folderCard.AddFull(Ui.Hint(Loc.T("target.folderHint")));
        Ui.Add(stack, folderCard);

        var photos = new Card(Loc.T("card.photos"), Glyph.Photo);
        photos.AddRow(Loc.T("target.source"), Ui.Column(Ui.Row(rbAlbum, cmbAlbum), rbFavorites));
        photos.AddRow(Loc.T("target.amount"), Ui.Column(rbAll, Ui.Row(rbLast, numLimit, Ui.Inline(Loc.T("target.photos")))));
        photos.AddRow(Loc.T("target.filter"), Ui.Row(tglOrientation, cmbOrientation));
        tglOrientation.Text = Loc.T("target.orientation");
        Ui.Add(stack, photos);

        var windows = BuildWindowsCard();
        Ui.Add(stack, windows);

        dependentCards.AddRange([folderCard, photos, windows]);
        Controls.Add(page);

        LoadValues(config);

        tglEnabled.CheckedChanged += (_, _) =>
        {
            SuggestFolder();
            UpdateEnabledState();
            SyncToggled?.Invoke(kind, tglEnabled.Checked);
        };
        btnBrowse.Click += (_, _) => BrowseFolder();
        // The radio buttons live in different containers, so keep them exclusive by hand.
        rbAlbum.CheckedChanged += (_, _) => { if (rbAlbum.Checked) rbFavorites.Checked = false; UpdateEnabledState(); };
        rbFavorites.CheckedChanged += (_, _) => { if (rbFavorites.Checked) rbAlbum.Checked = false; UpdateEnabledState(); };
        rbAll.CheckedChanged += (_, _) => { if (rbAll.Checked) rbLast.Checked = false; UpdateEnabledState(); };
        rbLast.CheckedChanged += (_, _) => { if (rbLast.Checked) rbAll.Checked = false; UpdateEnabledState(); };
        tglOrientation.CheckedChanged += (_, _) => UpdateEnabledState();
        btnApply.Click += async (_, _) => await ApplyToWindowsAsync();
        UpdateEnabledState();
    }

    public TargetKind Kind => kind;
    public bool IsEnabled => tglEnabled.Checked;

    Card BuildWindowsCard()
    {
        var (glyph, minutesKey, helpKey) = kind switch
        {
            TargetKind.Wallpaper => (Glyph.Monitor, "win.wallpaper.minutes", "win.wallpaper.help"),
            TargetKind.Screensaver => (Glyph.Moon, "win.screensaver.minutes", "win.screensaver.help"),
            _ => (Glyph.Lock, "win.lock.minutes", "win.lock.help"),
        };

        var card = new Card(Loc.T("win.group"), glyph);
        card.AddFull(Ui.Hint(Loc.T(helpKey)));
        if (kind == TargetKind.LockScreen)
            card.AddFull(Ui.Row(tglSelfRotate, Ui.Inline(Loc.T(minutesKey)), numMinutes));
        else
            card.AddFull(Ui.Row(Ui.Inline(Loc.T(minutesKey)), numMinutes, new Label { Width = 16 }, tglShuffle));
        btnApply.Margin = new Padding(0, 10, 0, 4);
        card.AddFull(btnApply);
        card.AddFull(applyResult);
        return card;
    }

    void LoadValues(TargetConfig c)
    {
        tglEnabled.Checked = c.Enabled;
        txtFolder.Text = c.Folder;
        rbAlbum.Checked = c.Source == SourceKind.Album;
        rbFavorites.Checked = c.Source == SourceKind.Favorites;
        rbAll.Checked = !c.LimitEnabled;
        rbLast.Checked = c.LimitEnabled;
        numLimit.Value = Math.Clamp(c.Limit, 1, 100_000);
        tglOrientation.Checked = c.OrientationEnabled;
        cmbOrientation.SelectedIndex = c.Orientation == OrientationFilter.Landscape ? 0 : 1;
        numMinutes.Value = Math.Clamp(c.WindowsMinutes, 1, 1440);
        tglShuffle.Checked = c.Shuffle;
        tglSelfRotate.Checked = c.SelfRotate;
        if (c.AlbumId != null)
        {
            cmbAlbum.Items.Add(new AlbumInfo(c.AlbumId, c.AlbumName ?? c.AlbumId, 0));
            cmbAlbum.SelectedIndex = 0;
        }
    }

    public void SetAlbums(IReadOnlyList<AlbumInfo> albums)
    {
        var selectedId = (cmbAlbum.SelectedItem as AlbumInfo)?.Id ?? original.AlbumId;
        cmbAlbum.BeginUpdate();
        cmbAlbum.Items.Clear();
        foreach (var a in albums) cmbAlbum.Items.Add(a);
        var selected = albums.FirstOrDefault(a => a.Id == selectedId);
        if (selected == null && selectedId != null)
        {
            // The configured album is not visible any more (deleted or unshared): keep showing it.
            selected = new AlbumInfo(selectedId, (original.AlbumName ?? selectedId) + " ⚠", 0);
            cmbAlbum.Items.Insert(0, selected);
        }
        cmbAlbum.SelectedItem = selected;
        cmbAlbum.EndUpdate();
    }

    public TargetConfig Read()
    {
        var c = original.Clone();
        c.Enabled = tglEnabled.Checked;
        c.Folder = txtFolder.Text.Trim();
        c.Source = rbAlbum.Checked ? SourceKind.Album : SourceKind.Favorites;
        if (cmbAlbum.SelectedItem is AlbumInfo album)
        {
            c.AlbumId = album.Id;
            c.AlbumName = album.Name.TrimEnd(' ', '⚠');
        }
        c.LimitEnabled = rbLast.Checked;
        c.Limit = (int)numLimit.Value;
        c.OrientationEnabled = tglOrientation.Checked;
        c.Orientation = cmbOrientation.SelectedIndex == 1 ? OrientationFilter.Portrait : OrientationFilter.Landscape;
        c.WindowsMinutes = (int)numMinutes.Value;
        c.Shuffle = tglShuffle.Checked;
        c.SelfRotate = tglSelfRotate.Checked;
        return c;
    }

    /// <returns>An error message, or null if the settings are valid.</returns>
    public string? ValidateTarget(TargetConfig c)
    {
        if (!c.Enabled) return null;
        if (string.IsNullOrWhiteSpace(c.Folder)) return Loc.T("err.noFolder");
        if (!Path.IsPathFullyQualified(c.Folder)) return Loc.T("err.folderNotAbsolute");
        if (c.Source == SourceKind.Album && c.AlbumId == null) return Loc.T("err.noAlbum");
        return null;
    }

    void UpdateEnabledState()
    {
        foreach (var card in dependentCards) card.Enabled = tglEnabled.Checked;
        cmbAlbum.Enabled = rbAlbum.Checked;
        numLimit.Enabled = rbLast.Checked;
        cmbOrientation.Enabled = tglOrientation.Checked;
    }

    void SuggestFolder()
    {
        if (!tglEnabled.Checked || txtFolder.Text.Length > 0) return;
        var name = kind switch
        {
            TargetKind.Wallpaper => "Wallpaper",
            TargetKind.Screensaver => "Screensaver",
            _ => "LockScreen",
        };
        txtFolder.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Immich BG", name);
    }

    void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            UseDescriptionForTitle = true,
            Description = Loc.T("card.folder"),
            ShowNewFolderButton = true,
        };
        if (Directory.Exists(txtFolder.Text)) dialog.InitialDirectory = txtFolder.Text;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var foreign = Directory.EnumerateFiles(dialog.SelectedPath)
            .Count(f => !OwnFileRegex().IsMatch(Path.GetFileName(f)));
        if (foreign > 0)
            MessageBox.Show(this, Loc.T("target.folderNotEmpty", foreign), "Immich BG", MessageBoxButtons.OK, MessageBoxIcon.Information);
        txtFolder.Text = dialog.SelectedPath;
    }

    void ShowApplyResult(string glyph, Color color, string text)
    {
        applyResult.Controls.Clear();
        applyResult.Controls.Add(Ui.StatusLine(glyph, color, text));
    }

    async Task ApplyToWindowsAsync()
    {
        var c = Read();
        if (string.IsNullOrWhiteSpace(c.Folder) || !Path.IsPathFullyQualified(c.Folder))
        {
            ShowApplyResult(Glyph.Error, Theme.Danger, Loc.T("err.noFolder"));
            return;
        }

        btnApply.Enabled = false;
        try
        {
            Directory.CreateDirectory(c.Folder);
            var hasImages = Directory.EnumerateFiles(c.Folder).Any(f => OwnFileRegex().IsMatch(Path.GetFileName(f)));
            string message;
            switch (kind)
            {
                case TargetKind.Wallpaper:
                    Wallpaper.ApplySlideshow(c.Folder, c.WindowsMinutes, c.Shuffle);
                    message = Loc.T("win.wallpaper.ok");
                    break;
                case TargetKind.Screensaver:
                    Screensaver.Apply(c.Folder, c.WindowsMinutes, c.Shuffle);
                    message = Loc.T("win.screensaver.ok");
                    break;
                default:
                    if (c.SelfRotate)
                    {
                        var chosen = await LockScreen.RotateAsync(c.Folder);
                        message = chosen == null ? Loc.T("win.lock.empty") : Loc.T("win.lock.rotated");
                    }
                    else
                    {
                        LockScreen.OpenSettings(c.Folder);
                        message = Loc.T("win.lock.instructions");
                    }
                    break;
            }
            if (hasImages)
                ShowApplyResult(Glyph.Check, Theme.Success, message);
            else
                ShowApplyResult(Glyph.Warning, Theme.Warning, message + " " + Loc.T("win.noImagesYet"));
            AppLog.Info($"{Loc.TargetName(kind)}: {message}");
        }
        catch (Exception ex)
        {
            AppLog.Error($"{Loc.TargetName(kind)}: {Loc.T("win.failed", ex.Message)}");
            var text = Loc.T("win.failed", ex.Message);
            if (kind == TargetKind.Screensaver)
            {
                // Fallback: let the user pick the folder in the classic dialog.
                Clipboard.SetText(c.Folder);
                try { Screensaver.OpenDialog(); } catch { }
                text += " " + Loc.T("win.screensaver.manual");
            }
            ShowApplyResult(Glyph.Error, Theme.Danger, text);
        }
        finally
        {
            btnApply.Enabled = true;
        }
    }
}
