using ImmichBg.Config;
using ImmichBg.Immich;

namespace ImmichBg.Sync;

public sealed record SyncPlan(IReadOnlyList<RemoteAsset> ToDownload, IReadOnlyList<string> ToDelete);

/// <summary>Pure decision logic: what the folder should contain and what to change to get there.</summary>
public static class SyncPlanner
{
    public static List<RemoteAsset> SelectDesired(IEnumerable<RemoteAsset> assets, TargetConfig target)
    {
        var query = assets;
        if (target.OrientationEnabled)
            query = query.Where(a => MatchesOrientation(a, target.Orientation));

        query = query
            .OrderByDescending(a => a.TakenAt)
            .ThenBy(a => a.Id, StringComparer.Ordinal);

        if (target.LimitEnabled)
            query = query.Take(Math.Max(1, target.Limit));

        return query.ToList();
    }

    /// <summary>Square images count as landscape. Images with unknown size are excluded when filtering.</summary>
    public static bool MatchesOrientation(RemoteAsset asset, OrientationFilter filter)
    {
        if (asset.Width <= 0 || asset.Height <= 0) return false;
        return filter == OrientationFilter.Landscape
            ? asset.Width >= asset.Height
            : asset.Height > asset.Width;
    }

    /// <param name="fileExists">Whether a manifest file name still exists in the folder (the user may have deleted it).</param>
    public static SyncPlan Plan(IReadOnlyList<RemoteAsset> desired, Manifest manifest, Func<string, bool> fileExists)
    {
        var desiredIds = desired.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);

        var toDelete = manifest.Entries.Keys
            .Where(id => !desiredIds.Contains(id))
            .ToList();

        var toDownload = desired
            .Where(a => !manifest.Entries.TryGetValue(a.Id, out var entry)
                        || entry.Version != a.Version
                        || !fileExists(entry.FileName))
            .ToList();

        return new SyncPlan(toDownload, toDelete);
    }

    /// <summary>"20240131_183005_1a2b3c4d": sorts by capture date and is unique per asset.</summary>
    public static string BaseFileName(RemoteAsset asset)
    {
        var id = asset.Id.Replace("-", "");
        return $"{asset.LocalTakenAt:yyyyMMdd_HHmmss}_{id[..Math.Min(8, id.Length)]}";
    }
}
