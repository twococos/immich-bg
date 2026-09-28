namespace ImmichBg.Logging;

/// <summary>
/// Tiny thread-safe logger: one file per day (kept 14 days) plus the latest lines in memory for the log window.
/// </summary>
public static class AppLog
{
    const int MaxRecentLines = 500;
    const int KeepDays = 14;

    static readonly object Gate = new();
    static readonly Queue<string> Recent = new();
    static string? logDir;

    public static event Action<string>? LineAdded;

    public static void Init(string directory)
    {
        logDir = directory;
        try
        {
            Directory.CreateDirectory(directory);
            var cutoff = DateTime.Now.AddDays(-KeepDays);
            foreach (var file in Directory.EnumerateFiles(directory, "log-*.txt"))
                if (File.GetLastWriteTime(file) < cutoff) File.Delete(file);
        }
        catch
        {
            // Logging must never crash the app.
        }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    public static string[] Snapshot()
    {
        lock (Gate) return Recent.ToArray();
    }

    static void Write(string level, string message)
    {
        var now = DateTime.Now;
        var line = $"{now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        lock (Gate)
        {
            Recent.Enqueue(line);
            while (Recent.Count > MaxRecentLines) Recent.Dequeue();
            if (logDir != null)
            {
                try { File.AppendAllText(Path.Combine(logDir, $"log-{now:yyyyMMdd}.txt"), line + Environment.NewLine); }
                catch { }
            }
        }
        LineAdded?.Invoke(line);
    }
}
