using ImmichBg.Logging;

namespace ImmichBg.Immich;

/// <summary>
/// Local + remote address handling (like the Immich mobile app): each sync uses the first address that answers.
/// Needed because many home routers can't reach their own public address from inside (no NAT loopback).
/// </summary>
public sealed class ServerConnection(IReadOnlyList<string> urls, string apiKey) : IDisposable
{
    static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(6);

    readonly Dictionary<string, ImmichClient> clients = new();
    string? activeUrl;

    public async Task<ImmichClient?> ResolveAsync(CancellationToken ct)
    {
        var url = await FirstReachableAsync(urls, (u, token) => GetClient(u).PingAsync(PingTimeout, token), ct);
        if (url == null) return null;
        if (url != activeUrl)
        {
            AppLog.Info(Loc.T("log.usingAddress", url));
            activeUrl = url;
        }
        return GetClient(url);
    }

    public static async Task<string?> FirstReachableAsync(
        IEnumerable<string> urls, Func<string, CancellationToken, Task<bool>> ping, CancellationToken ct)
    {
        foreach (var url in urls)
            if (await ping(url, ct)) return url;
        return null;
    }

    ImmichClient GetClient(string url)
    {
        if (!clients.TryGetValue(url, out var client))
            clients[url] = client = new ImmichClient(url, apiKey);
        return client;
    }

    public void Dispose()
    {
        foreach (var c in clients.Values) c.Dispose();
    }
}
