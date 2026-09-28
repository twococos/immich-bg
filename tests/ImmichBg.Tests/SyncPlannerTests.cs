using ImmichBg.Config;
using ImmichBg.Immich;
using ImmichBg.Sync;

namespace ImmichBg.Tests;

public class SyncPlannerTests
{
    internal static RemoteAsset Asset(string id, int day, int width = 1920, int height = 1080, string version = "v1") =>
        new(id, version, new DateTimeOffset(2024, 1, day, 12, 0, 0, TimeSpan.Zero), new DateTime(2024, 1, day, 12, 0, 0), width, height, false);

    static Manifest ManifestWith(params (string Id, string File, string Version)[] entries)
    {
        var m = new Manifest();
        foreach (var (id, file, version) in entries) m.Entries[id] = new ManifestEntry { FileName = file, Version = version };
        return m;
    }

    [Fact]
    public void SelectDesired_TakesLatestByCaptureDate()
    {
        var assets = new[] { Asset("a", 1), Asset("b", 5), Asset("c", 3) };
        var result = SyncPlanner.SelectDesired(assets, new TargetConfig { LimitEnabled = true, Limit = 2 });
        Assert.Equal(["b", "c"], result.Select(a => a.Id));
    }

    [Fact]
    public void SelectDesired_WithoutLimit_ReturnsAll()
    {
        var assets = new[] { Asset("a", 1), Asset("b", 5), Asset("c", 3) };
        Assert.Equal(3, SyncPlanner.SelectDesired(assets, new TargetConfig()).Count);
    }

    [Fact]
    public void SelectDesired_OrientationIsAppliedBeforeLimit()
    {
        var assets = new[]
        {
            Asset("portrait-new", 9, 1080, 1920),
            Asset("landscape-old", 1),
            Asset("landscape-mid", 4),
            Asset("unknown", 8, 0, 0),
        };
        var cfg = new TargetConfig { OrientationEnabled = true, Orientation = OrientationFilter.Landscape, LimitEnabled = true, Limit = 2 };
        Assert.Equal(["landscape-mid", "landscape-old"], SyncPlanner.SelectDesired(assets, cfg).Select(a => a.Id));

        cfg.Orientation = OrientationFilter.Portrait;
        Assert.Equal(["portrait-new"], SyncPlanner.SelectDesired(assets, cfg).Select(a => a.Id));
    }

    [Fact]
    public void Plan_AddsMissing_DeletesExtra_KeepsUnchanged()
    {
        var desired = new[] { Asset("keep", 1), Asset("new", 2) };
        var manifest = ManifestWith(("keep", "keep.jpg", "v1"), ("gone", "gone.jpg", "v1"));

        var plan = SyncPlanner.Plan(desired, manifest, _ => true);

        Assert.Equal(["new"], plan.ToDownload.Select(a => a.Id));
        Assert.Equal(["gone"], plan.ToDelete);
    }

    [Fact]
    public void Plan_RedownloadsChangedOrMissingFiles()
    {
        var desired = new[] { Asset("edited", 1, version: "v2"), Asset("deletedByUser", 2) };
        var manifest = ManifestWith(("edited", "edited.jpg", "v1"), ("deletedByUser", "x.jpg", "v1"));

        var plan = SyncPlanner.Plan(desired, manifest, file => file != "x.jpg");

        Assert.Equal(["edited", "deletedByUser"], plan.ToDownload.Select(a => a.Id));
        Assert.Empty(plan.ToDelete);
    }

    [Fact]
    public void BaseFileName_IsDatePlusShortId()
    {
        var asset = Asset("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d", 31);
        Assert.Equal("20240131_120000_1a2b3c4d", SyncPlanner.BaseFileName(asset));
    }
}
