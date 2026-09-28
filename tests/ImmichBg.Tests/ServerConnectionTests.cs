using ImmichBg.Immich;

namespace ImmichBg.Tests;

public class ServerConnectionTests
{
    static Func<string, CancellationToken, Task<bool>> Reachable(params string[] up) =>
        (url, _) => Task.FromResult(up.Contains(url));

    [Fact]
    public async Task PrefersLocalWhenItAnswers()
    {
        var url = await ServerConnection.FirstReachableAsync(["http://local", "https://remote"], Reachable("http://local", "https://remote"), default);
        Assert.Equal("http://local", url);
    }

    [Fact]
    public async Task FallsBackToRemoteWhenAwayFromHome()
    {
        var url = await ServerConnection.FirstReachableAsync(["http://local", "https://remote"], Reachable("https://remote"), default);
        Assert.Equal("https://remote", url);
    }

    [Fact]
    public async Task NullWhenNothingAnswers()
    {
        Assert.Null(await ServerConnection.FirstReachableAsync(["http://local", "https://remote"], Reachable(), default));
    }
}
