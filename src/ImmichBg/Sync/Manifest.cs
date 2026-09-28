using System.Text.Json;
using ImmichBg.Config;

namespace ImmichBg.Sync;

public sealed class ManifestEntry
{
    public string FileName { get; set; } = "";
    public string Version { get; set; } = "";
}

/// <summary>The files the app has written to a target folder. Only these are ever deleted.</summary>
public sealed class Manifest
{
    public string Folder { get; set; } = "";
    public Dictionary<string, ManifestEntry> Entries { get; set; } = new();

    public static Manifest Load(string path)
    {
        if (!File.Exists(path)) return new Manifest();
        return JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), ConfigStore.JsonOptions) ?? new Manifest();
    }

    public void Save(string path) =>
        AppPaths.WriteAllTextAtomic(path, JsonSerializer.Serialize(this, ConfigStore.JsonOptions));
}
