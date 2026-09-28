using Microsoft.Win32;

namespace ImmichBg.WindowsIntegration;

/// <summary>Start with Windows via the per-user Run key (no admin rights needed).</summary>
public static class Startup
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "ImmichBg";
    public const string TrayArgument = "--tray";

    public static void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled && Environment.ProcessPath is { } exe)
            key.SetValue(ValueName, $"\"{exe}\" {TrayArgument}");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
