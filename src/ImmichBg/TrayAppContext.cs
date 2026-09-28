using ImmichBg.Config;
using ImmichBg.Immich;
using ImmichBg.Logging;
using ImmichBg.Sync;
using ImmichBg.UI;
using Microsoft.Win32;

namespace ImmichBg;

/// <summary>The whole app: tray icon, periodic sync loop and the windows opened from the menu.</summary>
internal sealed class TrayAppContext : ApplicationContext
{
    static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    static readonly TimeSpan ResumeDelay = TimeSpan.FromSeconds(30);

    readonly SynchronizationContext ui;
    readonly NotifyIcon tray;
    readonly SyncEngine engine = new(AppPaths.StateDir);
    readonly SemaphoreSlim wake = new(0, 1);
    readonly CancellationTokenSource exitCts = new();
    readonly System.Windows.Forms.Timer lockScreenTimer = new();

    AppConfig config;
    ServerConnection? connection;
    string? connectionKey;
    bool syncing;
    string status = "";
    SyncState state = SyncState.Idle;
    ToolStripMenuItem? statusItem;
    SettingsForm? settingsForm;
    LogForm? logForm;

    public TrayAppContext(bool openSettings, int startPage = 0)
    {
        ui = SynchronizationContext.Current ?? throw new InvalidOperationException("No UI synchronization context");
        config = ConfigStore.Load();
        Loc.SetLanguage(config.Language);

        tray = new NotifyIcon { Icon = TrayIcons.Idle, Visible = true };
        tray.DoubleClick += (_, _) => ShowSettings();
        BuildMenu();
        SetStatus(SyncState.Idle, Loc.T("status.waiting"));

        lockScreenTimer.Tick += async (_, _) => await RotateLockScreenAsync();
        ConfigureLockScreenTimer();
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        // Keep the "start with Windows" entry pointing at this exe, in case it was moved since it was saved.
        if (config.StartWithWindows && config.HasConnection)
        {
            try { WindowsIntegration.Startup.Apply(true); } catch (Exception ex) { AppLog.Warn(ex.Message); }
        }

        AppLog.Info(Loc.T("log.started", Application.ProductVersion));
        _ = LoopAsync();

        if (openSettings || !config.HasConnection) ShowSettings(startPage);
    }

    public void ShowSettingsFromAnyThread() => ui.Post(_ => ShowSettings(), null);

