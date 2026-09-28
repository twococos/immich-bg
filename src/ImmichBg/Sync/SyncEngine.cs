using System.Text.RegularExpressions;
using ImmichBg.Config;
using ImmichBg.Immich;
using ImmichBg.Logging;

namespace ImmichBg.Sync;

public sealed record TargetResult(TargetKind Kind, bool Ok, int Added, int Removed, int Total, string? Error);

public sealed partial class SyncEngine(string stateDir)
{
    const int SaveEvery = 20;
    const int MaxFailuresPerTarget = 5;

    [GeneratedRegex(@"^\d{8}_\d{6}_[0-9a-f]{8}\.part$", RegexOptions.IgnoreCase)]
    private static partial Regex PartFileRegex();

    public async Task<IReadOnlyList<TargetResult>> RunAsync(AppConfig config, IImmichSource source, CancellationToken ct)
    {
        // Targets using the same source share one server query per cycle.
        var cache = new Dictionary<string, IReadOnlyList<RemoteAsset>>();
        var usedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<TargetResult>();

        foreach (var kind in Enum.GetValues<TargetKind>())
        {
            var target = config.Get(kind);
            if (!target.Enabled) continue;
            ct.ThrowIfCancellationRequested();
            try
            {
                var r = await SyncTargetAsync(kind, target, source, cache, usedFolders, ct);
                results.Add(r);
                AppLog.Info(Loc.T("log.targetDone", Loc.TargetName(kind), r.Added, r.Removed, r.Total, SourceLabel(target)));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Nothing has been deleted for this target unless the server listing succeeded.
                var message = ImmichClient.Describe(ex);
                AppLog.Error(Loc.T("log.targetFailed", Loc.TargetName(kind), message));
                results.Add(new TargetResult(kind, false, 0, 0, 0, message));
            }
        }
        return results;
    }

    async Task<TargetResult> SyncTargetAsync(
        TargetKind kind,
        TargetConfig target,
        IImmichSource source,
        Dictionary<string, IReadOnlyList<RemoteAsset>> cache,
        HashSet<string> usedFolders,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(target.Folder)) throw new InvalidOperationException(Loc.T("err.noFolder"));
        if (target.Source == SourceKind.Album && string.IsNullOrEmpty(target.AlbumId)) throw new InvalidOperationException(Loc.T("err.noAlbum"));

        var folder = Path.GetFullPath(target.Folder);
        if (!usedFolders.Add(folder)) throw new InvalidOperationException(Loc.T("err.sharedFolder"));

        // 1. Ask the server first: if this fails we stop before touching any file.
        var key = target.Source == SourceKind.Favorites ? "favorites" : "album:" + target.AlbumId;
        if (!cache.TryGetValue(key, out var all))
        {
            all = await source.GetAssetsAsync(target.Source, target.AlbumId, ct);
            cache[key] = all;
        }
        var desired = SyncPlanner.SelectDesired(all, target);

        // 2. Load what we wrote last time.
        Directory.CreateDirectory(stateDir);
        var manifestPath = Path.Combine(stateDir, $"{kind}.json");
        var manifest = Manifest.Load(manifestPath);
        if (!string.IsNullOrEmpty(manifest.Folder) && !string.Equals(Path.GetFullPath(manifest.Folder), folder, StringComparison.OrdinalIgnoreCase))
        {
            // Destination changed: remove our files from the old folder and start over.
            AppLog.Info(Loc.T("log.folderChanged", Loc.TargetName(kind), manifest.Folder));
            foreach (var entry in manifest.Entries.Values) TryDelete(Path.Combine(manifest.Folder, entry.FileName));
            manifest = new Manifest();
        }
        manifest.Folder = folder;
        Directory.CreateDirectory(folder);
        DeleteLeftoverPartFiles(folder);

        // 3. Apply the difference.
        var plan = SyncPlanner.Plan(desired, manifest, name => File.Exists(Path.Combine(folder, name)));
        int added = 0, removed = 0, failures = 0, unsaved = 0;
        try
        {
            foreach (var id in plan.ToDelete)
            {
                TryDelete(Path.Combine(folder, manifest.Entries[id].FileName));
                manifest.Entries.Remove(id);
                removed++;
                unsaved++;
            }

            foreach (var asset in plan.ToDownload)
            {
                ct.ThrowIfCancellationRequested();
                var baseName = SyncPlanner.BaseFileName(asset);
                var partPath = Path.Combine(folder, baseName + ".part");
                try
                {
                    // Download to .part and rename, so Windows never shows a half-written image.
                    var ext = await source.DownloadToFileAsync(asset, partPath, ct);
                    var fileName = baseName + ext;
                    File.Move(partPath, Path.Combine(folder, fileName), overwrite: true);

                    if (manifest.Entries.TryGetValue(asset.Id, out var old) && !string.Equals(old.FileName, fileName, StringComparison.OrdinalIgnoreCase))
                        TryDelete(Path.Combine(folder, old.FileName));
                    manifest.Entries[asset.Id] = new ManifestEntry { FileName = fileName, Version = asset.Version };
                    added++;

                    if (++unsaved >= SaveEvery)
                    {
                        manifest.Save(manifestPath);
                        unsaved = 0;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    TryDelete(partPath);
                    AppLog.Warn(Loc.T("log.downloadFailed", asset.Id, ImmichClient.Describe(ex)));
                    if (++failures >= MaxFailuresPerTarget)
                        throw new InvalidOperationException(Loc.T("err.tooManyFailures", failures));
                }
            }
        }
        finally
        {
            manifest.Save(manifestPath);
        }

        return new TargetResult(kind, failures == 0, added, removed, manifest.Entries.Count, failures == 0 ? null : Loc.T("err.someFailed", failures));
    }

    static string SourceLabel(TargetConfig t) =>
        t.Source == SourceKind.Favorites ? Loc.T("src.favorites") : Loc.T("src.album", t.AlbumName ?? t.AlbumId ?? "?");

    static void DeleteLeftoverPartFiles(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*.part"))
            if (PartFileRegex().IsMatch(Path.GetFileName(file))) TryDelete(file);
    }

    static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            AppLog.Warn(Loc.T("log.deleteFailed", path, ex.Message));
        }
    }
}
