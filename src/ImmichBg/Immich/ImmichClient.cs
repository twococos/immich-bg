using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImmichBg.Config;
using ImmichBg.Logging;

namespace ImmichBg.Immich;

/// <summary>What the sync engine needs from the server (faked in tests).</summary>
public interface IImmichSource
{
    Task<IReadOnlyList<RemoteAsset>> GetAssetsAsync(SourceKind source, string? albumId, CancellationToken ct);

    /// <summary>Downloads the wallpaper-quality image to <paramref name="path"/> and returns its extension (".jpg", ...).</summary>
    Task<string> DownloadToFileAsync(RemoteAsset asset, string path, CancellationToken ct);
}

public sealed class ImmichApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

public sealed class ImmichClient : IImmichSource, IDisposable
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    readonly HttpClient http;
    bool fullsizeUnsupported;

    public ImmichClient(string serverUrl, string apiKey)
    {
        BaseUrl = NormalizeBaseUrl(serverUrl);
        var handler = new SocketsHttpHandler
        {
            // Fail fast when an address is unreachable (e.g. the local one when away from home).
            ConnectTimeout = TimeSpan.FromSeconds(8),
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
            AutomaticDecompression = DecompressionMethods.All,
        };
        http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromMinutes(5),
        };
        http.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ImmichBg/0.1");
    }

    /// <summary>Accepts "host:2283", "http://host:2283/", ".../api" and returns "scheme://host/.../api/".</summary>
    public static string NormalizeBaseUrl(string url)
    {
        url = url.Trim().TrimEnd('/');
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "http://" + url;
        if (!url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            url += "/api";
        return url + "/";
    }

    public string BaseUrl { get; }

    /// <summary>Quick unauthenticated check that an Immich server answers at this address.</summary>
    public async Task<bool> PingAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            using var resp = await http.GetAsync("server/ping", cts.Token);
            return resp.IsSuccessStatusCode && (await resp.Content.ReadAsStringAsync(cts.Token)).Contains("pong");
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>A short, human explanation of a connection error.</summary>
    public static string Describe(Exception ex) => ex switch
    {
        ImmichApiException api => api.Message,
        TaskCanceledException or TimeoutException => Loc.T("err.timeout"),
        HttpRequestException { InnerException: System.Security.Authentication.AuthenticationException } => Loc.T("err.tls"),
        HttpRequestException { InnerException: System.Net.Sockets.SocketException se } => se.SocketErrorCode switch
        {
            System.Net.Sockets.SocketError.HostNotFound or System.Net.Sockets.SocketError.NoData => Loc.T("err.dns"),
            System.Net.Sockets.SocketError.ConnectionRefused => Loc.T("err.refused"),
            _ => Loc.T("err.timeout"),
        },
        HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError } => Loc.T("err.timeout"),
        HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError } => Loc.T("err.tls"),
        HttpRequestException { HttpRequestError: HttpRequestError.NameResolutionError } => Loc.T("err.dns"),
        UriFormatException => Loc.T("err.badUrl"),
        _ => ex.Message,
    };

    public async Task<(string Version, string UserName)> TestConnectionAsync(CancellationToken ct)
    {
        var v = await GetJsonAsync<ServerVersionDto>("server/version", ct);
        var me = await GetJsonAsync<UserDto>("users/me", ct);
        return ($"{v.Major}.{v.Minor}.{v.Patch}", me.Name);
    }

    public async Task<IReadOnlyList<AlbumInfo>> GetAlbumsAsync(CancellationToken ct)
    {
        var albums = await GetJsonAsync<List<AlbumDto>>("albums", ct);
        return albums
            .Select(a => new AlbumInfo(a.Id, a.AlbumName, a.AssetCount))
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<RemoteAsset>> GetAssetsAsync(SourceKind source, string? albumId, CancellationToken ct)
    {
        if (source == SourceKind.Album)
        {
            // Throws if the album was deleted or is no longer shared with us. Without this check,
            // searching a missing album returns an empty list and the sync would delete every local file.
            await GetJsonAsync<AlbumDto>($"albums/{Uri.EscapeDataString(albumId ?? "")}", ct);
        }

        var result = new List<RemoteAsset>();
        int page = 1;
        string? cursor = null;
        for (int guard = 0; guard < 10_000; guard++)
        {
            var body = new Dictionary<string, object>
            {
                ["type"] = "IMAGE",
                ["order"] = "desc",
                ["withExif"] = true,
                ["size"] = 1000,
            };
            if (source == SourceKind.Album) body["albumIds"] = new[] { albumId! };
            else body["isFavorite"] = true;
            // Immich >= 3.2 paginates with a cursor; older servers use page numbers.
            if (cursor != null) body["cursor"] = cursor;
            else body["page"] = page;

            using var resp = await http.PostAsJsonAsync("search/metadata", body, Json, ct);
            await EnsureSuccessAsync(resp, ct);
            var search = await resp.Content.ReadFromJsonAsync<SearchResponseDto>(Json, ct)
                         ?? throw new InvalidDataException("search/metadata");

            foreach (var a in search.Assets.Items)
                if (!a.IsTrashed && a.Type == "IMAGE") result.Add(AssetMapper.Map(a));

            if (!string.IsNullOrEmpty(search.Assets.NextCursor)) cursor = search.Assets.NextCursor;
            else if (int.TryParse(search.Assets.NextPage, out var next)) page = next;
            else break;
        }
        return result;
    }

    public async Task<string> DownloadToFileAsync(RemoteAsset asset, string path, CancellationToken ct)
    {
        HttpResponseMessage? resp = null;
        if (!fullsizeUnsupported)
        {
            resp = await GetThumbnailAsync(asset, "fullsize", ct);
            if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                // Servers older than ~1.127 don't know "fullsize" at all.
                fullsizeUnsupported = true;
                AppLog.Warn(Loc.T("log.fullsizeUnsupported"));
            }
            else if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                AppLog.Warn(Loc.T("log.fullsizeMissing", asset.Id));
            }
            if (!resp.IsSuccessStatusCode)
            {
                resp.Dispose();
                resp = null;
            }
        }
        resp ??= await GetThumbnailAsync(asset, "preview", ct);

        using (resp)
        {
            await EnsureSuccessAsync(resp, ct);
            var ext = ExtensionFor(resp.Content.Headers.ContentType?.MediaType);
            await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await resp.Content.CopyToAsync(file, ct);
            return ext;
        }
    }

    public void Dispose() => http.Dispose();

    Task<HttpResponseMessage> GetThumbnailAsync(RemoteAsset asset, string size, CancellationToken ct)
    {
        var url = $"assets/{asset.Id}/thumbnail?size={size}" + (asset.IsEdited ? "&edited=true" : "");
        return http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    internal static string ExtensionFor(string? mediaType) => mediaType?.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/avif" => ".avif",
        "image/gif" => ".gif",
        _ => ".jpg",
    };

    async Task<T> GetJsonAsync<T>(string url, CancellationToken ct)
    {
        using var resp = await http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
        return await resp.Content.ReadFromJsonAsync<T>(Json, ct) ?? throw new InvalidDataException(url);
    }

    static async Task EnsureSuccessAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;
        var status = resp.StatusCode;
        string detail;
        try
        {
            detail = await resp.Content.ReadAsStringAsync(ct);
            if (detail.Length > 300) detail = detail[..300];
        }
        catch { detail = ""; }

        var path = resp.RequestMessage?.RequestUri?.AbsolutePath ?? "";
        var message = status switch
        {
            HttpStatusCode.Unauthorized => Loc.T("err.unauthorized"),
            HttpStatusCode.Forbidden => Loc.T("err.forbidden", detail),
            HttpStatusCode.NotFound when path.Contains("/albums/") => Loc.T("err.albumNotFound"),
            _ => $"HTTP {(int)status} {path}: {detail}",
        };
        throw new ImmichApiException(status, message);
    }
}