    void BuildMenu()
    {
        var old = tray.ContextMenuStrip;
        var menu = new ContextMenuStrip
        {
            Renderer = new ToolStripProfessionalRenderer(new MenuColors()) { RoundedEdges = false },
            Font = Theme.Body,
            Padding = new Padding(2, 4, 2, 4),
        };
        statusItem = new ToolStripMenuItem(status) { Enabled = false, Image = Theme.DotBitmap(Theme.For(state)) };
        menu.Items.Add(new ToolStripLabel("Immich BG") { Font = Theme.BodyBold, ForeColor = Theme.Text, Padding = new Padding(4, 2, 0, 0) });
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Item(Loc.T("menu.syncNow"), Glyph.Sync, SyncNow));
        var settingsItem = Item(Loc.T("menu.settings"), Glyph.Settings, ShowSettings);
        settingsItem.Font = Theme.BodyBold;
        menu.Items.Add(settingsItem);
        menu.Items.Add(Item(Loc.T("menu.log"), Glyph.History, ShowLog));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Item(Loc.T("menu.exit"), Glyph.Exit, ExitThread));
        tray.ContextMenuStrip = menu;
        old?.Dispose();

        static ToolStripMenuItem Item(string text, string glyph, Action action) =>
            new(text, Theme.GlyphBitmap(glyph, Theme.Text), (_, _) => action()) { Padding = new Padding(0, 3, 0, 3) };
    }

    public void SyncNow()
    {
        if (wake.CurrentCount == 0)
        {
            try { wake.Release(); } catch (SemaphoreFullException) { }
        }
    }

    async Task LoopAsync()
    {
        try
        {
            await wake.WaitAsync(StartupDelay, exitCts.Token);
            while (!exitCts.IsCancellationRequested)
            {
                await RunCycleAsync();
                await wake.WaitAsync(TimeSpan.FromMinutes(Math.Max(1, config.IntervalMinutes)), exitCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task RunCycleAsync()
    {
        if (!config.HasConnection)
        {
            SetStatus(SyncState.Idle, Loc.T("status.notConfigured"));
            return;
        }
        if (!Enum.GetValues<TargetKind>().Any(k => config.Get(k).Enabled))
        {
            SetStatus(SyncState.Idle, Loc.T("status.nothingEnabled"));
            return;
        }

        var conn = GetConnection();
        if (conn == null)
        {
            SetStatus(SyncState.Error, Loc.T("status.noKey"));
            return;
        }

        syncing = true;
        SetStatus(SyncState.Syncing, Loc.T("status.syncing"));
        var snapshot = config.Clone();
        try
        {
            var source = await conn.ResolveAsync(exitCts.Token);
            if (source == null)
            {
                AppLog.Error(Loc.T("log.unreachable", string.Join(" · ", snapshot.ServerUrls)));
                SetStatus(SyncState.Error, Loc.T("status.unreachable", DateTime.Now.ToString("HH:mm")));
                return;
            }

            // Run the network/disk work off the UI thread.
            var results = await Task.Run(() => engine.RunAsync(snapshot, source, exitCts.Token));
            var ok = results.All(r => r.Ok);
            SetStatus(ok ? SyncState.Ok : SyncState.Error,
                Loc.T(ok ? "status.ok" : "status.error", DateTime.Now.ToString("HH:mm")));
            if (!ok)
                tray.ShowBalloonTip(5000, "Immich BG", string.Join("\n", results.Where(r => !r.Ok).Select(r => $"{Loc.TargetName(r.Kind)}: {r.Error}")), ToolTipIcon.Warning);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AppLog.Error(ex.ToString());
            SetStatus(SyncState.Error, Loc.T("status.error", DateTime.Now.ToString("HH:mm")));
        }
        finally
        {
            syncing = false;
        }
    }

    ServerConnection? GetConnection()
    {
        var apiKey = ConfigStore.GetApiKey(config);
        if (apiKey == null) return null;
        var key = string.Join("\n", config.ServerUrls) + "\n" + apiKey;
        if (connection == null || connectionKey != key)
        {
            connection?.Dispose();
            connection = new ServerConnection(config.ServerUrls, apiKey);
            connectionKey = key;
        }
        return connection;
    }

    void SetStatus(SyncState newState, string text)
    {
        state = newState;
        status = text;
        tray.Icon = TrayIcons.For(newState);
        var tip = "Immich BG – " + text;
        tray.Text = tip.Length > 127 ? tip[..127] : tip;
        if (statusItem != null)
        {
            statusItem.Text = text;
            statusItem.Image?.Dispose();
            statusItem.Image = Theme.DotBitmap(Theme.For(newState));
        }
        settingsForm?.SetStatus(newState, text);
    }

    void ShowSettings() => ShowSettings(0);

    void ShowSettings(int page)
    {
        if (settingsForm is { IsDisposed: false })
        {
            settingsForm.WindowState = FormWindowState.Normal;
            settingsForm.Activate();
            return;
        }
        settingsForm = new SettingsForm(config.Clone(), OnSettingsSaved, ShowLog, SyncNow, page);
        settingsForm.SetStatus(state, status);
        settingsForm.FormClosed += (_, _) => settingsForm = null;
        settingsForm.Show();
        settingsForm.Activate();
    }

    public void ShowLogWindow() => ShowLog();

    void ShowLog()
    {
        if (logForm is { IsDisposed: false })
        {
            logForm.WindowState = FormWindowState.Normal;
            logForm.Activate();
            return;
        }
        logForm = new LogForm();
        logForm.FormClosed += (_, _) => logForm = null;
        logForm.Show();
        logForm.Activate();
    }

    void OnSettingsSaved(AppConfig newConfig)
    {
        var languageChanged = newConfig.Language != config.Language;
        config = newConfig;
        ConfigStore.Save(config);
        try
        {
            WindowsIntegration.Startup.Apply(config.StartWithWindows);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex.Message);
        }
        if (languageChanged)
        {
            Loc.SetLanguage(config.Language);
            BuildMenu();
        }
        ConfigureLockScreenTimer();
        AppLog.Info(Loc.T("log.configSaved"));
        SyncNow();
    }

    void ConfigureLockScreenTimer()
    {
        var lockCfg = config.LockScreen;
        lockScreenTimer.Stop();
        if (lockCfg.Enabled && lockCfg.SelfRotate && !string.IsNullOrWhiteSpace(lockCfg.Folder))
        {
            lockScreenTimer.Interval = Math.Max(1, lockCfg.WindowsMinutes) * 60_000;
            lockScreenTimer.Start();
        }
    }

    async Task RotateLockScreenAsync()
    {
        if (syncing) return; // try again next tick rather than pick a file being written
        try
        {
            await WindowsIntegration.LockScreen.RotateAsync(config.LockScreen.Folder);
        }
        catch (Exception ex)
        {
            AppLog.Error(Loc.T("log.lockRotateFailed", ex.Message));
            lockScreenTimer.Stop();
        }
    }

    void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume) return;
        _ = Task.Delay(ResumeDelay, exitCts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled) SyncNow();
        }, TaskScheduler.Default);
    }

    protected override void ExitThreadCore()
    {
        AppLog.Info(Loc.T("log.exiting"));
        exitCts.Cancel();
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        lockScreenTimer.Stop();
        settingsForm?.Close();
        logForm?.Close();
        tray.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            tray.Dispose();
            lockScreenTimer.Dispose();
            connection?.Dispose();
            exitCts.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>Tray menu colours matching the settings window.</summary>
    sealed class MenuColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Theme.AccentLight;
        public override Color MenuItemBorder => Theme.AccentLight;
        public override Color MenuBorder => Theme.Border;
        public override Color ToolStripDropDownBackground => Color.White;
        public override Color ImageMarginGradientBegin => Color.White;
        public override Color ImageMarginGradientMiddle => Color.White;
        public override Color ImageMarginGradientEnd => Color.White;
        public override Color SeparatorDark => Theme.Border;
        public override Color SeparatorLight => Color.White;
    }
}
