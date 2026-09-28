using ImmichBg.Config;
using ImmichBg.Immich;
using ImmichBg.Sync;
using static ImmichBg.Tests.SyncPlannerTests;

namespace ImmichBg.Tests;

public sealed class SyncEngineTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "immichbg-tests-" + Guid.NewGuid().ToString("N"));
    string Folder => Path.Combine(root, "wallpaper");
    string StateDir => Path.Combine(root, "state");

    sealed class FakeSource : IImmichSource
    {
        public List<RemoteAsset> Assets { get; set; } = [];
        public bool Fail { get; set; }
        public int Downloads { get; private set; }

        public Task<IReadOnlyList<RemoteAsset>> GetAssetsAsync(SourceKind source, string? albumId, CancellationToken ct) =>
            Fail ? throw new HttpRequestException("server down") : Task.FromResult<IReadOnlyList<RemoteAsset>>(Assets);

        public async Task<string> DownloadToFileAsync(RemoteAsset asset, string path, CancellationToken ct)
        {
            Downloads++;
            await File.WriteAllTextAsync(path, asset.Id + asset.Version, ct);
            return ".jpg";
        }
    }

    AppConfig Config() => new()
    {
        Wallpaper = new TargetConfig { Enabled = true, Folder = Folder, Source = SourceKind.Favorites },
    };

    string[] Files() => Directory.GetFiles(Folder).Select(Path.GetFileName).Order().ToArray()!;

    [Fact]
    public async Task FullCycle_AddsRemovesAndKeepsForeignFiles()
    {
        var source = new FakeSource { Assets = [Asset("aaaaaaaa-1", 1), Asset("bbbbbbbb-2", 2)] };
        var engine = new SyncEngine(StateDir);
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, "my-own-photo.jpg"), "x");

        var r1 = await engine.RunAsync(Config(), source, CancellationToken.None);
        Assert.True(r1[0].Ok);
        Assert.Equal(["20240101_120000_aaaaaaaa.jpg", "20240102_120000_bbbbbbbb.jpg", "my-own-photo.jpg"], Files());

        // Second run with no changes downloads nothing.
        await engine.RunAsync(Config(), source, CancellationToken.None);
        Assert.Equal(2, source.Downloads);

        // One removed from the album, one added.
        source.Assets = [Asset("bbbbbbbb-2", 2), Asset("cccccccc-3", 3)];
        var r3 = await engine.RunAsync(Config(), source, CancellationToken.None);
        Assert.Equal((1, 1, 2), (r3[0].Added, r3[0].Removed, r3[0].Total));
        Assert.Equal(["20240102_120000_bbbbbbbb.jpg", "20240103_120000_cccccccc.jpg", "my-own-photo.jpg"], Files());
    }

    [Fact]
    public async Task ServerFailure_DeletesNothing()
    {
        var source = new FakeSource { Assets = [Asset("aaaaaaaa-1", 1)] };
        var engine = new SyncEngine(StateDir);
        await engine.RunAsync(Config(), source, CancellationToken.None);

        source.Fail = true;
        var result = await engine.RunAsync(Config(), source, CancellationToken.None);

        Assert.False(result[0].Ok);
        Assert.Equal(["20240101_120000_aaaaaaaa.jpg"], Files());
    }

    [Fact]
    public async Task FolderChange_MovesOwnFilesOnly()
    {
        var source = new FakeSource { Assets = [Asset("aaaaaaaa-1", 1)] };
        var engine = new SyncEngine(StateDir);
        await engine.RunAsync(Config(), source, CancellationToken.None);
        File.WriteAllText(Path.Combine(Folder, "foreign.jpg"), "x");

        var cfg = Config();
        cfg.Wallpaper.Folder = Path.Combine(root, "other");
        await engine.RunAsync(cfg, source, CancellationToken.None);

        Assert.Equal(["foreign.jpg"], Files());
        Assert.Single(Directory.GetFiles(cfg.Wallpaper.Folder));
    }

    [Fact]
    public async Task SameSourceForTwoTargets_QueriesServerOnce()
    {
        var calls = 0;
        var source = new CountingSource(() => calls++);
        var cfg = Config();
        cfg.Screensaver = new TargetConfig { Enabled = true, Folder = Path.Combine(root, "saver"), Source = SourceKind.Favorites };

        await new SyncEngine(StateDir).RunAsync(cfg, source, CancellationToken.None);

        Assert.Equal(1, calls);
    }

    sealed class CountingSource(Action onQuery) : IImmichSource
    {
        public Task<IReadOnlyList<RemoteAsset>> GetAssetsAsync(SourceKind source, string? albumId, CancellationToken ct)
        {
            onQuery();
            return Task.FromResult<IReadOnlyList<RemoteAsset>>([]);
        }

        public Task<string> DownloadToFileAsync(RemoteAsset asset, string path, CancellationToken ct) => Task.FromResult(".jpg");
    }

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); } catch { }
    }
}
