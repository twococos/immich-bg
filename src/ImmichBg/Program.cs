using System.Diagnostics;
using ImmichBg.Logging;

namespace ImmichBg;

static class Program
{
    const string MutexName = @"Local\ImmichBg.SingleInstance";
    const string ShowSettingsEventName = @"Local\ImmichBg.ShowSettings";

    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            // Already running: ask that instance to open its settings window, then quit.
            try
            {
                using var existing = EventWaitHandle.OpenExisting(ShowSettingsEventName);
                existing.Set();
            }
            catch { }
            return;
        }

        try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }

        AppLog.Init(AppPaths.LogDir);
        ApplicationConfiguration.Initialize();
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        Application.ThreadException += (_, e) => AppLog.Error(e.Exception.ToString());
        AppDomain.CurrentDomain.UnhandledException += (_, e) => AppLog.Error(e.ExceptionObject.ToString() ?? "");

        using var showSettings = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        // Started by Windows at login: stay quiet in the tray. Started by hand: show the settings.
        var fromStartup = args.Contains(WindowsIntegration.Startup.TrayArgument);
        // "--page N" / "--log" open a given settings page / the log window (handy while developing the UI).
        var pageIndex = Array.IndexOf(args, "--page");
        var startPage = pageIndex >= 0 && pageIndex + 1 < args.Length && int.TryParse(args[pageIndex + 1], out var n) ? n : 0;
        using var app = new TrayAppContext(openSettings: !fromStartup, startPage);
        if (args.Contains("--log")) app.ShowLogWindow();
        ThreadPool.RegisterWaitForSingleObject(showSettings, (_, _) => app.ShowSettingsFromAnyThread(), null, Timeout.Infinite, executeOnlyOnce: false);

        Application.Run(app);
    }
}
