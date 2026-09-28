namespace ImmichBg;

public static class AppPaths
{
    public static string ConfigDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImmichBg");

    public static string ConfigFile => Path.Combine(ConfigDir, "config.json");

    public static string StateDir => Path.Combine(ConfigDir, "state");

    public static string LogDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImmichBg", "logs");

    /// <summary>Writes to a temp file and renames it, so a crash never leaves a half-written file.</summary>
    public static void WriteAllTextAtomic(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, contents);
        File.Move(tmp, path, overwrite: true);
    }
}