internal static class AssetMapper
{
    public static RemoteAsset Map(AssetDto a)
    {
        var (w, h) = Dimensions(a.Width, a.Height, a.ExifInfo);
        var version = a.IsEdited ? $"{a.Checksum}|edit:{a.UpdatedAt.UtcTicks}" : a.Checksum;
        // localDateTime is the wall-clock capture time encoded as UTC.
        var local = a.LocalDateTime?.UtcDateTime ?? a.FileCreatedAt.LocalDateTime;
        return new RemoteAsset(a.Id, version, a.FileCreatedAt, local, w, h, a.IsEdited);
    }

    /// <summary>
    /// Asset width/height (recent servers) already have the EXIF orientation applied.
    /// The raw EXIF size needs swapping for rotated orientations (5-8).
    /// </summary>
    public static (int Width, int Height) Dimensions(int? width, int? height, ExifDto? exif)
    {
        if (width > 0 && height > 0) return (width.Value, height.Value);
        if (exif?.ExifImageWidth is not > 0 || exif.ExifImageHeight is not > 0) return (0, 0);
        var (w, h) = (exif.ExifImageWidth.Value, exif.ExifImageHeight.Value);
        return IsRotated(exif.Orientation) ? (h, w) : (w, h);
    }

    static bool IsRotated(string? orientation)
    {
        if (string.IsNullOrEmpty(orientation)) return false;
        if (int.TryParse(orientation, out var o)) return o is >= 5 and <= 8;
        return orientation.Contains("90") || orientation.Contains("270");
    }
}
